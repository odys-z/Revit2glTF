using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Newtonsoft.Json;
using Autodesk.Revit.DB;

using GLTFRevitExport.Extensions;

namespace GLTFRevitExport {
    #region Initialization
    public partial class GLTFExportContext : IExportContext {
        public GLTFExportContext(Document doc, GLTFExportConfigs configs = null) {
            // ensure base configs
            _cfgs = configs is null ? new GLTFExportConfigs() : configs;
            // place the root document on the stack
            _docStack.Push(doc);
        }
    }
    #endregion

    #region Data Stacks
    public partial class GLTFExportContext : IExportContext {
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
        private GLTFBuilder _glTF = new GLTFBuilder();

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
    public partial class GLTFExportContext : IExportContext {

        // Runs once at beginning of export. Sets up the root node
        // and scene.
        public bool Start() {
            // Do not need to do anything here
            // _glTF is already instantiated
            Log("+ start");
            return true;
        }

        // TODO:
        // Runs once at end of export. Serializes the gltf
        // properties and wites out the *.gltf and *.bin files
        public void Finish() {
            Log("- end");

            //glTFContainer container = _glTF.Finish();

            //if (_cfgs.IncludeNonStdElements) {
            //    // TODO: [RM] Standardize what non glTF spec elements will go into
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

            //if (_cfgs.UseSingleBinary) {
            //    int bytePosition = 0;
            //    int currentBuffer = 0;
            //    foreach (var view in container.glTF.bufferViews) {
            //        if (view.buffer == 0) {
            //            bytePosition += view.byteLength;
            //            continue;
            //        }

            //        if (view.buffer != currentBuffer) {
            //            view.buffer = 0;
            //            view.byteOffset = bytePosition;
            //            bytePosition += view.byteLength;
            //        }
            //    }

            //    glTFBuffer buffer = new glTFBuffer();
            //    buffer.uri = _filename + ".bin";
            //    buffer.byteLength = bytePosition;
            //    container.glTF.buffers.Clear();
            //    container.glTF.buffers.Add(buffer);

            //    using (FileStream f = File.Create(Path.Combine(_directory, buffer.uri))) {
            //        using (BinaryWriter writer = new BinaryWriter(f)) {
            //            foreach (var bin in container.binaries) {
            //                foreach (var coord in bin.contents.vertexBuffer) {
            //                    writer.Write((float)coord);
            //                }
            //                // TODO: add writer for normals buffer
            //                foreach (var index in bin.contents.indexBuffer) {
            //                    writer.Write((int)index);
            //                }
            //            }
            //        }
            //    }
            //}
            //else {
            //    // Write the *.bin files
            //    foreach (var bin in container.binaries) {
            //        using (FileStream f = File.Create(Path.Combine(_directory, bin.name))) {
            //            using (BinaryWriter writer = new BinaryWriter(f)) {
            //                foreach (var coord in bin.contents.vertexBuffer) {
            //                    writer.Write((float)coord);
            //                }
            //                // TODO: add writer for normals buffer
            //                foreach (var index in bin.contents.indexBuffer) {
            //                    writer.Write((int)index);
            //                }
            //            }
            //        }
            //    }
            //}

            //// Write the *.gltf file
            //string serializedModel = JsonConvert.SerializeObject(container.glTF, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            //File.WriteAllText(Path.Combine(_directory, _filename + ".gltf"), serializedModel);
        }

        // This method is invoked many times during the export process
        public bool IsCanceled() {
            if (_cfgs.CancelToken.IsCancellationRequested)
                Log("x cancelled");
            return _cfgs.CancelToken.IsCancellationRequested;
        }

        #region Views
        // revit calls this on every view that is being processed
        // all other methods are called after a view has begun
        public RenderNodeAction OnViewBegin(ViewNode node) {
            // if active doc and view is valid
            if (_docStack.Peek() is Document doc) {
                if (doc.GetElement(node.ViewId) is View view) {
                    if (ShouldSkipElement(view))
                        return RenderNodeAction.Skip;

                    // start a new gltf scene
                    _glTF.OpenScene();
                    
                    // add a root element (all other elements are its children)
                    // root node contains metadata about the scene e.g. bbox
                    _glTF.OpenNode(name: "::rootNode::", matrix: null);
                    
                    LogElement("+ view begin", view);
                    return RenderNodeAction.Proceed;
                }
            }
            // otherwise skip the view
            return RenderNodeAction.Skip;
        }

        public void OnViewEnd(ElementId elementId) {
            if (!_skipped)
                Log("- view end");
        }
        #endregion

