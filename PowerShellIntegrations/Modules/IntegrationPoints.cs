using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using Sitecore.Configuration;

namespace Cognifide.PowerShell.PowerShellIntegrations.Modules
{
    public static class IntegrationPoints
    {
        public const string ContentEditorContextMenuFeature = "contentEditorContextMenu";
        public const string ContentEditorGuttersFeature = "contentEditorGutters";
        public const string ContentEditorRibbonFeature = "contentEditorRibbon";
        public const string ControlPanelFeature = "controlPanel";
        public const string FunctionsFeature = "functions";
        public const string ListViewExportFeature = "listViewExport";
        public const string ListViewRibbonFeature = "listViewRibbon";
        public const string PipelineLoggedInFeature = "pipelineLoggedIn";
        public const string PipelineLoggingInFeature = "pipelineLoggingIn";
        public const string PipelineLogoutFeature = "pipelineLogout";
        public const string ToolboxFeature = "toolbox";
        public const string StartMenuReportsFeature = "startMenuReports";
        public const string EventHandlersFeature = "eventHandlers";

        public class IntegrationPoint
        {
            public string Id;
            public string Path;
            public string Name;
            public string CreationScript;
        }
        private static SortedList<string, IntegrationPoint> libraries = null;


        public static SortedList<string, IntegrationPoint> Libraries
        {
            get
            {
                if (libraries == null)
                {
                    libraries = new SortedList<string, IntegrationPoint>();
                    var ipNode = Factory.GetConfigNode("powershell/integrationPoints");
                    if (ipNode == null)
                    {
                        return libraries;
                    }

                    var allIntegrationPoints = ipNode.Cast<XmlNode>().ToList();

                    foreach (var integrationPoint in allIntegrationPoints)
                    {
                        libraries.Add(integrationPoint.Name, new IntegrationPoint
                        {
                            CreationScript = integrationPoint.Attributes["creationScript"].InnerText,
                            Name = integrationPoint.Attributes["name"].InnerText,
                            Id = integrationPoint.Name,
                            Path = integrationPoint.InnerText
                        });
                    }
                }
                return libraries;
            }
        }
    }
}