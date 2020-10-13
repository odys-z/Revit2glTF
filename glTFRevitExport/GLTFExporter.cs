using System;
using System.IO;
using Autodesk.Revit.DB;
using GLTFRevitExport.GLTF.Schema;

namespace GLTFRevitExport {
    public class GLTFExporter {
        private readonly GLTFExportConfigs _cfgs = null;        
        private readonly GLTFExportContext _ctx = null;

        public GLTFExporter(Document doc, GLTFExportConfigs configs = null) {
            _cfgs = configs ?? new GLTFExportConfigs();
            _ctx = new GLTFExportContext(doc, _cfgs);
        }

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
#endif
        }

        public string[] BuildGLTF(string filename, string directory,
                                  ElementFilter filter = null,
                                  Func<object, string[]> zoneFinder = null,
                                  Func<object, glTFExtras> extrasBuilder = null) {
            // ensure filename is really a file name and no extension
            filename = Path.GetFileNameWithoutExtension(filename);

            // build the glTF
            var glTF = _ctx.Build(filter, zoneFinder, extrasBuilder);

            // pack the glTF data and get the container
            var container = glTF.Pack(
                filename: filename,
                singleBinary: _cfgs.UseSingleBinary
            );

            return container.Write(directory);
        }
    }
}