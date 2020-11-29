using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace GLTFRevitExport.GLTF.Schema {
    /// <summary>
    /// A reference to the location and size of binary data.
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#buffers-and-buffer-views
    [Serializable]
    class glTFBuffer {
        /// <summary>
        /// The uri of the buffer.
        /// </summary>
        [JsonProperty("uri")]
        public string Uri { get; set; }

        /// <summary>
        /// The total byte length of the buffer.
        /// </summary>
        [JsonProperty("byteLength")]
        public uint ByteLength { get; set; }
    }
}
