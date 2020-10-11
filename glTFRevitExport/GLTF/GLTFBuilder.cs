using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;

using GLTFRevitExport.GLTF.Types;
using GLTFRevitExport.Properties;

namespace GLTFRevitExport.GLTF {
    #region Initialization, Completion
    internal sealed partial class GLTFBuilder {
        internal GLTFBuilder(string generatorId = null,
                             string copyright = null,
                             glTFExtension ext = null) {
            _gltf = new glTF();

            var assetExts = new Dictionary<string, glTFExtension>();
            if (ext != null) {
                assetExts.Add(ext.Name, ext);
                ensureExtensionUsed(ext);
            }

            _gltf.Asset = new glTFAsset {
                Generator = generatorId,
                Copyright = copyright,
                Extensions = assetExts.Count > 0 ? assetExts : null
            };

        }

        /// <summary>
        /// Pack the constructed glTF data into a container
        /// </summary>
        /// <returns></returns>
        internal GLTFContainer Pack(string filename, bool singleBinary = true) {
            // store snapshot of collected data into a gltf structure
            return new GLTFContainer() {
                Name = filename,
                Model = _gltf
            };
            //if (singleBinary) {
            //    int bytePosition = 0;
            //    int currentBuffer = 0;
            //    foreach (var _buffView in _bufferViews) {
            //        if (_buffView.buffer == 0) {
            //            bytePosition += _buffView.byteLength;
            //            continue;
            //        }

            //        if (_buffView.buffer != currentBuffer) {
            //            _buffView.buffer = 0;
            //            _buffView.byteOffset = bytePosition;
            //            bytePosition += _buffView.byteLength;
            //        }
            //    }

            //    var buffer = new glTFBuffer();
            //    buffer.uri = filename + ".bin";
            //    buffer.byteLength = bytePosition;
            //    _gltf.Buffers.Clear();
            //    _gltf.Buffers.Add(buffer);

            //    // TODO: binaries?!
            //    return new GLTFContainer() {
            //        Name = filename,
            //        Model = _gltf
            //    };
            //}
            //else {
            //    return new GLTFContainer() {
            //        Name = filename,
            //        Model = _gltf,
            //        Binaries = _binaryFileData
            //    };
            //}
        }
    }
    #endregion

    #region Data stacks
    internal sealed partial class GLTFBuilder {
        private readonly glTF _gltf = null;
        
        private glTFScene peekScene() => _gltf.Scenes.LastOrDefault();
        
        private glTFNode peekNode() => _gltf.Nodes.LastOrDefault();

        public void ensureExtensionUsed(glTFExtension ext) {
            if (_gltf.ExtensionsUsed is null)
                _gltf.ExtensionsUsed = new HashSet<string>();
            _gltf.ExtensionsUsed.Add(ext.Name);
        }

        public uint ensureNodeInScene(uint idx) {
            if (peekScene() is glTFScene scene) {
                if (!_gltf.Nodes.IsOpen())
                    scene.Nodes.Add(idx);
                return idx;
            }
            else
                throw new Exception(StringLib.NoParentScene);
        }

        public uint appendNode(string name, double[] matrix, glTFExtension[] exts, glTFExtras extras) {
            // create new node and set base properties
            var node = new glTFNode() {
                Name = name ?? "undefined",
                Matrix = matrix,
                Extensions = exts?.ToDictionary(x => x.Name, x => x),
                Extras = extras
            };

            var idx = _gltf.Nodes.Append(node);
            return ensureNodeInScene(idx);
        }
    }
    #endregion

    #region Builders
    internal sealed partial class GLTFBuilder {
        public void UseExtension(glTFExtension ext) => ensureExtensionUsed(ext);

        public uint OpenScene(string name, glTFExtension[] exts, glTFExtras extras) {
            _gltf.Scenes.Add(
                new glTFScene {
                    Name = name,
                    Extensions = exts?.ToDictionary(x => x.Name, x => x),
                    Extras = extras
                }
                );
            return (uint)_gltf.Scenes.Count - 1;
        }

