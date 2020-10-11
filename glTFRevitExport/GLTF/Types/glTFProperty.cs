using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using GLTFRevitExport.Properties;

namespace GLTFRevitExport.GLTF.Types {
    [Serializable]
    public abstract class glTFProperty {
        [JsonProperty("extensions")]
        public Dictionary<string, glTFExtension> Extensions { get; set; }

        [JsonProperty("extras")]
        public glTFExtras Extras { get; set; }
    }
}
