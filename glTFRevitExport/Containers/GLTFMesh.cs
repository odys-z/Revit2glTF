using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLTFRevitExport.Containers {
    internal class GLTFMesh {
        // TODO: ensure normals and vertices have the same length
        public List<GLTFVector> Vertices = new List<GLTFVector>();
        public List<GLTFVector> Normals = new List<GLTFVector>();
        public List<GLTFFace> Faces = new List<GLTFFace>();

        public Material Material;
        public Color Color;
        public double Transparency;

        private double[] makeArray(List<GLTFVector> vectors) {
            var vectorBuffer = new List<double>();
            foreach (var vtx in vectors) {
                vectorBuffer.Add(vtx.X);
                vectorBuffer.Add(vtx.Y);
                vectorBuffer.Add(vtx.Z);
            }
            return vectorBuffer.ToArray();
        }

        private uint[] makeArray(List<GLTFFace> faces) {
            var scalarBuffer = new List<uint>();
            foreach (var face in faces) {
                scalarBuffer.Add(face.V1);
                scalarBuffer.Add(face.V2);
                scalarBuffer.Add(face.V3);
            }
            return scalarBuffer.ToArray();
        }

        public double[] GetVertexBuffer() => makeArray(Vertices);
        public double[] GetNormalBuffer() => makeArray(Normals);
        public uint[] GetFaceBuffer() => makeArray(Faces);

        public static GLTFMesh operator +(GLTFMesh left, GLTFMesh right) {
            int startIdx = left.Vertices.Count;

            // new vertices array
            var vertices = new List<GLTFVector>(left.Vertices);
            vertices.AddRange(right.Vertices);

            // new normals array
            var normals = new List<GLTFVector>(left.Normals);
            normals.AddRange(right.Normals);

            // shift face indices
            var faces = new List<GLTFFace>(left.Faces);
            foreach (var faceIdx in right.Faces)
                faces.Add(faceIdx + (uint)startIdx);

            return new GLTFMesh {
                Material = left.Material,
                Color = left.Color,
                Transparency = left.Transparency,
                Vertices = vertices,
                Normals = normals,
                Faces = faces,
            };
        }
    }
}
