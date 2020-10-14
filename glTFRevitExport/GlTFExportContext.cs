using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Autodesk.Revit.DB;

using GLTFRevitExport.GLTF;
using GLTFRevitExport.Extensions;
using GLTFRevitExport.GLTF.Containers;
using GLTFRevitExport.GLTF.Schema;
using GLTFRevitExport.GLTF.Extensions.BIM;
using System.Runtime.CompilerServices;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.Visual;

namespace GLTFRevitExport {
    #region Initialization
    internal sealed partial class GLTFExportContext : IExportContext {
        public GLTFExportContext(Document doc, GLTFExportConfigs configs = null) {
            // ensure base configs
            _cfgs = configs is null ? new GLTFExportConfigs() : configs;

            // reset stacks
            resetExporter();
            // place doc on the stack
            _docStack.Push(doc);
        }
    }
    #endregion

    #region Data Stacks
    internal sealed partial class GLTFExportContext : IExportContext {
        /// <summary>
        /// Configurations for the active export
        /// </summary>
        private readonly GLTFExportConfigs _cfgs = new GLTFExportConfigs();

        /// <summary>
        /// Document stack to hold the documents being processed.
        /// A stack is used to allow processing nested documents (linked docs)
        /// </summary>
        private readonly Stack<Document> _docStack = new Stack<Document>();

        /// <summary>
        /// Queue of actions collected during export. These actions are then
        /// played back on each .Build call to create separate glTF outputs
        /// </summary>
        private readonly Queue<BaseExporterAction> _actions = new Queue<BaseExporterAction>();

        /// <summary>
        /// List of processed elements by their unique id
        /// </summary>
        private readonly List<string> _processed = new List<string>();

        /// <summary>
        /// Flag to mark current node as skipped
        /// </summary>
        private bool _skipElement = false;

        class VectorData : IComparable<VectorData> {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }

            public VectorData(XYZ vector) {
                //var xform = Transform.CreateRotation(new XYZ(1, 0, 0), -1.570796);
                //vector = xform.OfPoint(vector);

                X = vector.X.ToGLTFLength();
                Y = vector.Y.ToGLTFLength();
                Z = vector.Z.ToGLTFLength();

                // Y Up!
                ////X = -X;
                //float tmp = Y;
                //Y = Z;
                //Z = tmp;
            }

            public float[] ToArray() => new float[] { X, Y, Z };

            public int CompareTo(VectorData a) {
                float d = X - a.X;
                if (0 == d) {
                    d = Y - a.Y;
                    if (0 == d) {
                        d = Z - a.Z;
                    }
                }
                return (0 == d) ? 0 : ((0 < d) ? 1 : -1);
            }
        }

        class FacetData {
            public uint V1 { get; set; }
            public uint V2 { get; set; }
            public uint V3 { get; set; }

            public FacetData(PolymeshFacet f) {
                V1 = (uint)f.V1;
                V2 = (uint)f.V2;
                V3 = (uint)f.V3;
            }

            public uint[] ToArray() => new uint[] { V1, V2, V3 };

            public static FacetData operator +(FacetData left, uint shift) {
                left.V1 += shift;
                left.V2 += shift;
                left.V3 += shift;
                return left;
            }
        }

        class PrimitiveData {
            // TODO: ensure normals and vertices have the same length
            public List<VectorData> Vertices = new List<VectorData>();
            public List<VectorData> Normals = new List<VectorData>();
            public List<FacetData> Faces = new List<FacetData>();

            public static PrimitiveData operator +(PrimitiveData left, PrimitiveData right) {
                int startIdx = left.Vertices.Count;

                // new vertices array
                var vertices = new List<VectorData>(left.Vertices);
                vertices.AddRange(right.Vertices);

                // new normals array
                var normals = new List<VectorData>(left.Normals);
                normals.AddRange(right.Normals);

                // shift face indices
                var faces = new List<FacetData>(left.Faces);
                foreach (var faceIdx in right.Faces)
                    faces.Add(faceIdx + (ushort)startIdx);

                return new PrimitiveData {
                    Vertices = vertices,
                    Normals = normals,
                    Faces = faces,
                };
            }
        }

        class PartData {
            public PartData(PrimitiveData primitive) => Primitive = primitive;

            public PrimitiveData Primitive;

            public Material Material;
            public Color Color;
            public double Transparency;

