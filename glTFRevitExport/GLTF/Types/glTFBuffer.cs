using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLTFRevitExport.GLTF.Types {
    /// <summary>
    /// A reference to the location and size of binary data.
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#buffers-and-buffer-views
    internal class glTFBuffer {
        /// <summary>
        /// The uri of the buffer.
        /// </summary>
        public string uri { get; set; }

        /// <summary>
        /// The total byte length of the buffer.
        /// </summary>
        public uint byteLength { get; set; }
    }
}
