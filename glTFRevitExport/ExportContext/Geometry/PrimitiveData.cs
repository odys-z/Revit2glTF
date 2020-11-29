using System;
using System.Collections.Generic;

using GLTFRevitExport.Properties;

namespace GLTFRevitExport.ExportContext.Geometry {
    class PrimitiveData {
        private List<VectorData> _normals = null;

        // TODO: ensure normals and vertices have the same length
        public List<VectorData> Vertices { get; private set; }
        public List<VectorData> Normals {
            get => _normals;
            set {
                if (value is null)
                    return;

                if (value.Count != Vertices.Count)
                    throw new Exception(StringLib.NormalsMustMatchVertexCount);

                _normals = value;
            }
        }
        public List<FacetData> Faces { get; private set; }

        public PrimitiveData(List<VectorData> vertices, List<FacetData> faces) {
            if (vertices is null || faces is null)
                throw new Exception(StringLib.VertexFaceIsRequired);
            Vertices = vertices;
            Faces = faces;
        }

        public static PrimitiveData operator +(PrimitiveData left, PrimitiveData right) {
            int startIdx = left.Vertices.Count;

            // new vertices array
            var vertices = new List<VectorData>(left.Vertices);
            vertices.AddRange(right.Vertices);

            // new normals array
            // NOTE: we are dropping the normals if either side
            // is missing normal definition
            List<VectorData> normals = null;
            if (left.Normals != null && right.Normals != null) {
                normals = new List<VectorData>(left.Normals);
                normals.AddRange(right.Normals);
            }

            // shift face indices
            var faces = new List<FacetData>(left.Faces);
            foreach (var faceIdx in right.Faces)
                faces.Add(faceIdx + (ushort)startIdx);

            return new PrimitiveData(vertices, faces) {
                Normals = normals,
            };
        }
    }
}