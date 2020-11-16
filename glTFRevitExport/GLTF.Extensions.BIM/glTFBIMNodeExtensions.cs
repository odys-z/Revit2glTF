using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Autodesk.Revit.DB;

using GLTFRevitExport.Extensions;
using GLTFRevitExport.GLTF.Schema;
using GLTFRevitExport.GLTF;
using GLTFRevitExport.Properties;
using System.Runtime.Serialization;

namespace GLTFRevitExport.GLTF.Extensions.BIM {
#pragma warning disable IDE1006 // Naming Styles
    internal class glTFBIMNodeExtension : glTFBIMPropertyExtension {
#pragma warning restore IDE1006 // Naming Styles
        internal glTFBIMNodeExtension(Element e,
                                      Func<object, string[]> zoneFinder,
                                      bool includeParameters,
                                      glTFBIMPropertyContainer propContainer)
            : base(e, includeParameters, propContainer)
        {
            // set level
            if (e.Document.GetElement(e.LevelId) is Level level)
                Level = level.GetId();

            // set zones
            Zones = zoneFinder != null ? new HashSet<string>(zoneFinder(e)) : null;
        }

        [JsonProperty("level", Order = 21)]
        public string Level { get; set; }

        [JsonProperty("zones", Order = 22)]
        public HashSet<string> Zones { get; set; }

        [JsonProperty("bounds", Order = 23)]
        public glTFBIMBounds Bounds { get; set; }
    }

    [Serializable]
#pragma warning disable IDE1006 // Naming Styles
    internal class glTFBIMBounds : ISerializable {
#pragma warning restore IDE1006 // Naming Styles
        internal glTFBIMBounds(BoundingBoxXYZ bbox) {
            Min = new glTFBIMVector(bbox.Min);
            Max = new glTFBIMVector(bbox.Max);
        }

        public glTFBIMBounds(SerializationInfo info, StreamingContext context) {
            var min = (float[])info.GetValue("min", typeof(float[]));
            Min = new glTFBIMVector(min[0], min[1], min[2]);
            var max = (float[])info.GetValue("max", typeof(float[]));
            Max = new glTFBIMVector(max[0], max[1], max[2]);
        }

        [JsonProperty("min")]
        public glTFBIMVector Min { get; set; }

        [JsonProperty("max")]
        public glTFBIMVector Max { get; set; }

        public void Union(glTFBIMBounds other) {
            Min.ContractTo(other.Min);
            Max.ExpandTo(other.Max);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("min", new double[] { Min.X, Min.Y, Min.Z });
            info.AddValue("max", new double[] { Max.X, Max.Y, Max.Z });
        }
    }

    // TODO: serialize into 3 double values
    [Serializable]
#pragma warning disable IDE1006 // Naming Styles
    internal class glTFBIMVector {
#pragma warning restore IDE1006 // Naming Styles
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public glTFBIMVector(XYZ pt) {
            X = pt.X.ToSingle();
            Y = pt.Y.ToSingle();
            Z = pt.Z.ToSingle();
        }

        public glTFBIMVector(float x, float y, float z) {
            X = x; Y = y; Z = z;
        }

        public void ContractTo(glTFBIMVector other) {
            X = other.X < X ? other.X : X;
            Y = other.Y < Y ? other.Y : Y;
            Z = other.Z < Z ? other.Z : Z;
        }

        public void ExpandTo(glTFBIMVector other) {
            X = other.X > X ? other.X : X;
            Y = other.Y > Y ? other.Y : Y;
            Z = other.Z > Z ? other.Z : Z;
        }
    }
}
