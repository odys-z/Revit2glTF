using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLTFRevitExport.Containers {
    internal class GLTFFace {
        public uint V1 { get; set; }
        public uint V2 { get; set; }
        public uint V3 { get; set; }

        public GLTFFace(int v1, int v2, int v3) {

        }

        public static GLTFFace operator +(GLTFFace left, uint shift) {
            left.V1 += shift;
            left.V2 += shift;
            left.V3 += shift;
            return left;
        }
    }
}
