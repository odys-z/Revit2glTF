using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLTFRevitExport.GLTF.Schema {
    /// <summary>
    /// glTF Mesh Modes
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#primitivemode
    enum glTFMeshMode {
        POINTS,
        LINES,
        LINE_LOOP,
        LINE_STRIP,
        TRIANGLES,
        TRIANGLE_STRIP,
        TRIANGLE_FAN
    }

    /// <summary>
    /// Accessor data type identifier
    /// </summary>
    enum glTFAccessorType {
        SCALAR,
        VEC2,
        VEC3,
        VEC4,
        MAT2,
        MAT3,
        MAT4,
    }

    /// <summary>
    /// Magic numbers to differentiate array buffer component
    /// types.
    /// https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#accessor-element-size
    /// </summary>
    enum glTFAccessorComponentType {
        BYTE = 5120,
        UNSIGNED_BYTE = 5121,
        SHORT = 5122,
        UNSIGNED_SHORT = 5123,
        UNSIGNED_INT = 5125,
        FLOAT = 5126
    }

    /// <summary>
    /// Magic numbers to differentiate scalar and vector 
    /// array buffers.
    /// https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#buffers-and-buffer-views
    /// </summary>
    enum glTFBufferViewTargets {
        ARRAY_BUFFER = 34962, // signals vertex data
        ELEMENT_ARRAY_BUFFER = 34963 // signals index or face data
    }

    /// <summary>
    /// Material alpha mode
    /// </summary>
    enum glTFAlphaMode {
        OPAQUE,
        MASK,
        BLEND
    }
}
