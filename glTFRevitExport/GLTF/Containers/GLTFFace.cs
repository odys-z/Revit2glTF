using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLTFRevitExport.GLTF.Containers {
    internal class GLTFFace {
        public ushort V1 { get; set; }
        public ushort V2 { get; set; }
        public ushort V3 { get; set; }

        public GLTFFace(ushort v1, ushort v2, ushort v3) {
            V1 = v1;
            V2 = v2;
            V3 = v3;
        }

        public static GLTFFace operator +(GLTFFace left, ushort shift) {
            left.V1 += shift;
            left.V2 += shift;
            left.V3 += shift;
            return left;
        }
    }
}
