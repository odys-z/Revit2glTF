using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using GLTFRevitExport.GLTF.Schema;
using GLTFRevitExport.Properties;
using GLTFRevitExport.GLTF.BufferSegments;
using GLTFRevitExport.GLTF.Package;

namespace GLTFRevitExport.GLTF {
    #region Initialization, Completion
    sealed partial class GLTFBuilder {
        private string _name;

        public GLTFBuilder(string name) {
            _name = name;
            _gltf = new glTF();
        }

        /// <summary>
        /// Pack the constructed glTF data into a container
        /// </summary>
        /// <returns></returns>
        public List<GLTFPackageItem> Pack(bool singleBinary = true) {
            // TODO: Add glb option
            // create a gltf bundle
            var bundleItems = new List<GLTFPackageItem>();

            // add the buffers to the gltf and to the bundle
            List<byte> bufferBytes = new List<byte>();
            
            if (singleBinary) {
                uint bufferIndex = 0;
                foreach (var seg in _bufferSegments) {
                    // align the data correctly
                    uint dataSize = 0;
                    // calculate necessary padding
                    switch (seg.DataType) {
                        case glTFAccessorComponentType.SHORT:
                        case glTFAccessorComponentType.UNSIGNED_SHORT:
                            dataSize = 2;
                            break;
                        case glTFAccessorComponentType.UNSIGNED_INT:
                        case glTFAccessorComponentType.FLOAT:
                            dataSize = 4;
                            break;
                    }
                    if (dataSize > 0) {
                        // add padding
                        var bufferSize = (uint)bufferBytes.Count;
                        var padding =
                            (uint)(Math.Ceiling(bufferSize / (float)dataSize) * dataSize)
                            - bufferSize;
                        if (padding > 0)
                            bufferBytes.AddRange(new byte[padding]);
                    }

                    // make the buffer view now
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

                var buffer = new glTFBuffer {
                    ByteLength = (uint)bufferBytes.Count,
                    Uri = $"{_name}.bin"
                };
                _gltf.Buffers.Add(buffer);

                bundleItems.Add(new GLTFPackageBinaryItem($"{_name}.bin", bufferBytes.ToArray()));
            }
            else {
                // TODO: multiple binaries
            }

            // store snapshot of collected data into a gltf structure
            var model = new GLTFPackageModelItem(
                uri: $"{_name}.gltf",
                modelData: JsonConvert.SerializeObject(
                    _gltf,
                    new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore
                    }
                )
            );

            // finally add glTF model to the bundle
            bundleItems.Add(model);
            
            return bundleItems;
        }
    }
    #endregion

    #region Data stacks
    sealed partial class GLTFBuilder {
        private readonly glTF _gltf = null;

        private readonly List<GLTFBufferSegment> _bufferSegments = new List<GLTFBufferSegment>();
        private readonly Queue<glTFMeshPrimitive> _primQueue = new Queue<glTFMeshPrimitive>();
    }

    #endregion

    #region Builders
    sealed partial class GLTFBuilder {
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

        public void UseExtension(glTFExtension ext) {
            if (_gltf.ExtensionsUsed is null)
                _gltf.ExtensionsUsed = new HashSet<string>();
            _gltf.ExtensionsUsed.Add(ext.Name);
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
            return AppendNodeToScene(idx);
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
                AppendNodeToScene(nodeIndex);
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
                    var newMesh = new glTFMesh {
                        Primitives = _primQueue.ToList()
                    };
                    // check to see if there is a matching mesh
                    var meshIdx = _gltf.Meshes.IndexOf(newMesh);
                    if (meshIdx < 0) {
                        // otherwise create a new mesh
                        _gltf.Meshes.Add(newMesh);
                        meshIdx = _gltf.Meshes.Count - 1;
                    }

                    // set the mesh on the active node
                    currentNode.Mesh = (uint)meshIdx;
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
            // ensure vertex and face data is available
            if (vertices is null || faces is null)
                throw new Exception(StringLib.VertexFaceIsRequired);

            if (PeekNode() is glTFNode) {
                // process vertex data
                var vertexBuffer = new GLTFBufferVectorSegment(vertices);
                var vBuffIdx = _bufferSegments.IndexOf(vertexBuffer);
                if (vBuffIdx < 0) {
                    _bufferSegments.Add(vertexBuffer);
                    vBuffIdx = _bufferSegments.Count - 1;
                }

                // process normal data if available
                int nBuffIdx = -1;
                if (normals != null) {
                    var normalBuffer = new GLTFBufferVectorSegment(normals);
                    nBuffIdx = _bufferSegments.IndexOf(normalBuffer);
                    if (nBuffIdx < 0) {
                        _bufferSegments.Add(normalBuffer);
                        nBuffIdx = _bufferSegments.Count - 1;
                    }
                }

                // process face data
                uint maxIndex = faces.Max();
                GLTFBufferSegment faceBuffer;
                if (maxIndex < 0xFF) {
                    var byteFaces = new List<byte>();
                    foreach (var face in faces)
                        byteFaces.Add(Convert.ToByte(face));
                    faceBuffer = new GLTFBufferScalar1Segment(byteFaces.ToArray());
                }
                else if (maxIndex < 0xFFFF) {
                    var shortFaces = new List<ushort>();
                    foreach (var face in faces)
                        shortFaces.Add(Convert.ToUInt16(face));
                    faceBuffer = new GLTFBufferScalar2Segment(shortFaces.ToArray());
                }
                else {
                    faceBuffer = new GLTFBufferScalar4Segment(faces);
                }

                var fBuffIdx = _bufferSegments.IndexOf(faceBuffer);
                if (fBuffIdx < 0) {
                    _bufferSegments.Add(faceBuffer);
                    fBuffIdx = _bufferSegments.Count - 1;
                }

                // queue the primitive
                _primQueue.Enqueue(
                    new glTFMeshPrimitive {
                        Indices = (uint)fBuffIdx,
                        Attributes = new glTFAttributes {
                            Position = (uint)vBuffIdx,
                            Normal = nBuffIdx >= 0 ? (uint)nBuffIdx : (uint?)null
                        }
                    }
                );

                // return primitive index
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

                    if (_gltf.Materials is null)
                        _gltf.Materials = new List<glTFMaterial>();
                    _gltf.Materials.Add(material);
                    prim.Material = (uint)_gltf.Materials.Count - 1;
                    return prim.Material.Value;
                }
                else
                    throw new Exception(StringLib.NoParentPrimitive);
            }
            else
                throw new Exception(StringLib.NoParentNode);
        }

        public int FindMaterial(Func<glTFMaterial, bool> filter) {
            if (_gltf.Materials != null && _gltf.Materials.Count > 0) {
                foreach (var material in _gltf.Materials)
                    if (filter(material))
                        return (int)_gltf.Materials.IndexOf(material);
            }
            return -1;
        }

        public void UpdateMaterial(uint primitiveIndex, uint materialIndex) {
            if (PeekNode() is glTFNode) {
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
    sealed partial class GLTFBuilder {
        private uint AppendNodeToScene(uint idx) {
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