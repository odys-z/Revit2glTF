using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLTFRevitExport.GLTF.Types {
    /// <summary>
    /// The list of accessors available to the renderer for a particular mesh.
    /// </summary>
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#meshes
    internal class glTFAttribute {

        /// <summary>
        /// The index of the accessor for position data.
        /// </summary>
        public uint POSITION { get; set; }
        //public int NORMAL { get; set; }
    }
}
