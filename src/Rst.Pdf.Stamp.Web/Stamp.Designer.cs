﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Rst.Pdf.Stamp.Web {
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [CompilerGenerated()]
    public class Stamp {
        
        private static System.Resources.ResourceManager resourceMan;
        
        private static System.Globalization.CultureInfo resourceCulture;
        
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Stamp() {
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static System.Resources.ResourceManager ResourceManager {
            get {
                if (object.Equals(null, resourceMan)) {
                    System.Resources.ResourceManager temp = new System.Resources.ResourceManager("WebApplication.Stamp", typeof(Stamp).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        public static string Title {
            get {
                return ResourceManager.GetString("Title", resourceCulture);
            }
        }
        
        public static string Summary {
            get {
                return ResourceManager.GetString("Summary", resourceCulture);
            }
        }
        
        public static string Certificate {
            get {
                return ResourceManager.GetString("Certificate", resourceCulture);
            }
        }
        
        public static string Owner {
            get {
                return ResourceManager.GetString("Owner", resourceCulture);
            }
        }
        
        public static string Issuer {
            get {
                return ResourceManager.GetString("Issuer", resourceCulture);
            }
        }
        
        public static string Organization {
            get {
                return ResourceManager.GetString("Organization", resourceCulture);
            }
        }
        
        public static string Period {
            get {
                return ResourceManager.GetString("Period", resourceCulture);
            }
        }
        
        public static string PeriodFormat {
            get {
                return ResourceManager.GetString("PeriodFormat", resourceCulture);
            }
        }
    }
}