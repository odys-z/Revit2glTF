using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;

using GLTFRevitExport.GLTF.Types;
using GLTFRevitExport.Properties;

namespace GLTFRevitExport.GLTF {
    #region Initialization, Completion
    internal sealed partial class GLTFBuilder {
        internal GLTFBuilder() {
            _gltf = new glTF();
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

        public void SetAsset(string generatorId, string copyright,
                             glTFExtension[] exts, glTFExtras extras) {
            var assetExts = new Dictionary<string, glTFExtension>();
            if (exts != null) {
                foreach (var ext in exts)
                    if (ext != null) {
                        assetExts.Add(ext.Name, ext);
                        ensureExtensionUsed(ext);
                    }
            }

            _gltf.Asset = new glTFAsset {
                Generator = generatorId,
                Copyright = copyright,
                Extensions = assetExts.Count > 0 ? assetExts : null,
                Extras = extras
            };
        }
        
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

        public glTFNode GetActiveNode() => _gltf.Nodes.Peek();

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

                foreach (var childIdx in currentNode.Children) {
                    var node = _gltf.Nodes[childIdx];
                    if (filter(node))
                        return (int)childIdx;
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

        public uint NewMaterial(string name, float[] color, double transparency, glTFExtension[] exts) {
            var material = new glTFMaterial() {
                Name = name,
                PBRMetallicRoughness = new glTFPBRMetallicRoughness() {
                    BaseColorFactor = color,
                    MetallicFactor = 0f,
                    RoughnessFactor = 1f,
                },
                Extensions = exts?.ToDictionary(x => x.Name, x => x),
            };
            
            var matchingMatIdx = _gltf.Materials.IndexOf(material);
            if (matchingMatIdx >= 0)
                return (uint)matchingMatIdx;
            else {
                _gltf.Materials.Add(material);
                return (uint)_gltf.Materials.Count - 1;
            }
        }

        public uint NewMesh(double[] vertices, double[] normals, uint[] faces, int material = -1) {
            _gltf.Meshes.Add(
                new glTFMesh()
                );
            var index = (uint)_gltf.Meshes.Count - 1;
            if (_gltf.Nodes.Peek() is glTFNode activeNode)
                activeNode.Mesh = index;
            return index;
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


//class Util
//{
//    public static int[] GetVec3MinMax(List<int> vec3)
//    {
//        int minVertexX = int.MaxValue;
//        int minVertexY = int.MaxValue;
//        int minVertexZ = int.MaxValue;
//        int maxVertexX = int.MinValue;
//        int maxVertexY = int.MinValue;
//        int maxVertexZ = int.MinValue;
//        for (int i = 0; i < vec3.Count; i += 3)
//        {
//            if (vec3[i] < minVertexX) minVertexX = vec3[i];
//            if (vec3[i] > maxVertexX) maxVertexX = vec3[i];

//            if (vec3[i + 1] < minVertexY) minVertexY = vec3[i + 1];
//            if (vec3[i + 1] > maxVertexY) maxVertexY = vec3[i + 1];

//            if (vec3[i + 2] < minVertexZ) minVertexZ = vec3[i + 2];
//            if (vec3[i + 2] > maxVertexZ) maxVertexZ = vec3[i + 2];
//        }
//        return new int[] { minVertexX, maxVertexX, minVertexY, maxVertexY, minVertexZ, maxVertexZ };
//    }

//    public static long[] GetVec3MinMax(List<long> vec3)
//    {
//        long minVertexX = long.MaxValue;
//        long minVertexY = long.MaxValue;
//        long minVertexZ = long.MaxValue;
//        long maxVertexX = long.MinValue;
//        long maxVertexY = long.MinValue;
//        long maxVertexZ = long.MinValue;
//        for (int i = 0; i < (vec3.Count / 3); i += 3)
//        {
//            if (vec3[i] < minVertexX) minVertexX = vec3[i];
//            if (vec3[i] > maxVertexX) maxVertexX = vec3[i];

//            if (vec3[i + 1] < minVertexY) minVertexY = vec3[i + 1];
//            if (vec3[i + 1] > maxVertexY) maxVertexY = vec3[i + 1];

//            if (vec3[i + 2] < minVertexZ) minVertexZ = vec3[i + 2];
//            if (vec3[i + 2] > maxVertexZ) maxVertexZ = vec3[i + 2];
//        }
//        return new long[] { minVertexX, maxVertexX, minVertexY, maxVertexY, minVertexZ, maxVertexZ };
//    }

//    public static float[] GetVec3MinMax(List<float> vec3)
//    {

//        List<float> xValues = new List<float>();
//        List<float> yValues = new List<float>();
//        List<float> zValues = new List<float>();
//        for (int i = 0; i < vec3.Count; i++)
//        {
//            if ((i % 3) == 0) xValues.Add(vec3[i]);
//            if ((i % 3) == 1) yValues.Add(vec3[i]);
//            if ((i % 3) == 2) zValues.Add(vec3[i]);
//        }

//        float maxX = xValues.Max();
//        float minX = xValues.Min();
//        float maxY = yValues.Max();
//        float minY = yValues.Min();
//        float maxZ = zValues.Max();
//        float minZ = zValues.Min();

//        return new float[] { minX, maxX, minY, maxY, minZ, maxZ };
//    }

//    public static int[] GetScalarMinMax(List<int> scalars)
//    {
//        int minFaceIndex = int.MaxValue;
//        int maxFaceIndex = int.MinValue;
//        for (int i = 0; i < scalars.Count; i++)
//        {
//            int currentMin = Math.Min(minFaceIndex, scalars[i]);
//            if (currentMin < minFaceIndex) minFaceIndex = currentMin;

//            int currentMax = Math.Max(maxFaceIndex, scalars[i]);
//            if (currentMax > maxFaceIndex) maxFaceIndex = currentMax;
//        }
//        return new int[] { minFaceIndex, maxFaceIndex };
//    }

//    /// <summary>
//    /// From Jeremy Tammik's RvtVa3c exporter:
//    /// https://github.com/va3c/RvtVa3c
//    /// Return a string for an XYZ point
//    /// or vector with its coordinates
//    /// formatted to two decimal places.
//    /// </summary>
//    public static string PointString(XYZ p)
//    {
//        return string.Format("({0},{1},{2})",
//          RealString(p.X),
//          RealString(p.Y),
//          RealString(p.Z));
//    }

//    /// <summary>
//    /// From Jeremy Tammik's RvtVa3c exporter:
//    /// https://github.com/va3c/RvtVa3c
//    /// Extract a true or false value from the given
//    /// string, accepting yes/no, Y/N, true/false, T/F
//    /// and 1/0. We are extremely tolerant, i.e., any
//    /// value starting with one of the characters y, n,
//    /// t or f is also accepted. Return false if no 
//    /// valid Boolean value can be extracted.
//    /// </summary>
//    public static bool GetTrueOrFalse(string s, out bool val)
//    {
//        val = false;

//        if (s.Equals(Boolean.TrueString,
//          StringComparison.OrdinalIgnoreCase))
//        {
//            val = true;
//            return true;
//        }
//        if (s.Equals(Boolean.FalseString,
//          StringComparison.OrdinalIgnoreCase))
//        {
//            return true;
//        }
//        if (s.Equals("1"))
//        {
//            val = true;
//            return true;
//        }
//        if (s.Equals("0"))
//        {
//            return true;
//        }
//        s = s.ToLower();

//        if ('t' == s[0] || 'y' == s[0])
//        {
//            val = true;
//            return true;
//        }
//        if ('f' == s[0] || 'n' == s[0])
//        {
//            return true;
//        }
//        return false;
//    }

//    /// <summary>
//    /// From Jeremy Tammik's RvtVa3c exporter:
//    /// https://github.com/va3c/RvtVa3c
//    /// Return a string describing the given element:
//    /// .NET type name,
//    /// category name,
//    /// family and symbol name for a family instance,
//    /// element id and element name.
//    /// </summary>
//    public static string ElementDescription(Element e)
//    {
//        if (null == e)
//        {
//            return "<null>";
//        }

//        // For a wall, the element name equals the
//        // wall type name, which is equivalent to the
//        // family name ...

//        FamilyInstance fi = e as FamilyInstance;

//        string typeName = e.GetType().Name;

//        string categoryName = (null == e.Category)
//          ? string.Empty
//          : e.Category.Name + " ";

//        string familyName = (null == fi)
//          ? string.Empty
//          : fi.Symbol.Family.Name + " ";

//        string symbolName = (null == fi
//          || e.Name.Equals(fi.Symbol.Name))
//            ? string.Empty
//            : fi.Symbol.Name + " ";

//        return string.Format("{0} {1}{2}{3}<{4} {5}>",
//          typeName, categoryName, familyName,
//          symbolName, e.Id.IntegerValue, e.Name);
//    }
//}

//static class ManagerUtils {

//    public class HashSearch {
//        string _S;
//        public HashSearch(string s) {
//            _S = s;
//        }
//        public bool EqualTo(HashedType d) {
//            return d.hashcode.Equals(_S);
//        }
//    }
//}
