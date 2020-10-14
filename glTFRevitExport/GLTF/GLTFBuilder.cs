using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using GLTFRevitExport.GLTF.Containers;
using GLTFRevitExport.GLTF.Schema;
using GLTFRevitExport.Properties;

namespace GLTFRevitExport.GLTF {
    #region Initialization, Completion
    internal sealed partial class GLTFBuilder {
        internal GLTFBuilder() {
            _gltf = new glTF();
        }

        /// <summary>
        /// Pack the constructed glTF data into a container
        /// </summary>
        /// <returns></returns>
        internal Tuple<string, List<byte[]>> Pack(bool singleBinary = true) {
            List<byte> bufferBytes = new List<byte>();

            if (singleBinary) {
                uint bufferIndex = 0;
                foreach (var seg in _bufferSegments) {
                    var bytes = seg.ToByteArray();
                    var bufferView = new glTFBufferView {
                        Buffer = bufferIndex,
                        ByteLength = (uint)bytes.Length,
                        ByteOffset = (uint)bufferBytes.Count,
                        Target = seg.Target
                    };
                    _gltf.BufferViews.Add(bufferView);

                    // add the data to buffer
                    bufferBytes.AddRange(bytes);

                    var accessor = new glTFAccessor {
                        Type = seg.Type,
                        ComponentType = seg.DataType,
                        BufferView = (uint)_gltf.BufferViews.Count - 1,
                        ByteOffset = 0,
                        Count = seg.Count,
                        Min = seg.Min,
                        Max = seg.Max,
                    };
                    _gltf.Accessors.Add(accessor);
                }
            }
            else {
                // ?
            }

            var buffer = new glTFBuffer {
                ByteLength = (uint)bufferBytes.Count,
                Uri = "buffer.bin"
            };
            _gltf.Buffers.Add(buffer);

            // store snapshot of collected data into a gltf structure
            return new Tuple<string, List<byte[]>> (
                JsonConvert.SerializeObject(
                    _gltf,
                    new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore
                    }
                ),
                new List<byte[]> { bufferBytes.ToArray() }
            );
        }
    }
    #endregion

    #region Data stacks
    internal sealed partial class GLTFBuilder {
        private readonly glTF _gltf = null;

        abstract class BufferSegment {
            public abstract string Type { get; }
            public abstract ComponentType DataType { get; }
            public abstract Targets Target { get; }
            public abstract uint Count { get; }
            public abstract byte[] ToByteArray();

            public abstract object[] Min { get; }
            public abstract object[] Max { get; }
        }

        abstract class BufferSegment<T> : BufferSegment {
            protected T[] _data;
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

            public override uint Count => (uint)_data.Length;
        }

        abstract class BufferFloatSegment : BufferSegment<float> {
            public override ComponentType DataType => ComponentType.FLOAT;

            public override byte[] ToByteArray() {
                int dataSize = _data.Length * sizeof(float);
                var byteArray = new byte[dataSize];
                Buffer.BlockCopy(_data, 0, byteArray, 0, dataSize);
                return byteArray;
            }
        }

        class BufferVectorSegment : BufferFloatSegment {
            public override string Type => "VEC3";
            public override Targets Target => Targets.ARRAY_BUFFER;

            public BufferVectorSegment(float[] vectors) {
                if (vectors.Length % 3 != 0)
                    throw new Exception(StringLib.ArrayIsNotVector3Data);
                _data = vectors;
                setBounds(_data);
            }

            public override uint Count => (uint)(_data.Length / 3);

            public void setBounds(float[] vectors) {
                // TODO: improve logic and performance
                List<float> vx = new List<float>();
                List<float> vy = new List<float>();
                List<float> vz = new List<float>();
                for (int i = 0; i < vectors.Length; i += 3) {
                    vx.Add(vectors[i]);
                    vy.Add(vectors[i + 1]);
                    vz.Add(vectors[i + 2]);
                }

                _min = new float[] { vx.Min(), vy.Min(), vz.Min() };
                _max = new float[] { vx.Max(), vy.Max(), vz.Max() };
            }
        }

        abstract class BufferScalarSegment : BufferSegment<uint> {
            public override string Type => "SCALAR";
            public override ComponentType DataType => ComponentType.UNSIGNED_INT;
            public override Targets Target => Targets.ELEMENT_ARRAY_BUFFER;

            public BufferScalarSegment(uint[] scalars) {
                _data = scalars;
                _min = new uint[] { _data.Min() };
                _max = new uint[] { _data.Max() };
            }

            public override byte[] ToByteArray() {
                int dataSize = _data.Length * sizeof(float);
                var byteArray = new byte[dataSize];
                Buffer.BlockCopy(_data, 0, byteArray, 0, dataSize);
                return byteArray;
            }
        }

        class BufferFaceSegment : BufferScalarSegment {
            public BufferFaceSegment(uint[] faces) : base(faces) { }
        }

        private readonly List<BufferSegment> _bufferSegments = new List<BufferSegment>();
        private readonly Queue<glTFMeshPrimitive> _primQueue = new Queue<glTFMeshPrimitive>();
    }

    #endregion

    #region Builders
    internal sealed partial class GLTFBuilder {
        public void UseExtension(glTFExtension ext) {
            if (_gltf.ExtensionsUsed is null)
                _gltf.ExtensionsUsed = new HashSet<string>();
            _gltf.ExtensionsUsed.Add(ext.Name);
        }
        #region Asset
        public void SetAsset(string generatorId, string copyright,
                             glTFExtension[] exts, glTFExtras extras) {
            var assetExts = new Dictionary<string, glTFExtension>();
            if (exts != null) {
                foreach (var ext in exts)
                    if (ext != null) {
                        assetExts.Add(ext.Name, ext);
                        UseExtension(ext);
                    }
            }

            _gltf.Asset = new glTFAsset {
                Generator = generatorId,
                Copyright = copyright,
                Extensions = assetExts.Count > 0 ? assetExts : null,
                Extras = extras
            };
        }
        #endregion

        #region Scenes

        public uint OpenScene(string name,
                              glTFExtension[] exts, glTFExtras extras) {
            _gltf.Scenes.Add(
                new glTFScene {
                    Name = name,
                    Extensions = exts?.ToDictionary(x => x.Name, x => x),
                    Extras = extras
                }
                );
            return (uint)_gltf.Scenes.Count - 1;
        }

        public glTFScene PeekScene() => _gltf.Scenes.LastOrDefault();

        public int FindScene(Func<glTFScene, bool> filter) {
            foreach (var scene in _gltf.Scenes)
                if (filter(scene))
                    return _gltf.Scenes.IndexOf(scene);
            return -1;
        }

        public void CloseScene() { }

        #endregion

        #region Nodes
        public uint AppendNode(string name, float[] matrix,
                               glTFExtension[] exts, glTFExtras extras) {
            // create new node and set base properties
            var node = new glTFNode() {
                Name = name ?? "undefined",
                Matrix = matrix,
                Extensions = exts?.ToDictionary(x => x.Name, x => x),
                Extras = extras
            };

            var idx = _gltf.Nodes.Append(node);
            return appendNodeToScene(idx);
        }

        public uint OpenNode(string name, float[] matrix,
                             glTFExtension[] exts, glTFExtras extras) {
            var idx = AppendNode(name, matrix, exts, extras);
            _gltf.Nodes.Open(idx);
            return idx;
        }

        public glTFNode PeekNode() => _gltf.Nodes.LastOrDefault();

        public glTFNode GetNode(uint idx) => _gltf.Nodes[idx];

        public glTFNode GetActiveNode() => _gltf.Nodes.Peek();

        public int FindNode(Func<glTFNode, bool> filter) {
            foreach (var node in _gltf.Nodes)
                if (filter(node))
                    return (int)_gltf.Nodes.IndexOf(node);
            return -1;
        }

        public int FindChildNode(Func<glTFNode, bool> filter) {
            if (_gltf.Nodes.Peek() is glTFNode currentNode) {
                if (currentNode.Children is null)
                    return -1;

                foreach (var childIdx in currentNode.Children) {
                    var node = _gltf.Nodes[childIdx];
                    if (filter(node))
                        return (int)childIdx;
                }
                return -1;
            }
            else
                return FindNode(filter);
        }

        public int FindParentNode(uint nodeIndex) {
            foreach (var node in _gltf.Nodes)
                if (node.Children != null && node.Children.Contains(nodeIndex))
                    return (int)_gltf.Nodes.IndexOf(node);
            return -1;
        }

        public uint OpenExistingNode(uint nodeIndex) {
            if (_gltf.Nodes.Contains(nodeIndex)) {
                appendNodeToScene(nodeIndex);
                _gltf.Nodes.Open(nodeIndex);
                return nodeIndex;
            }
            else
                throw new Exception(StringLib.NodeNotExist);
        }

        public void CloseNode() {
            if (PeekNode() is glTFNode currentNode) {
                if (_primQueue.Count > 0) {
                    // combine all collected primitives into a mesh
                    _gltf.Meshes.Add(
                        new glTFMesh {
                            Primitives = _primQueue.ToList()
                        }
                    );
                    currentNode.Mesh = (uint)_gltf.Meshes.Count - 1;
                }

                // and close the node
                _gltf.Nodes.Close();

                // clean queue
                _primQueue.Clear();
            }
        }

        #endregion

        #region Node Mesh
        public uint AddPrimitive(float[] vertices, float[] normals, uint[] faces) {
            if (PeekNode() is glTFNode) {
                var vertexBuffer = new BufferVectorSegment(vertices);
                var vBuffIdx = _bufferSegments.IndexOf(vertexBuffer);
                if (vBuffIdx < 0) {
                    _bufferSegments.Add(vertexBuffer);
                    vBuffIdx = _bufferSegments.Count - 1;
                }

                //var normalBuffer = new BufferVectorSegment(normals);
                //var nBuffIdx = _bufferSegments.IndexOf(normalBuffer);
                //if (nBuffIdx < 0) {
                //    _bufferSegments.Add(normalBuffer);
                //    nBuffIdx = _bufferSegments.Count - 1;
                //}

                var faceBuffer = new BufferFaceSegment(faces);
                var fBuffIdx = _bufferSegments.IndexOf(faceBuffer);
                if (fBuffIdx < 0) {
                    _bufferSegments.Add(faceBuffer);
                    fBuffIdx = _bufferSegments.Count - 1;
                }

                _primQueue.Enqueue(
                    new glTFMeshPrimitive {
                        Indices = (uint)fBuffIdx,
                        Attributes = new glTFAttributes {
                            Position = (uint)vBuffIdx,
                            //Normal = (uint)nBuffIdx,
                        }
                    }
                );
                return (uint)_primQueue.Count - 1;
            }
            else
                throw new Exception(StringLib.NoParentNode);
        }

        public uint AddMaterial(uint primitiveIndex,
                                string name, float[] color,
                                glTFExtension[] exts, glTFExtras extras) {
            if (PeekNode() is glTFNode currentNode) {
                if (_primQueue.Count > primitiveIndex) {
                    var prim = _primQueue.ElementAt((int)primitiveIndex);

                    var material = new glTFMaterial() {
                        Name = name,
                        PBRMetallicRoughness = new glTFPBRMetallicRoughness() {
                            BaseColorFactor = color,
                            MetallicFactor = 0f,
                            RoughnessFactor = 1f,
                        },
                        Extensions = exts?.ToDictionary(x => x.Name, x => x),
                        Extras = extras
                    };

                    _gltf.Materials.Add(material);
                    prim.Material = (uint)_gltf.Materials.Count - 1;
                    return prim.Material;
                }
                else
                    throw new Exception(StringLib.NoParentPrimitive);
            }
            else
                throw new Exception(StringLib.NoParentNode);
        }

        public int FindMaterial(Func<glTFMaterial, bool> filter) {
            foreach (var material in _gltf.Materials)
                if (filter(material))
                    return (int)_gltf.Materials.IndexOf(material);
            return -1;
        }

        public void UpdateMaterial(uint primitiveIndex, uint materialIndex) {
            if (PeekNode() is glTFNode currentNode) {
                if (_primQueue.Count > primitiveIndex) {
                    var prim = _primQueue.ElementAt((int)primitiveIndex);
                    prim.Material = materialIndex;
                }
                else
                    throw new Exception(StringLib.NoParentPrimitive);
            }
            else
                throw new Exception(StringLib.NoParentNode);
        }

        #endregion
    }
    #endregion

    #region Privates
    internal sealed partial class GLTFBuilder {
        private uint appendNodeToScene(uint idx) {
            if (PeekScene() is glTFScene scene) {
                if (!_gltf.Nodes.IsOpen())
                    scene.Nodes.Add(idx);
                return idx;
            }
            else
                throw new Exception(StringLib.NoParentScene);
        }
    }
}
#endregion