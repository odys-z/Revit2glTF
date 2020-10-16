using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace GLTFRevitExport.GLTF.Schema {
    /// <summary>
    /// The list of accessors available to the renderer for a particular mesh.
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#meshes
    internal class glTFAttributes {

        /// <summary>
        /// The index of the accessor for position data
        /// </summary>
        [JsonProperty("POSITION")]
        public uint? Position { get; set; }

        /// <summary>
        /// The index of the accessor for normal data
        /// </summary>
        [JsonProperty("NORMAL")]
        public uint? Normal { get; set; }

        public override bool Equals(object obj) {
            if (obj is glTFAttributes other)
                return Position.Equals(other.Position) && Normal.Equals(other.Normal);
            return false;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