        public uint OpenNode(string name, double[] matrix, glTFExtension[] exts, glTFExtras extras) {
            var idx = appendNode(name, matrix, exts, extras);
            _gltf.Nodes.Open(idx);
            return idx;
        }

        public glTFNode GetNode(uint idx) => _gltf.Nodes[idx];

        public int FindNode(Func<glTFNode, bool> filter) {
            foreach (var node in _gltf.Nodes)
                if (filter(node))
                    return (int)_gltf.Nodes.IndexOf(node);
            return -1;
        }

        public int FindChildNode(Func<glTFNode, bool> filter) {
            if (_gltf.Nodes.Peek() is glTFNode currentNode) {
                if (currentNode.Children is null)
                    return -1;

                uint idx = _gltf.Nodes.IndexOf(currentNode) + 1;
                foreach (var childIdx in currentNode.Children) {
                    var node = _gltf.Nodes[childIdx];
                    if (filter(node))
                        return (int)idx;
                    idx++;
                }
                return -1;
            }
            else
                return FindNode(filter);
        }

        public int FindParentNode(uint idx) {
            foreach (var node in _gltf.Nodes)
                if (node.Children != null && node.Children.Contains(idx))
                    return (int)_gltf.Nodes.IndexOf(node);
            return -1;
        }

        public uint OpenExistingNode(uint idx) {
            if (_gltf.Nodes.Contains(idx)) {
                ensureNodeInScene(idx);
                _gltf.Nodes.Open(idx);
                return idx;
            }
            else
                throw new Exception(StringLib.NodeNotExist);
        }

        public void UpdateNodeMatrix(double[] matrix) {
            if (_gltf.Nodes.Peek() is glTFNode currentNode)
                currentNode.Matrix = matrix;
            else
                throw new Exception(StringLib.NoParentNode);
        }

        public void UpdateNodeGeometryMaterial(string name, Color color = null, double transparency = 0.0) {
            if (_gltf.Nodes.Peek() is glTFNode) {
                // TODO: add this to materials or use existing
                var material = new glTFMaterial() {
                    Name = name,
                    PBRMetallicRoughness = new glTFPBRMetallicRoughness() {
                        BaseColorFactor = new List<float>() {
                        color.Red / 255f,
                        color.Green / 255f,
                        color.Blue / 255f,
                        1f - (float)transparency
                    },
                        MetallicFactor = 0f,
                        RoughnessFactor = 1f,
                    }
                };
                _gltf.Materials.Add(material);
            }
            else
                throw new Exception(StringLib.NoParentNode);
        }

        public void UpdateNodeGeometry(GLTFVector[] vertices, GLTFVector[] normals, GLTFFace[] faces) {
            //if (_gltf.Nodes.Peek() is glTFNode parent) {
            //    _geoms.Add(
            //        new GLTFGeom {
            //            Vertices = vertices,
            //            Normals = normals,
            //            Faces = faces,
            //            MaterialIndex = _path.MaterialIdx
            //        }
            //    );
            //}
            //else
            //    throw new Exception(StringLib.NoParentNode);
        }

        public void CloseNode() => _gltf.Nodes.Close();

        public void CloseScene() { }
    }
    #endregion

    #region Utils
    internal sealed partial class GLTFBuilder {
        private readonly Logger _logger = new Logger();

        ///// <summary>
        ///// Takes the intermediate geometry data and performs the calculations
        ///// to convert that into glTF buffers, views, and accessors
        ///// </summary>
        //private glTFBinaryData processGeometry(GLTFVector[] vertices, GLTFVector[] normals, GLTFFace[] facets) {
        //    // TODO: rename this type to glTFBufferMeta ?
        //    glTFBinaryData bufferData = new glTFBinaryData();

        //    // store vertex and vertex index (face) data in a buffer
        //    var bufferSegment = new glTFBinaryBufferSegment();
        //    bufferSegment.vertexBuffer =
        //        vertices.Select(x => Convert.ToSingle(x)).ToList();

