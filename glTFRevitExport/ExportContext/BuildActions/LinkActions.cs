using Autodesk.Revit.DB;

namespace GLTFRevitExport.ExportContext.BuildActions {
    class LinkBeginAction : ElementBeginAction {
        public Document LinkDocument { get; private set; }

        public LinkBeginAction(RevitLinkInstance link, RevitLinkType linkType, Document linkedDoc)
            : base(link, linkType) {
            LinkDocument = linkedDoc;
        }
    }

    class LinkEndAction : ElementEndAction {
    }
}