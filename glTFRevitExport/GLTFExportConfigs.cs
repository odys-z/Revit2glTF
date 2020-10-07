using System;
using System.Threading;
using Autodesk.Revit.DB;

namespace GLTFRevitExport {
    /// <summary>
    /// Up direction for geometry export
    /// </summary>
    public enum UpDirection {
        Y,
        Z
    }

    /// <summary>
    /// Export configurations
    /// </summary>
    public class GLTFExportConfigs {
        /// <summary>
        /// Export all buffers into a single binary file
        /// </summary>
        public bool UseSingleBinary { get; set; } = true;

        /// <summary>
        /// Export linked Revit models
        /// </summary>
        public bool ExportLinkedModels { get; set; } = true;

        /// <summary>
        /// Export all the properties for each element
        /// </summary>
        public bool ExportProperties { get; set; } = true;

        /// <summary>
        /// Export all the type properties for each element type
        /// </summary>
        public bool ExportTypeProperties { get; set; } = true;

        /// <summary>
        /// Up direction
        /// </summary>
        public UpDirection UpAxis { get; set; } = UpDirection.Y;

        /// <summary>
        /// Filter to filter the scene data. Only elements passing this filter
        /// will be included in the export
        /// </summary>
        public ElementFilter Filter;

        public CancellationToken CancelToken;
    }
}