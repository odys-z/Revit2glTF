using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Autodesk.Revit.DB;

using GLTFRevitExport.GLTF;
using GLTFRevitExport.Extensions;
using GLTFRevitExport.GLTFExtensions;
using GLTFRevitExport.Containers;
using GLTFRevitExport.GLTF.Types;
using System.Runtime.CompilerServices;
using Autodesk.Revit.UI;

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

        private readonly Stack<GLTFMesh> _meshStack = new Stack<GLTFMesh>();
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
            // TODO: process extra content
            //if (_cfgs.IncludeNonStdElements) {
            //    // this "BIM glTF superset" and write a spec for it. Gridlines below
            //    // are an example.

            //    // Add gridlines as gltf nodes in the format:
            //    // Origin {Vec3<double>}, Direction {Vec3<double>}, Length {double}
            //    FilteredElementCollector col = new FilteredElementCollector(_doc)
            //        .OfClass(typeof(Grid));

            //    var grids = col.ToElements();
            //    foreach (Grid g in grids) {
            //        Line l = g.Curve as Line;

            //        var origin = l.Origin;
            //        var direction = l.Direction;
            //        var length = l.Length;

            //        var xtras = new glTFExtras();
            //        var grid = new GridParameters();
            //        grid.origin = new List<double>() { origin.X, origin.Y, origin.Z };
            //        grid.direction = new List<double>() { direction.X, direction.Y, direction.Z };
            //        grid.length = length;
            //        xtras.GridParameters = grid;
            //        xtras.UniqueId = g.UniqueId;
            //        xtras.Properties = Util.GetElementProperties(g, true);

            //        var gridNode = new glTFNode();
            //        gridNode.name = g.Name;
            //        gridNode.extras = xtras;

            //        container.glTF.nodes.Add(gridNode);
            //        container.glTF.nodes[0].children.Add(container.glTF.nodes.Count - 1);
            //    }
            //}

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
                                new OnMetaNodeBeginAction(
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
                            new OnMetaNodeBeginAction(
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
                                new OnMetaNodeBeginAction(
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
                                    new OnMetaNodeBeginAction(
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
                if (_meshStack.Count > 0)
                    foreach(var mesh in _meshStack)
                        _actions.Enqueue(new OnMeshNodeAction(mesh));
                _meshStack.Clear();

                Logger.Log("- element end");
                // end the element
                _actions.Enqueue(new OnMetaNodeEndAction());
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
                    if (_meshStack.Count > 0
                            && _meshStack.Peek() is GLTFMesh activeMesh) {
                        // if material is same as active, ignore
                        if (activeMesh.Material != null
                                && m.UniqueId == activeMesh.Material.UniqueId) {
                            Logger.Log("> material keep");
                            return;
                        }
                    }
                    Logger.LogElement("> material", m);
                    _meshStack.Push(
                        new GLTFMesh {
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
                    if (_meshStack.Count > 0
                            && _meshStack.Peek() is GLTFMesh activeMesh) {
                        // if color and transparency are the same
                        if (activeMesh.Material is null
                                && node.Color.Compare(activeMesh.Color)
                                && node.Transparency == activeMesh.Transparency) {
                            Logger.Log("> material keep");
                            return;
                        }
                    }
                    Logger.LogElement("> material", m);
                    _meshStack.Push(
                        new GLTFMesh {
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
            if (_meshStack.Count > 0) {
                Logger.Log("> polymesh");
                var activeMesh = _meshStack.Pop();

                var newMesh = new GLTFMesh {
                    Vertices = polymesh.GetPoints().Select(x => x.ToGLTF()).ToList(),
                    Normals = polymesh.GetNormals().Select(x => x.ToGLTF()).ToList(),
                    Faces = polymesh.GetFacets().Select(x => x.ToGLTF()).ToList()
                };

                activeMesh = activeMesh + newMesh;
                _meshStack.Push(activeMesh);
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
                        new glTFBIMExtensionNode(element, zoneFinder, IncludeProperties)
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

        class OnMetaNodeBeginAction : ExporterBeginAction {
            private readonly double[] _xform;
            private readonly bool _link;
            private readonly ElementType _elementType;

            public OnMetaNodeBeginAction(Element element, ElementType type,
                                        double[] xform, bool link = false)
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
                        foreach (var nodeExt in node.Extensions)
                            if (nodeExt.Value is glTFBIMExtensionBaseNodeData bimExt)
                                return bimExt.Id == targetId;
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
                                new glTFBIMExtensionNode(_elementType, null, IncludeProperties)
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
                    var bimExt =
                        _link ?
                        new glTFBIMExtensionLinkNode(element, zoneFinder, IncludeProperties)
                            : (glTFExtension)new glTFBIMExtensionNode(element, zoneFinder, IncludeProperties);

                    var newNodeIdx = gltf.OpenNode(
                        name: element.Name,
                        matrix: _xform,
                        exts: new glTFExtension[] {
                            bimExt
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
                glTFNode currentNode = gltf.GetNode(idx);
                if (currentNode.Extensions != null) {
                    foreach (var nodeExt in currentNode.Extensions) {
                        if (nodeExt.Value is glTFBIMExtensionBaseNodeData bimExt) {
                            if (bimExt.Bounds != null)
                                bimExt.Bounds.Union(bounds);
                            else
                                bimExt.Bounds = bounds;

                            int parentIdx = gltf.FindParentNode(idx);
                            if (parentIdx >= 0)
                                updateBounds(gltf, (uint)parentIdx, bimExt.Bounds);
                        }
                    }
                }
            }
        }

        class OnMetaNodeEndAction : ExporterEndAction {
            public override void Execute(GLTFBuilder gltf) {
                Logger.Log("- element end");
                // close instance node
                gltf.CloseNode();
                // close type node
                if (IncludeHierarchy)
                    gltf.CloseNode();
            }
        }

        class OnMeshNodeAction : BaseExporterAction {
            private GLTFMesh _mesh;

            public OnMeshNodeAction(GLTFMesh mesh) => _mesh = mesh;

            public override void Execute(GLTFBuilder gltf) {
                if (gltf.GetActiveNode() is glTFNode activeNode) {
                    // create a new node for this mesh
                    gltf.OpenNode(
                        name: $"{activeNode.Name} - Part",
                        matrix: activeNode.Matrix,
                        exts: new glTFExtension[] {
                            new glTFBIMExtensionNodePart()
                        },
                        extras: null
                    );

                    Logger.Log("> material");
                    uint matIdx;
                    if (_mesh.Material is null) {
                        // make a new material from color and transparency
                        matIdx = gltf.NewMaterial(
                            name: _mesh.Color.GetId(),
                            color: _mesh.Color.ToGLTF((float)_mesh.Transparency),
                            transparency: (float)_mesh.Transparency,
                            exts: null
                        );

                    }
                    else {
                        // make a new material and get its index
                        matIdx = gltf.NewMaterial(
                            name: _mesh.Material.Name,
                            color: _mesh.Color.ToGLTF((float)_mesh.Transparency),
                            transparency: (float)_mesh.Transparency,
                            exts: new glTFExtension[] {
                                new glTFBIMExtensionNode(_mesh.Material, null, IncludeProperties)
                            }
                        );
                    }

                    Logger.Log("> mesh");
                    // make a new mesh and assign the new material
                    gltf.NewMesh(
                        vertices: _mesh.GetVertexBuffer(),
                        normals: _mesh.GetNormalBuffer(),
                        faces: _mesh.GetFaceBuffer(),
                        material: (int)matIdx
                        );

                    // close the node
                    gltf.CloseNode();
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
                    new glTFBIMExtensionDocumentData(doc, _cfgs.ExportParameters)
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
