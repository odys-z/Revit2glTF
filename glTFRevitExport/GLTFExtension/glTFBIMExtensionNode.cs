﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Autodesk.Revit.DB;

using GLTFRevitExport.Extensions;
using GLTFRevitExport.GLTF.Types;
using GLTFRevitExport.GLTF;
using GLTFRevitExport.Properties;

namespace GLTFRevitExport.GLTFExtension {
    [Serializable]
    internal class glTFBIMExtensionNode : glTFBIMExtensionBaseNodeData {
        internal glTFBIMExtensionNode(Element e) : base(e) { }

        public override string Type => "meta";
    }

}
