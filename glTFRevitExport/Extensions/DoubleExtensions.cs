using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLTFRevitExport.Extensions {
    internal static class DoubleExtensions {
        /// <summary>
        /// Consider a Revit length zero 
        /// if is smaller than this.
        /// </summary>
        const float _eps = 1.0e-9f;

        /// <summary>
        /// Conversion factor from feet to meter.
        /// </summary>
        const float _feet_to_m = 0.3048f;

        public static float ToSingle(this double d)
            => Convert.ToSingle(d);

        /// <summary>
        /// Convert double length value from feet to meter
        /// </summary>
        public static float ToGLTFLength(this double d) {
            var f = d.ToSingle();
            if (Math.Abs(f) <= _eps)
                return 0f;
            return _feet_to_m * f;
        }
    }
}
