using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Autodesk.Revit.DB;

using GLTFRevitExport.GLTF;
using GLTFRevitExport.Extensions;
using GLTFRevitExport.Properties;

namespace GLTFRevitExport {
    #region Initialization
    public sealed partial class GLTFExportContext : IExportContext {
        public GLTFExportContext(Document doc, GLTFExportConfigs configs = null) {
            // reset the logger
            Logger.Reset();
            // ensure base configs
            _cfgs = configs is null ? new GLTFExportConfigs() : configs;
            // place the root document on the stack
            _docStack.Push(doc);

            // create a new glTF builder
            _glTF = new GLTFBuilder(configs.GeneratorId,
                                    configs.CopyrightMessage);
        }
    }
    #endregion

    #region Data Stacks
    public sealed partial class GLTFExportContext : IExportContext {
        /// <summary>
        /// Configurations for the active export
        /// </summary>
        private GLTFExportConfigs _cfgs = new GLTFExportConfigs();

        /// <summary>
        /// Document stack to hold the documents being processed.
        /// A stack is used to allow processing nested documents (linked docs)
        /// </summary>
        private Stack<Document> _docStack = new Stack<Document>();

        /// <summary>
        /// Instance of glTF data structure
        /// </summary>
        private GLTFBuilder _glTF = null;

        /// <summary>
        /// List of processed elements by their unique id
        /// </summary>
        private List<string> _processed = new List<string>();

        /// <summary>
        /// Flag to mark current node as skipped
        /// </summary>
        private bool _skipped = false;
    }
    #endregion

    #region IExportContext Implementation
    public sealed partial class GLTFExportContext : IExportContext {

        // Runs once at beginning of export. Sets up the root node
        // and scene.
        public bool Start() {
            // Do not need to do anything here
            // _glTF is already instantiated
            Logger.Log("+ start");
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

            Logger.Log("- end");
        }

        // This method is invoked many times during the export process
        public bool IsCanceled() {
            if (_cfgs.CancelToken.IsCancellationRequested)
                Logger.Log("x cancelled");
            return _cfgs.CancelToken.IsCancellationRequested;
        }

        #region Views
        // revit calls this on every view that is being processed
        // all other methods are called after a view has begun
        public RenderNodeAction OnViewBegin(ViewNode node) {
            // if active doc and view is valid
            if (_docStack.Peek() is Document doc) {
                if (doc.GetElement(node.ViewId) is View view) {
                    if (RecordOrSkip(view, "x duplicate view", setFlag: true))
                        return RenderNodeAction.Skip;

                    // start a new gltf scene
                    _glTF.OpenScene(name: view.Name);

                    // add a root element (all other elements are its children)
                    // root node contains metadata about the scene e.g. bbox
                    _glTF.OpenNode(name: StringLib.RootNodeName, matrix: null);

                    Logger.LogElement("+ view begin", view);
                    return RenderNodeAction.Proceed;
                }
            }
            // otherwise skip the view
            return RenderNodeAction.Skip;
        }

        public void OnViewEnd(ElementId elementId) {
            if (_skipped)
                _skipped = false;
            else {
                Logger.Log("- view end");
                _glTF.CloseScene();
            }
        }
        #endregion

        #region Linked Models
        public RenderNodeAction OnLinkBegin(LinkNode node) {
            if (_docStack.Peek() is Document doc) {
                // grab link data from node
                ElementId linkId = node.GetSymbolId();
                Element link = doc.GetElement(linkId);

                if (_cfgs.ExportLinkedModels) {
                    if (RecordOrSkip(link, "x duplicate link", setFlag: true))
                        return RenderNodeAction.Skip;

                    // open a new gltf link node
                    var xform = node.GetTransform();
                    _glTF.OpenNode(
                        name: link.Name,
                        matrix: xform.ToColumnMajorMatrix()
                    );

                    // store the link document
                    // all other element calls belong to this linked model
                    // and will use this doc to grab data
                    _docStack.Push(node.GetDocument());

                    Logger.LogElement("+ link begin", link);
                    return RenderNodeAction.Proceed;
                }
                else
                    Logger.LogElement("~ exclude links", link);
            }
            return RenderNodeAction.Skip;
        }

