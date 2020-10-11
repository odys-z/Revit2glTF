using System;
using System.Collections.Generic;

using Newtonsoft.Json;

using Autodesk.Revit.DB;

using GLTFRevitExport.Extensions;

namespace GLTFRevitExport.GLTFExtension {
    [Serializable]
    internal class glTFBIMExtensionAssetData : glTFBIMExtension {
        internal glTFBIMExtensionAssetData(Document d) : base() {
            App = "revit";
            Id = getDocumentId(d).ToString();
            Title = d.Title;
            Source = d.PathName;
            Properties = getProjectInfo(d);
        }

        private static Guid getDocumentId(Document doc) {
            if (doc?.IsValidObject != true)
                return Guid.Empty;
            return ExportUtils.GetGBXMLDocumentId(doc);
        }

        private static Dictionary<string, object> getProjectInfo(Document doc) {
            var docProps = new Dictionary<string, object>();
            if (doc != null) {
                var pinfo = doc.ProjectInformation;

                foreach (BuiltInParameter paramId in new BuiltInParameter[] {
                    BuiltInParameter.PROJECT_ORGANIZATION_NAME,
                    BuiltInParameter.PROJECT_ORGANIZATION_DESCRIPTION,
                    BuiltInParameter.PROJECT_NUMBER,
                    BuiltInParameter.PROJECT_NAME,
                    BuiltInParameter.CLIENT_NAME,
                    BuiltInParameter.PROJECT_BUILDING_NAME,
                    BuiltInParameter.PROJECT_ISSUE_DATE,
                    BuiltInParameter.PROJECT_STATUS,
                    BuiltInParameter.PROJECT_AUTHOR,
                    BuiltInParameter.PROJECT_ADDRESS,
                }) {
                    var param = pinfo.get_Parameter(paramId);
                    if (param != null) {
                        var paramValue = param.TryGetValue();
                        if (paramValue != null)
                            docProps.Add(param.Definition.Name, paramValue);
                    }
                }

                foreach (Parameter param in pinfo.Parameters)
                    if (param.Id.IntegerValue > 0) {
                        var paramValue = param.TryGetValue();
                        if (paramValue != null)
                            docProps.Add(param.Definition.Name, paramValue);
                    }
            }
            return docProps;
        }

        public override string Type => "model";

        [JsonProperty("application")]
        public string App { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("properties")]
        public Dictionary<string, object> Properties { get; set; }
    }
}
