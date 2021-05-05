using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;

using GLTFRevitExport.GLTF.Extensions.BIM.BaseTypes;

namespace GLTFRevitExport.GLTF.Extensions.BIM {
    class GLTFBIMMaterialExtensions : GLTFBIMPropertyExtension {
        public GLTFBIMMaterialExtensions(Element e,
                                         bool includeParameters,
                                         GLTFBIMPropertyContainer propContainer)
            : base(e, includeParameters, propContainer) { }
    }
}
