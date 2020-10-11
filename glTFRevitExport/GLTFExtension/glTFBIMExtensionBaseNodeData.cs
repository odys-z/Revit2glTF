using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Autodesk.Revit.DB;

using GLTFRevitExport.Extensions;
using GLTFRevitExport.GLTF.Types;
using GLTFRevitExport.GLTF;
using GLTFRevitExport.Properties;

namespace GLTFRevitExport.GLTFExtension {
    [Serializable]
    public abstract class glTFBIMExtensionBaseNodeData : glTFBIMExtension {
        private readonly BuiltInParameter[] excludeBuiltinParams =
            Enum.GetNames(typeof(BuiltInParameter))
                .Where(x => 
                    x.Contains("_NAME")
                 || x.Contains("NAME_")
                 || x.Contains("UNIFORMAT_")
                 || x.Contains("OMNICLASS_")
                )
                .Select(x => (BuiltInParameter)Enum.Parse(typeof(BuiltInParameter), x))
                .ToArray();

        internal glTFBIMExtensionBaseNodeData(Element e, Func<string, int> nodeFinder) {
            bool isType = e is ElementType;

            // exclude list for parameters that are processed by this
            // constructor and should not be included in 'this.Properties'
            var excludeParams = new List<BuiltInParameter>(excludeBuiltinParams);

            // identity data
            UniqueId = e.UniqueId;
            Taxonomies = e.GetTaxonomies();
            Classes.Add(
                $"uniformat::{e.GetParamValue(BuiltInParameter.UNIFORMAT_CODE)}"
                );
            Classes.Add(
                $"omniclass::{e.GetParamValue(BuiltInParameter.OMNICLASS_CODE)}"
                );
            
            // set the properties on this object from their associated builtin params
            foreach(var propInfo in GetType().GetProperties()) {
                var apiParamInfo = 
                    propInfo.GetCustomAttributes(typeof(APIBuiltinParametersAttribute), false)
                            .Cast<APIBuiltinParametersAttribute>()
                            .FirstOrDefault();
                if (apiParamInfo != null) {
                    object paramValue =
                        isType ?
                        e.GetParamValue(apiParamInfo.TypeParam) :
                        e.GetParamValue(apiParamInfo.InstanceParam);

                    // if there is compatible value, set the prop on this
                    if(paramValue != null 
                            && propInfo.PropertyType.IsAssignableFrom(paramValue.GetType()))
                        propInfo.SetValue(this, paramValue);

                    // add the processed params to exclude
                    excludeParams.Add(apiParamInfo.TypeParam);
                    excludeParams.Add(apiParamInfo.InstanceParam);
                }
            }

            // use node finder to set references to existing nodes
            // TODO: is the node already processed?
            if (nodeFinder != null) {
                if (e.Document.GetElement(e.LevelId) is Level level) {
                    var levelNodeIdx = nodeFinder(level.UniqueId);
                    if (levelNodeIdx >= 0)
                        Level = (uint)levelNodeIdx;
                }
            }

            Properties = e.GetParamDict(exclude: excludeParams);
        }

        [JsonProperty("id")]
        public string UniqueId { get; set; }

        // e.g. revit::Door::MyFamily::MyFamilyType
        [JsonProperty("taxonomies")]
        public List<string> Taxonomies { get; set; } = new List<string>();

        [JsonProperty("classes")]
        public List<string> Classes { get; set; } = new List<string>();

        [JsonProperty("mark")]
        [APIBuiltinParameters(
            BuiltInParameter.ALL_MODEL_TYPE_MARK,
            BuiltInParameter.ALL_MODEL_MARK
            )
        ]
        public string Mark { get; set; }

        [JsonProperty("description")]
        [APIBuiltinParameters(
            BuiltInParameter.ALL_MODEL_DESCRIPTION,
            BuiltInParameter.ALL_MODEL_DESCRIPTION
            )
        ]
        public string Description { get; set; }

        [JsonProperty("comment")]
        [APIBuiltinParameters(
            BuiltInParameter.ALL_MODEL_TYPE_COMMENTS,
            BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS
            )
        ]
        public string Comment { get; set; }

        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("dataUrl")]
        [APIBuiltinParameters(
            BuiltInParameter.ALL_MODEL_URL,
            BuiltInParameter.ALL_MODEL_URL
            )
        ]
        public string DataUrl { get; set; }

        [JsonProperty("imageUrl")]
        [APIBuiltinParameters(
            BuiltInParameter.ALL_MODEL_TYPE_IMAGE,
            BuiltInParameter.ALL_MODEL_IMAGE
            )
        ]
        public string ImageUrl { get; set; }

        [JsonProperty("level")]
        public uint? Level { get; set; }

        [JsonProperty("zones")]
        public HashSet<uint> Zones { get; set; }

        [JsonProperty("bounds")]
        public glTFBIMBounds Bounds { get; set; }

        [JsonProperty("properties")]
        public Dictionary<string, object> Properties { get; set; }
    }

    [Serializable]
    public class glTFBIMBounds {
        internal glTFBIMBounds(BoundingBoxXYZ bbox) {
            Min = new glTFBIMVector(bbox.Min);
            Max = new glTFBIMVector(bbox.Max);
        }

        [JsonProperty("min")]
        public glTFBIMVector Min { get; set; }

        [JsonProperty("max")]
        public glTFBIMVector Max { get; set; }

        public void Union(glTFBIMBounds other) {
            Min.ContractTo(other.Min);
            Max.ExpandTo(other.Max);
        }
    }

    // TODO: serialize into 3 double values
    [Serializable]
    public class glTFBIMVector {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public glTFBIMVector(XYZ pt) { X = pt.X; Y = pt.Y; Z = pt.Z; }
        public glTFBIMVector(double x, double y, double z) { X = x; Y = y; Z = z; }

        public void ContractTo(glTFBIMVector other) {
            X = other.X < X ? other.X : X;
            Y = other.Y < Y ? other.Y : Y;
            Z = other.Z < Z ? other.Z : Z;
        }

        public void ExpandTo(glTFBIMVector other) {
            X = other.X > X ? other.X : X;
            Y = other.Y > Y ? other.Y : Y;
            Z = other.Z > Z ? other.Z : Z;
        }
    }
}
