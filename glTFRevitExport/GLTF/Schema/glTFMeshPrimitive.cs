using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace GLTFRevitExport.GLTF.Schema {
    /// <summary>
    /// Properties defining where the GPU should look to find the mesh and material data.
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#meshes
    class glTFMeshPrimitive : glTFProperty {

        [JsonProperty("attributes")]
        public glTFAttributes Attributes { get; set; }

        [JsonProperty("indices")]
        public uint Indices { get; set; }

        [JsonProperty("material")]
        public uint? Material { get; set; } = null;

        [JsonProperty("mode")]
        public glTFMeshMode Mode { get; set; } = glTFMeshMode.TRIANGLES;

        public override bool Equals(object obj) {
            if (obj is glTFMeshPrimitive other)
                return Attributes.Equals(other.Attributes)
                        && Indices.Equals(other.Indices)
                        && Material.Equals(other.Material)
                        && Mode.Equals(other.Mode);
            return false;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
