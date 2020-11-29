using System;
using System.Collections.Generic;

using Autodesk.Revit.DB;

using GLTFRevitExport.GLTF.Schema;
using GLTFRevitExport.GLTF.Package;
using GLTFRevitExport.ExportContext;

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

        public List<GLTFPackageItem> BuildGLTF(ElementFilter filter = null,
                                              Func<object, string[]> zoneFinder = null,
                                              Func<object, glTFExtras> extrasBuilder = null,
                                              GLTFBuildConfigs configs = null)
            => _ctx.Build(filter, zoneFinder, extrasBuilder, configs);
    }
}