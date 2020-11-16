using System;
using System.Collections.Generic;

using Newtonsoft.Json;

using Autodesk.Revit.DB;

using GLTFRevitExport.Extensions;

namespace GLTFRevitExport.GLTF.Extensions.BIM {
    [Serializable]
#pragma warning disable IDE1006 // Naming Styles
    internal abstract class glTFBIMContainer {
#pragma warning restore IDE1006 // Naming Styles

        [JsonProperty("$type")]
        public abstract string Type { get; }

        [JsonProperty("uri")]
        public abstract string Uri { get; }
    }
}
