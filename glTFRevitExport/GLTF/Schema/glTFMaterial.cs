using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GLTFRevitExport.GLTF.Schema {
    /// <summary>
    /// The glTF PBR Material format
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#materials
    [Serializable]
    internal class glTFMaterial : glTFProperty {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("pbrMetallicRoughness")]
        public glTFPBRMetallicRoughness PBRMetallicRoughness { get; set; }

        [JsonProperty("alphaMode")]
        [JsonConverter(typeof(StringEnumConverter))]
        public glTFAlphaMode AlphaMode { get; set; } = glTFAlphaMode.BLEND;

        // TODO: override
        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public override bool Equals(object obj) {
            if (obj is glTFMaterial other)
                return Name == other.Name
                    && PBRMetallicRoughness.Equals(other.PBRMetallicRoughness);
            return false;
        }
    }

    /// <summary>
    /// glTF PBR Material Metallic Roughness
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#materials
    [Serializable]
    internal class glTFPBRMetallicRoughness {
        [JsonProperty("baseColorFactor")]
        public float[] BaseColorFactor { get; set; }

        [JsonProperty("metallicFactor")]
        public float MetallicFactor { get; set; }

        [JsonProperty("roughnessFactor")]
        public float RoughnessFactor { get; set; }
    }
}
