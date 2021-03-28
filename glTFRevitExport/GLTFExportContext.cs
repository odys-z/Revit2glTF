using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;

using GLTFRevitExport.GLTF.Schema;
using GLTFRevitExport.GLTF.Package;
using GLTFRevitExport.Extensions;
using GLTFRevitExport.ExportContext;
using GLTFRevitExport.ExportContext.BuildActions;
using GLTFRevitExport.ExportContext.Geometry;

using Autodesk.Revit.DB.Architecture;

namespace GLTFRevitExport {
    #region Initialization
    sealed partial class GLTFExportContext : IExportContext {
        public GLTFExportContext(Document doc, GLTFExportConfigs exportConfigs = null) {
            // ensure base configs
            _cfgs = exportConfigs is null ? new GLTFExportConfigs() : exportConfigs;

            // reset stacks
            ResetExporter();
            // place doc on the stack
            _docStack.Push(doc);
        }

        private void ResetExporter() {
            // reset the logger
            Logger.Reset();
            _actions.Clear();
            _processed.Clear();
            _skipElement = false;
        }
    }
    #endregion

    #region Data Stacks
    sealed partial class GLTFExportContext : IExportContext {
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
        /// View stack to hold the view being processed.
        /// A stack is used to allow referencing view when needed.
        /// It is not expected for this stack to hold more than one view,
        /// however stack has been used for consistency
        /// </summary>
        private readonly Stack<View> _viewStack = new Stack<View>();

        /// <summary>
        /// Queue of actions collected during export. These actions are then
        /// played back on each .Build call to create separate glTF outputs
        /// </summary>
        private readonly Queue<BaseAction> _actions = new Queue<BaseAction>();

        /// <summary>
        /// List of processed elements by their unique id
        /// </summary>
        private readonly List<string> _processed = new List<string>();

        /// <summary>
        /// Flag to mark current node as skipped
        /// </summary>
        private bool _skipElement = false;


        private readonly Stack<PartData> _partStack = new Stack<PartData>();

        private BoundsData CalculateBounds(float[] matrix = null) {
            float minx, miny, minz, maxx, maxy, maxz;
            minx = miny = minz = maxx = maxy = maxz = float.NaN;

            Transform xform = null;
            if (matrix != null)
                xform = matrix.FromGLTFMatrix();
            
            foreach (var partData in _partStack)
                foreach (var vertex in partData.Primitive.Vertices) {
                    var vtx = vertex;
                    if (xform != null)
                        vtx = vertex.Transform(xform);

                    minx = minx is float.NaN || vtx.X < minx ? vtx.X : minx;
                    miny = miny is float.NaN || vtx.Y < miny ? vtx.Y : miny;
                    minz = minz is float.NaN || vtx.Z < minz ? vtx.Z : minz;
                    maxx = maxx is float.NaN || vtx.X > maxx ? vtx.X : maxx;
                    maxy = maxy is float.NaN || vtx.Y > maxy ? vtx.Y : maxy;
                    maxz = maxz is float.NaN || vtx.Z > maxz ? vtx.Z : maxz;
                }

            return new BoundsData(
                new VectorData(minx, miny, minz),
                new VectorData(maxx, maxy, maxz)
            );
        }

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
    sealed partial class GLTFExportContext : IExportContext, IModelExportContext {
#else
    sealed partial class GLTFExportContext : IExportContext, IExportContextBase, IModelExportContext {
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
                    _actions.Enqueue(new SceneBeginAction(view: view));
                    _viewStack.Push(view);

