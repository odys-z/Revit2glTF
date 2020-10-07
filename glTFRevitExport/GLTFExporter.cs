using System;

using Autodesk.Revit.DB;

using GLTFRevitExport.Properties;

namespace GLTFRevitExport {
    public class GLTFExporter : CustomExporter {
        public GLTFExporter(Document doc, GLTFExportConfigs configs)
            : base(doc, new GLTFExportContext(doc, configs)) {
        }

        public void ExportView(View view) {
#if (REVIT2017 || REVIT2018 || REVIT2019)
            if (view is View3D view3d)
                base.Export(view3d);
            else
                throw new Exception(StringLib.NoSupportedView);
#else
            // export View3D was deprecated in Revit 2020 and above
            base.Export(view);
#endif
        }
    }
}