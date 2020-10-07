using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using Autodesk.Revit.DB;

using GLTFRevitExport.GLTFTypes;
using GLTFRevitExport.Properties;
using Newtonsoft.Json.Bson;

namespace GLTFRevitExport {
    #region Initialization
    internal partial class GLTFBuilder {

    }
    #endregion

    #region Data stacks
    internal partial class GLTFBuilder {
        private GLTFBuilderScenePath _path = null;

        private List<glTFScene> _scenes = new List<glTFScene>();
        private int _currentSceneIndex => _scenes.Count() - 1;
        private glTFScene _scene => _scenes[_currentSceneIndex];

        private List<glTFNode> _nodes = new List<glTFNode>();
        private int _currentNodeIndex => _nodes.Count() - 1;
        private glTFNode _node => _nodes[_currentNodeIndex];

        private List<glTFMaterial> _materials = new List<glTFMaterial>();
        private int _currentMaterialIndex => _nodes.Count() - 1;
        private glTFMaterial _material => _materials[_currentMaterialIndex];

        private List<GLTFGeom> _geoms = new List<GLTFGeom>();
    }

    internal class GLTFBuilderScenePath {
        // scene -> node.>.>.node -> material -> geometry
        internal GLTFBuilderScenePath(int sceneIdx) => SceneIdx = sceneIdx;

        internal int SceneIdx { get; private set; } = -1;

        private Stack<int> _nodes = new Stack<int>();
        internal void PushNodeIdx(int node) => _nodes.Push(node);
        internal void PopNodeIdx() {
            MaterialIdx = -1;
            if (_nodes.Count > 0)
                _nodes.Pop();
        }
        internal int? PeekNodeIdx() {
            if (_nodes.Count > 0)
                return _nodes.Peek();
            return null;
        }

        internal int MaterialIdx { get; set; } = -1;
    }
    #endregion

    #region Builders
    internal partial class GLTFBuilder {
        public void OpenScene() {
            var scene = new glTFScene();
            _scenes.Add(scene);
            _path = new GLTFBuilderScenePath(_currentSceneIndex);
        }

        public void OpenNode(string name, double[] matrix) {
            if (_path is GLTFBuilderScenePath path) {
                // create new node and set base properties
                var node = new glTFNode() {
                    name = name ?? "undefined",
                    matrix = matrix?.ToList()
                };

                // add to container node data
                _nodes.Add(node);

                // if a parent node exists, add to the node children nodes
                if (path.PeekNodeIdx() is int parentNodeIdx) {
                    var parent = _nodes[parentNodeIdx];
                    parent.children.Add(_currentNodeIndex);
                }
                // otherwise add to the scene nodes
                else {
                    var scene = _scenes[path.SceneIdx];
                    scene.nodes.Add(_currentNodeIndex);
                }

                // now set the new node as parent since we are
                // opening a new node
                path.PushNodeIdx(_currentNodeIndex);
            }
            else
                throw new Exception(StringLib.NoParentScene);
        }

        public void UpdateNodeMatrix(double[] matrix) {
            if (_path.PeekNodeIdx() is int currentNodeIdx) {
                var currentNode = _nodes[currentNodeIdx];
                currentNode.matrix = matrix?.ToList();
            }
            else
                throw new Exception(StringLib.NoParentNode);
        }

        public void UpdateNodeMaterial(string name, Color color = null, double transparency = 0.0) {
            if (_path.PeekNodeIdx() is int) {
                // TODO: add this to materials or use existing
                var material = new glTFMaterial() {
                    name = name,
                    pbrMetallicRoughness = new glTFPBR() {
                        baseColorFactor = new List<float>() {
                        color.Red / 255f,
                        color.Green / 255f,
                        color.Blue / 255f,
                        1f - (float)transparency
                    },
                        metallicFactor = 0f,
                        roughnessFactor = 1f,
                    }
                };
                _materials.Add(material);
                _path.MaterialIdx = _currentMaterialIndex;
            }
            else
                throw new Exception(StringLib.NoParentNode);
        }

        public void UpdateNodeGeometry(GLTFVector[] vertices, GLTFVector[] normals, GLTFFace[] faces) {
            if (_path.PeekNodeIdx() is int) {
                _geoms.Add(
                    new GLTFGeom {
                        Vertices = vertices,
                        Normals = normals,
                        Faces = faces,
                        MaterialIndex = _path.MaterialIdx
                    }
                );
            }
            else
                throw new Exception(StringLib.NoParentNode);
        }

        public void CloseNode() {
            if (_path.PeekNodeIdx() is int)
                _path.PopNodeIdx();
            else
                throw new Exception(StringLib.NoParentNode);
        }
    }
    #endregion

    #region Intermediate containers
    /// <summary>
    /// An integer-based 3D point class
    /// </summary>
    // From Jeremy Tammik's RvtVa3c exporter:
    // https://github.com/va3c/RvtVa3c
    public class GLTFVector : IComparable<GLTFVector> {
        public long X { get; set; }
        public long Y { get; set; }
        public long Z { get; set; }

        public GLTFVector(long x, long y, long z) {
            X = x;
            Y = y;
            Z = z;

            //if (switch_coordinates) {
            //    X = -X;
            //    long tmp = Y;
            //    Y = Z;
            //    Z = tmp;
            //}
        }

        public int CompareTo(GLTFVector a) {
            long d = X - a.X;
            if (0 == d) {
                d = Y - a.Y;
                if (0 == d) {
                    d = Z - a.Z;
                }
            }
            return (0 == d) ? 0 : ((0 < d) ? 1 : -1);
        }
    }

    public class GLTFFace {
        public int V1 { get; set; }
        public int V2 { get; set; }
        public int V3 { get; set; }

        public GLTFFace(int v1, int v2, int v3) {

        }
    }
    
    public class GLTFGeom {
        public GLTFVector[] Vertices;
        public GLTFVector[] Normals;
        public GLTFFace[] Faces;
        public int MaterialIndex;
    }

    #endregion

    #region Utils
    internal partial class GLTFBuilder {
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
        //}

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
    //    /// List of all buffers referencing the binary file data.
    //    /// </summary>
    //    public List<glTFBuffer> buffers = new List<glTFBuffer>();
    //    /// <summary>
    //    /// List of all BufferViews referencing the buffers.
    //    /// </summary>
    //    public List<glTFBufferView> bufferViews = new List<glTFBufferView>();
    //    /// <summary>
    //    /// List of all Accessors referencing the BufferViews.
    //    /// </summary>
    //    public List<glTFAccessor> accessors = new List<glTFAccessor>();
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
    //    /// List of all meshes referenced by nodes.
    //    /// </summary>
    //    public List<glTFMesh> meshes {
    //        get {
    //            return meshContainers.Select(x => x.contents).ToList();
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

    //    public glTFContainer Finish() {
    //        glTF model = new glTF();
    //        model.asset = new glTFAsset();
    //        model.scenes = _scenes;
    //        model.nodes = nodes;
    //        model.meshes = meshes;
    //        model.materials = materials;
    //        model.buffers = buffers;
    //        model.bufferViews = bufferViews;
    //        model.accessors = accessors;

    //        glTFContainer container = new glTFContainer();
    //        container.glTF = model;
    //        container.binaries = binaryFileData;

    //        return container;
    //    }
    //}
}
