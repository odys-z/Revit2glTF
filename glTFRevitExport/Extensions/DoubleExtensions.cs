﻿using System;
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
        const double _eps = 1.0e-9;

        /// <summary>
        /// Conversion factor from feet to millimetres.
        /// </summary>
        const double _feet_to_mm = 25.4 * 12;

        /// <summary>
        /// Convert double length value from feet to millimetre
        /// </summary>
        public static long ToMM(this double d) {
            if (0 < d) {
                return _eps > d
                  ? 0
                  : (long)(_feet_to_mm * d + 0.5);
            }
            else {
                return _eps > -d
                  ? 0
                  : (long)(_feet_to_mm * d - 0.5);
            }
        }
    }
}
