using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;

namespace GLTFRevitExport.GLTF.Extensions.BIM {
    internal class glTFBIMMaterialExtensions : glTFBIMPropertyExtension {
        internal glTFBIMMaterialExtensions(Element e, bool includeParameters = true)
            : base(e, includeParameters) { }
    }
}
