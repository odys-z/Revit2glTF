using System;
using System.Linq;

using GLTFRevitExport.GLTF.Schema;

namespace GLTFRevitExport.GLTF.BufferSegments {
    class GLTFBufferScalar4Segment : GLTFBufferSegment<uint> {
        public override glTFAccessorType Type => glTFAccessorType.SCALAR;
        public override glTFAccessorComponentType DataType => glTFAccessorComponentType.UNSIGNED_INT;
        public override glTFBufferViewTargets Target => glTFBufferViewTargets.ELEMENT_ARRAY_BUFFER;

        public GLTFBufferScalar4Segment(uint[] scalars) {
            Data = scalars;
            _min = new uint[] { Data.Min() };
            _max = new uint[] { Data.Max() };
        }

        public override byte[] ToByteArray() {
            int dataSize = Data.Length * sizeof(uint);
            var byteArray = new byte[dataSize];
            Buffer.BlockCopy(Data, 0, byteArray, 0, dataSize);
            return byteArray;
        }
    }
}