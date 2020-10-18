using System;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using GLTFRevitExport.GLTF.Containers;
using GLTFRevitExport.GLTF.Schema;

namespace GLTFRevitExport {
    public class GLTFExporter {
        private readonly GLTFExportContext _ctx = null;

        public GLTFExporter(Document doc, GLTFExportConfigs configs = null)
            => _ctx = new GLTFExportContext(doc, configs ?? new GLTFExportConfigs());

        public void ExportView(View view, ElementFilter filter = null) {
            var exp = new CustomExporter(view.Document, _ctx) {
                ShouldStopOnError = true
            };

#if (REVIT2017 || REVIT2018 || REVIT2019)
            if (view is View3D view3d)
                exp.Export(view3d);
            else
                throw new Exception(StringLib.NoSupportedView);
#else
            // export View3D was deprecated in Revit 2020 and above
            exp.Export(view);
            // TODO: handle cancel
#endif
        }

        public GLTFContainer BuildGLTF(ElementFilter filter = null,
                                       Func<object, string[]> zoneFinder = null,
                                       Func<object, glTFExtras> extrasBuilder = null,
                                       GLTFBuildConfigs configs = null)
        {
            // ensure configs
            configs = configs ?? new GLTFBuildConfigs();
            // build the glTF
            var glTF = _ctx.Build(filter, zoneFinder, extrasBuilder);
            // pack the glTF data and get the container
            var gltfPack = glTF.Pack(
                singleBinary: configs.UseSingleBinary
            );

            return new GLTFContainer {
                Model = gltfPack.Item1,
                Binaries = gltfPack.Item2
            };
        }
    }
}