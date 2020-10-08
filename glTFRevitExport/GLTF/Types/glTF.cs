using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace GLTFRevitExport.GLTF.Types {
    /// <summary>
    /// The json serializable glTF file format.
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0
    [Serializable]
    public class glTF {
        public glTF(string genertor = null, string copyright = null) {
            Asset = new glTFAsset {
                Generator = genertor,
                Copyright = copyright
            };
        }

        [JsonProperty("asset")]
        public glTFAsset Asset = null;

        [JsonProperty("scenes")]
        public List<glTFScene> Scenes { get; set; } = new List<glTFScene>();

        [JsonProperty("scene")]
        public glTFScene StartingScene { get; set; } = null;

        [JsonProperty("nodes")]
        public glTFNodes Nodes { get; set; } = new glTFNodes();

        [JsonProperty("meshes")]
        public List<glTFMesh> Meshes { get; set; } = new List<glTFMesh>();

        [JsonProperty("buffers")]
        public List<glTFBuffer> Buffers { get; set; } = new List<glTFBuffer>();

        [JsonProperty("bufferViews")]
        public List<glTFBufferView> BufferViews { get; set; } = new List<glTFBufferView>();

        [JsonProperty("accessors")]
        public List<glTFAccessor> Accessors { get; set; } = new List<glTFAccessor>();

        [JsonProperty("materials")]
        public List<glTFMaterial> Materials { get; set; } = new List<glTFMaterial>();
    }
}
