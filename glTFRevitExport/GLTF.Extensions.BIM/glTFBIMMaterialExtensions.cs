using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;

namespace GLTFRevitExport.GLTF.Extensions.BIM {
    internal class GLTFBIMMaterialExtensions : GLTFBIMPropertyExtension {
        internal GLTFBIMMaterialExtensions(Element e,
                                           bool includeParameters,
                                           GLTFBIMPropertyContainer propContainer)
            : base(e, includeParameters, propContainer) { }
    }
}
