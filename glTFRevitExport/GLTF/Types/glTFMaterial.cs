using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace GLTFRevitExport.GLTF.Types {
    /// <summary>
    /// The glTF PBR Material format
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#materials
    [Serializable]
    internal class glTFMaterial {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("pbrMetallicRoughness")]
        public glTFPBRMetallicRoughness PBRMetallicRoughness { get; set; }
    }

    /// <summary>
    /// glTF PBR Material Metallic Roughness
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#materials
    [Serializable]
    internal class glTFPBRMetallicRoughness {
        [JsonProperty("baseColorFactor")]
        public List<float> BaseColorFactor { get; set; }

        [JsonProperty("metallicFactor")]
        public float MetallicFactor { get; set; }

        [JsonProperty("roughnessFactor")]
        public float RoughnessFactor { get; set; }
    }
}
