using Autodesk.Revit.DB;

namespace GLTFRevitExport.ExportContext.BuildActions {
    class LinkBeginAction : ElementBeginAction {
        public Document LinkDocument { get; private set; }
        public string LinkId { get; private set; }

        public LinkBeginAction(RevitLinkInstance link, RevitLinkType linkType, Document linkedDoc)
            : base(link, linkType) {
            LinkDocument = linkedDoc;
            LinkId = element.UniqueId;
        }
    }

    class LinkEndAction : ElementEndAction {
    }

    class LinkTransformAction : ElementTransformAction {
        public LinkTransformAction(float[] xform) : base(xform) { }
    }
}