using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Autodesk.Revit.DB;

using GLTFRevitExport.Extensions;
using GLTFRevitExport.GLTF.Schema;
using GLTFRevitExport.GLTF;
using GLTFRevitExport.Properties;

namespace GLTFRevitExport.GLTF.Extensions.BIM {
    [Serializable]
#pragma warning disable IDE1006 // Naming Styles
    internal abstract class glTFBIMExtension: glTFExtension {
#pragma warning restore IDE1006 // Naming Styles
        internal glTFBIMExtension() { }

        internal override string Name => StringLib.GLTFExtensionName;
    }
}
