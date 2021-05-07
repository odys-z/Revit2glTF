using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace GLTFRevitExport {
    /// <summary>
    /// Source from CustomExporterCollada.zip, see the blog at
    /// https://thebuildingcoder.typepad.com/blog/2013/07/graphics-pipeline-custom-exporter.html
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    class TestGltfExporter : IExternalCommand {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;
            Document doc = uidoc.Document;

            if (doc.ActiveView as View3D != null)
                ExportView3D(doc, doc.ActiveView as View3D);
            else
                MessageBox.Show("You must be in 3D view to export.");

            return Result.Succeeded;
        }

        internal void ExportView3D(Document document, View3D view3D)
        {
            GLTFExportContext context = new GLTFExportContext(document);

            // Create an instance of a custom exporter by giving it a document and the context.
            CustomExporter exporter = new CustomExporter(document, context);

            //    Note: Excluding faces just excludes the calls, not the actual processing of
            //    face tessellation. Meshes of the faces will still be received by the context.
            // exporter.IncludeFaces = false;

            exporter.ShouldStopOnError = false;
            exporter.Export(view3D);
        }
    }
}
