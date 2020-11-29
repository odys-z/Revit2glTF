using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace GLTFRevitExport.GLTF.Schema {
    /// <summary>
    /// The nodes defining individual (or nested) elements in the scene
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#nodes-and-hierarchy
    [Serializable]
    class glTFNode: glTFProperty {
        /// <summary>
        /// The user-defined name of this object
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The index of the mesh in this node.
        /// </summary>
        [JsonProperty("mesh")]
        public uint? Mesh { get; set; } = null;

        /// <summary>
        /// A floating-point 4x4 transformation matrix stored in column major order.
        /// </summary>
        [JsonProperty("matrix")]
        public float[] Matrix { get; set; }

        /// <summary>
        /// The indices of this node's children.
        /// </summary>
        [JsonProperty("children")]
        public HashSet<uint> Children { get; set; }
    }
}
