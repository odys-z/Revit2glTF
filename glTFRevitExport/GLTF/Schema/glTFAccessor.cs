using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace GLTFRevitExport.GLTF.Schema {
    /// <summary>
    /// A reference to a subsection of a BufferView containing a particular data type.
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#accessors
    [Serializable]
    internal class glTFAccessor : glTFProperty {
        /// <summary>
        /// The index of the bufferView.
        /// </summary>
        [JsonProperty("bufferView")]
        public uint BufferView { get; set; }

        /// <summary>
        /// The offset relative to the start of the bufferView in bytes.
        /// </summary>
        [JsonProperty("byteOffset")]
        public uint ByteOffset { get; set; }

        /// <summary>
        /// the datatype of the components in the attribute
        /// </summary>
        [JsonProperty("componentType")]
        public ComponentType ComponentType { get; set; }

        /// <summary>
        /// The number of attributes referenced by this accessor.
        /// </summary>
        [JsonProperty("count")]
        public uint Count { get; set; }

        /// <summary>
        /// Specifies if the attribute is a scalar, vector, or matrix
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Maximum value of each component in this attribute.
        /// </summary>
        [JsonProperty("max")]
        public object[] Max { get; set; }

        /// <summary>
        /// Minimum value of each component in this attribute.
        /// </summary>
        [JsonProperty("min")]
        public object[] Min { get; set; }
    }
}
