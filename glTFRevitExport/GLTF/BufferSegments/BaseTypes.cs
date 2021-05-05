using System;
using System.Security.Cryptography;
using System.Text;

using GLTFRevitExport.GLTF.Schema;

namespace GLTFRevitExport.GLTF.BufferSegments {
    abstract class GLTFBufferSegment {
        public abstract glTFAccessorType Type { get; }
        public abstract glTFAccessorComponentType DataType { get; }
        public abstract glTFBufferViewTargets Target { get; }
        public abstract uint Count { get; }
        public abstract byte[] ToByteArray();

        public abstract object[] Min { get; }
        public abstract object[] Max { get; }
    }

    abstract class GLTFBufferSegment<T> : GLTFBufferSegment {
        private string _hash = null;
        public T[] Data;
        protected T[] _min;
        protected T[] _max;

        public override object[] Min {
            get {
                var min = new object[_min.Length];
                Array.Copy(_min, min, _min.Length);
                return min;
            }
        }
        public override object[] Max {
            get {
                var max = new object[_max.Length];
                Array.Copy(_max, max, _max.Length);
                return max;
            }
        }

        public override uint Count => (uint)Data.Length;

        public override bool Equals(object obj) {
            if (obj is GLTFBufferSegment<T> other)
                return ComputeHash() == other.ComputeHash();
            return false;
        }

        public override int GetHashCode() => base.GetHashCode();

        private string ComputeHash() {
            if (_hash is null)
                _hash = Encoding.UTF8.GetString(
                    SHA256.Create().ComputeHash(ToByteArray())
                    );
            return _hash;
        }
    }
}