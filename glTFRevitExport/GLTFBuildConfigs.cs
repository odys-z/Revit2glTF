using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLTFRevitExport {
    public class GLTFBuildConfigs {
        /// <summary>
        /// Export all buffers into a single binary file
        /// </summary>
        public bool UseSingleBinary { get; set; } = true;
    }
}
