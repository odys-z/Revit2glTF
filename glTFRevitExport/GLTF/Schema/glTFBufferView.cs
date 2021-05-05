using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace GLTFRevitExport.GLTF.Schema {
    /// <summary>
    /// A reference to a subsection of a buffer containing either vector or scalar data.
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#buffers-and-buffer-views
    [Serializable]
    class glTFBufferView {
        /// <summary>
        /// The index of the buffer.
        /// </summary>
        [JsonProperty("buffer")]
        public uint Buffer { get; set; }

        /// <summary>
        /// The offset into the buffer in bytes.
        /// </summary>
        [JsonProperty("byteOffset")]
        public uint ByteOffset { get; set; }

        /// <summary>
        /// The length of the bufferView in bytes.
        /// </summary>
        [JsonProperty("byteLength")]
        public uint ByteLength { get; set; }

        /// <summary>
        /// The target that the GPU buffer should be bound to.
        /// </summary>
        [JsonProperty("target")]
        public glTFBufferViewTargets Target { get; set; }
    }
}
