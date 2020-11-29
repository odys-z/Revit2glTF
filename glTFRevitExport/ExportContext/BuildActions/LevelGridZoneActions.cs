using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Autodesk.Revit.DB;

using GLTFRevitExport.GLTF;
using GLTFRevitExport.Extensions;
using GLTFRevitExport.GLTF.Schema;
using GLTFRevitExport.GLTF.Extensions.BIM;
using GLTFRevitExport.Properties;
using System.Runtime.CompilerServices;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.Visual;
using Autodesk.Revit.DB.Architecture;

namespace GLTFRevitExport.ExportContext.BuildActions {
    class LevelAction : BuildBeginAction {
        public LevelAction(Level level) : base(level) { }

        public override void Execute(GLTFBuilder gltf,
                                        GLTFExportConfigs cfg,
                                        Func<object, string[]> zoneFinder,
                                        Func<object, glTFExtras> extrasBuilder) {
            Logger.Log("> level");

            Level level = (Level)element;

            // make a matrix from level elevation
            float elev = level.Elevation.ToGLTFLength();
            float[] elevMatrix = null;
            if (elev != 0f) {
                elevMatrix = new float[16] {
                            1f,   0f,   0f,   0f,
                            0f,   1f,   0f,   0f,
                            0f,   0f,   1f,   0f,
                            0f,   elev, 0f,   1f
                        };
            }

            // create level node
            var levelNodeIdx = gltf.OpenNode(
                name: level.Name,
                matrix: elevMatrix,
                exts: new glTFExtension[] {
                        new GLTFBIMNodeExtension(level, null, IncludeProperties, PropertyContainer)
                },
                extras: extrasBuilder(level)
            );

            gltf.CloseNode();

            // record the level in asset
            if (AssetExt != null) {
                if (AssetExt.Levels is null)
                    AssetExt.Levels = new List<uint>();
                AssetExt.Levels.Add(levelNodeIdx);
            }

            // not need to do anything else
            return;
        }
    }
}