        //    foreach (var facet in facets) {
        //        bufferSegment.faceVertexIndexBuffer.Add(facet.V1);
        //        bufferSegment.faceVertexIndexBuffer.Add(facet.V2);
        //        bufferSegment.faceVertexIndexBuffer.Add(facet.V3);
        //    }

        //    // prevent buffer duplication by hash checking
        //    string bufferHash = ComputeSHA256Hash(bufferSegment);
        //    ManagerUtils.HashSearch hs = new ManagerUtils.HashSearch(bufferHash);
        //    var match = binaryFileData.Find(hs.EqualTo);

        //    if (match != null) {
        //        // return previously created buffer metadata
        //        bufferData.vertexAccessorIndex = match.vertexAccessorIndex;
        //        bufferData.indexAccessorIndex = match.indexAccessorIndex;
        //        return bufferData;
        //    }
        //    else {
        //        // add a buffer
        //        glTFBuffer buffer = new glTFBuffer();
        //        buffer.uri = name + ".bin";
        //        buffers.Add(buffer);
        //        int bufferIdx = buffers.Count - 1;

        //        /**
        //         * Buffer Data
        //         **/
        //        bufferData.name = buffer.uri;
        //        bufferData.contents = bufferSegment;
        //        // TODO: Uncomment for normals
        //        //foreach (var normal in geomData.normals)
        //        //{
        //        //    bufferData.normalBuffer.Add((float)normal);
        //        //}

        //        // Get max and min for vertex data
        //        float[] vertexMinMax = Util.GetVec3MinMax(bufferSegment.vertexBuffer);
        //        // Get max and min for index data
        //        int[] faceMinMax = Util.GetScalarMinMax(bufferSegment.indexBuffer);
        //        // TODO: Uncomment for normals
        //        // Get max and min for normal data
        //        //float[] normalMinMax = getVec3MinMax(bufferData.normalBuffer);

        //        /**
        //         * BufferViews
        //         **/
        //        // Add a vec3 buffer view
        //        int elementsPerVertex = 3;
        //        int bytesPerElement = 4;
        //        int bytesPerVertex = elementsPerVertex * bytesPerElement;
        //        int numVec3 = (geom.vertices.Count) / elementsPerVertex;
        //        int sizeOfVec3View = numVec3 * bytesPerVertex;
        //        glTFBufferView vec3View = new glTFBufferView();
        //        vec3View.buffer = bufferIdx;
        //        vec3View.byteOffset = 0;
        //        vec3View.byteLength = sizeOfVec3View;
        //        vec3View.target = Targets.ARRAY_BUFFER;
        //        bufferViews.Add(vec3View);
        //        int vec3ViewIdx = bufferViews.Count - 1;

        //        // TODO: Add a normals (vec3) buffer view

        //        // Add a faces / indexes buffer view
        //        int elementsPerIndex = 1;
        //        int bytesPerIndexElement = 4;
        //        int bytesPerIndex = elementsPerIndex * bytesPerIndexElement;
        //        int numIndexes = geom.faces.Count;
        //        int sizeOfIndexView = numIndexes * bytesPerIndex;
        //        glTFBufferView facesView = new glTFBufferView();
        //        facesView.buffer = bufferIdx;
        //        facesView.byteOffset = vec3View.byteLength;
        //        facesView.byteLength = sizeOfIndexView;
        //        facesView.target = Targets.ELEMENT_ARRAY_BUFFER;
        //        bufferViews.Add(facesView);
        //        int facesViewIdx = bufferViews.Count - 1;

        //        buffers[bufferIdx].byteLength = vec3View.byteLength + facesView.byteLength;

        //        /**
        //         * Accessors
        //         **/
        //        // add a position accessor
        //        glTFAccessor positionAccessor = new glTFAccessor();
        //        positionAccessor.bufferView = vec3ViewIdx;
        //        positionAccessor.byteOffset = 0;
        //        positionAccessor.componentType = ComponentType.FLOAT;
        //        positionAccessor.count = geom.vertices.Count / elementsPerVertex;
        //        positionAccessor.type = "VEC3";
        //        positionAccessor.max = new List<float>() { vertexMinMax[1], vertexMinMax[3], vertexMinMax[5] };
        //        positionAccessor.min = new List<float>() { vertexMinMax[0], vertexMinMax[2], vertexMinMax[4] };
        //        accessors.Add(positionAccessor);
        //        bufferData.vertexAccessorIndex = accessors.Count - 1;

