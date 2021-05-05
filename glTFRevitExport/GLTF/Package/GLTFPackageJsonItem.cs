namespace GLTFRevitExport.GLTF.Package {
    public class GLTFPackageJsonItem : GLTFPackageItem {
        public GLTFPackageJsonItem(string uri, string jsonData) {
            Uri = uri;
            Data = jsonData;
        }

        public override string Uri { get; }
        public string Data { get; }
    }
}
