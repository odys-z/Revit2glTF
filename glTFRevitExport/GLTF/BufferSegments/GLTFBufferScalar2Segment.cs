using System;
using System.Linq;

using GLTFRevitExport.GLTF.Schema;

namespace GLTFRevitExport.GLTF.BufferSegments {
    class GLTFBufferScalar2Segment : GLTFBufferSegment<ushort> {
        public override glTFAccessorType Type => glTFAccessorType.SCALAR;
        public override glTFAccessorComponentType DataType => glTFAccessorComponentType.UNSIGNED_SHORT;
        public override glTFBufferViewTargets Target => glTFBufferViewTargets.ELEMENT_ARRAY_BUFFER;

        public GLTFBufferScalar2Segment(ushort[] scalars) {
            Data = scalars;
            _min = new ushort[] { Data.Min() };
            _max = new ushort[] { Data.Max() };
        }

        public override byte[] ToByteArray() {
            int dataSize = Data.Length * sizeof(ushort);
            var byteArray = new byte[dataSize];
            Buffer.BlockCopy(Data, 0, byteArray, 0, dataSize);
            return byteArray;
        }
    }
}