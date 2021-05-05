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
        /// Export Revit type data
        /// </summary>
        public bool ExportHierarchy { get; set; } = true;

        /// <summary>
        /// Export linked Revit models
        /// </summary>
        public bool ExportLinkedModels { get; set; } = true;

        /// <summary>
        /// Embed linked Revit models
        /// </summary>
        public bool EmbedLinkedModels { get; set; } = false;

        /// <summary>
        /// Export Revit element parameter data
        /// </summary>
        public bool ExportParameters { get; set; } = true;

        /// <summary>
        /// Whether to embed parameter data inside glTF file or write to external file
        /// </summary>
        public bool EmbedParameters { get; set; } = false;

        /// <summary>
        /// Export Revit material data
        /// </summary>
        public bool ExportMaterials { get; set; } = false;

        /// <summary>
        /// Cancellation toke for cancelling the export progress
        /// </summary>
        public CancellationToken CancelToken;

        public Color DefaultColor = new Color(255, 255, 255);
    }
}