        public void OnLinkEnd(LinkNode node) {
            if (_skipped)
                _skipped = false;
            else {
                if (_cfgs.ExportLinkedModels) {
                    Logger.Log("- link end");
                    _glTF.CloseNode();
                    _docStack.Pop();
                }
            }
        }
        #endregion

        #region Elements
        // Runs once for each element.
        public RenderNodeAction OnElementBegin(ElementId eid) {
            if (_docStack.Peek() is Document doc) {
                Element e = doc.GetElement(eid);

                // check if this element has been processed before
                if (RecordOrSkip(e, "x duplicate element", setFlag: true))
                    return RenderNodeAction.Skip;

                // open a new node and store its id
                _glTF.OpenNode(name: e.Name, matrix: null);

                Logger.LogElement("+ element begin", e);
                return RenderNodeAction.Proceed;
            }
            return RenderNodeAction.Skip;
        }

        // Runs at the end of an element being processed, after all other calls for that element.
        public void OnElementEnd(ElementId eid) {
            if (_skipped)
                _skipped = false;
            else {
                Logger.Log("- element end");
                _glTF.CloseNode();
            }
        }

        // This is called when family instances are encountered,
        // after OnElementBegin. We're using it here to maintain the transform
        // stack for that element's heirarchy.
        public RenderNodeAction OnInstanceBegin(InstanceNode node) {
            Logger.Log("+ instance start");
            Logger.Log("> transform");
            _glTF.UpdateNodeMatrix(
                node.GetTransform().ToColumnMajorMatrix()
                );
            return RenderNodeAction.Proceed;
        }

        // do nothing. OnElementClose will close the element later
        public void OnInstanceEnd(InstanceNode node) {
            Logger.Log("- instance end");
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

                // build a name for the material
                string name = string.Empty;
                if (m != null) {
                    // check if this element has been processed before
                    if (RecordOrSkip(m, "x duplicate material"))
                        return;
                    name = m.Name;

                    Logger.LogElement("> material", m);
                    _glTF.UpdateNodeMaterial(
                        name: name,
                        color: node.Color,
                        transparency: node.Transparency
                    );
                }
                else
                    Logger.Log("> material keep");
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
            Logger.Log("> polymesh");
            _glTF.UpdateNodeGeometry(
                vertices: polymesh.GetPoints().Select(x => x.ToGLTF()).ToArray(),
                normals: polymesh.GetNormals().Select(x => x.ToGLTF()).ToArray(),
                faces: polymesh.GetFacets().Select(x => x.ToGLTF()).ToArray()
                );
        }

        public void OnFaceEnd(FaceNode node) {
            Logger.Log("- face end");
        }
        #endregion

        #region Misc
        public void OnRPC(RPCNode node) {
            // TODO: on RPC
            Logger.Log("> rpc");
        }

        public void OnLight(LightNode node) {
            // TODO: on light
            Logger.Log("> light");
        }
        #endregion
    }
    #endregion

    #region Utility Methods
    public sealed partial class GLTFExportContext : IExportContext {
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
            else if (e != null && _processed.Contains(e.UniqueId)) {
                Logger.LogElement(skipMessage, e);
                skip = true;
            }
            else
                _processed.Add(e.UniqueId);

            if (setFlag)
                _skipped = skip;
            return skip;
        }

        // Serializes the gltf write out the *.gltf and *.bin files
        public bool Write(string filename, string directory) {
            // ensure filename is really a file name and no extension
            filename = Path.GetFileNameWithoutExtension(filename);

            // pack the glTF data and get the container
            var container = _glTF.Pack(
                filename: filename,
                singleBinary: _cfgs.UseSingleBinary
            );

            container.Write(directory);
            return true;
        }
    }
    #endregion
}
