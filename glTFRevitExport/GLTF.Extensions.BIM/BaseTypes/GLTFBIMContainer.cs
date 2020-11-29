using System;
using System.Collections.Generic;

using Newtonsoft.Json;

using Autodesk.Revit.DB;

using GLTFRevitExport.Extensions;

namespace GLTFRevitExport.GLTF.Extensions.BIM.BaseTypes {
    [Serializable]
    abstract class GLTFBIMContainer {
        [JsonProperty("$type")]
        public abstract string Type { get; }

        [JsonProperty("uri")]
        public abstract string Uri { get; }
    }
}
