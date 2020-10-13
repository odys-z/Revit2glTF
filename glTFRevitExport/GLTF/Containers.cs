using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using Autodesk.Revit.DB;

using GLTFRevitExport.GLTF.Schema;
using GLTFRevitExport.Properties;
using Autodesk.Private.InfoCenterLib;

namespace GLTFRevitExport.GLTF {
    [Serializable]
    internal class GLTFBinaryBufferSegment {
        public List<float> vertexBuffer { get; set; } = new List<float>();
        public List<int> faceVertexIndexBuffer { get; set; } = new List<int>();

    }

    //public class HashedType {
    //    public string hashcode { get; set; }
    //}

    //public class MeshContainer : HashedType {
    //    //public string hashcode { get; set; }
    //    public glTFMesh contents { get; set; }
    //}

    //public class GridParameters {
    //    public List<double> origin { get; set; }
    //    public List<double> direction { get; set; }
    //    public double length { get; set; }
    //}


    /// <summary>
    /// A binary data store serialized to a *.bin file
    /// </summary>
    internal class GLTFBinaryData {
        public string name { get; set; }
        public GLTFBinaryBufferSegment contents { get; set; }
        public int vertexAccessorIndex { get; set; }
        public int indexAccessorIndex { get; set; }
    }

    internal class GLTFContainer {
        public string Name;
        public glTF Model;
        public List<GLTFBinaryData> Binaries;

        public string[] Write(string directory) {
            // write the container data
            // write the *.bin files
            //if () {
            //    using (FileStream f = File.Create(Path.Combine(_directory, buffer.uri))) {
            //        using (BinaryWriter writer = new BinaryWriter(f)) {
            //            foreach (var bin in container.binaries) {
            //                foreach (var coord in bin.contents.vertexBuffer) {
            //                    writer.Write((float)coord);
            //                }
            //                foreach (var index in bin.contents.indexBuffer) {
            //                    writer.Write((int)index);
            //                }
            //            }
            //        }
            //    }
            //}
            //else {
            //    // Write the *.bin files
            //    foreach (var bin in container.binaries) {
            //        using (FileStream f = File.Create(Path.Combine(_directory, bin.name))) {
            //            using (BinaryWriter writer = new BinaryWriter(f)) {
            //                foreach (var coord in bin.contents.vertexBuffer) {
            //                    writer.Write((float)coord);
            //                }
            //                foreach (var index in bin.contents.indexBuffer) {
            //                    writer.Write((int)index);
            //                }
            //            }
            //        }
            //    }
            //}

            // Write the *.gltf file
            string serializedModel =
                JsonConvert.SerializeObject(
                    Model,
                    new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore
                    });

            var gltfFile = Path.Combine(directory, Name + ".gltf");
            var outputs = new List<string> { gltfFile };
            File.WriteAllText(
                gltfFile,
                serializedModel
            );

