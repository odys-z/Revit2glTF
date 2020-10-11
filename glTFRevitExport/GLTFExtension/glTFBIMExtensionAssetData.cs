using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Autodesk.Revit.DB;

using GLTFRevitExport.Extensions;
using GLTFRevitExport.GLTF.Types;
using GLTFRevitExport.GLTF;
using GLTFRevitExport.Properties;

namespace GLTFRevitExport.GLTFExtension {
    [Serializable]
    public class glTFBIMExtensionAssetData : glTFBIMExtension {
        internal glTFBIMExtensionAssetData(Document d) : base() {
            App = "revit";
            Title = d.Title;
            Source = d.PathName;
        }

        public override string Type => "model";

        [JsonProperty("application")]
        public string App { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }
    }
}
