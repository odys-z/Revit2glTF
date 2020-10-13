using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace GLTFRevitExport.GLTF.Schema {
    /// <summary>
    /// The array of primitives defining the mesh of an object.
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#meshes
    internal class glTFMesh : glTFProperty {

        [JsonProperty("primitives")]
        public List<glTFMeshPrimitive> Primitives { get; set; }
    }
}
