using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace glTFRevitExport
{
    [TestClass]
    public class SharpGltfTest
    {
        private const string _directory = "";
        private const string _filename = "door-desk";

        [TestMethod]
        public void TestGltf2Glb()
        {
            string pgltf = Path.Combine(_directory, _filename + ".gltf");

            var mglb = SharpGLTF.Schema2.ModelRoot.Load(pgltf);
            mglb.SaveGLB(Path.Combine(_directory, _filename + ".glb"));
        }
    }
}
