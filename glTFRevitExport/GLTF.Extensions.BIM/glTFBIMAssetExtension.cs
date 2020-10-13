using System;
using System.Collections.Generic;

using Newtonsoft.Json;

using Autodesk.Revit.DB;

using GLTFRevitExport.Extensions;

namespace GLTFRevitExport.GLTF.Extensions.BIM {
    [Serializable]
    internal class glTFBIMAssetExtension : glTFBIMExtension {
        internal glTFBIMAssetExtension(Document d, bool includeParameters = true) : base() {
            App = getAppName(d);
            Id = getDocumentId(d).ToString();
            Title = d.Title;
            Source = d.PathName;
            if (includeParameters)
                Properties = getProjectInfo(d);
        }

        private static string getAppName(Document doc) {
            var app = doc.Application;
            var hostName = app.VersionName;
            hostName = hostName.Replace(app.VersionNumber, app.SubVersionNumber);
            return $"{hostName} {app.VersionBuild}";
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
                        var paramValue = param.ToGLTF();
                        if (paramValue != null)
                            docProps.Add(param.Definition.Name, paramValue);
                    }
                }

                foreach (Parameter param in pinfo.Parameters)
                    if (param.Id.IntegerValue > 0) {
                        var paramValue = param.ToGLTF();
                        if (paramValue != null)
                            docProps.Add(param.Definition.Name, paramValue);
                    }
            }
            return docProps;
        }

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
