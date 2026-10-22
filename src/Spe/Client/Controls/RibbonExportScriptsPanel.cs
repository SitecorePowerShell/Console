using System;
using System.Linq;
using System.Web.UI;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Resources;
using Sitecore.Rules;
using Sitecore.Shell.Applications.ContentEditor.Galleries;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Shell.Web.UI.WebControls;
using Sitecore.Text;
using Sitecore.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.WebControls.Ribbons;
using Spe.Commands.Interactive;
using Spe.Core.Modules;
using Spe.Core.Settings;
using Spe.Core.Utility;
using Control = Sitecore.Web.UI.HtmlControls.Control;

namespace Spe.Client.Controls
{
    // Renders all exporters as one split button: the top runs the last used exporter,
    // the dropdown opens Gallery.ExportGallery with the full list.
    public class RibbonExportScriptsPanel : RibbonPanel
    {
        public const string LastExportRegistryKey = "/Current_User/SPE/ListView/LastExport";

        public override void Render(HtmlTextWriter output, Ribbon ribbon, Item button, CommandContext context)
        {
            var viewName = context.Parameters["viewName"];
            var ruleContext = new RuleContext
            {
                Item = context.CustomData as Item
            };
            ruleContext.Parameters["ViewName"] = viewName;

            ShowListViewFeatures features;
            var showShared = Enum.TryParse(context.Parameters["features"] ?? "", out features) &&
                             features.HasFlag(ShowListViewFeatures.SharedExport);

            var scripts = ModuleManager.GetFeatureRoots(IntegrationPoints.ReportExportFeature)
                .SelectMany(parent => parent.Children)
                .Where(scriptItem => RulesUtils.EvaluateRulesForView(scriptItem[FieldNames.ShowRule], ruleContext, !showShared))
                .ToList();
            if (scripts.Count == 0)
            {
                return;
            }

            var notRunning = context.Parameters["ScriptRunning"] == "0";
            var disabled = scripts
                .Where(scriptItem => !notRunning ||
                                     !RulesUtils.EvaluateRules(scriptItem[FieldNames.EnableRule], context.CustomData as Item))
                .ToList();

            var lastExport = Registry.GetString(LastExportRegistryKey);
            var current = scripts.FirstOrDefault(s => s.ID.ToString() == lastExport) ?? scripts[0];
            var enabled = !disabled.Contains(current);

            var galleryUrl = new UrlString();
            galleryUrl.Append("current", current.ID.ToString());
            galleryUrl.Append("exporters", string.Join("|", scripts.Select(s => s.Uri.ToString())));
            galleryUrl.Append("disabled", string.Join("|", disabled.Select(s => s.Uri.ToString())));

            var comboButton = new LargeComboButton
            {
                // Stable ID so a resized gallery keeps its size across ribbon re-renders
                ID = "ExportResultsButton",
                Header = Translate.Text(current.DisplayName),
                Icon = enabled
                    ? Images.GetThemedImageSource(current[Sitecore.FieldIDs.Icon], ImageDimension.id32x32)
                    : current[Sitecore.FieldIDs.Icon],
                ToolTip = current.Appearance.ShortDescription,
                Command = $"export:results(scriptDb={current.Database.Name},scriptID={current.ID})",
                Enabled = enabled,
                Gallery = "Gallery.ExportGallery",
                GalleryUrl = galleryUrl.ToString()
            };
            var width = "200";
            // Header and rows are 39px each in Gallery.css, plus the grip; open without a scrollbar
            var height = (39 + scripts.Count * 39 + 30).ToString();
            GalleryManager.GetGallerySize(comboButton.ID + "_frame", ref width, ref height);
            comboButton.GalleryWidth = width;
            comboButton.GalleryHeight = height;
            comboButton.RenderControl(output);
        }
    }
}