            return outputs.ToArray();
        }
    }

    ///// <summary>
    ///// Intermediate data format for 
    ///// converting between Revit Polymesh
    ///// and glTF buffers.
    ///// </summary>
    //public class GeometryData
    //{
    //    public VertexLookupInt vertDictionary = new VertexLookupInt();
    //    public List<long> vertices = new List<long>();
    //    public List<double> normals = new List<double>();
    //    public List<double> uvs = new List<double>();
    //    public List<int> faces = new List<int>();
    //}

    ///// <summary>
    ///// Container for holding a strict set of items
    ///// that is also addressable by a unique ID.
    ///// </summary>
    ///// <typeparam name="T">The type of item contained.</typeparam>
    //public class IndexedDictionary<T>
    //{
    //    private Dictionary<string, int> _dict = new Dictionary<string, int>();
    //    public List<T> List { get; } = new List<T>();
    //    public string CurrentKey { get; private set; }
    //    public Dictionary<string,T> Dict
    //    {
    //        get
    //        {
    //            var output = new Dictionary<string, T>();
    //            foreach (var kvp in _dict)
    //            {
    //                output.Add(kvp.Key, List[kvp.Value]);
    //            }
    //            return output;
    //        }
    //    }

    //    /// <summary>
    //    /// The most recently accessed item (not effected by GetElement()).
    //    /// </summary>
    //    public T CurrentItem
    //    {
    //        get { return List[_dict[CurrentKey]]; }
    //    }

    //    /// <summary>
    //    /// The index of the most recently accessed item (not effected by GetElement()).
    //    /// </summary>
    //    public int CurrentIndex
    //    {
    //        get { return _dict[CurrentKey]; }
    //    }

    //    /// <summary>
    //    /// Add a new item to the list, if it already exists then the 
    //    /// current item will be set to this item.
    //    /// </summary>
    //    /// <param name="uuid">Unique identifier for the item.</param>
    //    /// <param name="elem">The item to add.</param>
    //    /// <returns>true if item did not already exist.</returns>
    //    public bool AddOrUpdateCurrent(string uuid, T elem)
    //    {
    //        if (!_dict.ContainsKey(uuid))
    //        {
    //            List.Add(elem);
    //            _dict.Add(uuid, (List.Count - 1));
    //            CurrentKey = uuid;
    //            return true;
    //        }

    //        CurrentKey = uuid;
    //        return false;
    //    }

    //    /// <summary>
    //    /// Check if the container already has an item with this key.
    //    /// </summary>
    //    /// <param name="uuid">Unique identifier for the item.</param>
    //    /// <returns></returns>
    //    public bool Contains(string uuid)
    //    {
    //        return _dict.ContainsKey(uuid);
    //    }

    //    /// <summary>
    //    /// Returns the index for an item given it's unique identifier.
    //    /// </summary>
    //    /// <param name="uuid">Unique identifier for the item.</param>
    //    /// <returns>index of item or -1</returns>
    //    public int GetIndexFromUUID(string uuid)
    //    {
    //        if (!Contains(uuid)) throw new Exception("Specified item could not be found.");
    //        return _dict[uuid];
    //    }

    //    /// <summary>
    //    /// Returns an item given it's unique identifier.
    //    /// </summary>
    //    /// <param name="uuid">Unique identifier for the item</param>
    //    /// <returns>the item</returns>
    //    public T GetElement(string uuid)
    //    {
    //        int index = GetIndexFromUUID(uuid);
    //        return List[index];
    //    }

    //    /// <summary>
    //    /// Returns as item given it's index location.
    //    /// </summary>
    //    /// <param name="index">The item's index location.</param>
    //    /// <returns>the item</returns>
    //    public T GetElement(int index)
    //    {
    //        if (index < 0 || index > List.Count - 1) throw new Exception("Specified item could not be found.");
    //        return List[index];
    //    }
    //}

    ///// <summary>
    ///// From Jeremy Tammik's RvtVa3c exporter:
    ///// https://github.com/va3c/RvtVa3c
    ///// A vertex lookup class to eliminate 
    ///// duplicate vertex definitions.
    ///// </summary>
    //public class VertexLookupInt : Dictionary<GLTFVector, int>
    //{
    //    /// <summary>
    //    /// Define equality for integer-based PointInt.
    //    /// </summary>
    //    class PointIntEqualityComparer : IEqualityComparer<GLTFVector>
    //    {
    //        public bool Equals(GLTFVector p, GLTFVector q)
    //        {
    //            return 0 == p.CompareTo(q);
    //        }

    //        public int GetHashCode(GLTFVector p)
    //        {
    //            return (p.X.ToString()
    //              + "," + p.Y.ToString()
    //              + "," + p.Z.ToString())
    //              .GetHashCode();
    //        }
    //    }

    //    public VertexLookupInt() : base(new PointIntEqualityComparer())
    //    {
    //    }

    //    /// <summary>
    //    /// Return the index of the given vertex,
    //    /// adding a new entry if required.
    //    /// </summary>
    //    public int AddVertex(GLTFVector p)
    //    {
    //        return ContainsKey(p)
    //          ? this[p]
    //          : this[p] = Count;
    //    }
    //}
}
