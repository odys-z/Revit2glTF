using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


using GLTFRevitExport.GLTF.Schema;

namespace GLTFRevitExport.GLTF.Containers {
    public class GLTFContainer {
        public string Model;
        public string Properties;
        public List<byte[]> Binaries;
    }
}
