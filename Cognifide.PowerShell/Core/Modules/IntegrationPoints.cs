using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Sitecore.Configuration;

namespace Cognifide.PowerShell.Core.Modules
{
    public static class IntegrationPoints
    {
        public const string ContentEditorContextMenuFeature = "contentEditorContextMenu";
        public const string ContentEditorGuttersFeature = "contentEditorGutters";
        public const string ContentEditorInsertItemFeature = "contentEditorInsertItem";
        public const string ContentEditorRibbonFeature = "contentEditorRibbon";
        public const string ContentEditorWarningFeature = "contentEditorWarning";
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
        public const string WebApi = "webAPI";
        public const string PageEditorNotificationFeature = "pageEditorNotification";
        public const string IsePluginFeature = "isePlugin";
        public const string PageEditorExperienceButtonFeature = "pageEditorExperienceButton";

        private static SortedList<string, IntegrationPoint> libraries;

        public static SortedList<string, IntegrationPoint> Libraries
        {
            get
            {
                if (libraries == null)
                {
                    libraries = new SortedList<string, IntegrationPoint>(StringComparer.OrdinalIgnoreCase);
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
                            CreationScript = integrationPoint.Attributes?["creationScript"].InnerText,
                            Name = integrationPoint.Attributes?["name"].InnerText,
                            Id = integrationPoint.Name,
                            Path = integrationPoint.InnerText.Trim()
                        });
                    }
                }
                return libraries;
            }
        }
    }
}