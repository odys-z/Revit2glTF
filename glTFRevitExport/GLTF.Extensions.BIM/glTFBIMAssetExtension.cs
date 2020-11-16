using System;
using System.Collections.Generic;

using Newtonsoft.Json;

using Autodesk.Revit.DB;

using GLTFRevitExport.Extensions;

namespace GLTFRevitExport.GLTF.Extensions.BIM {
    [Serializable]
    internal class GLTFBIMAssetExtension : GLTFBIMExtension {
        internal GLTFBIMAssetExtension(Document d, bool includeParameters = true, GLTFBIMPropertyContainer propContainer = null) : base() {
            App = GetAppName(d);
            Id = GetDocumentId(d).ToString();
            Title = d.Title;
            Source = d.PathName;
            if (includeParameters) {
                if (propContainer is null)
                    // embed properties
                    Properties = GetProjectInfo(d);
                else {
                    // record properties
                    propContainer.Record(Id, GetProjectInfo(d));
                    // ensure property sources list is initialized
                    if (Containers is null)
                        Containers = new List<GLTFBIMPropertyContainer>();
                    // add the new property source
                    if (!Containers.Contains(propContainer))
                        Containers.Add(propContainer);
                }
            }
        }

        private static string GetAppName(Document doc) {
            var app = doc.Application;
            var hostName = app.VersionName;
            hostName = hostName.Replace(app.VersionNumber, app.SubVersionNumber);
            return $"{hostName} {app.VersionBuild}";
        }

        private static Guid GetDocumentId(Document doc) {
            if (doc?.IsValidObject != true)
                return Guid.Empty;
            return ExportUtils.GetGBXMLDocumentId(doc);
        }

        private static Dictionary<string, object> GetProjectInfo(Document doc) {
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

        [JsonProperty("levels")]
        public List<uint> Levels { get; set; }

        [JsonProperty("grids")]
        public List<uint> Grids { get; set; }

        [JsonProperty("zones")]
        public List<uint> Zones { get; set; }

        [JsonProperty("containers")]
        public List<GLTFBIMPropertyContainer> Containers { get; set; }

        [JsonProperty("properties")]
        public Dictionary<string, object> Properties { get; set; }
    }
}