            public static PartData operator +(PartData left, PartData right) {
                PrimitiveData prim;
                if (left.Primitive is null)
                    prim = right.Primitive;
                else if (right.Primitive is null)
                    prim = left.Primitive;
                else
                    prim = left.Primitive + right.Primitive;

                return new PartData(prim) {
                    Material = left.Material,
                    Color = left.Color,
                    Transparency = left.Transparency,
                };
            }
        }

        private readonly Stack<PartData> _partStack = new Stack<PartData>();
    }
    #endregion

    #region IExportContext Implementation
    internal sealed partial class GLTFExportContext : IExportContext {
        #region Start, Stop, Cancel
        // Runs once at beginning of export. Sets up the root node
        // and scene.
        public bool Start() {
            // Do not need to do anything here
            // _glTF is already instantiated
            Logger.Log("+ start collect");

            // reset other stacks
            _processed.Clear();
            _skipElement = false;

            var doc = _docStack.Last();
            _docStack.Clear();
            // place the root document on the stack
            _docStack.Push(doc);

            return true;
        }

        // Runs once at end of export
        // Collects any data that is not passed by default to this context
        public void Finish() {
            Logger.Log("- end collect");
        }

        // This method is invoked many times during the export process
        public bool IsCanceled() {
            if (_cfgs.CancelToken.IsCancellationRequested) {
                Logger.Log("x cancelled");
                resetExporter();
            }
            return _cfgs.CancelToken.IsCancellationRequested;
        }
        #endregion

        #region Views
        // revit calls this on every view that is being processed
        // all other methods are called after a view has begun
        public RenderNodeAction OnViewBegin(ViewNode node) {
            // if active doc and view is valid
            if (_docStack.Peek() is Document doc) {
                if (doc.GetElement(node.ViewId) is View view) {
                    if (recordOrSkip(view, "x duplicate view", setFlag: true))
                        return RenderNodeAction.Skip;

                    // if active doc and view is valid
                    _actions.Enqueue(new OnSceneBeginAction(view: view));

                    Logger.LogElement("+ view begin", view);
                    return RenderNodeAction.Proceed;
                }
            }
            // otherwise skip the view
            return RenderNodeAction.Skip;
        }

        public void OnViewEnd(ElementId elementId) {
            if (_skipElement)
                _skipElement = false;
            else {
                Logger.Log("- view end");
                _actions.Enqueue(new OnSceneEndAction());
            }
        }
        #endregion

        #region Elements
        // Runs once for each element.
        public RenderNodeAction OnElementBegin(ElementId eid) {
            if (_docStack.Peek() is Document doc) {
                Element e = doc.GetElement(eid);
                ElementType et = doc.GetElement(e.GetTypeId()) as ElementType;

                // TODO: take a look at elements that have no type
                // skipping these for now
                // DB.CurtainGridLine
                // DB.Opening
                // DB.FaceSplitter
                // DB.Spatial
                if (et is null)
                    goto SkipElementLabel;

                // TODO: fix inneficiency in getting linked elements multiple times
                // this affects links that have multiple instances
                // remember glTF nodes can not have multiple parents
                // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#nodes-and-hierarchy
                if (!doc.IsLinked) {
                    // check if this element has been processed before
                    if (recordOrSkip(e, "x duplicate element", setFlag: true))
                        return RenderNodeAction.Skip;
                }

                // Begin: Element
                switch (e) {
                    case View _:
                        goto SkipElementLabel;

                    case RevitLinkInstance linkInst:
                        if (_cfgs.ExportLinkedModels) {
                            Logger.LogElement("+ element (link) begin", e);
                            var lixform =
                                linkInst.GetTotalTransform().ToGLTF();
                            _actions.Enqueue(
                                new OnNodeBeginAction(
                                    element: e,
                                    type: et,
                                    xform: lixform,
                                    link: true
                                    )
                                );
                            break;
                        }
                        else {
                            Logger.Log("~ exclude link element");
                            goto SkipElementLabel;
                        }

                    case FamilyInstance famInst:
                        Logger.LogElement("+ element (instance) begin", e);
                        var fixform =
                            famInst.GetTotalTransform().ToGLTF();
                        _actions.Enqueue(
                            new OnNodeBeginAction(
                                element: famInst,
                                type: et,
                                xform: fixform
                                )
                            );
                        break;

                    case Element generic:
                        var c = e.Category;
                        if (c is null) {
                            Logger.LogElement($"+ element (generic) begin", e);
                            _actions.Enqueue(
                                new OnNodeBeginAction(
                                    element: generic,
                                    type: et,
                                    xform: null
                                    )
                                );
                        }
                        else {
                            if (c.IsCategory(BuiltInCategory.OST_Cameras)) {
                                // TODO: enqueue camera node
                                goto SkipElementLabel;
                            }
                            else {
                                var cname = c.Name.ToLower();
                                Logger.LogElement($"+ element ({cname}) begin", e);
                                _actions.Enqueue(
                                    new OnNodeBeginAction(
                                        element: generic,
                                        type: et,
                                        xform: null
                                        )
                                    );
                            }
                        }
                        break;
                }

                return RenderNodeAction.Proceed;
            }
            return RenderNodeAction.Skip;

        SkipElementLabel:
            _skipElement = true;
            return RenderNodeAction.Skip;
        }

        // Runs at the end of an element being processed, after all other calls for that element.
        public void OnElementEnd(ElementId eid) {
            if (_skipElement)
                _skipElement = false;
            else {
                // if has mesh data
                if (_partStack.Count > 0)
                    foreach (var primitive in _partStack)
                        _actions.Enqueue(new OnPartNodeAction(primitive));
                _partStack.Clear();

                Logger.Log("- element end");
                // end the element
                _actions.Enqueue(new OnNodeEndAction());
            }
        }

        // This is called when family instances are encountered,
        // after OnElementBegin. We're using it here to maintain the transform
        // stack for that element's heirarchy.
        public RenderNodeAction OnInstanceBegin(InstanceNode node) {
            Logger.Log("+ instance start");
            Logger.Log("> transform");
            return RenderNodeAction.Proceed;
        }

        // do nothing. OnElementClose will close the element later
        public void OnInstanceEnd(InstanceNode node) {
            Logger.Log("- instance end");
        }
        #endregion

        #region Linked Models
        public RenderNodeAction OnLinkBegin(LinkNode node) {
            if (_docStack.Peek() is Document) {
                if (_cfgs.ExportLinkedModels) {
                    // Link element info is processed by the OnElement before
                    // we will just push the linked doc in the stack
                    // so all subsequent calls to OnElement can grab the element
                    // from the linked document correctly
                    _docStack.Push(node.GetDocument());

                    Logger.Log("+ link document begin");
                    return RenderNodeAction.Proceed;
                }
                else
                    Logger.Log("~ exclude link document");
            }
            return RenderNodeAction.Skip;
        }

        public void OnLinkEnd(LinkNode node) {
            if (_skipElement)
                _skipElement = false;
            else {
                if (_cfgs.ExportLinkedModels) {
                    Logger.Log("- link document end");
                    _docStack.Pop();
                }
            }
        }
        #endregion

        #region Material and Geometry
        // Runs every time, and immediately prior to, a mesh being processed
        // e.g. OnMaterial->OnFace->OnPolymesh
        // It supplies the material for the mesh, and we use this to create
        // a new material in our material container, or switch the
        // current material if it already exists
        // TODO: Handle more complex materials.
        public void OnMaterial(MaterialNode node) {
            if (_docStack.Peek() is Document doc) {
                Material m = doc.GetElement(node.MaterialId) as Material;
                // if there is a material element
                if (m != null) {
                    // if mesh stack has a mesh
                    if (_partStack.Count > 0
                            && _partStack.Peek() is PartData partPrim) {
                        // if material is same as active, ignore
                        if (partPrim.Material != null
                                && m.UniqueId == partPrim.Material.UniqueId) {
                            Logger.Log("> material keep");
                            return;
                        }
                    }
                    Logger.LogElement("> material", m);
                    _partStack.Push(
                        new PartData(primitive: null) {
                            Material = m,
                            Color = node.Color,
                            Transparency = node.Transparency
                        });
                }
                // or there is no material
                // lets grab the color and transparency from node
                else {
                    Logger.Log("x material empty");
                    // if mesh stack has a mesh
                    if (_partStack.Count > 0
                            && _partStack.Peek() is PartData partPrim) {
                        // if color and transparency are the same
                        if (partPrim.Material is null
                                && node.Color.Compare(partPrim.Color)
                                && node.Transparency == partPrim.Transparency) {
                            Logger.Log("> material keep");
                            return;
                        }
                    }
                    Logger.LogElement("> material", m);
                    _partStack.Push(
                        new PartData(primitive: null) {
                            Color = node.Color,
                            Transparency = node.Transparency
                        });
                }
            }
        }

        // provides access to the DB.Face that includes the polymesh
        // can be used to extract more information from the actual face
        public RenderNodeAction OnFaceBegin(FaceNode node) {
            Logger.Log("+ face begin");
            return RenderNodeAction.Proceed;
        }

        // Runs for every polymesh being processed. Typically this is a single
        // face of an element's mesh
        public void OnPolymesh(PolymeshTopology polymesh) {
            // TODO: anything to do with .DistributionOfNormals or .GetUV?
            if (_partStack.Count > 0) {
                Logger.Log("> polymesh");
                var activePart = _partStack.Peek();

                var newPrim = new PrimitiveData {
                    Vertices = polymesh.GetPoints().Select(x => new VectorData(x)).ToList(),
                    Normals = polymesh.GetNormals().Select(x => new VectorData(x)).ToList(),
                    Faces = polymesh.GetFacets().Select(x => new FacetData(x)).ToList()
                };

                if (activePart.Primitive is null)
                    activePart.Primitive = newPrim;
                else
                    activePart.Primitive = activePart.Primitive + newPrim;
            }
        }

        public void OnFaceEnd(FaceNode node) {
            Logger.Log("- face end");
        }
        #endregion

        #region Misc
        public void OnRPC(RPCNode node) {
            // TODO: on RPC
            //Logger.Log("> rpc");
        }

        public void OnLight(LightNode node) {
            // TODO: on light
            //Logger.Log("> light");
        }
        #endregion
    }
    #endregion

    #region Exporter Actions
    internal sealed partial class GLTFExportContext : IExportContext {
        abstract class BaseExporterAction {
            public bool IncludeHierarchy = true;
            public bool IncludeProperties = true;

            public abstract void Execute(GLTFBuilder gltf);
        }

        abstract class BaseElementExporterAction : BaseExporterAction {
            protected Element element;

            public BaseElementExporterAction(Element e) => element = e;

            public override void Execute(GLTFBuilder gltf)
                => Execute(
                    gltf,
                    (e) => {
                        if (e is FamilyInstance famInst) {
                            var zones = new HashSet<string>();
                            if (famInst.FromRoom != null)
                                zones.Add(famInst.FromRoom.GetId());
                            if (famInst.ToRoom != null)
                                zones.Add(famInst.ToRoom.GetId());
                            if (famInst.Room != null)
                                zones.Add(famInst.Room.GetId());
                            if (famInst.Space != null)
                                zones.Add(famInst.Space.GetId());
                        }
                        return null;
                    },
                    (e) => new glTFExtras()
                    );
            public abstract void Execute(GLTFBuilder gltf,
                                         Func<object, string[]> zoneFinder,
                                         Func<object, glTFExtras> extrasBuilder);

            public bool Passes(ElementFilter filter) {
                if (element is null)
                    return true;
                return filter.PassesFilter(element);
            }
        }

        abstract class ExporterBeginAction : BaseElementExporterAction {
            public ExporterBeginAction(Element e) : base(e) { }
        }

        abstract class ExporterEndAction : BaseExporterAction { }

        class OnSceneBeginAction : ExporterBeginAction {
            public OnSceneBeginAction(View view) : base(view) { }

            public override void Execute(GLTFBuilder gltf,
                                         Func<object, string[]> zoneFinder,
                                         Func<object, glTFExtras> extrasBuilder) {
                // start a new gltf scene
                Logger.Log("+ view begin");
                gltf.OpenScene(
                    name: element.Name,
                    exts: new glTFExtension[] {
                        new glTFBIMNodeExtension(element, zoneFinder, IncludeProperties)
                    },
                    extras: extrasBuilder(element)
                    );
            }
        }

        class OnSceneEndAction : ExporterEndAction {
            public override void Execute(GLTFBuilder gltf) {
                Logger.Log("- view end");
                gltf.CloseScene();
            }
        }

        class OnNodeBeginAction : ExporterBeginAction {
            private readonly float[] _xform;
            private readonly bool _link;
            private readonly ElementType _elementType;

            public OnNodeBeginAction(Element element, ElementType type,
                                     float[] xform, bool link = false)
                : base(element) {
                _elementType = type;
                _xform = xform;
                _link = link;
            }

            public override void Execute(GLTFBuilder gltf,
                                         Func<object, string[]> zoneFinder,
                                         Func<object, glTFExtras> extrasBuilder) {
                // open a new node and store its id
                Logger.Log("+ element begin");

                // node filter to pass to gltf builder
                string targetId = string.Empty;
                Func<glTFNode, bool> nodeFilter = node => {
                    if (node.Extensions != null) {
                        foreach (var ext in node.Extensions)
                            if (ext.Value is glTFBIMNodeExtension nodeExt)
                                return nodeExt.Id == targetId;
                    }
                    return false;
                };

                // create a node for its type
                // attemp at finding previously created node for this type
                // but only search children of already open node
                if (IncludeHierarchy) {
                    targetId = _elementType.GetId();
                    var typeNodeIdx = gltf.FindChildNode(nodeFilter);

                    if (typeNodeIdx >= 0) {
                        gltf.OpenExistingNode((uint)typeNodeIdx);
                    }
                    // otherwise create and open a new node for this type
                    else {
                        gltf.OpenNode(
                            name: _elementType.Name,
                            matrix: null,
                            exts: new glTFExtension[] {
                                new glTFBIMNodeExtension(_elementType, null, IncludeProperties)
                            },
                            extras: extrasBuilder(_elementType)
                        );
                    }
                }

                // create a node for this instance
                // attemp at finding previously created node for this instance
                // but only search children of already open type node
                targetId = element.GetId();
                var instNodeIdx = gltf.FindChildNode(nodeFilter);

                if (instNodeIdx >= 0) {
                    gltf.OpenExistingNode((uint)instNodeIdx);
                }
                // otherwise create and open a new node for this type
                else {
                    var newNodeIdx = gltf.OpenNode(
                        name: element.Name,
                        matrix: _xform,
                        exts: new glTFExtension[] {
                            new glTFBIMNodeExtension(element, zoneFinder, IncludeProperties)
                        },
                        extras: extrasBuilder(element)
                    );

                    var bbox = element.get_BoundingBox(null);
                    if (bbox != null)
                        updateBounds(
                            gltf: gltf,
                            idx: newNodeIdx,
                            bounds: new glTFBIMBounds(bbox)
                        );
                }
            }

            private void updateBounds(GLTFBuilder gltf, uint idx, glTFBIMBounds bounds) {
                glTFNode node = gltf.GetNode(idx);
                if (node.Extensions != null) {
                    foreach (var ext in node.Extensions) {
                        if (ext.Value is glTFBIMNodeExtension nodeExt) {
                            if (nodeExt.Bounds != null)
                                nodeExt.Bounds.Union(bounds);
                            else
                                nodeExt.Bounds = bounds;

                            int parentIdx = gltf.FindParentNode(idx);
                            if (parentIdx >= 0)
                                updateBounds(gltf, (uint)parentIdx, nodeExt.Bounds);
                        }
                    }
                }
            }
        }

        class OnNodeEndAction : ExporterEndAction {
            public override void Execute(GLTFBuilder gltf) {
                Logger.Log("- element end");
                // close instance node
                gltf.CloseNode();
                // close type node
                if (IncludeHierarchy)
                    gltf.CloseNode();
            }
        }

        class OnPartNodeAction : BaseExporterAction {
            private PartData _partp;

            public OnPartNodeAction(PartData partp) => _partp = partp;

            public override void Execute(GLTFBuilder gltf) {
                Logger.Log("> primitive");

                // make a new mesh and assign the new material
                var vertices = new List<float>();
                foreach(var vec in _partp.Primitive.Vertices)
                    vertices.AddRange(vec.ToArray());
                
                var normals = new List<float>();
                foreach (var vec in _partp.Primitive.Normals)
                    normals.AddRange(vec.ToArray());

                var faces = new List<uint>();
                foreach (var facet in _partp.Primitive.Faces)
                    faces.AddRange(facet.ToArray());

                var primIndex = gltf.AddPrimitive(
                    vertices: vertices.ToArray(),
                    normals: normals.ToArray(),
                    faces: faces.ToArray()
                    );

                Logger.Log("> material");
                
                // if material information is not provided, make a material
                // based on color and transparency
                if (_partp.Material is null) {
                    string matName = _partp.Color.GetId();
                    var existingMaterialIndex =
                        gltf.FindMaterial((mat) => mat.Name == matName);

                    // check if material already exists
                    if (existingMaterialIndex >= 0) {
                        gltf.UpdateMaterial(
                            primitiveIndex: primIndex,
                            materialIndex: (uint)existingMaterialIndex
                        );
                    }
                    // otherwise make a new material from color and transparency
                    else {
                        gltf.AddMaterial(
                            primitiveIndex: primIndex,
                            name: matName,
                            color: _partp.Color.ToGLTF(_partp.Transparency.ToSingle()),
                            exts: null,
                            extras: null
                        );
                    }
                }
                // otherwise process the material
                else {
                    var existingMaterialIndex =
                        gltf.FindMaterial(
                            (mat) => {
                                if (mat.Extensions != null) {
                                    foreach (var ext in mat.Extensions)
                                        if (ext.Value is glTFBIMMaterialExtensions matExt)
                                            return matExt.Id == _partp.Material.UniqueId;
                                }
                                return false;
                            }
                        );

                    // check if material already exists
                    if (existingMaterialIndex >= 0) {
                        gltf.UpdateMaterial(
                            primitiveIndex: primIndex,
                            materialIndex: (uint)existingMaterialIndex
                        );
                    }
                    // otherwise make a new material and get its index
                    else {
                        gltf.AddMaterial(
                            primitiveIndex: primIndex,
                            name: _partp.Material.Name,
                            color: _partp.Color.ToGLTF(_partp.Transparency.ToSingle()),
                            exts: new glTFExtension[] {
                            new glTFBIMMaterialExtensions(_partp.Material, IncludeProperties)
                            },
                            extras: null
                        );
                    }
                }
            }
        }
    }
    #endregion

    #region Utility Methods
    internal sealed partial class GLTFExportContext : IExportContext {
        /// <summary>
        /// Determine if given element should be skipped
        /// </summary>
        /// <param name="e">Target element</param>
        /// <returns>True if element should be skipped</returns>
        private bool recordOrSkip(Element e, string skipMessage, bool setFlag = false) {
            bool skip = false;
            if (e is null) {
                Logger.Log(skipMessage);
                skip = true;
            }
            else if (e != null && _processed.Contains(e.GetId())) {
                Logger.LogElement(skipMessage, e);
                skip = true;
            }
            else
                _processed.Add(e.GetId());

            if (setFlag)
                _skipElement = skip;
            return skip;
        }

        private void resetExporter() {
            // reset the logger
            Logger.Reset();
            _actions.Clear();
            _processed.Clear();
            _skipElement = false;
        }

        internal GLTFBuilder Build(ElementFilter filter,
                                   Func<object, string[]> zoneFinder,
                                   Func<object, glTFExtras> extrasBuilder) {
            var glTF = new GLTFBuilder();

            // build asset info
            var doc = _docStack.Last();
            glTF.SetAsset(
                generatorId: _cfgs.GeneratorId,
                copyright: _cfgs.CopyrightMessage,
                exts: new glTFExtension[] {
                    new glTFBIMAssetExtension(doc, _cfgs.ExportParameters)
                },
                extras: extrasBuilder != null ? extrasBuilder(doc) : null
                );

            // combine default filter with build filter
            ElementFilter actionFilter = null;
            if (filter != null) {
                actionFilter = new LogicalOrFilter(
                    new List<ElementFilter> {
                        // always include these categories no matter the build filter
                        new ElementMulticategoryFilter(
                            new List<BuiltInCategory> {
                                BuiltInCategory.OST_RvtLinks,
                                BuiltInCategory.OST_Views
                            }
                        ),
                        filter
                    }
                );
            }

            Logger.Log("+ start build");

            // filter and process each action
            // the loop tests each BEGIN action with a filter
            // and needs to remember the result of the filter test
            // so it knows whether to run the corresponding END action or not
            var passResults = new Stack<bool>();
            foreach (var action in _actions) {
                action.IncludeHierarchy = _cfgs.ExportHierarchy;
                action.IncludeProperties = _cfgs.ExportParameters;

                switch (action) {
                    case ExporterBeginAction beg:
                        if (actionFilter is null) {
                            if (extrasBuilder != null)
                                beg.Execute(glTF, zoneFinder, extrasBuilder);
                            else
                                beg.Execute(glTF);
                            passResults.Push(true);
                        }
                        else if (beg.Passes(actionFilter)) {
                            if (extrasBuilder != null)
                                beg.Execute(glTF, zoneFinder, extrasBuilder);
                            else
                                beg.Execute(glTF);
                            passResults.Push(true);
                        }
                        else
                            passResults.Push(false);
                        break;

                    case ExporterEndAction end:
                        if (passResults.Pop())
                            end.Execute(glTF);
                        break;

                    case BaseExporterAction ea:
                        ea.Execute(glTF);
                        break;
                }
            }


            Logger.Log("- end build");

            return glTF;
        }
    }
    #endregion
}
