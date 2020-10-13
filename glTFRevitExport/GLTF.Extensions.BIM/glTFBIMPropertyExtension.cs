using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Autodesk.Revit.DB;

using GLTFRevitExport.Extensions;
using GLTFRevitExport.GLTF.Schema;
using GLTFRevitExport.GLTF;
using GLTFRevitExport.Properties;
using System.Runtime.Serialization;

namespace GLTFRevitExport.GLTF.Extensions.BIM {
    [Serializable]
    internal abstract class glTFBIMPropertyExtension : glTFBIMExtension {

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

        internal glTFBIMPropertyExtension(Element e, bool includeParameters = true) {
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

        [JsonProperty("id", Order = 1)]
        public string Id { get; set; }

        // e.g. revit::Door::MyFamily::MyFamilyType
        [JsonProperty("taxonomies", Order = 2)]
        public List<string> Taxonomies { get; set; } = new List<string>();

        [JsonProperty("classes", Order = 3)]
        public List<string> Classes { get; set; } = new List<string>();

        [JsonProperty("mark", Order = 4)]
        [APIBuiltinParameters(
            BuiltInParameter.ALL_MODEL_TYPE_MARK,
            BuiltInParameter.ALL_MODEL_MARK
            )
        ]
        public string Mark { get; set; }

        [JsonProperty("description", Order = 5)]
        [APIBuiltinParameters(
            BuiltInParameter.ALL_MODEL_DESCRIPTION,
            BuiltInParameter.ALL_MODEL_DESCRIPTION
            )
        ]
        public string Description { get; set; }

        [JsonProperty("comment", Order = 6)]
        [APIBuiltinParameters(
            BuiltInParameter.ALL_MODEL_TYPE_COMMENTS,
            BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS
            )
        ]
        public string Comment { get; set; }

        [JsonProperty("uri", Order = 7)]
        public string Uri { get; set; }

        [JsonProperty("dataUrl", Order = 8)]
        [APIBuiltinParameters(
            BuiltInParameter.ALL_MODEL_URL,
            BuiltInParameter.ALL_MODEL_URL
            )
        ]
        public string DataUrl { get; set; }

        [JsonProperty("imageUrl", Order = 9)]
        [APIBuiltinParameters(
            BuiltInParameter.ALL_MODEL_TYPE_IMAGE,
            BuiltInParameter.ALL_MODEL_IMAGE
            )
        ]
        public string ImageUrl { get; set; }

        [JsonProperty("properties", Order = 99)]
        public Dictionary<string, object> Properties { get; set; }
    }
}