                    // add an action to the queue that collects the elements
                    // not collected by the IExporter
                    QueueLevelActions(doc, view);
                    QueueGridActions(doc);
                    QueuePartFromElementActions(
                        doc,
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

        private void QueueLevelActions(Document doc, View view) {
            Logger.Log("> collecting levels");

            // collect levels from project or view only?
            foreach (var e in new FilteredElementCollector(doc, view.Id)
                                  .OfCategory(BuiltInCategory.OST_Levels)
                                  .WhereElementIsNotElementType())
                _actions.Enqueue(
                    new LevelAction(element: e, extents: e.get_BoundingBox(view))
                    );
        }

        private void QueueGridActions(Document doc) {
            Logger.Log("> collecting grids");
            
            // first collect the multisegment grids and record their children
            // multi-segment grids are not supported and the segments will not
            // be procesed as grids
            var childGrids = new HashSet<ElementId>();
            foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(MultiSegmentGrid)).WhereElementIsNotElementType()) {
                if (e is MultiSegmentGrid multiGrid) {
                    childGrids.UnionWith(multiGrid.GetGridIds());
                }
            }

            // then record the rest of the grids and omit the already recorded ones
            foreach (var e in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Grids).WhereElementIsNotElementType())
                if (!childGrids.Contains(e.Id))
                    _actions.Enqueue(new GridAction(element: e));
        }

        private void QueuePartFromElementActions(Document doc, View view, ElementFilter filter) {
            foreach (var e in new FilteredElementCollector(view.Document, view.Id).WherePasses(filter))
                _actions.Enqueue(new PartFromElementAction(view: view, element: e));
        }