        #region Linked Models
        public RenderNodeAction OnLinkBegin(LinkNode node) {
            if (_cfgs.ExportLinkedModels) {
                if (_docStack.Peek() is Document doc) {
                    // grab link data from node
                    ElementId linkId = node.GetSymbolId();
                    Element link = doc.GetElement(linkId);

                    if (ShouldSkipElement(link))
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

                    LogElement("+ link begin", link);
                    return RenderNodeAction.Proceed;
                }
            }
            return RenderNodeAction.Skip;
        }

        public void OnLinkEnd(LinkNode node) {
            if (_cfgs.ExportLinkedModels && !_skipped) {
                Log("- link end");
                _glTF.CloseNode();
                _docStack.Pop();
            }
        }
        #endregion

        #region Elements
        // Runs once for each element.
        public RenderNodeAction OnElementBegin(ElementId eid) {
            if (_docStack.Peek() is Document doc) {
                Element e = doc.GetElement(eid);

                // check if this element has been processed before
                if (ShouldSkipElement(e))
                    return RenderNodeAction.Skip;

                // open a new node and store its id
                _glTF.OpenNode(name: e.Name, matrix: null);

                LogElement("+ element begin", e);
                return RenderNodeAction.Proceed;
            }
            return RenderNodeAction.Skip;
        }

        // Runs at the end of an element being processed, after all other calls for that element.
        public void OnElementEnd(ElementId eid) {
            if (!_skipped) {
                Log("- element end");
                _glTF.CloseNode();
            }
        }

        // This is called when family instances are encountered,
        // after OnElementBegin. We're using it here to maintain the transform
        // stack for that element's heirarchy.
        public RenderNodeAction OnInstanceBegin(InstanceNode node) {
            _glTF.UpdateNodeMatrix(
                node.GetTransform().ToColumnMajorMatrix()
                );
            Log("+ instance start");
            return RenderNodeAction.Proceed;
        }

        // do nothing. OnElementClose will close the element later
        public void OnInstanceEnd(InstanceNode node) {
            Log("- instance end");
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
                    if (ShouldSkipElement(m))
                        return;
                    name = m.Name;
                }
                else {
                    // grab color and transparency
                    var c = node.Color;
                    var t = node.Transparency * 100;
                    var uniqueId = 
                        $"a{t.ToFormattedString()}r{c.Red}g{c.Green}b{c.Blue}";
                    name = $"MaterialNode_{uniqueId}";
                }

                LogElement("+ material", m);
                _glTF.UpdateNodeMaterial(
                    name: name,
                    color: node.Color,
                    transparency: node.Transparency
                );
            }
        }

        // TODO:
        public RenderNodeAction OnFaceBegin(FaceNode node) {
            Log("+ face begin");
            return RenderNodeAction.Proceed;
        }

        // Runs for every polymesh being processed. Typically this is a single
        // face of an element's mesh
        public void OnPolymesh(PolymeshTopology polymesh) {
            // TODO: anything to do with .DistributionOfNormals or .GetUV?
            Log("> polymesh");
            _glTF.UpdateNodeGeometry(
                vertices: polymesh.GetPoints().Select(x => x.ToGLTF()).ToArray(),
                normals: polymesh.GetNormals().Select(x => x.ToGLTF()).ToArray(),
                faces: polymesh.GetFacets().Select(x => x.ToGLTF()).ToArray()
                );
        }

        // TODO:
        public void OnFaceEnd(FaceNode node) {
            Log("+ face end");
        }
        #endregion

        #region Misc
        public void OnRPC(RPCNode node) {
            // do nothing
        }

        public void OnLight(LightNode node) {
            // do nothing
        }
        #endregion
    }
    #endregion

    #region Utility Methods
    public partial class GLTFExportContext : IExportContext {
        /// <summary>
        /// Determine if given element should be skipped
        /// </summary>
        /// <param name="e">Target element</param>
        /// <returns>True if element should be skipped</returns>
        private bool ShouldSkipElement(Element e) {
            _skipped = false;
            if (e is null || _processed.Contains(e.UniqueId)) {
                Log("x skipped duplicate");
                _skipped = true;
            }
            else
                _processed.Add(e.UniqueId);
            return _skipped;
        }

        /// <summary>
        /// Log debug message with element info
        /// </summary>
        /// <param name="message">Debug message</param>
        /// <param name="e">Target Element</param>
        private void LogElement(string message, Element e) {
#if DEBUG
            message += $"\n| id={e.Id.IntegerValue} name={e.Name} type={e.GetType()} category={e.Category?.Name}";
            Log(message);
#endif
        }

        private int Depth = 0;

        /// <summary>
        /// Log debug message
        /// </summary>
        /// <param name="message">Debug message</param>
        private void Log(string message) {
#if DEBUG
            if (message.StartsWith("+"))
                Depth++;
            else if (message.StartsWith("-"))
                Depth--;

            string indent = "";
            for (int i = 0; i < Depth; i++)
                indent += "  ";
            Debug.WriteLine(indent + message);
#endif
        }
    }
    #endregion
}
