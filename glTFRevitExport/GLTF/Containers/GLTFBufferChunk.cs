using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLTFRevitExport.GLTF.Containers {
    internal abstract class GLTFBufferChunk {

    }

    internal class GLTFBufferChunk<T> : GLTFBufferChunk {
        
        public GLTFBufferChunk(T[] buffer) {

        }
    }
}
