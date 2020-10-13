using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLTFRevitExport.GLTF.Containers {
    internal class GLTFPrimitive {
        // TODO: ensure normals and vertices have the same length
        public List<GLTFVector> Vertices = new List<GLTFVector>();
        public List<GLTFVector> Normals = new List<GLTFVector>();
        public List<GLTFFace> Faces = new List<GLTFFace>();

        public uint MaterialIdx;

        private float[] makeArray(List<GLTFVector> vectors) {
            var vectorBuffer = new List<float>();
            foreach (var vtx in vectors) {
                vectorBuffer.Add(vtx.X);
                vectorBuffer.Add(vtx.Y);
                vectorBuffer.Add(vtx.Z);
            }
            return vectorBuffer.ToArray();
        }

        private ushort[] makeArray(List<GLTFFace> faces) {
            var scalarBuffer = new List<ushort>();
            foreach (var face in faces) {
                scalarBuffer.Add(face.V1);
                scalarBuffer.Add(face.V2);
                scalarBuffer.Add(face.V3);
            }
            return scalarBuffer.ToArray();
        }

        public float[] GetVertexBuffer() => makeArray(Vertices);
        public float[] GetNormalBuffer() => makeArray(Normals);
        public ushort[] GetFaceBuffer() => makeArray(Faces);

        public static GLTFPrimitive operator +(GLTFPrimitive left, GLTFPrimitive right) {
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
                faces.Add(faceIdx + (ushort)startIdx);

            return new GLTFPrimitive {
                MaterialIdx = left.MaterialIdx,
                Vertices = vertices,
                Normals = normals,
                Faces = faces,
            };
        }
    }
}
