using System;

using GLTFRevitExport.GLTF.Schema;
using GLTFRevitExport.Properties;

namespace GLTFRevitExport.GLTF.Extensions.BIM.BaseTypes {
    [Serializable]
    abstract class GLTFBIMExtension: glTFExtension {
        public GLTFBIMExtension() { }

        public override string Name => StringLib.GLTFExtensionName;
    }
}
