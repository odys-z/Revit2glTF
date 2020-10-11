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
    internal abstract class glTFBIMExtension: glTFExtension {
        internal glTFBIMExtension() { }

        internal override string Name => StringLib.GLTFExtensionName;

        [JsonProperty("$type")]
        public abstract string Type { get; } 
    }
}
