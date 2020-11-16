using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Autodesk.Revit.DB;

using GLTFRevitExport.GLTF;
using GLTFRevitExport.Extensions;
using GLTFRevitExport.GLTF.Schema;
using GLTFRevitExport.GLTF.Extensions.BIM;
using GLTFRevitExport.Properties;
using System.Runtime.CompilerServices;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.Visual;
using Autodesk.Revit.DB.Architecture;

namespace GLTFRevitExport {
    #region Initialization
    internal sealed partial class GLTFExportContext : IExportContext {
        public GLTFExportContext(Document doc, GLTFExportConfigs configs = null) {
            // ensure base configs
            _cfgs = configs is null ? new GLTFExportConfigs() : configs;

            // reset stacks
            ResetExporter();
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
        /// Property value container when property information is not embedded
        /// </summary>
        private GLTFBIMPropertyContainer _propContainer;

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

            public VectorData(float x, float y, float z) {
                X = x;
                Y = y;
                Z = z;
            }

            public VectorData(XYZ vector) {
                var gltfVector = vector.ToGLTF();
                X = gltfVector[0];
                Y = gltfVector[1];
                Z = gltfVector[2];
            }

            public float[] ToArray() => new float[] { X.Round(), Y.Round(), Z.Round() };

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

            public XYZ ToXYZ() => new XYZ(X, Y, Z);
            
            public static VectorData operator +(VectorData left, VectorData right) {
                return new VectorData(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
            }

            public static VectorData operator -(VectorData left, VectorData right) {
                return new VectorData(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
            }

            public static VectorData operator /(VectorData left, float divisor) {
                return new VectorData(left.X / divisor, left.Y / divisor, left.Z / divisor);
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
            private List<VectorData> _normals = null;

            // TODO: ensure normals and vertices have the same length
            public List<VectorData> Vertices { get; private set; }
            public List<VectorData> Normals {
                get => _normals;
                set {
                    if (value is null)
                        return;

                    if (value.Count != Vertices.Count)
                        throw new Exception(StringLib.NormalsMustMatchVertexCount);

                    _normals = value;
                }
            }
            public List<FacetData> Faces { get; private set; }

            public PrimitiveData(List<VectorData> vertices, List<FacetData> faces) {
                if (vertices is null || faces is null)
                    throw new Exception(StringLib.VertexFaceIsRequired);
                Vertices = vertices;
                Faces = faces;
            }

            public static PrimitiveData operator +(PrimitiveData left, PrimitiveData right) {
                int startIdx = left.Vertices.Count;

                // new vertices array
                var vertices = new List<VectorData>(left.Vertices);
                vertices.AddRange(right.Vertices);

                // new normals array
                // NOTE: we are dropping the normals if either side
                // is missing normal definition
                List<VectorData> normals = null;
                if (left.Normals != null && right.Normals != null) {
                    normals = new List<VectorData>(left.Normals);
                    normals.AddRange(right.Normals);
                }

                // shift face indices
                var faces = new List<FacetData>(left.Faces);
                foreach (var faceIdx in right.Faces)
                    faces.Add(faceIdx + (ushort)startIdx);

                return new PrimitiveData(vertices, faces) {
                    Normals = normals,
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

        private float[] LocalizePartStack() {
            List<float> vx = new List<float>();
            List<float> vy = new List<float>();
            List<float> vz = new List<float>();

            foreach (var partData in _partStack)
                foreach (var vtx in partData.Primitive.Vertices) {
                    vx.Add(vtx.X);
                    vy.Add(vtx.Y);
                    vz.Add(vtx.Z);
                }

            var min = new VectorData(vx.Min(), vy.Min(), vz.Min());
            var max = new VectorData(vx.Max(), vy.Max(), vz.Max());
            var anchor = min + ((max - min) / 2f);
            var translate = new VectorData(0, 0, 0) - anchor;

            foreach (var partData in _partStack)
                foreach (var vtx in partData.Primitive.Vertices) {
                    vtx.X += translate.X;
                    vtx.Y += translate.Y;
                    vtx.Z += translate.Z;
                }

            return new float[16] {
                1f,             0f,             0f,             0f,
                0f,             1f,             0f,             0f,
                0f,             0f,             1f,             0f,
                -translate.X,   -translate.Y,   -translate.Z,    1f
            };
        }
    }
    #endregion

    #region IExportContext Implementation
#if REVIT2019
    internal sealed partial class GLTFExportContext : IExportContext, IModelExportContext {
#else
    internal sealed partial class GLTFExportContext : IExportContext, IExportContextBase, IModelExportContext {
#endif
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
                ResetExporter();
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
                    if (RecordOrSkip(view, "x duplicate view", setFlag: true))
                        return RenderNodeAction.Skip;

                    // if active doc and view is valid
                    _actions.Enqueue(new OnSceneBeginAction(view: view));

                    // add an action to the queue that collects the elements
                    // not collected by the IExporter
                    QueueMeshGeometryActions(
                        view,
                        new ElementClassFilter(typeof(TopographySurface))
                        );

                    Logger.LogElement("+ view begin", view);
                    return RenderNodeAction.Proceed;
                }
            }
            // otherwise skip the view
            return RenderNodeAction.Skip;
        }

        private void QueueMeshGeometryActions(View view, ElementFilter filter) {
            foreach(var e in new FilteredElementCollector(view.Document, view.Id).WherePasses(filter))
                _actions.Enqueue(new OnMeshGeometryNodeAction(view: view, element: e));
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

                // TODO: take a look at elements that have no type
                // skipping these for now
                // DB.CurtainGridLine
                // DB.Opening
                // DB.FaceSplitter
                // DB.Spatial
                if (!(doc.GetElement(e.GetTypeId()) is ElementType et))
                    goto SkipElementLabel;

                // TODO: fix inneficiency in getting linked elements multiple times
                // this affects links that have multiple instances
                // remember glTF nodes can not have multiple parents
                // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#nodes-and-hierarchy
                if (!doc.IsLinked) {
                    // check if this element has been processed before
                    if (RecordOrSkip(e, "x duplicate element", setFlag: true))
                        return RenderNodeAction.Skip;
                }

                // Begin: Element
                switch (e) {
                    case View _:
                        goto SkipElementLabel;

                    case RevitLinkInstance linkInst:
                        if (_cfgs.ExportLinkedModels) {
                            Logger.LogElement("+ element (link) begin", e);
                            _actions.Enqueue(
                                new OnNodeBeginAction(
                                    element: e,
                                    type: et,
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
                        _actions.Enqueue(
                            new OnNodeBeginAction(
                                element: famInst,
                                type: et
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
                                    type: et
                                    )
                                );
                        }
                        else {
                            if (c.IsBIC(BuiltInCategory.OST_Cameras)) {
                                // TODO: enqueue camera node
                                goto SkipElementLabel;
                            }
                            else {
                                var cname = c.Name.ToLower();
                                Logger.LogElement($"+ element ({cname}) begin", e);
                                _actions.Enqueue(
                                    new OnNodeBeginAction(
                                        element: generic,
                                        type: et
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
                if (_partStack.Count > 0) {
                    bool alreadyLocalized = false;
                    if (_actions.Last() is OnTransformAction)
                        alreadyLocalized = true;


                    float[] xform = null;
                    if (!alreadyLocalized) {
                        Logger.Log("> localized transform");
                        xform = LocalizePartStack();
                    }
                    
                    foreach (var partData in _partStack)
                        _actions.Enqueue(new OnPartNodeAction(partData));

                    if (xform != null)
                        _actions.Enqueue(new OnTransformAction(xform));
                }
                _partStack.Clear();

                Logger.Log("- element end");
                // end the element
                _actions.Enqueue(new OnNodeEndAction());
            }
        }

        // This is called when family instances are encountered, after OnElementBegin
        public RenderNodeAction OnInstanceBegin(InstanceNode node) {
            Logger.Log("+ instance start");            
            return RenderNodeAction.Proceed;
        }

        public void OnInstanceEnd(InstanceNode node) {
            // NOTE: only add the transform if geometry has already collected
            // for this instance, from the OnFace and OnPolymesh calls between
            // OnInstanceBegin and  OnInstanceEnd
            if (_partStack.Count > 0) {
                Logger.Log("> transform");
                float[] xform = node.GetTransform().ToGLTF();
                _actions.Enqueue(new OnTransformAction(xform));
            }
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
                    Logger.Log("> transform (link)");
                    float[] xform = node.GetTransform().ToGLTF();
                    _actions.Enqueue(new OnTransformAction(xform));

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
                    Logger.Log("x material empty (use color)");
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
            // TODO: anything to do with .GetUV?
            if (_partStack.Count > 0) {
                Logger.Log("> polymesh");
                var activePart = _partStack.Peek();

                List<VectorData> vertices =
                    polymesh.GetPoints().Select(x => new VectorData(x)).ToList();

                List<VectorData> normals = null;
                // TODO: what about the other .DistributionOfNormals options?
                if (polymesh.DistributionOfNormals == DistributionOfNormals.AtEachPoint)
                    normals = polymesh.GetNormals().Select(x => new VectorData(x)).ToList();

                List<FacetData> faces =
                    polymesh.GetFacets().Select(x => new FacetData(x)).ToList();

                var newPrim = new PrimitiveData(vertices, faces) {
                    Normals = normals,
                };

                if (activePart.Primitive is null)
                    activePart.Primitive = newPrim;
                else
                    activePart.Primitive += newPrim;
            }
        }

        public void OnFaceEnd(FaceNode node) {
            Logger.Log("- face end");
        }
#endregion

#region Misc
        public void OnRPC(RPCNode node) {
            Logger.Log("> rpc");
        }

        public void OnLight(LightNode node) {
            Logger.Log("> light");
        }

        public RenderNodeAction OnCurve(CurveNode node) {
            Logger.Log("> curve");
            return RenderNodeAction.Skip;
        }

        public RenderNodeAction OnPolyline(PolylineNode node) {
            Logger.Log("> polyline");
            return RenderNodeAction.Skip;
        }

        public void OnLineSegment(LineSegment segment) {
            Logger.Log("> line segment");
        }

        public void OnPolylineSegments(PolylineSegments segments) {
            Logger.Log("> polyline segment");
        }

        public void OnText(TextNode node) {
            Logger.Log("> text");
        }

        public RenderNodeAction OnPoint(PointNode node) {
            Logger.Log("> point");
            return RenderNodeAction.Skip;
        }

        //public RenderNodeAction OnElementBegin2D(ElementNode node) {
        //    Logger.Log("+ element begin 2d");
        //    return RenderNodeAction.Proceed;
        //}

        //public void OnElementEnd2D(ElementNode node) {
        //    Logger.Log("- element end 2d");
        //}

        //public RenderNodeAction OnFaceEdge2D(FaceEdgeNode node) {
        //    Logger.Log("> face edge 2d");
        //    return RenderNodeAction.Proceed;
        //}

        //public RenderNodeAction OnFaceSilhouette2D(FaceSilhouetteNode node) {
        //    Logger.Log("> face silhouette 2d");
        //    return RenderNodeAction.Proceed;
        //}
#endregion
    }
#endregion

#region Exporter Actions
    internal sealed partial class GLTFExportContext : IExportContext {
        abstract class BaseExporterAction {
            public GLTFBIMAssetExtension AssetExt;
            public bool IncludeHierarchy = true;
            public bool IncludeProperties = true;
            public GLTFBIMPropertyContainer PropertyContainer = null;

            public abstract void Execute(GLTFBuilder gltf, GLTFExportConfigs cfg);
        }

        abstract class BaseElementExporterAction : BaseExporterAction {
            protected Element element;

            public BaseElementExporterAction(Element e) => element = e;

            public override void Execute(GLTFBuilder gltf, GLTFExportConfigs cfg)
                => Execute(
                    gltf,
                    cfg,
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
                                         GLTFExportConfigs cfg,
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
                                         GLTFExportConfigs cfg,
                                         Func<object, string[]> zoneFinder,
                                         Func<object, glTFExtras> extrasBuilder) {
                // start a new gltf scene
                Logger.Log("+ view begin");
                gltf.OpenScene(
                    name: element.Name,
                    exts: new glTFExtension[] {
                        new GLTFBIMNodeExtension(element, zoneFinder, IncludeProperties, PropertyContainer)
                    },
                    extras: extrasBuilder(element)
                    );

                // open a root node for the scene
                gltf.OpenNode(
                    name: string.Format(StringLib.SceneRootNodeName, element.Name),
                    matrix: Transform.CreateTranslation(new XYZ(0, 0, 0)).ToGLTF(),
                    extras: null,
                    exts: null
                    );
            }
        }

        class OnSceneEndAction : ExporterEndAction {
            public override void Execute(GLTFBuilder gltf, GLTFExportConfigs cfg) {
                Logger.Log("- view end");
                // close root node
                gltf.CloseNode();
                // close scene
                gltf.CloseScene();
            }
        }

        class OnMeshGeometryNodeAction : BaseElementExporterAction {
            private View _view = null;
            public OnMeshGeometryNodeAction(View view, Element element) : base(element) { _view = view;  }

            public override void Execute(GLTFBuilder gltf,
                                         GLTFExportConfigs cfg,
                                         Func<object, string[]> zoneFinder,
                                         Func<object, glTFExtras> extrasBuilder) {
                // open a new node and store its id
                Logger.Log("> custom element");

                foreach(var geom in element.get_Geometry(new Options { View = _view })) {
                    if(geom is Mesh mesh) {

                        gltf.OpenNode(
                            name: element.Name,
                            matrix: null,
                            exts: new glTFExtension[] {
                                new GLTFBIMNodeExtension(element, null, IncludeProperties, PropertyContainer)
                            },
                            extras: extrasBuilder(element)
                            );

                        var vertices = new List<float>();
                        foreach (var vec in mesh.Vertices)
                            vertices.AddRange(vec.ToGLTF());
                        
                        var faces = new List<uint>();
                        for(int i = 0; i < mesh.NumTriangles; i++) {
                            var t = mesh.get_Triangle(i);

                            // if element is a topography change associated with
                            // a building pad, the face normals need to be flipped for
                            // the side walls, but not for the base faces
                            if (element is TopographySurface tp
                                    && tp.IsAssociatedWithBuildingPad) {
                                // if the vertices are horizontal (their Z are almost identical)
                                double zAvg = (t.get_Vertex(0).Z + t.get_Vertex(1).Z + t.get_Vertex(2).Z) / 3.0;
                                if (zAvg.AlmostEquals(t.get_Vertex(0).Z)) {
                                    // then add the faces
                                    faces.Add(t.get_Index(0));
                                    faces.Add(t.get_Index(1));
                                    faces.Add(t.get_Index(2));
                                }
                                // otherwise flip their normal
                                else {
                                    faces.Add(t.get_Index(2));
                                    faces.Add(t.get_Index(1));
                                    faces.Add(t.get_Index(0));
                                }
                            }
                            else {
                                faces.Add(t.get_Index(0));
                                faces.Add(t.get_Index(1));
                                faces.Add(t.get_Index(2));
                            }
                        }

                        var primIndex = gltf.AddPrimitive(
                            vertices: vertices.ToArray(),
                            normals: null,
                            faces: faces.ToArray()
                            );

                        // if mesh has material
                        if (mesh.MaterialElementId != ElementId.InvalidElementId) {
                            Material material = element.Document.GetElement(mesh.MaterialElementId) as Material;
                            var existingMaterialIndex =
                                gltf.FindMaterial(
                                    (mat) => {
                                        if (mat.Extensions != null) {
                                            foreach (var ext in mat.Extensions)
                                                if (ext.Value is GLTFBIMMaterialExtensions matExt)
                                                    return matExt.Id == material.UniqueId;
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
                                    name: material.Name,
                                    color: material.Color.ToGLTF(),
                                    exts: new glTFExtension[] {
                            new GLTFBIMMaterialExtensions(material, IncludeProperties, PropertyContainer)
                                    },
                                    extras: null
                                );
                            }
                        }

                        // TODO: otherwise grab the color from graphics styles?
                        else if (mesh.GraphicsStyleId != ElementId.InvalidElementId) {
                        }

                        gltf.CloseNode();
                    }
                }
            }
        }

        class OnNodeBeginAction : ExporterBeginAction {
            private readonly bool _link;
            private readonly ElementType _elementType;

            public OnNodeBeginAction(Element element, ElementType type, bool link = false)
                : base(element) {
                _elementType = type;
                _link = link;
            }

            public override void Execute(GLTFBuilder gltf,
                                         GLTFExportConfigs cfg,
                                         Func<object, string[]> zoneFinder,
                                         Func<object, glTFExtras> extrasBuilder) {
                // open a new node and store its id
                Logger.Log("+ element begin");

                // node filter to pass to gltf builder
                string targetId = string.Empty;
                bool nodeFilter(glTFNode node) {
                    if (node.Extensions != null) {
                        foreach (var ext in node.Extensions)
                            if (ext.Value is GLTFBIMNodeExtension nodeExt)
                                return nodeExt.Id == targetId;
                    }
                    return false;
                }

                // process special cases
                switch (element) {
                    case Level level:
                        // make a matrix from level elevation
                        float elev = level.Elevation.ToGLTFLength();
                        float[] elevMatrix = null;
                        if (elev != 0f) {
                            elevMatrix = new float[16] {
                                1f,   0f,   0f,   0f,
                                0f,   1f,   0f,   0f,
                                0f,   0f,   1f,   0f,
                                0f,   elev, 0f,   1f
                            };
                        }

                        // create level node
                        var levelNodeIdx = gltf.OpenNode(
                            name: level.Name,
                            matrix: elevMatrix,
                            exts: new glTFExtension[] {
                            new GLTFBIMNodeExtension(level, null, IncludeProperties, PropertyContainer)
                            },
                            extras: extrasBuilder(level)
                        );
                        
                        // record the level in asset
                        if (AssetExt != null) {
                            if (AssetExt.Levels is null)
                                AssetExt.Levels = new List<uint>();
                            AssetExt.Levels.Add(levelNodeIdx);
                        }
                        
                        // not need to do anything else
                        return;

                    default:
                        break;
                }

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
                                new GLTFBIMNodeExtension(_elementType, null, IncludeProperties, PropertyContainer)
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
                        matrix: null,
                        exts: new glTFExtension[] {
                            new GLTFBIMNodeExtension(element, zoneFinder, IncludeProperties, PropertyContainer)
                        },
                        extras: extrasBuilder(element)
                    );

                    var bbox = element.get_BoundingBox(null);
                    if (bbox != null)
                        UpdateBounds(
                            gltf: gltf,
                            idx: newNodeIdx,
                            bounds: new GLTFBIMBounds(bbox)
                        );
                }
            }

            private void UpdateBounds(GLTFBuilder gltf, uint idx, GLTFBIMBounds bounds) {
                glTFNode node = gltf.GetNode(idx);
                if (node.Extensions != null) {
                    foreach (var ext in node.Extensions) {
                        if (ext.Value is GLTFBIMNodeExtension nodeExt) {
                            if (nodeExt.Bounds != null)
                                nodeExt.Bounds.Union(bounds);
                            else
                                nodeExt.Bounds = bounds;

                            int parentIdx = gltf.FindParentNode(idx);
                            if (parentIdx >= 0)
                                UpdateBounds(gltf, (uint)parentIdx, nodeExt.Bounds);
                        }
                    }
                }
            }
        }

        class OnNodeEndAction : ExporterEndAction {
            public override void Execute(GLTFBuilder gltf, GLTFExportConfigs cfg) {
                Logger.Log("- element end");
                // close instance node
                gltf.CloseNode();
                // close type node
                if (IncludeHierarchy)
                    gltf.CloseNode();
            }
        }

        class OnTransformAction : BaseExporterAction {
            private readonly float[] _xform;

            public OnTransformAction(float[] xform) => _xform = xform;

            public override void Execute(GLTFBuilder gltf, GLTFExportConfigs cfg) {
                if (gltf.GetActiveNode() is glTFNode activeNode) {
                    Logger.Log("> transform");
                    activeNode.Matrix = _xform;
                }
                else
                    Logger.Log("x transform");
            }
        }

        class OnPartNodeAction : BaseExporterAction {
            private readonly PartData _partData;

            public OnPartNodeAction(PartData partData) => _partData = partData;

            public override void Execute(GLTFBuilder gltf, GLTFExportConfigs cfg) {
                Logger.Log("> primitive");

                // make a new mesh and assign the new material
                var vertices = new List<float>();
                foreach (var vec in _partData.Primitive.Vertices)
                    vertices.AddRange(vec.ToArray());

                var normals = new List<float>();
                //if (_partData.Primitive.Normals != null) {
                //    foreach (var vec in _partData.Primitive.Normals)
                //        normals.AddRange(vec.ToArray());
                //}

                var faces = new List<uint>();
                foreach (var facet in _partData.Primitive.Faces)
                    faces.AddRange(facet.ToArray());

                var primIndex = gltf.AddPrimitive(
                    vertices: vertices.ToArray(),
                    normals: normals.Count > 0 ? normals.ToArray() : null,
                    faces: faces.ToArray()
                    );

                Logger.Log("> material");

                // make sure color is valid, otherwise it will throw
                // exception that color is not initialized
                Color color = _partData.Color.IsValid ? _partData.Color : cfg.DefaultColor;

                // if material information is not provided, make a material
                // based on color and transparency
                if (_partData.Material is null) {
                    string matName = color.GetId();
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
                            color: color.ToGLTF(_partData.Transparency.ToSingle()),
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
                                        if (ext.Value is GLTFBIMMaterialExtensions matExt)
                                            return matExt.Id == _partData.Material.UniqueId;
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
                            name: _partData.Material.Name,
                            color: _partData.Material.Color.ToGLTF(_partData.Material.Transparency / 128f),
                            exts: new glTFExtension[] {
                            new GLTFBIMMaterialExtensions(_partData.Material, IncludeProperties, PropertyContainer)
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
        private bool RecordOrSkip(Element e, string skipMessage, bool setFlag = false) {
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

        private void ResetExporter() {
            // reset the logger
            Logger.Reset();
            _actions.Clear();
            _processed.Clear();
            _skipElement = false;
        }

        internal string Properties {
            get {
                if (_propContainer is null)
                    return null;
                else
                    return _propContainer.Pack();
            }
        }
        
        internal GLTFBuilder Build(ElementFilter filter,
                                   Func<object, string[]> zoneFinder,
                                   Func<object, glTFExtras> extrasBuilder) {
            var glTF = new GLTFBuilder();

            // build asset info
            var doc = _docStack.Last();

            // build asset extension and property source (if needed)
            GLTFBIMAssetExtension assetExt;
            _propContainer = new GLTFBIMPropertyContainer("properties.json");
            if(_cfgs.EmbedParameters)
                assetExt = new GLTFBIMAssetExtension(doc, _cfgs.ExportParameters);
            else {
                assetExt = new GLTFBIMAssetExtension(doc, _cfgs.ExportParameters, _propContainer);
            }

            glTF.SetAsset(
                generatorId: _cfgs.GeneratorId,
                copyright: _cfgs.CopyrightMessage,
                exts: new glTFExtension[] { assetExt },
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
                                BuiltInCategory.OST_Views,
                                BuiltInCategory.OST_Levels,
                                BuiltInCategory.OST_Grids
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
                action.AssetExt = assetExt;
                
                action.IncludeHierarchy = _cfgs.ExportHierarchy;
                action.IncludeProperties = _cfgs.ExportParameters;
                // set the property source for the action if needed
                if (!_cfgs.EmbedParameters)
                    action.PropertyContainer = _propContainer;

                switch (action) {
                    case ExporterBeginAction beg:
                        if (actionFilter is null) {
                            if (extrasBuilder != null)
                                beg.Execute(glTF, _cfgs, zoneFinder, extrasBuilder);
                            else
                                beg.Execute(glTF, _cfgs);
                            passResults.Push(true);
                        }
                        else if (beg.Passes(actionFilter)) {
                            if (extrasBuilder != null)
                                beg.Execute(glTF, _cfgs, zoneFinder, extrasBuilder);
                            else
                                beg.Execute(glTF, _cfgs);
                            passResults.Push(true);
                        }
                        else
                            passResults.Push(false);
                        break;

                    case ExporterEndAction end:
                        if (passResults.Pop())
                            end.Execute(glTF, _cfgs);
                        break;

                    case BaseExporterAction ea:
                        ea.Execute(glTF, _cfgs);
                        break;
                }
            }

            Logger.Log("- end build");

            return glTF;
        }
    }
#endregion
}
