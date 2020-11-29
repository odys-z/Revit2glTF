using System;

using Autodesk.Revit.DB;

using GLTFRevitExport.GLTF;
using GLTFRevitExport.Extensions;
using GLTFRevitExport.GLTF.Schema;
using GLTFRevitExport.GLTF.Extensions.BIM;

namespace GLTFRevitExport.ExportContext.BuildActions {
    class ElementBeginAction : BuildBeginAction {
        private readonly ElementType _elementType;

        public string Uri { get; set; } = null;

        public ElementBeginAction(Element element, ElementType type) : base(element) {
            _elementType = type;
        }

        public override void Execute(GLTFBuilder gltf,
                                        GLTFExportConfigs cfg,
                                        Func<object, string[]> zoneFinder,
                                        Func<object, glTFExtras> extrasBuilder) {
            // open a new node and store its id
            Logger.Log("+ element begin");

            // node filter to pass to gltf builder
            string targetId = string.Empty;
            bool nodeFilter(glTFNode node) {
                if (node.Extensions != null) {
                    foreach (var ext in node.Extensions)
                        if (ext.Value is GLTFBIMNodeExtension nodeExt)
                            return nodeExt.Id == targetId;
                }
                return false;
            }

            // create a node for its type
            // attemp at finding previously created node for this type
            // but only search children of already open node
            if (IncludeHierarchy) {
                targetId = _elementType.GetId();
                var typeNodeIdx = gltf.FindChildNode(nodeFilter);

                if (typeNodeIdx >= 0) {
                    gltf.OpenExistingNode((uint)typeNodeIdx);
                }
                // otherwise create and open a new node for this type
                else {
                    var bimExt = new GLTFBIMNodeExtension(
                        e: _elementType,
                        zoneFinder: null,
                        includeParameters: IncludeProperties,
                        propContainer: PropertyContainer
                    );

                    gltf.OpenNode(
                        name: _elementType.Name,
                        matrix: null,
                        exts: new glTFExtension[] { bimExt },
                        extras: extrasBuilder(_elementType)
                    );
                }
            }

            // create a node for this instance
            // attemp at finding previously created node for this instance
            // but only search children of already open type node
            targetId = element.GetId();
            var instNodeIdx = gltf.FindChildNode(nodeFilter);

            if (instNodeIdx >= 0) {
                gltf.OpenExistingNode((uint)instNodeIdx);
            }
            // otherwise create and open a new node for this type
            else {
                var bimExt = new GLTFBIMNodeExtension(
                    e: element,
                    zoneFinder: zoneFinder,
                    includeParameters: IncludeProperties,
                    propContainer: PropertyContainer
                );
                bimExt.Uri = Uri;

                var newNodeIdx = gltf.OpenNode(
                    name: element.Name,
                    matrix: null,
                    exts: new glTFExtension[] { bimExt },
                    extras: extrasBuilder(element)
                );

                var bbox = element.get_BoundingBox(null);
                if (bbox != null)
                    UpdateBounds(
                        gltf: gltf,
                        idx: newNodeIdx,
                        bounds: new GLTFBIMBounds(bbox)
                    );
            }
        }

        private void UpdateBounds(GLTFBuilder gltf, uint idx, GLTFBIMBounds bounds) {
            glTFNode node = gltf.GetNode(idx);
            if (node.Extensions != null) {
                foreach (var ext in node.Extensions) {
                    if (ext.Value is GLTFBIMNodeExtension nodeExt) {
                        if (nodeExt.Bounds != null)
                            nodeExt.Bounds.Union(bounds);
                        else
                            nodeExt.Bounds = bounds;

                        int parentIdx = gltf.FindParentNode(idx);
                        if (parentIdx >= 0)
                            UpdateBounds(gltf, (uint)parentIdx, nodeExt.Bounds);
                    }
                }
            }
        }
    }

    class ElementEndAction : BuildEndAction {
        public override void Execute(GLTFBuilder gltf, GLTFExportConfigs cfg) {
            Logger.Log("- element end");
            // close instance node
            gltf.CloseNode();
            // close type node
            if (IncludeHierarchy)
                gltf.CloseNode();
        }
    }

    class ElementTransformAction : BaseAction {
        private readonly float[] _xform;

        public ElementTransformAction(float[] xform) => _xform = xform;

        public override void Execute(GLTFBuilder gltf, GLTFExportConfigs cfg) {
            if (gltf.GetActiveNode() is glTFNode activeNode) {
                Logger.Log("> transform");
                activeNode.Matrix = _xform;
            }
            else
                Logger.Log("x transform");
        }
    }
}