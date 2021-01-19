using System;

using Autodesk.Revit.DB;

using GLTFRevitExport.Extensions;


namespace GLTFRevitExport.ExportContext.Geometry {
    class BoundsData {
        public VectorData Min { get; set; }
        public VectorData Max { get; set; }

        public BoundsData(VectorData min, VectorData max) {
            Min = min;
            Max = max;
        }

        public BoundsData Transform(float[] matrix) {
            Transform xform = matrix.FromGLTFMatrix();
            var min = Min.Transform(xform);
            var max = Max.Transform(xform);
            return new BoundsData(
                new VectorData(
                    min.X < max.X ? min.X : max.X,
                    min.Y < max.Y ? min.Y : max.Y,
                    min.Z < max.Z ? min.Z : max.Z
                    ),
                new VectorData(
                    min.X > max.X ? min.X : max.X,
                    min.Y > max.Y ? min.Y : max.Y,
                    min.Z > max.Z ? min.Z : max.Z
                    )
            );
        }
    }
}
