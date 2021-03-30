using System;
using System.Collections.Generic;

using Autodesk.Revit.DB;

using GLTFRevitExport.GLTF;
using GLTFRevitExport.Extensions;
using GLTFRevitExport.GLTF.Schema;
using GLTFRevitExport.GLTF.Extensions.BIM;

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

    class LinkTransformAction : ElementTransformAction {
        public LinkTransformAction(float[] xform) : base(xform) { }
    }

    class LinkEndAction : ElementEndAction {
    }
}