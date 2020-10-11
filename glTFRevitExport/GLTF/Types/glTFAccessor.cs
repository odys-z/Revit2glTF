using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLTFRevitExport.GLTF.Types {
    /// <summary>
    /// A reference to a subsection of a BufferView containing a particular data type.
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#accessors
    internal class glTFAccessor {
        /// <summary>
        /// The index of the bufferView.
        /// </summary>
        public uint bufferView { get; set; }

        /// <summary>
        /// The offset relative to the start of the bufferView in bytes.
        /// </summary>
        public uint byteOffset { get; set; }

        /// <summary>
        /// the datatype of the components in the attribute
        /// </summary>
        public ComponentType componentType { get; set; }

        /// <summary>
        /// The number of attributes referenced by this accessor.
        /// </summary>
        public uint count { get; set; }

        /// <summary>
        /// Specifies if the attribute is a scalar, vector, or matrix
        /// </summary>
        public string type { get; set; }

        /// <summary>
        /// Maximum value of each component in this attribute.
        /// </summary>
        public List<float> max { get; set; }

        /// <summary>
        /// Minimum value of each component in this attribute.
        /// </summary>
        public List<float> min { get; set; }

        /// <summary>
        /// A user defined name for this accessor.
        /// </summary>
        public string name { get; set; }
    }
}
