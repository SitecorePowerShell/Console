(function($, window, spe, ace, undefined) {
    $(function() {
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
            "You can download files from the Website and Data folders using the <strong>Send-File</strong> command.",
            "You can show Sitecore dialogs from your scripts using the <strong>Show-*</strong> cmdlets.",
            "You can increase the font size using the <strong>Ctrl+Alt+Shift++</strong> (plus) or <strong>Ctrl+Alt+Shift+=</strong> (equals) hotkey.",
            "You can decrease the font size using the <strong>Ctrl+Alt+Shift+-</strong> (minus) hotkey.",
            "You can search for keywords using the <strong>Ctrl+F</strong> hotkey.",
            "You can toggle a comment block using the <strong>Ctrl+Shift+/</strong> hotkey.",
            "You can toggle a comment using the <strong>Ctrl+/</strong> hotkey.",
			"You can find more documentation in the Sitecore PowerShell Extensions <a href='https://doc.sitecorepowershell.com/' target='_blank'>book</a>."
        ];

        var TokenTooltip = ace.require("tooltip").TokenTooltip;
        var guid = "ISE_Editing_Session";
        const speVariablesCacheKey = "spe::variables";
        var debugLine = 0;
        var debugSessionId = "";
        var debugMarkers = [];
        var resultsBottomOffset = 10;
        var typingTimer;
        
        var editor = $($("#Editor")[0]);
        editor.hide();

        // Setup the ace code editor.
        var codeeditor = ace.edit("CodeEditor");
        codeeditor.setTheme("ace/theme/powershellise");
        codeeditor.session.setMode("ace/mode/powershell");
        codeeditor.setShowPrintMargin(false);
        codeeditor.session.setValue(editor.val());
        codeeditor.session.on("change", function () {
            editor.val(codeeditor.session.getValue());
        });
        codeeditor.tokenTooltip = new TokenTooltip(codeeditor);

        addProxy(codeeditor, "onPaste", function () {
            spe.updateRibbon();
        });

        addProxy(scForm, "invoke", function (args) {
            if (args[0] === "ise:immediatewindow") {
                clearVariablesCache();
            }
        });
		
        function registerEventListenersForRibbonButtons() {
            console.log('initialize');
            [].forEach.call(document.querySelectorAll('.scRibbonToolbarSmallGalleryButton, .scRibbonToolbarLargeComboButtonBottom'), function (div) {
                div.addEventListener("click",
                    function() {
                        clearTimeout(typingTimer);
                    });
            });

            [].forEach.call(document.querySelectorAll('.scRibbonNavigatorButtonsGroupButtons > a'), function (div) {
                div.addEventListener("click", function () {
                    spe.updateRibbon();
                })
            });
        }

        registerEventListenersForRibbonButtons();
		
        window.parent.focus();
        window.focus();

        function addProxy(obj, functionName, proxyFn) {
            var proxied = obj[functionName];
            obj[functionName] = function () {
                var result = proxied.apply(this, arguments);
                proxyFn(arguments);
                return result;
            };
        }

        function setFocusOnConsole() {
            $("body").focus();
            $(codeeditor).focus();
            ("WebForm_AutoFocus" in this) && WebForm_AutoFocus && WebForm_AutoFocus("CodeEditor");
        }

        window.addEventListener("focus", function(event) {
            setFocusOnConsole();
        }, false);

        function getQueryStringValue(key) {
            key = key.replace(/[*+?^$.\[\]{}()|\\\/]/g, "\\$&");
            var match = location.search.match(new RegExp("[?&]"+key+"=([^&]+)(&|$)"));
            return match && decodeURIComponent(match[1].replace(/\+/g, " "));
        }

        if(getQueryStringValue("sc_bw") === "1"){
            //$("#RibbonPanel").css("padding-top","50px");
            $("#Wrapper").css("padding-top","0px");
        }

        setTimeout(setFocusOnConsole, 1000);        

        spe.updateRibbon = function () {
            if (!codeeditor.getReadOnly()) {
                scForm.postRequest("", "", "", "ise:scriptchanged(modified=" + !codeeditor.session.getUndoManager().isClean() + ")");
                registerEventListenersForRibbonButtons();
            }
        };

        spe.updateRibbonNeeded = function () {
            clearTimeout(typingTimer);
            var timeout = 2000;
            if (document.querySelector('.scGalleryFrame') != null) {
                var timeout = 20;
            }
            typingTimer = setTimeout(spe.updateRibbon, timeout);
        };

        var posx = $("#PosX");
        var posy = $("#PosY");
        $("#CodeEditor").on("keyup mousedown", function() {
            var position = codeeditor.getCursorPosition();
            posx.text(position.column);
            posy.text((position.row + 1));
            spe.updateRibbonNeeded();
        });

        $("#CodeEditor").on("keyup mouseup", function() {
            var range = codeeditor.getSelectionRange();
            $("#SelectionText")[0].value = codeeditor.session.getTextRange(range);
		});

        $("#CopyResultsToClipboard").on("click", function() {
			clipboard.copy(spe.getOutput());
		});

        ace.config.loadModule("ace/ext/emmet", function() {
            ace.require("ace/lib/net").loadScript("/sitecore modules/PowerShell/Scripts/ace/emmet-core/emmet.js", function() {
                codeeditor.setOption("enableEmmet", true);
            });

            codeeditor.setOptions({
                enableSnippets: true,
                enableBasicAutocompletion: true
            });
        });

        ace.config.loadModule("ace/ext/language_tools", function(module) {
            codeeditor.setOptions({
                enableSnippets: true,
                enableBasicAutocompletion: true
            });

            var keyWordCompleter = {
                insertMatch: function(editor) {

                    var data = editor.completer.popup.getData(editor.completer.popup.getRow());

                    var ranges = editor.selection.getAllRanges();
                    for (var i = 0, range; range = ranges[i]; i++) {
                        if (data.meta === "Signature") {
                            data.value = data.fullValue;
                        } else if (data.meta === "Type") {
                            while (range.start.column > 0 && codeeditor.session.getTextRange(range).lastIndexOf("[") !== 0) {
                                range.start.column--;
                            }
                            range.start.column++;
                            data.value = data.fullValue;
                        } else if (data.meta === "Item" || data.meta === "ProviderItem" || data.meta === "ProviderContainer") {
                            range.start.column = data.position;
                            data.value = data.fullValue;

                            //try trim prefix quotes
                            range.start.column--;
                            range.end.column++;
                            var replacedText = codeeditor.session.getTextRange(range);
                            var charStart = replacedText.charAt(0);
                            var charEnd = replacedText.charAt(replacedText.length-1);
                            if (charStart !== '"' && charStart !== "'") {
                                range.start.column++;
                            }

                            //try trim trailing quotes
                            if (charEnd !== '"' && charEnd !== "'") {
                                range.end.column--;
                            }
                        } else {
                            range.start.column -= editor.completer.completions.filterText.length;
                        }
                        editor.session.remove(range);
                    }

                    editor.execCommand("insertstring", data.value || data);
                    $.lastPrefix = "";

                },
                getCompletions: function(editor, session, pos, prefix, callback) {
                    session.$mode.$keywordList = [];

                    var range = codeeditor.getSelectionRange();
                    range.start.column = 0;
                    var line = codeeditor.session.getTextRange(range);

                    if (line) {

                        if (!$.tabCompletions || !$.lastPrefix || $.lastPrefix.length === 0 || prefix.indexOf($.lastPrefix) !== 0) {
                            $.lastPrefix = prefix;
                            _getTabCompletions(line);
                        }
                    } else {
                        $.tabCompletions = [""];
                    }
                    var keywords = $.tabCompletions;

                    if (keywords && keywords.length > 0 && keywords[0].indexOf("Signature", 0) === 0) {

                        callback(null, []);
                        $.tabCompletions = null;

                        var msgType = "information";
                        if (keywords.length === 1 && keywords[0].indexOf("not found in session", 0) > 0) {
                            msgType = "error";
                        }

                        session.setAnnotations(keywords.map(function (word) {
                            var hint = word.split("|");
                            return {
                                row: pos.row,
                                column: pos.column,
                                text: hint[3],
                                type: msgType // error, warning or information
                            };
                        }));
                        ace.config.loadModule("ace/ext/error_marker", function (module) {
                            module.showErrorMarker(codeeditor, -1);
                        });
                        return;
                    }

                    var psCompleter = this;
                    callback(null, keywords.map(function(word) {
                        var hint = word.split("|");
                        return {
                            name: hint[1],
                            value: hint[1],
                            score: 1000,
                            meta: hint[0],
                            position: hint[2],
                            fullValue: hint[3],
                            completer: psCompleter
                        };
                    }));
                }
            };

            module.addCompleter(keyWordCompleter);
        });


        codeeditor.setAutoScrollEditorIntoView(true);

        codeeditor.on("guttermousedown", function(editor) {
            var target = editor.domEvent.target;
            if (target.className.indexOf("ace_gutter-cell") === -1)
                return;

            if (editor.clientX > 25 + target.getBoundingClientRect().left)
                return;

            var currRow = editor.getDocumentPosition().row;
            spe.breakpointSet(currRow, "toggle");
            editor.stop();
            var sparseKeys = Object.keys(editor.editor.session.getBreakpoints());
            $("#Breakpoints")[0].value  = sparseKeys.toString();
        });

        codeeditor.on("input", function () {
            spe.updateModificationFlag(false);
        });

        var codeeeditorcommands = [
            {
                name: "help",
                bindKey: { win: "ctrl-enter|shift-enter", mac: "ctrl-enter|command-enter", sender: "codeeditor|cli" },
                exec: function(env, args, request) {
                    var range = codeeditor.getSelectionRange();
                    if (range.start.row === range.end.row && range.start.column === range.end.column) {
                        range.start.column = 0;
                    }
                    var command = codeeditor.session.getTextRange(range);
                    if (command) {
                        spe.showCommandHelp(command);
                    }
                },
                readOnly: true
            }, {
                name: "fontSizeIncrease",
                bindKey: { win: "Ctrl-Alt-Shift-=|Ctrl-Alt-Shift-+", mac: "Ctrl-Alt-Shift-=|Ctrl-Alt-Shift-+" },
                exec: function(editor) {
                    spe.changeFontSize(editor.getFontSize() + 1);
                },
                readOnly: true
            }, {
                name: "fontSizeDecrease",
                bindKey: { win: "Ctrl-Alt-Shift--", mac: "Ctrl-Alt-Shift--" },
                exec: function(editor) {
                    spe.changeFontSize(Math.max(editor.getFontSize() - 1, 8));
                },
                readOnly: true
            }, {
                name: "setDebugPoint",
                bindKey: { win: "F8", mac: "F8" },
                exec: function (editor) {
                    var currRow = editor.selection.getCursor().row;
                    spe.breakpointSet(currRow, "toggle");
                },
                readOnly: true
            }
        ];

		codeeditor.commands.addCommands(codeeeditorcommands);

        spe.getOutput = function() {
            return $("#ScriptResultCode")[0].innerText;
        };

        spe.changeFontSize = function(setting) {
            setting = parseInt(setting) || 12;
            codeeditor.setOption("fontSize", setting);
            $("#ScriptResult").css({ "font-size": setting + "px" });
        };

        spe.changeLiveAutocompletion = function(liveAutocompletion) {
            codeeditor.setOptions({
                enableLiveAutocompletion: liveAutocompletion
            });
        };

        spe.clearOutput = function() {
            $("#ScriptResultCode").text("");
            $("#Result").scrollTop($("#Result")[0].scrollHeight);
            clearVariablesCache();
        };

        spe.appendOutput = function(outputToAppend) {
            var decoded = $("<div/>").html(outputToAppend).text();
            $("#ScriptResultCode").append(decoded);
            $("#Result").scrollTop($("#Result")[0].scrollHeight);
            clearVariablesCache();
        };

        spe.changeFontFamily = function(setting) {
            setting = setting || "Monaco";
            codeeditor.setOption("fontFamily", setting);
            document.getElementById("ScriptResult").style.fontFamily = setting;
        };

        spe.changeBackgroundColor = function (setting) {
            $("#ScriptResult").css({ "background-color": setting });
            $("#Result").css({ "background-color": setting });
        };

        spe.changeSettings = function(fontFamily, fontSize, backgroundColor, bottomOffset, liveAutocompletion) {
            spe.changeBackgroundColor(backgroundColor);
            spe.changeFontFamily(fontFamily);
            spe.changeFontSize(fontSize);
            if (liveAutocompletion !== undefined) {
                spe.changeLiveAutocompletion(liveAutocompletion);
            }
            resultsBottomOffset = bottomOffset;
        };

        spe.changeSessionId = function (sessionId) {
            guid = sessionId;
        };

        spe.debugStart = function(sessionId) {
            codeeditor.setReadOnly(true);
        };

        spe.debugStop = function (sessionId) {
            setTimeout(spe.breakpointHandled, 100);
            codeeditor.setReadOnly(false);
        };

        spe.updateModificationFlag = function (clear) {
            if (clear) {
                codeeditor.getSession().getUndoManager().markClean();
            }
            var scriptModified = $("#scriptModified", window.parent.document);
            if (codeeditor.getSession().getUndoManager().isClean())
                scriptModified.hide();
            else
                scriptModified.show();
        };

        spe.toggleBreakpoint = function(row, set) {
            if (set) {
                codeeditor.session.setBreakpoint(row);
            } else {
                codeeditor.session.clearBreakpoint(row);
            }
            scForm.postRequest("", "", "", "ise:togglebreakpoint(line=" + row + ",state=" + set + ")");
        };

        spe.breakpointSet = function(row, action) {
            if (action === "toggle") {
                if (codeeditor.session.getBreakpoints()[row] === "ace_breakpoint") {
                    spe.toggleBreakpoint(row, false);
                } else {
                    spe.toggleBreakpoint(row, true);
                }
            } else if (action === "Set" || action === "Enabled") {
                if (codeeditor.session.getBreakpoints()[row] !== "ace_breakpoint") {
                    spe.toggleBreakpoint(row, true);
                }
            } else if (action === "Removed" || action === "Disabled") {
                if (codeeditor.session.getBreakpoints()[row] === "ace_breakpoint") {
                    spe.toggleBreakpoint(row, false);
                }
            }
        };

        spe.breakpointHit = function (line, column, endLine, endColumn, sessionId) {
            debugLine = line;
            debugSessionId = sessionId;
            scContent.ribbonNavigatorButtonClick(this, event, "PowerShellRibbon_Strip_DebugStrip");
            var Range = ace.require("ace/range").Range;
            setTimeout(function () {
                debugMarkers.push(codeeditor.session.addMarker(new Range(line, column, endLine, endColumn + 1), "breakpoint", "text"));
            }, 100);

        };

        spe.breakpointHandled = function() {
            while (debugMarkers.length > 0) {
                codeeditor.session.removeMarker(debugMarkers.shift());
            }
            scContent.ribbonNavigatorButtonClick(this, event, "PowerShellRibbon_Strip_ImageStrip");
        };

        scForm.postRequest("", "", "", "ise:updatesettings");

        spe.updateEditor = function () {
            codeeditor.getSession().setValue(editor.val());
            spe.clearBreakpoints();
        };

        spe.insertEditorContent = function (text) {
            var position = codeeditor.getCursorPosition();
            codeeditor.getSession().insert(position, text);
            spe.clearBreakpoints();
        };

        spe.scriptExecutionEnded = function () {
            if (spe.preventCloseWhenRunning) {
                spe.preventCloseWhenRunning(false);
            }
            clearVariablesCache();
        };

        spe.clearBreakpoints = function() {
            var breakPoints = Object.keys(codeeditor.session.getBreakpoints());
            var bpCount = breakPoints.length;
            for (var i = 0; i < bpCount; i++) {
                codeeditor.session.clearBreakpoint(breakPoints[i]);
            }
        };

        function escapeRegExp(string) {
            return string.replace(/([.*+?^=!:${}()|\[\]\/\\])/g, "\\$1");
        }

        function replaceAll(string, find, replace) {
            return string.replace(new RegExp(escapeRegExp(find), "g"), replace);
        }

        spe.changeWindowTitle = function(newTitle, clearWindow) {
            if (clearWindow) {
                codeeditor.getSession().setValue("");
            }
            newTitle = replaceAll(newTitle, "/", "</i> / <i style=\"font-style: italic; color: #bbb;\">");
            var windowCaption = $("#WindowCaption", window.parent.document);
            if (windowCaption.length > 0) {
                windowCaption[0].innerHTML = "<i style=\"font-style: italic; color: #bbb;\">" + newTitle + "</i> <span id=\"scriptModified\" style=\"display:none;color:#fc2929;\">(*)</span> - ";
            }
            codeeditor.getSession().getUndoManager().markClean();
            spe.clearBreakpoints();
        };

        spe.resizeEditor = function() {
            codeeditor.resize();
            var resultsHeight =$(window).height() -$("#ResultsSplitter").offset().top - $("#ResultsSplitter").height() - $("#StatusBar").height() - resultsBottomOffset - 10;
	        $("#Result").height(resultsHeight);
            $("#Result").width($(window).width() - $("#Result").offset().left * 2);
            $("#ProgressOverlay").css("top",($("#Result").offset().top+4)+"px");
            $("#ResultsClose").css("top", ($("#Result").offset().top + 4) + "px");
            $("#ResultsClose").css("right", 15 + "px");
        };

        function isEmpty(val) {
            return (val === undefined || val == null || val.length <= 0) ? true : false;
        }

        spe.showInfoPanel = function(showPanel, updateFromMessage) {
            if (showPanel) {
                $("#InfoPanel").css("display", "block");
            } else {
                $("#InfoPanel").css("display", "none");
            }
            spe.resizeEditor();
            if (!isEmpty(updateFromMessage)) {
                scForm.invoke(updateFromMessage + "(elevationResult=1)");
            }
        };

        spe.requestElevation = function() {
            scForm.postRequest("", "", "", "ise:requestelevation");
        };

        spe.restoreResults = function() {
            $("#ResultsSplitter").show();
            $("#ResultsRow").show();
            codeeditor.resize();
        };

        spe.closeResults = function() {
            $("#ResultsSplitter").hide();
            $("#ResultsRow").hide("slow", function() {
                codeeditor.resize();
            });
        };

        function getCachedVariableValue(storageKey, variableName) {
            var storageRawValue = localStorage[storageKey];
            if (storageRawValue) {
                var variables = JSON.parse(storageRawValue);
                var variable = variables[variableName];
                if (variable) {
                    return variable;
                }
            }
            return null;
        }

        function setCachedVariableValue(storageKey, variableName, data) {
            var storageRawValue = localStorage[storageKey];
            var variables = null;
            if (storageRawValue) {
                variables = JSON.parse(storageRawValue);
            }

            if (variables === null) {
                variables = {};
            }

            variables[variableName] = data;
            localStorage[storageKey] = JSON.stringify(variables);
        }

        function clearVariablesCache() {
            for (var key in localStorage) {
                if (key.startsWith(speVariablesCacheKey)) {
                    localStorage.removeItem(key);
                }
            }
        };

        spe.variableValue = function (variableName) {
            var data;
            var sessionId = debugSessionId;
            if (!debugSessionId || 0 === debugSessionId.length) {
                sessionId = guid;
            }

            var storageKey = speVariablesCacheKey + sessionId;
            var cachedValue = getCachedVariableValue(storageKey, variableName)
            if (cachedValue) {
                return cachedValue;
            }

            getPowerShellResponse({ "guid": sessionId, "variableName": variableName }, "GetVariableValue",
                function (json) {
                    data = json.d;
                });

            setCachedVariableValue(storageKey, variableName, data);
            return data;
        };

        spe.getAutocompletionPrefix = function(text) {
            var data;
            getPowerShellResponse({ "guid": guid, "command": text }, "GetAutoCompletionPrefix",
                function(json) {
                    data = JSON.parse(json.d);
                });
            return data;
        };

        spe.showCommandHelp = function(command) {
            _getCommandHelp(command);
            if (spe.ajaxDialog)
                spe.ajaxDialog.remove();
            spe.ajaxDialog = $("<div id=\"ajax-dialog\"/>").append("<div id=\"HelpClose\">X</div>").append($.commandHelp).appendTo("body");
            spe.ajaxDialog.dialog({
                modal: true,
                open: function () {
                    $(this).scrollTop("0");

                    $("#HelpClose, .ui-widget-overlay").on("click", function () {
                        spe.ajaxDialog.dialog("close");
                    });
                },
                close: function(event, ui) {
                    $(this).remove();
                },
                height: $(window).height() - 20,
                width: $(window).width() * 18 / 20,
                show: "slow",
                hide: "slow"
            });

            return false;
        };

        $.commandHelp = "";

        spe.changeWindowTitle($("#ScriptName")[0].innerHTML, false);
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
                function() {
                    $(".status-bar-text").animate({ backgroundColor: "#fcefa1" }).animate({ backgroundColor: "#fff" });
                });

        $(".status-bar-text").click(function() {
            tipIndex++;
            if (tipIndex >= tips.length) {
                tipIndex = 0;
            }
            var nextTip = tips[tipIndex];
            $(".status-bar-text").animate({ backgroundColor: "#012456" },
                function() {
                    $("#TipText").html(nextTip);
                    $("#StatusTip").html(nextTip);
                }).animate({ backgroundColor: "#fff" });

        });

        $("#ResultsClose").click(function() {
            $("#ResultsSplitter").hide();
            $("#ResultsRow").hide("slow", function() {
                codeeditor.resize(); /* do something cool here? */
            });
        });

        function _getCommandHelp(str) {
            getPowerShellResponse({ "guid": guid, "command": str }, "GetHelpForCommand",
                function(json) {
                    var data = JSON.parse(json.d);
                    $.commandHelp = data[0];
                });
        }

        function _getTabCompletions(str) {
            getPowerShellResponse({ "guid": guid, "command": str }, "CompleteAceCommand",
                function(json) {
                    var data = JSON.parse(json.d);
                    $.tabCompletions = data;
                });
        }

        function getPowerShellResponse(callData, remotefunction, doneFunction, errorFunction) {
            if(remotefunction != "GetVariableValue"){
                spe.requestElevation();
            };
            var datastring = JSON.stringify(callData);
            $.ajax({
                    type: "POST",
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    url: "/sitecore modules/PowerShell/Services/PowerShellWebService.asmx/" + remotefunction,
                    data: datastring,
                    processData: false,
                    cache: false,
                    async: false
                }).done(doneFunction)
                .fail(errorFunction);
        }

	$(window).on('resize', function(){
	    spe.resizeEditor();
        }).trigger('resize');

    });
}(jQuery, window, window.spe = window.spe || {}, window.ace = window.ace || {}));
