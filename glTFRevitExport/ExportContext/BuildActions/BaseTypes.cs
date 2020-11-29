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
    abstract class BaseAction {
        public GLTFBIMAssetExtension AssetExt;
        public bool IncludeHierarchy = true;
        public bool IncludeProperties = true;
        public GLTFBIMPropertyContainer PropertyContainer = null;

        public abstract void Execute(GLTFBuilder gltf, GLTFExportConfigs cfg);
    }

    abstract class BaseElementAction : BaseAction {
        protected Element element;

        public BaseElementAction(Element e) => element = e;

        public override void Execute(GLTFBuilder gltf, GLTFExportConfigs cfg)
            => Execute(
                gltf,
                cfg,
                (e) => {
                    if (e is FamilyInstance famInst) {
                        var zones = new HashSet<string>();
                        if (famInst.FromRoom != null)
                            zones.Add(famInst.FromRoom.GetId());
                        if (famInst.ToRoom != null)
                            zones.Add(famInst.ToRoom.GetId());
                        if (famInst.Room != null)
                            zones.Add(famInst.Room.GetId());
                        if (famInst.Space != null)
                            zones.Add(famInst.Space.GetId());
                    }
                    return null;
                },
                (e) => new glTFExtras()
                );
        public abstract void Execute(GLTFBuilder gltf,
                                        GLTFExportConfigs cfg,
                                        Func<object, string[]> zoneFinder,
                                        Func<object, glTFExtras> extrasBuilder);

        public bool Passes(ElementFilter filter) {
            if (element is null)
                return true;
            return filter.PassesFilter(element);
        }
    }

    abstract class BuildBeginAction : BaseElementAction {
        public BuildBeginAction(Element e) : base(e) { }
    }

    abstract class BuildEndAction : BaseAction { }
}