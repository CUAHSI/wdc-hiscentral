﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18034
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MultiEndpoint.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "11.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.ConnectionString)]
        [global::System.Configuration.DefaultSettingValueAttribute("Data Source=WIN-QQF4QFO5REO\\HISCENTRAL;Initial Catalog=hiscentral;Persist Securit" +
            "y Info=True;User ID=sa;Password=r3tn3CDW")]
        public string hiscentralConnectionString {
            get {
                return ((string)(this["hiscentralConnectionString"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.ConnectionString)]
        [global::System.Configuration.DefaultSettingValueAttribute("Data Source=WIN-QQF4QFO5REO\\HISCENTRAL;Initial Catalog=OD_1_1;Persist Security In" +
            "fo=True;User ID=sa;Password=r3tn3CDW")]
        public string OD_1_1ConnectionString1 {
            get {
                return ((string)(this["OD_1_1ConnectionString1"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.ConnectionString)]
        [global::System.Configuration.DefaultSettingValueAttribute("Data Source=WIN-QQF4QFO5REO\\HISCENTRAL;Initial Catalog=HealthQuery;Persist Securi" +
            "ty Info=True;User ID=sa;Password=r3tn3CDW")]
        public string HealthQueryConnectionString {
            get {
                return ((string)(this["HealthQueryConnectionString"]));
            }
        }
    }
}
