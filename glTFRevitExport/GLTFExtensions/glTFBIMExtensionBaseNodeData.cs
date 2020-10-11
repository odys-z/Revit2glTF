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
using System.Runtime.Serialization;

namespace GLTFRevitExport.GLTFExtensions {
    [Serializable]
    internal abstract class glTFBIMExtensionBaseNodeData : glTFBIMExtension {

        private readonly BuiltInParameter[] excludeBuiltinParams =
            Enum.GetNames(typeof(BuiltInParameter))
                .Where(x =>
                    x.Contains("_NAME")
                 || x.Contains("NAME_")
                 || x.StartsWith("UNIFORMAT_")
                 || x.StartsWith("OMNICLASS_")
                 || x.StartsWith("HOST_ID_")
                 || x.StartsWith("INSTANCE_FREE_HOST_OFFSET_")
                )
                .Select(x => (BuiltInParameter)Enum.Parse(typeof(BuiltInParameter), x))
                .ToArray();

        internal glTFBIMExtensionBaseNodeData(Element e, Func<object, string[]> zoneFinder, bool includeParameters = true) {
            // identity data
            Id = e.GetId();
            Taxonomies = getTaxonomies(e);
            // TODO: get correct uniformat category
            Classes.Add(
                $"uniformat/{getParamValue(e, BuiltInParameter.UNIFORMAT_CODE)}".UriEncode()
                );
            Classes.Add(
                $"omniclass/{getParamValue(e, BuiltInParameter.OMNICLASS_CODE)}".UriEncode()
                );

            // set level
            if (e.Document.GetElement(e.LevelId) is Level level)
                Level = level.GetId();

            // set zones
            Zones = zoneFinder != null ? new HashSet<string>(zoneFinder(e)) : null;
            
            // include parameters
            if (includeParameters)
                setProperties(e);
        }

        private void setProperties(Element e) {
            // exclude list for parameters that are processed by this
            // constructor and should not be included in 'this.Properties'
            var excludeParams = new List<BuiltInParameter>(excludeBuiltinParams);

            bool isType = e is ElementType;

            // set the properties on this object from their associated builtin params
            foreach (var propInfo in GetType().GetProperties()) {
                var apiParamInfo =
                    propInfo.GetCustomAttributes(typeof(APIBuiltinParametersAttribute), false)
                            .Cast<APIBuiltinParametersAttribute>()
                            .FirstOrDefault();
                if (apiParamInfo != null) {
                    object paramValue =
                        isType ?
                        getParamValue(e, apiParamInfo.TypeParam) :
                        getParamValue(e, apiParamInfo.InstanceParam);

                    // if there is compatible value, set the prop on this
                    if (paramValue != null
                            && propInfo.PropertyType.IsAssignableFrom(paramValue.GetType()))
                        propInfo.SetValue(this, paramValue);

                    // add the processed params to exclude
                    excludeParams.Add(apiParamInfo.TypeParam);
                    excludeParams.Add(apiParamInfo.InstanceParam);
                }
            }

            Properties = getParamValues(e, exclude: excludeParams);
        }

        private List<string> getTaxonomies(Element e) {
            // TODO: add all categories
            var categories = new List<string>();
            if (e.Category != null)
                categories.Add(
                        $"revit/{e.Category.Name}".UriEncode()
                    );
            // TODO: add phases
            // TODO: add design options
            // TODO: add worksets
            // TODO: add groups?
            return categories;
        }

        /// <summary>
        /// From Jeremy Tammik's RvtVa3c exporter:
        /// https://github.com/va3c/RvtVa3c
        /// Return a dictionary of all the given 
        /// element parameter names and values.
        /// </summary>
        private Dictionary<string, object> getParamValues(Element e, List<BuiltInParameter> exclude = null) {
            // private function to find a parameter in a list of builins
            bool containsParameter(List<BuiltInParameter> paramList, Parameter param) {
                if (param.Definition is InternalDefinition paramDef)
                    foreach (var paramId in paramList)
                        if (paramDef.Id.IntegerValue == (int)paramId)
                            return true;
                return false;
            }
            // TODO: this needs a formatter for prop name and value
            var paramData = new Dictionary<string, object>();
            foreach (var param in e.GetOrderedParameters()) {
                // exclude requested params (only applies to internal params)
                if (exclude != null && containsParameter(exclude, param))
                    continue;

                // otherwise process the parameter value
                // skip useless names
                string paramName = param.Definition.Name;
                // skip useless values
                var paramValue = param.ToGLTF();
                if (paramValue is null) continue;
                if (paramValue is int intVal && intVal == -1) continue;

                // add value to dict
                if (!paramData.ContainsKey(paramName))
                    paramData.Add(paramName, paramValue);
            }
            return paramData;
        }

        private object getParamValue(Element e, BuiltInParameter p) {
            if (e.get_Parameter(p) is Parameter param)
                return param.ToGLTF();
            return null;
        }


        [JsonProperty("id")]
        public string Id { get; set; }

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
        public string Level { get; set; }

        [JsonProperty("zones")]
        public HashSet<string> Zones { get; set; }

        [JsonProperty("bounds")]
        public glTFBIMBounds Bounds { get; set; }

        [JsonProperty("properties")]
        public Dictionary<string, object> Properties { get; set; }
    }

    [Serializable]
    internal class glTFBIMBounds : ISerializable {
        internal glTFBIMBounds(BoundingBoxXYZ bbox) {
            Min = new glTFBIMVector(bbox.Min);
            Max = new glTFBIMVector(bbox.Max);
        }

        public glTFBIMBounds(SerializationInfo info, StreamingContext context) {
            var min = (double[])info.GetValue("min", typeof(double[]));
            Min = new glTFBIMVector(min[0], min[1], min[2]);
            var max = (double[])info.GetValue("max", typeof(double[]));
            Max = new glTFBIMVector(max[0], max[1], max[2]);
        }

        [JsonProperty("min")]
        public glTFBIMVector Min { get; set; }

        [JsonProperty("max")]
        public glTFBIMVector Max { get; set; }

        public void Union(glTFBIMBounds other) {
            Min.ContractTo(other.Min);
            Max.ExpandTo(other.Max);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("min", new double[] { Min.X, Min.Y, Min.Z });
            info.AddValue("max", new double[] { Max.X, Max.Y, Max.Z });
        }
    }

    // TODO: serialize into 3 double values
    [Serializable]
    internal class glTFBIMVector {
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
