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
    internal class glTFMeshPrimitive :glTFProperty {

        [JsonProperty("attributes")]
        public glTFAttributes Attributes { get; set; }

        [JsonProperty("indices")]
        public uint Indices { get; set; }

        [JsonProperty("material")]
        public uint Material { get; set; }

        [JsonProperty("mode")]
        public glTFMeshMode Mode { get; set; } = glTFMeshMode.TRIANGLES;
    }
}