        public void OnViewEnd(ElementId elementId) {
            if (_skipElement)
                _skipElement = false;
            else {
                Logger.Log("- view end");
                _actions.Enqueue(new SceneEndAction());
                _viewStack.Pop();
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
                    // Skip all these element types
                    case View _:
                    case Level _:
                    case Grid _:
                        goto SkipElementLabel;

                    case RevitLinkInstance linkInst:
                        if (_cfgs.ExportLinkedModels) {
                            Logger.LogElement("+ element (link) begin", e);
                            _actions.Enqueue(
                                new LinkBeginAction(
                                    link: linkInst,
                                    linkType: (RevitLinkType)et,
                                    linkedDoc: linkInst.GetLinkDocument()
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
                            new ElementBeginAction(element: famInst, type: et)
                            );
                        break;

                    case Element generic:
                        var c = e.Category;
                        if (c is null) {
                            Logger.LogElement($"+ element (generic) begin", e);
                            _actions.Enqueue(
                                new ElementBeginAction(
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
                                    new ElementBeginAction(
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
                    // calculate the bounding box from the parts data
                    BoundsData bounds;
                    if (_actions.Last() is ElementTransformAction action) {
                        // transform bounds with existing transform
                        Logger.Log("> determine instance bounding box");
                        bounds = CalculateBounds(action.Matrix);
                    } else {
                        Logger.Log("> determine bounding box");
                        bounds = CalculateBounds();
                        
                        Logger.Log("> localized transform");
                        float[] xform = LocalizePartStack();
                        _actions.Enqueue(new ElementTransformAction(xform));
                    }

                    _actions.Enqueue(new ElementBoundsAction(bounds));
                    
                    foreach (var partData in _partStack)
                        _actions.Enqueue(new PartFromDataAction(partData));
                }
                _partStack.Clear();

                // end the element
                Logger.Log("- element end");
                if (_docStack.Peek() is Document doc) {
                    Element e = doc.GetElement(eid);
                    if (e is RevitLinkInstance)
                        _actions.Enqueue(new LinkEndAction());
                    else
                        _actions.Enqueue(new ElementEndAction());
                }
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
                _actions.Enqueue(new ElementTransformAction(xform));
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
                    _actions.Enqueue(new LinkTransformAction(xform));

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
                                && node.Color.IsValid
                                && partPrim.Color.IsValid
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

                List<FacetData> faces =
                    polymesh.GetFacets().Select(x => new FacetData(x)).ToList();

                var newPrim = new PrimitiveData(vertices, faces);

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

    #region Build
    sealed partial class GLTFExportContext : IExportContext {
        public List<GLTFPackageItem> Build(ElementFilter filter,
                                           Func<object, string[]> zoneFinder,
                                           Func<object, glTFExtras> extrasBuilder,
                                           GLTFBuildConfigs buildConfigs = null) {
            // ensure configs
            buildConfigs = buildConfigs ?? new GLTFBuildConfigs();

            // build asset info
            var doc = _docStack.Last();

            // create main gltf builder
            var mainCtx = new BuildContext("model", doc, _cfgs, extrasBuilder);
            var buildContexts = new List<BuildContext> { mainCtx };

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
            BuildContext currentCtx = mainCtx;
            BuildContext activeLinkCtx = null;
            foreach (var action in _actions) {
                action.AssetExt = currentCtx.AssetExtension;

                action.IncludeHierarchy = _cfgs.ExportHierarchy;
                action.IncludeProperties = _cfgs.ExportParameters;
                // set the property source for the action if needed
                if (!_cfgs.EmbedParameters)
                    action.PropertyContainer = currentCtx.PropertyContainer;

                if (!_cfgs.EmbedLinkedModels) {
                    if (action is LinkBeginAction linkBeg) {
                        linkBeg.Uri = $"{linkBeg.LinkId}.gltf";
                    }
                    else if (activeLinkCtx != null)
                        // Note:
                        // LinkEndAction should be always preceded by ElementTransformAction
                        // switch to main builder. We need to switch to main builder on 
                        // ElementTransformAction to apply the correct transform
                        // to the link instance node in the main builder
                        if (action is LinkTransformAction) {
                            // switch to main builder
                            currentCtx = mainCtx;
                        }
                        // close the link builder
                        else if (action is LinkEndAction) {
                            // close the link
                            activeLinkCtx.Builder.CloseScene();
                            buildContexts.Add(activeLinkCtx);
                            // switch to main builder
                            activeLinkCtx = null;
                            currentCtx = mainCtx;
                        }
                }

                switch (action) {
                    case BuildBeginAction beg:
                        if (actionFilter is null) {
                            if (extrasBuilder != null)
                                beg.Execute(currentCtx.Builder, _cfgs, zoneFinder, extrasBuilder);
                            else
                                beg.Execute(currentCtx.Builder, _cfgs);
                            passResults.Push(true);
                        }
                        else if (beg.Passes(actionFilter)) {
                            if (extrasBuilder != null)
                                beg.Execute(currentCtx.Builder, _cfgs, zoneFinder, extrasBuilder);
                            else
                                beg.Execute(currentCtx.Builder, _cfgs);
                            passResults.Push(true);
                        }
                        else
                            passResults.Push(false);
                        break;

                    case BuildEndAction end:
                        if (passResults.Pop())
                            end.Execute(currentCtx.Builder, _cfgs);
                        break;

                    case BaseAction ea:
                        ea.Execute(currentCtx.Builder, _cfgs);
                        break;
                }

                // use this link builder for the rest of actions
                // that happen inside the link
                if (!_cfgs.EmbedLinkedModels)
                    if (action is LinkBeginAction linkBeg) {
                        // create a new glTF for this link
                        activeLinkCtx = new BuildContext(
                            name: linkBeg.LinkId,
                            doc: linkBeg.LinkDocument,
                            exportCfgs: _cfgs,
                            extrasBuilder: extrasBuilder
                        );

                        activeLinkCtx.Builder.OpenScene(name: "default", exts: null, extras: null);

                        // use this builder for all subsequent elements
                        currentCtx = activeLinkCtx;
                    }
            }

            Logger.Log("- end build");

            Logger.Log("+ start pack");

            // prepare pack
            var gltfPack = new List<GLTFPackageItem>();

            foreach (var buildCtx in buildContexts)
                gltfPack.AddRange(buildCtx.Pack(buildConfigs));

            Logger.Log("- end pack");

            return gltfPack;
        }
    }
    #endregion

    #region Utility Methods
    sealed partial class GLTFExportContext : IExportContext {
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
    }
    #endregion
}
