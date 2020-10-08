using System;
using System.Collections.Generic;

namespace GLTFRevitExport.GLTF.Types {
    /// <summary>
    /// Magic numbers to differentiate scalar and vector 
    /// array buffers.
    /// https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#buffers-and-buffer-views
    /// </summary>
    public enum Targets {
        ARRAY_BUFFER = 34962, // signals vertex data
        ELEMENT_ARRAY_BUFFER = 34963 // signals index or face data
    }

    /// <summary>
    /// Magic numbers to differentiate array buffer component
    /// types.
    /// https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#accessor-element-size
    /// </summary>
    public enum ComponentType {
        BYTE = 5120,
        UNSIGNED_BYTE = 5121,
        SHORT = 5122,
        UNSIGNED_SHORT = 5123,
        UNSIGNED_INT = 5125,
        FLOAT = 5126
    }

    [Serializable]
    public class glTFBinaryBufferSegment {
        public List<float> vertexBuffer { get; set; } = new List<float>();
        public List<int> faceVertexIndexBuffer { get; set; } = new List<int>();
    
    }


    public class HashedType {
        public string hashcode { get; set; }
    }

    public class MeshContainer : HashedType {
        //public string hashcode { get; set; }
        public glTFMesh contents { get; set; }
    }


    public class GridParameters {
        public List<double> origin { get; set; }
        public List<double> direction { get; set; }
        public double length { get; set; }
    }

    //public class glTFFunctions
    //{
    //    public static glTFBinaryData getMeshData(glTFNode node, glTF gltf)
    //    {
    //        if(node.mesh.HasValue)
    //        {
    //            glTFMesh mesh = gltf.meshes[node.mesh.Value];
    //            mesh.
    //        }
    //    }
    //}
}
