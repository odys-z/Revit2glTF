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
#pragma warning disable IDE1006 // Naming Styles
    internal abstract class glTFBIMPropertyExtension : glTFBIMExtension {
#pragma warning restore IDE1006 // Naming Styles
        private const string _revitPrefix = "Revit";

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

        internal glTFBIMPropertyExtension(Element e, bool includeParameters = true, glTFBIMPropertyContainer propContainer = null) {
            // identity data
            Id = e.GetId();
            Taxonomies = GetTaxonomies(e);
            // TODO: get correct uniformat category
            Classes.Add(
                $"uniformat/{GetParamValue(e, BuiltInParameter.UNIFORMAT_CODE)}".UriEncode()
                );
            Classes.Add(
                $"omniclass/{GetParamValue(e, BuiltInParameter.OMNICLASS_CODE)}".UriEncode()
                );

            // include parameters
            if (includeParameters) {
                if (propContainer is null)
                    // embed properties
                    Properties = GetProperties(e);
                else
                    // record properties
                    propContainer.Record(Id, GetProperties(e));
            }
        }

        private Dictionary<string, object> GetProperties(Element e) {
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
                        GetParamValue(e, apiParamInfo.TypeParam) :
                        GetParamValue(e, apiParamInfo.InstanceParam);

                    // if there is compatible value, set the prop on this
                    if (paramValue != null
                            && propInfo.PropertyType.IsAssignableFrom(paramValue.GetType()))
                        propInfo.SetValue(this, paramValue);

                    // add the processed params to exclude
                    excludeParams.Add(apiParamInfo.TypeParam);
                    excludeParams.Add(apiParamInfo.InstanceParam);
                }
            }

            return GetParamValues(e, exclude: excludeParams);
        }

        private List<string> GetTaxonomies(Element e) {
            var taxonomies = new List<string>();
            // types show the hierarchical structure of data (vertical)
            if (e is ElementType et) {
                string categoryName = et.Category != null ? et.Category.Name : et.ToString();
                string familyName = et.FamilyName;
                taxonomies.Add(
                        $"{_revitPrefix}/Categories/{categoryName}/{familyName}".UriEncode()
                    );
            }
            // instances show various containers that include them (horizontal)
            else {
                // NOTE: Subcategories are another container but they are applied
                // to sub-elements in external families only
                // Phases
                string createdPhaseName = e.Document.GetElement(e.CreatedPhaseId)?.Name;
                if (createdPhaseName != null)
                    taxonomies.Add(
                            $"{_revitPrefix}/Phases/Created/{createdPhaseName}".UriEncode()
                        );
                string demolishedPhaseName = e.Document.GetElement(e.DemolishedPhaseId)?.Name;
                if (demolishedPhaseName != null)
                    taxonomies.Add(
                        $"{_revitPrefix}/Phases/Demolished/{demolishedPhaseName}".UriEncode()
                    );

                // Design options
                string designOptsName = e.DesignOption?.Name;
                if (designOptsName != null)
                    taxonomies.Add(
                        $"{_revitPrefix}/Design Options/{designOptsName}".UriEncode()
                    );

                // Worksets
                if (e.Document.IsWorkshared
                        && e.WorksetId != WorksetId.InvalidWorksetId) {
                    var ws = e.Document.GetWorksetTable().GetWorkset(e.WorksetId);
                    if (ws != null)
                        taxonomies.Add(
                            $"{_revitPrefix}/WorkSets/{ws.Name}".UriEncode()
                        );
                }

                // Groups
                if (e.GroupId != ElementId.InvalidElementId) {
                    var grp = e.Document.GetElement(e.GroupId);
                    if (grp != null)
                        taxonomies.Add(
                            $"{_revitPrefix}/Groups/{grp.Name}".UriEncode()
                        );
                }
            }

            return taxonomies;
        }

        /// <summary>
        /// From Jeremy Tammik's RvtVa3c exporter:
        /// https://github.com/va3c/RvtVa3c
        /// Return a dictionary of all the given 
        /// element parameter names and values.
        /// </summary>
        private Dictionary<string, object> GetParamValues(Element e, List<BuiltInParameter> exclude = null) {
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

        private object GetParamValue(Element e, BuiltInParameter p) {
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
