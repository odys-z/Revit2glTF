using System;
using System.Collections.Generic;

using Autodesk.Revit.DB;

using GLTFRevitExport.GLTF;
using GLTFRevitExport.GLTF.Schema;
using GLTFRevitExport.GLTF.Extensions.BIM;
using GLTFRevitExport.GLTF.Package;

namespace GLTFRevitExport.ExportContext {
    class BuildContext {
        public GLTFBuilder Builder { get; private set; }
        public GLTFBIMAssetExtension AssetExtension { get; private set; }
        public GLTFBIMPropertyContainer PropertyContainer { get; private set; }

        public BuildContext(string name, Document doc, GLTFExportConfigs exportCfgs, Func<object, glTFExtras> extrasBuilder) {
            // create main gltf builder
            Builder = new GLTFBuilder(name);

            // build asset extension and property source (if needed)
            if (exportCfgs.EmbedParameters)
                AssetExtension = new GLTFBIMAssetExtension(doc, exportCfgs.ExportParameters);
            else {
                PropertyContainer = new GLTFBIMPropertyContainer($"{name}-properties.json");
                AssetExtension = new GLTFBIMAssetExtension(doc, exportCfgs.ExportParameters, PropertyContainer);
            }

            Builder.SetAsset(
                generatorId: exportCfgs.GeneratorId,
                copyright: exportCfgs.CopyrightMessage,
                exts: new glTFExtension[] { AssetExtension },
                extras: extrasBuilder != null ? extrasBuilder(doc) : null
            );
        }

        public List<GLTFPackageItem> Pack(GLTFBuildConfigs configs) {
            var gltfPack = new List<GLTFPackageItem>();

            // pack the glTF data and get the container
            gltfPack.AddRange(
                Builder.Pack(singleBinary: configs.UseSingleBinary)
            );

            if (PropertyContainer != null && PropertyContainer.HasPropertyData)
                gltfPack.Add(
                    new GLTFPackageJsonItem(PropertyContainer.Uri, PropertyContainer.Pack())
                );

            return gltfPack;
        }
    }
}