using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Globalization;

namespace Cognifide.PowerShell
{
    [LocalizationTexts(ModuleName = "Sitecore.PowerShell.Extensions")]
    public class Texts
    {
        public const string PowerShellRunner_OnLoad_Show_copyright__ = "Show copyright..";
        public const string PowerShellRunner_OnLoad_View_script_results = "View script results";
        public const string PowerShellRunner_OnLoad_View_script_results_and_errors = "View script results and errors";
        public const string PowerShellRunner_UpdateProgress_Operation_ = "Operation:";
        public const string PowerShellRunner_UpdateProgress_Running_script___ = "Running script...";
        public const string PowerShellRunner_UpdateProgress_Status_ = "Status:";
        public const string PowerShellRunner_UpdateProgress_Time_remaining_ = "Time remaining:";

        public const string PowerShellRunner_UpdateResults_Script_finished___no_results_to_display_ =
            "Script finished - no results to display.";

        public const string PowerShellResultViewerList_UpdateProgress_remaining = "remaining";
        public const string PowerShellIse_JobExecuteScript_Please_wait___0_ = "Please wait, {0}";
        public const string PowerShellIse_JobExecuteScript_Working = "Working";
        public const string PowerShellIse_LoadItem_The_item_is_not_a_script_ = "The item is not a script.";
        public const string PowerShellIse_Open_Open_Script = "Open Script";

        public const string PowerShellIse_Open_Select_the_script_item_that_you_want_to_open_ =
            "Select the script item that you want to open.";

        public const string PowerShellIse_SaveAs_Select_Script_Library = "Select Script Library";

        public const string PowerShellIse_SaveAs_Select_the_Library_that_you_want_to_save_your_script_to_ =
            "Select the Library that you want to save your script to.";

        public const string PowerShellIse_UpdateProgress_remaining = "remaining";
        public const string PowerShellIse_UpdateRibbon_Script_defined___0_ = "Script defined: {0}";
        public const string PowerShellIse_UpdateRibbon_Single_execution = "Single execution";
        public const string IseContextPanel_Render_Context = "Context";
        public const string IseContextPanel_Render_none = "none";
        public const string IseContextPanel_Render_Session = "Session";
        public const string IseContextPanelEx_Render_Language = "Language";
        public const string IseContextPanelEx_Render_User = "User";
        public const string MruGallery_ChangeSearchPhrase_Most_Recently_opened_scripts_ =
            "Most Recently opened scripts:";
        public const string MruGallery_ChangeSearchPhrase_No_scripts_found____Do_you_need_to_re_index_your_databases_ =
            "No scripts found...Do you need to re-index your databases?";
        public const string MruGallery_ChangeSearchPhrase_Scripts_matching____0___in___1____database =
            "Scripts matching: '{0}' in '{1}*' database";
        public const string MruGallery_ChangeSearchPhrase_Scripts_matching____0___in_all_databases =
            "Scripts matching: '{0}' in all databases";
        public const string MruGallery_RenderRecent_Xml_Control___0___not_found = "Xml Control \"{0}\" not found";

        public const string
            MruGallery_OnLoad_Script_name_to_search_for___prefix_with_e_g___master___to_narrow_to_specific_database =
                "Script name to search for - prefix with e.g. 'master:' to narrow to specific database";
        public const string
            PowerShellMultiValuePrompt_AddControls_Error_while_rendering_this_editor = "Error while rendering this editor";

        public const string
            PowerShellMultiValuePrompt_GetVariableEditor_DropList_control_cannot_render_items_from_the_database___0___because_it_its_not_the_same_as___1___which_is_the_current_content_database__
                =
                "DropList control cannot render items from the database '{0}' because it its not the same as '{1}' which is the current content database .";

        public const string PowerShellMultiValuePrompt_GetVariableEditor_Edit_rule = "Edit rule";
    }
}