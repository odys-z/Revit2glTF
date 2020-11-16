using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


using GLTFRevitExport.GLTF.Schema;

namespace GLTFRevitExport.GLTF {
    public abstract class GLTFPackageItem {
        public abstract string Name { get; }
    }

    public class GLTFPackageJsonItem : GLTFPackageItem {
        public GLTFPackageJsonItem(string name, string jsonData) {
            Name = name;
            Data = jsonData;
        }

        public override string Name { get; }
        public string Data { get; }
    }

    public class GLTFPackageModelItem : GLTFPackageJsonItem {
        public GLTFPackageModelItem(string name, string modelData)
            : base(name, modelData) { }
    }

    public class GLTFPackageBinaryItem : GLTFPackageItem {
        public GLTFPackageBinaryItem(string name, byte[] binaryData) {
            Name = name;
            Data = binaryData;
        }

        public override string Name { get; }
        public byte[] Data { get; }
    }
}
