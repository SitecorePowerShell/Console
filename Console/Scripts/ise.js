
function onExecute() {
    document.getElementById("Result").innerHTML = "&lt;div align='Center' style='padding:32px 0px 32px 0px'&gt;Please wait... Executing script...&lt;/div&gt;&lt;img src='../../../../Console/Assets/working.gif' alt='Working'/&gt;";
}

// a convenience function for parsing string namespaces and
// automatically generating nested namespaces
function extend(e, t) { var n = t.split("."), r = e, i, s; if (n[0] == "cognifide") { n = n.slice(1) } i = n.length; for (s = 0; s < i; s++) { if (typeof r[n[s]] == "undefined") { r[n[s]] = {} } r = r[n[s]] } return r }

var cognifide = cognifide || {};
extend(cognifide, 'powershell');

(function ($, window, cognifide, ace, undefined) {
    $(function () {
        var tips = [
                "You can press <strong>Ctrl+Space</strong> to show the Auto Suggest drop down that will show you all the matching comands/parameters/files depending on your caret position",
                "You can show help by pressing <strong>Ctrl+Enter</strong> for the closest command to the left of the cursor.",
                "Your script will start in the location/folder picked by the <strong>Content Item</strong> dropdown.",
                "You can change the color of the results dialog shown to your script users using the <strong>Console</strong> ribbon button.",
                "If you save your script in the <strong>Content Editor Context Menu</strong> it will automatically show as a context menu option for items that users Right Click in the tree and will start in the location of that item.",
                "All your scripts that share the same <strong>Persistent Session ID</strong> can re-use variables that were created by the scripts with the same session id that were run before.",
                "<strong>Runtime</strong> ribbon button is active only if you're editong a script from library. Save your script in script library to enable it.",
                "<strong>Script Library</strong> comes with a wealth of samples and useful scripts that you can base your scripts upon.",
                "You can execute your script with the <strong>Ctrl+E</strong> hotkey.",
                "You can abort a script running in ISE with the <strong>Ctrl+Shift+E</strong> hotkey.",
                "You can download files from the Website and Data folders using the <strong>Get-File</strong> commandlet.",
                "You can show Sitecore dialogs from your scripts using the <strong>Show-*</strong> commandlets.",
                "You can increase the font size using the <strong>Ctrl+Alt+Shift++</strong> (plus) or <strong>Ctrl+Alt+Shift+=</strong> (equals) hotkey.",
                "You can decrease the font size using the <strong>Ctrl+Alt+Shift+-</strong> (minus) hotkey.",
                "You can search for keywords using the <strong>Ctrl+F</strong> hotkey.",
                "You can toggle a comment block using the <strong>Ctrl+Shift+/</strong> hotkey.",
                "You can toggle a comment using the <strong>Ctrl+/</strong> hotkey."
        ];

        window.parent.focus();
        window.focus();

        function setFocusOnConsole() {
            $('body').focus();
            $('#Editor').focus();
            ('WebForm_AutoFocus' in this) && WebForm_AutoFocus && WebForm_AutoFocus('Editor');
        }

        $("body").on("click", "#HelpClose", function () {
            $("#ajax-dialog").dialog("close");
        });

        setTimeout(setFocusOnConsole, 1000);

        var guid = "ECBC33D9-A623-4A97-888B-375B627B4189";

        var editor = $($('#Editor')[0]);
        editor.hide();

        // Setup the ace code editor.
        var codeeditor = ace.edit("CodeEditor");
        codeeditor.setTheme("ace/theme/powershellise");
        codeeditor.session.setMode("ace/mode/powershell");
        codeeditor.setShowPrintMargin(false);
        codeeditor.session.setValue(editor.val());
        codeeditor.session.on('change', function () {
            editor.val(codeeditor.session.getValue());
        });
        var posx = $("#PosX");
        var posy = $("#PosY");
        $('#CodeEditor').on('keyup mousedown', function () {
            var position = codeeditor.getCursorPosition();
            posx.text(position.column);
            posy.text((position.row + 1));
        });

        ace.config.loadModule("ace/ext/emmet", function () {
            ace.require("ace/lib/net").loadScript("/Console/Scripts/ace/emmet-core/emmet.js", function () {
                codeeditor.setOption("enableEmmet", true);
            });

            codeeditor.setOptions({
                enableSnippets: true,
                enableBasicAutocompletion: true
            });
        });

        ace.config.loadModule("ace/ext/language_tools", function (module) {
            codeeditor.setOptions({
                enableSnippets: true,
                enableBasicAutocompletion: true
            });

            var keyWordCompleter = {
                getCompletions: function (editor, session, pos, prefix, callback) {
                    session.$mode.$keywordList = [];

                    var range = codeeditor.getSelectionRange();
                    range.start.column = 0;
                    var line = codeeditor.session.getTextRange(range);

                    if (line) {
                        _getTabCompletions(line);
                    } else {
                        $.tabCompletions = [""];
                    }
                    var keywords = $.tabCompletions;

                    callback(null, keywords.map(function (word) {
                        var hint = word.split('|');
                        return {
                            name: hint[1],
                            value: hint[1],
                            score: 1000,
                            meta: hint[0],
                            completer: this
                    };
                    }));
                }
            };

            module.addCompleter(keyWordCompleter);
        });

        codeeditor.setAutoScrollEditorIntoView(true);

        var codeeeditorcommands = [ {
            name: "help",
            bindKey: { win: "ctrl-enter|shift-enter", mac: "ctrl-enter|command-enter", sender: 'codeeditor|cli' },
            exec: function (env, args, request) {
                var range = codeeditor.getSelectionRange();
                if (range.start.row === range.end.row && range.start.column === range.end.column) {
                    range.start.column = 0;
                }
                var command = codeeditor.session.getTextRange(range);
                if (command) {
                    _getCommandHelp(command);
                    var ajaxDialog = $('<div id="ajax-dialog"/>').html($.commandHelp).appendTo('body');
                    ajaxDialog.dialog({
                        modal: true,
                        close: function (event, ui) {
                            $(this).remove();
                        },
                        height: $(window).height() - 20,
                        width: $(window).width() * 18/20,
                        show: "slow",
                        hide: "slow"
                    });
                    $('#ajax-dialog').scrollTop("0");
                }
            },
            readOnly: true
        }, {
            name: "fontSizeIncrease",
            bindKey: {win: "Ctrl-Alt-Shift-=|Ctrl-Alt-Shift-+", mac: "Ctrl-Alt-Shift-=|Ctrl-Alt-Shift-+"},
            exec: function(editor) { 
                editor.setFontSize(Math.min(editor.getFontSize() + 1, 25)); 
            },
            readOnly: true
        }, {
            name: "fontSizeDecrease",
            bindKey: {win: "Ctrl-Alt-Shift--", mac: "Ctrl-Alt-Shift--"},
            exec: function(editor) { 
                editor.setFontSize(Math.max(editor.getFontSize() - 1, 12)); 
            },
            readOnly: true
        }];

        codeeditor.commands.addCommands(codeeeditorcommands);

        cognifide.powershell.updateEditor = function () {
            codeeditor.getSession().setValue(editor.val());
        };

        cognifide.powershell.clearEditor = function () {
            codeeditor.getSession().setValue('');
        };

        cognifide.powershell.resizeEditor = function() {
            codeeditor.resize();
        };

        cognifide.powershell.restoreResults = function () {
            $("#ResultsSplitter").show();
            $("#ResultsRow").show();
            codeeditor.resize();
        };

        cognifide.powershell.closeResults = function () {
            $("#ResultsSplitter").hide();
            $("#ResultsRow").hide("slow", function () { codeeditor.resize(); /* do something cool here? */ });
        };


        cognifide.powershell.getAutocompletionPrefix = function (text) {
            var data;
                getPowerShellResponse({ "guid": guid, "command": text }, "GetAutoCompletionPrefix",
                function(json) {
                    data = JSON.parse(json.d);
                });
            return data;
        };

        cognifide.powershell.showCommandHelp = function (command) {
            _getCommandHelp(command);
            var ajaxDialog = $('<div id="ajax-dialog"/>').html($.commandHelp).appendTo('body');
            ajaxDialog.dialog({
                modal: true,
                close: function (event, ui) {
                    $(this).remove();
                },
                height: $(window).height() - 20,
                width: $(window).width() * 18 / 20,
                show: "slow",
                hide: "slow"
            });
            $('#ajax-dialog').scrollTop("0");
            $("#HelpClose").click(function () {
                $("#HelpClose").hide("slow", function () { $("#HelpClose").remove(); });
            });
            return false;
        };

        $.commandHelp = "";
        $("#Help").dialog({ autoOpen: false });

        var tipIndex = Math.floor(Math.random() * tips.length);
        var tip = tips[tipIndex];

        $("#TipText").html(tip);
        $("#StatusTip").html(tip);

        $("#TipOfTheSession").position({
            my: "left bottom",
            at: "left bottom-30px",
            within: $("#Result"),
            of: $("#Result")
        }).css({
            right: 0,
            left: 0
        }).hide()
            .show("drop", { direction: "down" }, "400")
            .delay(2000)
            .hide("drop", { direction: "down" }, "400",
                function () {
                    $(".status-bar-text").animate({ backgroundColor: "#fcefa1" }).animate({ backgroundColor: "#fff" });
                });

        $(".status-bar-text").click(function () {
            tipIndex++;
            if (tipIndex >= tips.length) {
                tipIndex = 0;
            }
            var tip = tips[tipIndex];
            $(".status-bar-text").animate({ backgroundColor: "#012456" },
            function () {
                $("#TipText").html(tip);
                $("#StatusTip").html(tip);
            }).animate({ backgroundColor: "#fff" });

        });

        $("#ResultsClose").click(function () {
	    $("#ResultsSplitter").hide();
            $("#ResultsRow").hide( "slow", function() { codeeditor.resize(); /* do something cool here? */ } );
        });

        function _getCommandHelp(str) {
            getPowerShellResponse({ "guid": guid, "command": str }, "GetHelpForCommand",
                function (json) {
                    var data = JSON.parse(json.d);
                    $.commandHelp = data[0];
                });
        }

        function _getTabCompletions(str) {
            getPowerShellResponse({ "guid": guid, "command": str }, "CompleteAceCommand",
                function (json) {
                    var data = JSON.parse(json.d);
                    $.tabCompletions = data;
                });
        }
        function getPowerShellResponse(callData, remotefunction, doneFunction, errorFunction) {
            var datastring = JSON.stringify(callData);
            var ajax =
                $.ajax({
                    type: "POST",
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    url: "/Console/Services/PowerShellWebService.asmx/" + remotefunction,
                    data: datastring,
                    processData: false,
                    cache: false,
                    async: false
                }).done(doneFunction)
                  .fail(errorFunction);
        }
    });
}(jQuery, window, window.cognifide = window.cognifide || {}, window.ace = window.ace || {}));
