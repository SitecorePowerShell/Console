using Sitecore.Data;

namespace Spe
{
    public class Templates
    {
        public struct Script
        {
            public static readonly ID Id = new ID("{DD22F1B3-BD87-4DB2-9E7D-F7A496888D43}");

            public struct Fields
            {
                public static readonly ID ScriptBody = new ID("{B1A94FF0-6897-47C0-9C51-AA6ACB80B1F0}");
                public static readonly ID ShowRule = new ID("{1C76313E-3C8C-4807-A826-135D10C39299}");
                public static readonly ID EnableRule = new ID("{F62C67B4-28B6-4A6F-91CB-DB78CF054F4E}");
                public static readonly ID PersistentSessionId = new ID("{7FA141B7-7473-44A9-9BD9-2739C51FF8DA}");
            }
        }

        public struct ScriptLibrary
        {
            public static readonly ID Id = new ID("{AB154D3D-1126-4AB4-AC21-8B86E6BD70EA}");

            public struct Fields
            {
                public static readonly ID ShowRule = new ID("{613D128B-7BF2-40F4-84CB-6717408F9806}");
                public static readonly ID EnableRule = new ID("{05EECF47-0BB9-4B85-B926-0BC6DD987EC8}");
            }
        }

        public struct ScriptModule
        {
            public static readonly ID Id = new ID("{6D82FCD8-C379-443C-97A9-C6423C71E7D5}");
        }

        public struct ScriptModuleFolder
        {
            public static readonly ID Id = new ID("{B6A55AC6-A602-4C09-AC3A-1D2938621D5B}");
        }

        public struct ScriptWorkflowAction
        {
            public static readonly ID Id = new ID("{02BD31B0-CED3-46F4-AB42-11BDFD8D967C}");

            public struct Fields
            {
                public static readonly ID ScriptBody = new ID("{CD8DA5E2-3B65-4A14-B7A6-9F41181CE172}");
                public static readonly ID EnableRule = new ID("{D51CFE9B-67D9-40C3-9AA7-31456FB9AEFB}");
            }
        }

        public struct Settings
        {
            public static readonly ID Id = new ID("{69316117-03A6-4679-A34A-21E8CE8701D5}");
        }

        public struct SnippetDefinition
        {
            public static readonly ID Id = new ID("{B8BC40A8-1560-42C6-AA05-911C9C140AFE}");

            public struct Fields
            {
                public static readonly ID Alias = new ID("{526F4C8C-0A0D-462E-A4ED-D7635DFEDB41}");
                public static readonly ID Text = new ID("{C03734C0-EBE6-4BD0-9F5F-1EEE1F862439}");
            }
        }
    }
}