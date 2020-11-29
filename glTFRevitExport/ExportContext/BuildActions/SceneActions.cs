using System;

using Autodesk.Revit.DB;

using GLTFRevitExport.GLTF;
using GLTFRevitExport.Extensions;
using GLTFRevitExport.GLTF.Schema;
using GLTFRevitExport.GLTF.Extensions.BIM;
using GLTFRevitExport.Properties;

namespace GLTFRevitExport.ExportContext.BuildActions {
    class SceneBeginAction : BuildBeginAction {
        public SceneBeginAction(View view) : base(view) { }

        public override void Execute(GLTFBuilder gltf,
                                        GLTFExportConfigs cfg,
                                        Func<object, string[]> zoneFinder,
                                        Func<object, glTFExtras> extrasBuilder) {
            // start a new gltf scene
            Logger.Log("+ view begin");
            gltf.OpenScene(
                name: element.Name,
                exts: new glTFExtension[] {
                    new GLTFBIMNodeExtension(element, zoneFinder, IncludeProperties, PropertyContainer)
                },
                extras: extrasBuilder(element)
                );

            // open a root node for the scene
            gltf.OpenNode(
                name: string.Format(StringLib.SceneRootNodeName, element.Name),
                matrix: Transform.CreateTranslation(new XYZ(0, 0, 0)).ToGLTF(),
                extras: null,
                exts: null
                );
        }
    }

    class SceneEndAction : BuildEndAction {
        public override void Execute(GLTFBuilder gltf, GLTFExportConfigs cfg) {
            Logger.Log("- view end");
            // close root node
            gltf.CloseNode();
            // close scene
            gltf.CloseScene();
        }
    }
}