using System;
using System.Threading;
using Autodesk.Revit.DB;

using GLTFRevitExport.Properties;

namespace GLTFRevitExport {
    /// <summary>
    /// Export configurations
    /// </summary>
    public class GLTFExportConfigs {
        /// <summary>
        /// Id of the generator
        /// </summary>
        public string GeneratorId => StringLib.GLTFGeneratorName;

        /// <summary>
        /// Generator copyright message
        /// </summary>
        public string CopyrightMessage;

        /// <summary>
        /// Export linked Revit models
        /// </summary>
        public bool ExportLinkedModels { get; set; } = true;

        /// <summary>
        /// Export Revit type data
        /// </summary>
        public bool ExportHierarchy { get; set; } = true;

        /// <summary>
        /// Export Revit element parameter data
        /// </summary>
        public bool ExportParameters { get; set; } = true;

        /// <summary>
        /// Cancellation toke for cancelling the export progress
        /// </summary>
        public CancellationToken CancelToken;

        public Color DefaultColor = new Color(0, 0, 0);
    }
}