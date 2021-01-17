using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Newtonsoft.Json;

using Autodesk.Revit.DB;

using GLTFRevitExport.Extensions;
using GLTFRevitExport.GLTF.Extensions.BIM.BaseTypes;

namespace GLTFRevitExport.GLTF.Extensions.BIM {
    class GLTFBIMNodeExtension : GLTFBIMPropertyExtension {
        public GLTFBIMNodeExtension(Element e,
                                    Func<object, string[]> zoneFinder,
                                    bool includeParameters,
                                    GLTFBIMPropertyContainer propContainer)
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
        public GLTFBIMBounds Bounds { get; set; }
    }

    [Serializable]
    class GLTFBIMBounds : ISerializable {
        public GLTFBIMBounds(BoundingBoxXYZ bbox) {
            Min = new GLTFBIMVector(bbox.Min);
            Max = new GLTFBIMVector(bbox.Max);
        }

        public GLTFBIMBounds(SerializationInfo info, StreamingContext context) {
            var min = (float[])info.GetValue("min", typeof(float[]));
            Min = new GLTFBIMVector(min[0], min[1], min[2]);
            var max = (float[])info.GetValue("max", typeof(float[]));
            Max = new GLTFBIMVector(max[0], max[1], max[2]);
        }

        [JsonProperty("min")]
        public GLTFBIMVector Min { get; set; }

        [JsonProperty("max")]
        public GLTFBIMVector Max { get; set; }

        public void Union(GLTFBIMBounds other) {
            Min.ContractTo(other.Min);
            Max.ExpandTo(other.Max);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("min", new double[] { Min.X, Min.Y, Min.Z });
            info.AddValue("max", new double[] { Max.X, Max.Y, Max.Z });
        }
    }

    [Serializable]
    class GLTFBIMVector {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public GLTFBIMVector(XYZ pt) {
            X = pt.X.ToGLTFLength();
            Y = pt.Y.ToGLTFLength();
            Z = pt.Z.ToGLTFLength();
        }

        public GLTFBIMVector(float x, float y, float z) {
            X = x; Y = y; Z = z;
        }

        public void ContractTo(GLTFBIMVector other) {
            X = other.X < X ? other.X : X;
            Y = other.Y < Y ? other.Y : Y;
            Z = other.Z < Z ? other.Z : Z;
        }

        public void ExpandTo(GLTFBIMVector other) {
            X = other.X > X ? other.X : X;
            Y = other.Y > Y ? other.Y : Y;
            Z = other.Z > Z ? other.Z : Z;
        }
    }
}
