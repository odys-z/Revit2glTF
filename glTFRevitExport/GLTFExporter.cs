using System;
using System.Collections.Generic;
using System.Windows;

using Autodesk.Revit.DB;

using GLTFRevitExport.Properties;
using GLTFRevitExport.GLTF.Schema;
using GLTFRevitExport.GLTF.Package;
using GLTFRevitExport.ExportContext;

namespace GLTFRevitExport {
    /// <summary>
    /// Revit knowledge base about CustomExporter:
    /// https://knowledge.autodesk.com/support/revit-products/learn-explore/caas/CloudHelp/cloudhelp/2014/ENU/Revit/files/GUID-27FD73B7-B569-4507-AAC4-B17F3728D178-htm.html
    /// and helpful blog:
    /// https://www.programmersought.com/article/60105035818/
    /// </summary>
    public class GLTFExporter {
        private readonly GLTFExportContext _ctx = null;

        public GLTFExporter(Document doc, GLTFExportConfigs configs = null)
            => _ctx = new GLTFExportContext(doc, configs ?? new GLTFExportConfigs());

        public void ExportView(View view, ElementFilter filter = null) {

            // make sure view is ready for export
            var levelsCat = view.Document.Settings.Categories.get_Item(BuiltInCategory.OST_Levels);
            if (view.GetCategoryHidden(levelsCat.Id))
                throw new Exception("Levels are hidden in this view.");

            //// make necessary view adjustments
            //if (view.CanUseTemporaryVisibilityModes()) {
            //    // make sure levels are visible
            //    view.EnableTemporaryViewPropertiesMode(view.Id);
            //    var levelsCat = view.Document.Settings.Categories.get_Item(BuiltInCategory.OST_Levels);
            //    view.SetCategoryHidden(levelsCat.Id, false);
            //}


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

            //// reset visibility changes
            //view.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
        }

        public List<GLTFPackageItem> BuildGLTF(ElementFilter filter = null,
                                              Func<object, string[]> zoneFinder = null,
                                              Func<object, glTFExtras> extrasBuilder = null,
                                              GLTFBuildConfigs configs = null)
            => _ctx.Build(filter, zoneFinder, extrasBuilder, configs);
    }
}
