using Sitecore.Globalization;

namespace Spe
{
    [LocalizationTexts(ModuleName = "Sitecore.PowerShell.Extensions")]
    public class Texts
    {
        public const string General_Operation_failed_wrong_data_template = "Script cannot be executed as it is of a wrong data template!";

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

        public const string PowerShellResultViewerList_datamissing =
            "The data for the dialog is not available. Either your server was restarted or the server cache was flushed. If this is a recurring problem, contact your system administrator.";
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

        public const string MruGallery_ChangeSearchPhrase_Most_Recently_opened_scripts_ =
            "Most Recently opened scripts:";
        public const string MruGallery_ChangeSearchPhrase_No_scripts_found____Do_you_need_to_re_index_your_databases_ =
            "No scripts found...Do you need to re-index your databases?";
        public const string MruGallery_ChangeSearchPhrase_Scripts_matching____0___in___1____database =
            "Scripts matching: '{0}' in '{1}*' database";
        public const string MruGallery_ChangeSearchPhrase_Scripts_matching____0___in_all_databases =
            "Scripts matching: '{0}' in all databases";

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
        public const string PowerShellMultiValuePrompt_EditCondition_Please_select_a_rule = "Please select a rule";
        public const string PowerShellMultiValuePrompt_GetCheckboxControl_Select_all = "Select all";
        public const string PowerShellMultiValuePrompt_GetCheckboxControl_Unselect_all = "Unselect all";
        public const string PowerShellMultiValuePrompt_GetCheckboxControl_Invert_selection = "Invert selection";
        public const string PowerShellMultiValuePrompt_InsecureData_error = "One or more field values include insecure data.";

        public const string PowerShellSessionElevation_Execution_prevented = "Execution prevented!";

        public const string PowerShellSessionElevation_Operation_requires_elevation =
            "Operation cannot be performed due to session elevation restrictions. Elevate your session and try again.";
        public const string PowerShellSessionElevation_Could_not_validate = "Could not validate access using the provided credentials.";

        public const string DownloadFile_No_file_attached = "There is no file attached.";
        public const string DownloadFile_Files_outside_Sitecore_cannot_be_downloaded = "Files from outside of the Sitecore Data and Website folders cannot be downloaded.\n\n" +
                                                                                       "Copy the file to the Sitecore Data folder and try again.";

        public const string PowerShellScriptBrowser_Select_a_script = "Select a script you want to open.";
        public const string PowerShellScriptBrowser_Select_a_library = "Select a library where you want your script saved and specify a name for your script.";
        public const string PowerShellScriptBrowser_Specify_a_name = "Specify a name for your script.";
        public const string PowerShellScriptBrowser_Are_you_sure_you_want_to_overwrite = "Are you sure you want to overwrite the selected script?";
        public const string PowerShellScriptBrowser_Script_with_name_already_exists = "Script with that name already exists, are you sure you want to overwrite the script?";

        public const string SessionIDGallery_ID = "ID: <b>{0}</b>";
        public const string SessionIDGallery_Location = "Location: <b>{0}</b>";
        public const string SessionIDGallery_User = "User: <b>{0}</b>";

        public const string TemplateMissing = "The template with ID {0} is missing in the {1} database.";
    }
}