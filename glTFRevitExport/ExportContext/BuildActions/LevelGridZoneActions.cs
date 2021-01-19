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
        private BoundingBoxXYZ _extentsBbox;

        public LevelAction(Element element, BoundingBoxXYZ extents) : base(element) {
            _extentsBbox = extents;
        }

        public override void Execute(GLTFBuilder gltf,
                                     GLTFExportConfigs cfg,
                                     Func<object, string[]> zoneFinder,
                                     Func<object, glTFExtras> extrasBuilder) {
            Logger.Log("> level");

            Level level = (Level)element;

            // make a matrix from level elevation
            float elev = level.Elevation.ToGLTFLength();
            float[] elevMatrix = null;
            // no matrix is specified for a level at elev 0
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

            // set level bounds
            if (_extentsBbox != null) {
                var bounds = new GLTFBIMBounds(_extentsBbox);
                glTFNode node = gltf.GetNode(levelNodeIdx);
                if (node.Extensions != null) {
                    foreach (var ext in node.Extensions) {
                        if (ext.Value is GLTFBIMNodeExtension nodeExt) {
                            if (nodeExt.Bounds != null)
                                nodeExt.Bounds.Union(bounds);
                            else
                                nodeExt.Bounds = bounds;
                        }
                    }
                }
            }

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
    
    class GridAction : BuildBeginAction {
        public GridAction(Element element) : base(element) { }

        public override void Execute(GLTFBuilder gltf,
                                     GLTFExportConfigs cfg,
                                     Func<object, string[]> zoneFinder,
                                     Func<object, glTFExtras> extrasBuilder) {
            Logger.Log("> grid");

            Grid grid = (Grid)element;

            // TODO: make a matrix from grid
            float[] gridMatrix = null;

            if (grid.Curve is Line gridLine) {
                // add gltf-bim extension data
                var gltfBim = new GLTFBIMNodeExtension(grid, null, IncludeProperties, PropertyContainer);

                // grab the two ends of the grid line as grid bounds
                gltfBim.Bounds = new GLTFBIMBounds(
                    gridLine.GetEndPoint(0),
                    gridLine.GetEndPoint(1)
                );

                // create level node
                var gridNodeIdx = gltf.OpenNode(
                    name: grid.Name,
                    matrix: gridMatrix,
                    exts: new glTFExtension[] { gltfBim },
                    extras: extrasBuilder(grid)
                );

                gltf.CloseNode();

                // record the grid in asset
                if (AssetExt != null) {
                    if (AssetExt.Grids is null)
                        AssetExt.Grids = new List<uint>();
                    AssetExt.Grids.Add(gridNodeIdx);
                }
            }

            // not need to do anything else
            return;
        }
    }
}