using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLTFRevitExport.GLTF.Types {
    /// <summary>
    /// The array of primitives defining the mesh of an object.
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#meshes
    public class glTFMesh {
        public List<glTFMeshPrimitive> primitives { get; set; }
    }

    /// <summary>
    /// Properties defining where the GPU should look to find the mesh and material data.
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#meshes
    public class glTFMeshPrimitive {
        public glTFAttribute attributes { get; set; }
        public uint indices { get; set; }
        public uint material { get; set; }
        public glTFMeshMode mode { get; set; } = glTFMeshMode.TRIANGLES;
    }

    /// <summary>
    /// glTF Mesh 
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#primitivemode
    public enum glTFMeshMode {
        POINTS,
        LINES,
        LINE_LOOP,
        LINE_STRIP,
        TRIANGLES,
        TRIANGLE_STRIP,
        TRIANGLE_FAN
    }
}
