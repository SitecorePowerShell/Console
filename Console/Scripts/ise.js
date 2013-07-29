
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
                "By pressing <strong>Ctrl+Enter</strong> you can see help for the command closest to the left of your cursor.",
                "Your script will start in the location/folder picked by the <strong>Content Item</strong> dropdown.",
                "You can change the color of the results dialog shown to your script users using the <strong>Console</strong> ribbon button.",
                "If you save your script in the <strong>Content Editor Context Menu</strong> it will automatically show as a context menu option for items that users Right Click in the tree and will start in the location of that item.",
                "All your scripts that share the same <strong>Persistent Session ID</strong> can re-use variables that were created by the scripts with the same session id that were run before.",
                "<strong>Runtime</strong> ribbon button is active only if you're editong a script from library. Save your script in script library to enable it.",
                "<strong>Script Library</strong> comes with a wealth of samples and useful scripts that you can base your scripts upon.",
                "You can execute your script with the <strong>Ctrl+E</strong> hotkey.",
                "You can abort a script running in ISE with the <strong>Ctrl+Shift+E</strong> hotkey.",
                "You can download files from the Website and Data folders using the <strong>Get-File</strong> commandlet.",
                "You can show Sitecore dialogs from your scripts using the <strong>Show-*</strong> commandlets."
        ];

        window.parent.focus();
        window.focus();

        function setFocusOnConsole() {
            $('body').focus();
            $('#Editor').focus();
            ('WebForm_AutoFocus' in this) && WebForm_AutoFocus && WebForm_AutoFocus('Editor');
        }

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
                    var prefixLower = prefix.toLowerCase();
                    if (prefix.length > 0) {
                        keywords = keywords.filter(function(w) {
                            var pipeIndex = w.indexOf('|', 0) + 1;
                            return w.toLowerCase().indexOf(prefixLower, 0) == pipeIndex;
                        });
                    }
                    callback(null, keywords.map(function (word) {
                        var hint = word.split('|');
                        return {
                            name: hint[1],
                            value: hint[1],
                            score: 30,
                            meta: hint[0]
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
                        width: $(window).width() * 2 / 3,
                        show: "slow",
                        hide: "slow"
                    });
                    $('#ajax-dialog').scrollTop("0");
                }
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
            left: 0,
        }).hide()
            .show("drop", { direction: "down" }, "400")
            .delay(2000)
            .hide("drop", { direction: "down" }, "400",
                function () {
                    $(".status-bar-text").animate({ backgroundColor: "#012456" }).animate({ backgroundColor: "#fff" });
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
