using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLTFRevitExport.GLTF.Schema {
    /// <summary>
    /// Magic numbers to differentiate scalar and vector 
    /// array buffers.
    /// https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#buffers-and-buffer-views
    /// </summary>
    internal enum Targets {
        ARRAY_BUFFER = 34962, // signals vertex data
        ELEMENT_ARRAY_BUFFER = 34963 // signals index or face data
    }

    /// <summary>
    /// Magic numbers to differentiate array buffer component
    /// types.
    /// https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#accessor-element-size
    /// </summary>
    internal enum ComponentType {
        BYTE = 5120,
        UNSIGNED_BYTE = 5121,
        SHORT = 5122,
        UNSIGNED_SHORT = 5123,
        UNSIGNED_INT = 5125,
        FLOAT = 5126
    }

    /// <summary>
    /// glTF Mesh Modes
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#primitivemode
    internal enum glTFMeshMode {
        POINTS,
        LINES,
        LINE_LOOP,
        LINE_STRIP,
        TRIANGLES,
        TRIANGLE_STRIP,
        TRIANGLE_FAN
    }
}
