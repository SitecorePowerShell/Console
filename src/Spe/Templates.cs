using Sitecore.Data;

namespace Spe
{
    public class Templates
    {
        public readonly struct Script
        {
            public static readonly ID Id = new ID("{DD22F1B3-BD87-4DB2-9E7D-F7A496888D43}");

            public readonly struct Fields
            {
                public static readonly ID ScriptBody = new ID("{B1A94FF0-6897-47C0-9C51-AA6ACB80B1F0}");
                public static readonly ID ShowRule = new ID("{1C76313E-3C8C-4807-A826-135D10C39299}");
                public static readonly ID EnableRule = new ID("{F62C67B4-28B6-4A6F-91CB-DB78CF054F4E}");
                public static readonly ID PersistentSessionId = new ID("{7FA141B7-7473-44A9-9BD9-2739C51FF8DA}");
            }
        }

        public readonly struct ScriptLibrary
        {
            public static readonly ID Id = new ID("{AB154D3D-1126-4AB4-AC21-8B86E6BD70EA}");

            public readonly struct Fields
            {
                public static readonly ID ShowRule = new ID("{613D128B-7BF2-40F4-84CB-6717408F9806}");
                public static readonly ID EnableRule = new ID("{05EECF47-0BB9-4B85-B926-0BC6DD987EC8}");
            }
        }

        public readonly struct ScriptModule
        {
            public static readonly ID Id = new ID("{6D82FCD8-C379-443C-97A9-C6423C71E7D5}");
        }

        public readonly struct ScriptModuleFolder
        {
            public static readonly ID Id = new ID("{B6A55AC6-A602-4C09-AC3A-1D2938621D5B}");
        }

        public readonly struct ScriptWorkflowAction
        {
            public static readonly ID Id = new ID("{02BD31B0-CED3-46F4-AB42-11BDFD8D967C}");

            public struct Fields
            {
                public static readonly ID ScriptBody = new ID("{CD8DA5E2-3B65-4A14-B7A6-9F41181CE172}");
                public static readonly ID EnableRule = new ID("{D51CFE9B-67D9-40C3-9AA7-31456FB9AEFB}");
            }
        }

        public readonly struct Settings
        {
            public static readonly ID Id = new ID("{69316117-03A6-4679-A34A-21E8CE8701D5}");
        }

        public readonly struct SnippetDefinition
        {
            public static readonly ID Id = new ID("{B8BC40A8-1560-42C6-AA05-911C9C140AFE}");

            public struct Fields
            {
                public static readonly ID Alias = new ID("{526F4C8C-0A0D-462E-A4ED-D7635DFEDB41}");
                public static readonly ID Text = new ID("{C03734C0-EBE6-4BD0-9F5F-1EEE1F862439}");
            }
        }

        public readonly struct RestrictionProfile
        {
            public static readonly ID Id = new ID("{AF864A3C-6D3D-4889-AFEF-9B1D427F4EA8}");

            public struct Fields
            {
                public static readonly ID Enabled = new ID("{4DA86B82-4784-44E9-9491-DE6DE80E8145}");
                public static readonly ID BaseProfile = new ID("{89C20525-01B0-456D-BFFA-7317A3BFE88A}");
                public static readonly ID AdditionalBlockedCommands = new ID("{5F4BC217-69A2-4019-8631-761349A100CB}");
                public static readonly ID AdditionalAllowedCommands = new ID("{2F56DDAD-73F7-4E05-9A61-2C8C1FD97779}");
                public static readonly ID AuditLevelOverride = new ID("{C2508B30-7407-405B-A2A2-BCCE3A1E42B8}");
            }
        }

        public readonly struct TrustedScript
        {
            public static readonly ID Id = new ID("{C30C6FF9-EEF9-49C5-9787-44AAAAB70563}");

            public struct Fields
            {
                public static readonly ID Enabled = new ID("{9A64EEB5-EF6E-4F00-B26E-7DCEDDCE48B2}");
                public static readonly ID Script = new ID("{01F26B2A-C396-4B06-A826-A625B62721B5}");
                public static readonly ID AllowedProfiles = new ID("{ADBD8BBE-2C9C-41C8-935A-D43D3C8474DD}");
            }
        }

        public readonly struct RemotingApiKey
        {
            public static readonly ID Id = new ID("{55AB1AA8-890E-401E-AF06-094CA21E0E2D}");

            public struct Fields
            {
                public static readonly ID SharedSecret = new ID("{BBF52C26-7825-4F7B-88FF-2DB2785C5954}");
                public static readonly ID Enabled = new ID("{8D158FCA-E8F3-4D94-8469-C782B099EC07}");
                public static readonly ID Profile = new ID("{ECB2A0C9-3AC3-4FF8-A66C-6D4AE4AA2C21}");
                public static readonly ID ImpersonateUser = new ID("{5EB16BF4-605A-457C-8588-5D9833FF4DD9}");
                public static readonly ID RequestLimit = new ID("{33D88116-A954-4954-A94C-A7AE083BC983}");
                public static readonly ID ThrottleWindow = new ID("{9F12735C-65C2-401E-A499-3C3597452440}");
            }
        }

        public readonly struct DelegatedAccess
        {
            public static readonly ID Id = new ID("{6111D5BE-EC09-4A5C-AD27-7D8005E91216}");

            public struct Fields
            {
                public static readonly ID Enabled = new ID("{464C9789-617A-4CE6-988C-8AF421B6D385}");
                public static readonly ID ElevatedRole = new ID("{A0506748-B498-49C9-8B25-23F9030C1A98}");
                public static readonly ID ImpersonatedUser = new ID("{FF4855AB-377F-4CF5-94C7-4A8EE32D143F}");
                public static readonly ID ScriptItemId = new ID("{75560CD0-EB3F-45D4-AB41-4B37DDE1C741}");
            }
        }
    }
}