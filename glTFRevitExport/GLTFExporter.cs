using System;

using Autodesk.Revit.DB;

using GLTFRevitExport.Properties;

namespace GLTFRevitExport {
    public class GLTFExporter {
        private Document _doc = null;
        private GLTFExportConfigs _cfg = null;
        
        private GLTFExportContext _ctx = null;
        private CustomExporter _exp = null;

        public GLTFExporter(Document document, GLTFExportConfigs configs = null) {
            _doc = document;
            _cfg = configs ?? new GLTFExportConfigs();
            
            _ctx = new GLTFExportContext(_doc, _cfg);
            _exp = new CustomExporter(_doc, _ctx);
            _exp.ShouldStopOnError = _cfg.StopOnErrors;
        }

        public void ExportView(View view) {
#if (REVIT2017 || REVIT2018 || REVIT2019)
            if (view is View3D view3d)
                _exporter.Export(view3d);
            else
                throw new Exception(StringLib.NoSupportedView);
#else
            // export View3D was deprecated in Revit 2020 and above
            _exp.Export(view);
#endif
        }

        public bool Write(string filename, string directory)
            => _ctx.Write(filename, directory);

        public static void ExportViewToFile(View view,
                                            string filename, string directory,
                                            GLTFExportConfigs configs = null)
        {
            var gltfExporter = new GLTFExporter(view.Document, configs);
            gltfExporter.ExportView(view);
            gltfExporter.Write(filename, directory);
        }
    }
}