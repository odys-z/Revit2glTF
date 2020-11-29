using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace GLTFRevitExport.GLTF.Schema {
    /// <summary>
    /// The scenes available to render
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#scenes
    [Serializable]
    class glTFScene : glTFProperty {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("nodes")]
        public HashSet<uint> Nodes { get; set; } = new HashSet<uint>();
    }
}
