using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Autodesk.Revit.DB;
using GLTFRevitExport.Extensions;

namespace GLTFRevitExport.GLTF.Types.BIMExtension {
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class glTFBIMExtras : glTFExtras {
        public glTFBIMExtras(Element e) {
            Categories = getCategories(e);
            Properties = getProperties(e);
        }

        /// <summary>
        /// The Revit created UniqueId for this object
        /// </summary>
        [JsonProperty("id")]
        public string UniqueId { get; set; }

        [JsonProperty("categories")]
        public List<string> Categories { get; set; } = new List<string>();

        [JsonProperty("properties")]
        public Dictionary<string, object> Properties { get; set; }

        private List<string> getCategories(Element e) {
            // TODO: add all categories
            var categories = new List<string>();
            if (e.Category != null)
                categories.Add($"revit::{e.Category.Name}");
            return categories;
        }

        /// <summary>
        /// From Jeremy Tammik's RvtVa3c exporter:
        /// https://github.com/va3c/RvtVa3c
        /// Return a dictionary of all the given 
        /// element parameter names and values.
        /// </summary>
        private Dictionary<string, object> getProperties(Element e) {
            // TODO: this needs a formatter for prop name and value
            // TODO: skip name property
            var propData = new Dictionary<string, object>();
            foreach (var param in e.GetOrderedParameters()) {
                string paramName = param.Definition.Name;
                if (!propData.ContainsKey(paramName)) {
                    switch (param.StorageType) {
                        case StorageType.None: break;

                        case StorageType.String:
                            propData.Add(paramName, param.AsString());
                            break;

                        case StorageType.Integer:
                            if (param.Definition.ParameterType == ParameterType.YesNo)
                                propData.Add(paramName, param.AsInteger() != 0);
                            else
                                propData.Add(paramName, param.AsInteger());
                            break;

                        case StorageType.Double:
                            propData.Add(paramName, param.AsDouble().ToMM());
                            break;

                        case StorageType.ElementId:
                            propData.Add(
                              paramName,
                              param.AsElementId().IntegerValue
                              );
                            break;
                    }
                }
            }
            return propData;
        }
    }
}
