using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Autodesk.Revit.DB;

using GLTFRevitExport.Extensions;
using GLTFRevitExport.GLTF.Types;

namespace GLTFRevitExport.GLTFExtension {
    [Serializable]
    public class glTFBIMExtras : glTFExtras {
    }
}
