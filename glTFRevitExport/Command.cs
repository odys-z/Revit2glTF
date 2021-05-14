using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32;

namespace glTFRevitExport
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public glTF resultGltf { get; protected set; }
        public string filename { get; protected set; }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Execute(commandData);
        }

       public Result Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            return Execute(uidoc);
        }
 
        public Result Execute(ExternalCommandData commandData)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            return Execute(uidoc);
        }

        public Result Execute(UIDocument uidoc)
        {
            Document doc = uidoc.Document;

            View3D view = doc.ActiveView as View3D;
            if (view == null)
            {
                TaskDialog.Show("glTFRevitExport", "You must be in a 3D view to export.");
                return Result.Failed;
            }

            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.FileName = "xv-import"; // default file name
            fileDialog.DefaultExt = ".gltf";   // default file extension

            bool? dialogResult = fileDialog.ShowDialog();
            if (dialogResult == true)
            {
                filename = fileDialog.FileName;
                string directory = Path.GetDirectoryName(filename) + "\\";
                glTFContainer glConter = ExportView3D(view, filename, directory);
                resultGltf = glConter.glTF;
            }

            return Result.Succeeded;
        }

        public glTFContainer ExportView3D(View3D view3d, string filename, string directory)
        {
            Document doc = view3d.Document;

            // Use our custom implementation of IExportContext as the exporter context.
            glTFExportContext ctx = new glTFExportContext(doc, filename, directory);
            // Create a new custom exporter with the context.
            CustomExporter exporter = new CustomExporter(doc, ctx);

            exporter.ShouldStopOnError = true;
            exporter.Export(view3d);

            return ctx.gltfContainer;
        }
 
    }
}
