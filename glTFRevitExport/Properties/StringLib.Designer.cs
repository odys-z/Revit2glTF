﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GLTFRevitExport.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class StringLib {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal StringLib() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("GLTFRevitExport.Properties.StringLib", typeof(StringLib).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Array is not vector3 data.
        /// </summary>
        internal static string ArrayIsNotVector3Data {
            get {
                return ResourceManager.GetString("ArrayIsNotVector3Data", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to EXT_bim_metadata.
        /// </summary>
        internal static string GLTFExtensionName {
            get {
                return ResourceManager.GetString("GLTFExtensionName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to GLTFRevitExport.
        /// </summary>
        internal static string GLTFGeneratorName {
            get {
                return ResourceManager.GetString("GLTFGeneratorName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Item does not exist in this listed tree.
        /// </summary>
        internal static string ItemNotExist {
            get {
                return ResourceManager.GetString("ItemNotExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Node does not exist.
        /// </summary>
        internal static string NodeNotExist {
            get {
                return ResourceManager.GetString("NodeNotExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There are not active nodes in this container.
        /// </summary>
        internal static string NoParentNode {
            get {
                return ResourceManager.GetString("NoParentNode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There are no primitives with given index is this node&apos;s mesh.
        /// </summary>
        internal static string NoParentPrimitive {
            get {
                return ResourceManager.GetString("NoParentPrimitive", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There are no active scenes in this container.
        /// </summary>
        internal static string NoParentScene {
            get {
                return ResourceManager.GetString("NoParentScene", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Exporting non-3d views are not supported in this Revit version.
        /// </summary>
        internal static string NoSupportedView {
            get {
                return ResourceManager.GetString("NoSupportedView", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} Root.
        /// </summary>
        internal static string SceneRootNodeName {
            get {
                return ResourceManager.GetString("SceneRootNodeName", resourceCulture);
            }
        }
    }
}