        //        // TODO: Uncomment for normals
        //        // add a normals accessor
        //        //glTFAccessor normalsAccessor = new glTFAccessor();
        //        //normalsAccessor.bufferView = vec3ViewIdx;
        //        //normalsAccessor.byteOffset = (positionAccessor.count) * bytesPerVertex;
        //        //normalsAccessor.componentType = ComponentType.FLOAT;
        //        //normalsAccessor.count = geom.data.normals.Count / elementsPerVertex;
        //        //normalsAccessor.type = "VEC3";
        //        //normalsAccessor.max = new List<float>() { normalMinMax[1], normalMinMax[3], normalMinMax[5] };
        //        //normalsAccessor.min = new List<float>() { normalMinMax[0], normalMinMax[2], normalMinMax[4] };
        //        //this.accessors.Add(normalsAccessor);
        //        //bufferData.normalsAccessorIndex = this.accessors.Count - 1;

        //        // add a face accessor
        //        glTFAccessor faceAccessor = new glTFAccessor();
        //        faceAccessor.bufferView = facesViewIdx;
        //        faceAccessor.byteOffset = 0;
        //        faceAccessor.componentType = ComponentType.UNSIGNED_INT;
        //        faceAccessor.count = numIndexes;
        //        faceAccessor.type = "SCALAR";
        //        faceAccessor.max = new List<float>() { faceMinMax[1] };
        //        faceAccessor.min = new List<float>() { faceMinMax[0] };
        //        accessors.Add(faceAccessor);
        //        bufferData.indexAccessorIndex = accessors.Count - 1;

        //        bufferData.hashcode = bufferHash;

        //        return bufferData;
        //    }
        }

        //static public string ComputeSHA256Hash<T>(T data) {
        //    var binFormatter = new BinaryFormatter();
        //    var mStream = new MemoryStream();
        //    binFormatter.Serialize(mStream, data);

        //    using (SHA256 hasher = SHA256.Create()) {
        //        mStream.Position = 0;
        //        byte[] byteHash = hasher.ComputeHash(mStream);

        //        var sBuilder = new StringBuilder();
        //        for (int i = 0; i < byteHash.Length; i++) {
        //            sBuilder.Append(byteHash[i].ToString("x2"));
        //        }

        //        return sBuilder.ToString();
        //    }
        //}
    }
    #endregion

    //internal class GLTFBuilder {

    //    /// <summary>
    //    /// Stateful, uuid indexable list of all materials in the export.
    //    /// </summary>
    //    private IndexedDictionary<glTFMaterial> materialDict = new IndexedDictionary<glTFMaterial>();
    //    /// <summary>
    //    /// Hashable container for mesh data, to aid instancing.
    //    /// </summary>
    //    private List<MeshContainer> meshContainers = new List<MeshContainer>();

    //    /// <summary>
    //    /// Container for the vertex/face/normal information
    //    /// that will be serialized into a binary format
    //    /// for the final *.bin files.
    //    /// </summary>
    //    public List<glTFBinaryData> binaryFileData = new List<glTFBinaryData>();

    //    /// <summary>
    //    /// List of all materials referenced by meshes.
    //    /// </summary>
    //    public List<glTFMaterial> materials {
    //        get {
    //            return materialDict.List;
    //        }
    //    }

    //    /// <summary>
    //    /// Stack maintaining the geometry containers for each
    //    /// node down the current scene graph branch. These are popped
    //    /// as we retreat back up the graph.
    //    /// </summary>
    //    private Stack<Dictionary<string, GeometryData>> geometryStack = new Stack<Dictionary<string, GeometryData>>();

    //    /// <summary>
    //    /// The geometry container for the currently open node.
    //    /// </summary>
    //    private Dictionary<string, GeometryData> currentGeom {
    //        get {
    //            return geometryStack.Peek();
    //        }
    //    }
    //}
