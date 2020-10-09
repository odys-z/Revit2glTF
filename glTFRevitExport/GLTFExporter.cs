using System;

using Autodesk.Revit.DB;

using GLTFRevitExport.Properties;

namespace GLTFRevitExport {
    public class GLTFExporter {
        private GLTFExportConfigs _cfg = null;        
        private GLTFExportContext _ctx = null;

        public GLTFExporter(GLTFExportConfigs configs = null) {
            _cfg = configs ?? new GLTFExportConfigs();
        }

        public void ExportView(View view, ElementFilter filter = null) {
            _ctx = new GLTFExportContext(view.Document, _cfg);
            var exp = new CustomExporter(view.Document, _ctx);
            exp.ShouldStopOnError = _cfg.StopOnErrors;

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

        public void WriteGLTF(string filename, string directory, ElementFilter filter = null)
            => _ctx.Write(filename, directory, filter);
    }
}