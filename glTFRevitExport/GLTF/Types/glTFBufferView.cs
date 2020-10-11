using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLTFRevitExport.GLTF.Types {
    /// <summary>
    /// A reference to a subsection of a buffer containing either vector or scalar data.
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#buffers-and-buffer-views
    internal class glTFBufferView {
        /// <summary>
        /// The index of the buffer.
        /// </summary>
        public uint buffer { get; set; }

        /// <summary>
        /// The offset into the buffer in bytes.
        /// </summary>
        public uint byteOffset { get; set; }

        /// <summary>
        /// The length of the bufferView in bytes.
        /// </summary>
        public uint byteLength { get; set; }

        /// <summary>
        /// The target that the GPU buffer should be bound to.
        /// </summary>
        public Targets target { get; set; }

        /// <summary>
        /// A user defined name for this view.
        /// </summary>
        public string name { get; set; }
    }
}
