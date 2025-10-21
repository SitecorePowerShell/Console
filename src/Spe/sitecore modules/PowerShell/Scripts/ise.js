(function ($, window, spe, ace, undefined) {
    $(function () {
        var tips = [
            "You can press <strong>Ctrl+Space</strong> to show the Auto Suggest drop down that will show you all the matching comands/parameters/files depending on your caret position",
            "You can show help by pressing <strong>Ctrl+Enter</strong> for the closest command to the left of the cursor.",
            "Your script will start in the location/folder picked by the <strong>Content Item</strong> dropdown.",
            "You can change the color of the results dialog shown to your script users using the <strong>Console</strong> ribbon button.",
            "If you save your script in the <strong>Content Editor Context Menu</strong> it will automatically show as a context menu option for items that users Right Click in the tree and will start in the location of that item.",
            "All your scripts that share the same <strong>Persistent Session ID</strong> can re-use variables that were created by the scripts with the same session id that were run before.",
            "<strong>Runtime</strong> ribbon button is active only if you're editing a script from library. Save your script in script library to enable it.",
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
            "You can find more documentation in the <br/><a href='https://doc.sitecorepowershell.com/' target='_blank'>Sitecore PowerShell Extensions book</a>.",
            "You can contribute code and ideas to the project at <br/><a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>Sitecore PowerShell Extensions GitHub repository</a>."
        ];

        class EditorSession {
            constructor(selectedText, editor, name, editorContainerId, path, index) {
                this.index = index;               // int: sort order
                this.editor = editor;             // string: the ace editor
                this.name = name;                 // string: Script name
                this.editorContainerId = editorContainerId;
                this.path = path;                 // string: path
                this.selectedText = selectedText; // string: selected text
                this.startBarHtml = "";           // string: start bar html
                this.tabTitleInnerHTML = "";      // string: tab title inner html
                this.windowTitleInnerHTML = "";   // string: window title inner html
                this.isModified = false;          // bool: is modified
                this.initialAssignment = true;    // bool: initial assignment
                this.breakpoints = "";            // string: breakpoints
                this.results = "";                // string: Script execution results
            }
        }

        var TokenTooltip = ace.require("tooltip").TokenTooltip;
        var guid = "ISE_Editing_Session";
        const speVariablesCacheKey = "spe::variables";
        var debugLine = 0;
        var debugSessionId = "";
        var debugMarkers = [];
        var resultsBottomOffset = 10;
        var typingTimer;
        var resultsVisibilityIntent = true;
        var editorSessions = [];
        var editorFontFamily = "Monaco";
        var editorFontSize = 12;
        var editorLiveAutocompletion = false;
        var currentEditorIndex = 1;
        var currentEditorSession;
        var currentAceEditor;
        var previousIndex = 1;
        var perTabResults = true;

        var editor = $($("#Editor")[0]);
        var openedScriptsMemo = $($("#OpenedScripts")[0]);
        var selectionTextMemo = $($("#SelectionText")[0]);
        var breakpointsMemo = $($("#Breakpoints")[0]);
        var scriptItemIdMemo = $($("#ScriptItemIdMemo")[0]);
        var scriptItemDbMemo = $($("#ScriptItemDbMemo")[0]);
        // Setup the ace code editor.

        addProxy(scForm, "invoke", function (args) {
            if (args[0] === "ise:immediatewindow") {
                clearVariablesCache();
            }
        });

        function registerEventListenersForRibbonButtons() {
            [].forEach.call(document.querySelectorAll('.scRibbonToolbarSmallGalleryButton, .scRibbonToolbarLargeComboButtonBottom'), function (div) {
                div.addEventListener("click",
                    function () {
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

        window.addEventListener("focus", function (event) {
            setFocusOnConsole();
        }, false);

        function getQueryStringValue(key) {
            key = key.replace(/[*+?^$.\[\]{}()|\\\/]/g, "\\$&");
            var match = location.search.match(new RegExp("[?&]" + key + "=([^&]+)(&|$)"));
            return match && decodeURIComponent(match[1].replace(/\+/g, " "));
        }

        if (getQueryStringValue("sc_bw") === "1") {
            $("#Wrapper").css("padding-top", "0px");
        }

        function hasSessionWithIndex(targetIndex) {
            return editorSessions.some(session => session.index === targetIndex);
        }

        function getSessionByIndex(index) {
            // Find the session with the specified index
            return editorSessions.find(session => session.index === index);
        }

        function setFocusOnConsole() {
            $("body").focus();
            $(currentAceEditor).focus();
            ("WebForm_AutoFocus" in this) && WebForm_AutoFocus && WebForm_AutoFocus("CodeEditor");
        }

        setTimeout(setFocusOnConsole, 1000);

        spe.updateRibbon = function () {
            if (!currentAceEditor.getReadOnly()) {
                scForm.postRequest("", "", "", "ise:scriptchanged(modified=" + !currentEditorSession.isModified + ")");
                registerEventListenersForRibbonButtons();
            }
        };
        
        spe.showSessionIDGallery = function () {
            contextGalery = jQuery("#B0C784F542B464EE2B0BA72384125E123")
            contextGalery.click();
        };

        spe.updateRibbonNeeded = function () {
            clearTimeout(typingTimer);
            var timeout = 2000;
            if (document.querySelector('.scGalleryFrame') != null) {
                var timeout = 20;
            }
            typingTimer = setTimeout(spe.updateRibbon, timeout);
        };

        scContent.toggleRibbon = function (animationTime) {
            jQuery("#PowerShellRibbon_Toolbar").slideToggle(animationTime, function () {
                var button = document.getElementById('scRibbonToggle');
                button.className = this.style.display == "none" ? 'scRibbonOpen' : "scRibbonClose";
                spe.resizeEditor();
            });
        }

        var posx = $("#PosX");
        var posy = $("#PosY");

        spe.updateModificationFlag = function (clear) {

            if (currentEditorSession.initialAssignment) {
                currentEditorSession.initialAssignment = false;
            } else if (clear === currentEditorSession.isModified) {
                currentEditorSession.isModified = !clear;
                spe.applyWindowTitle(currentEditorIndex);
            }
        };

        spe.changeTab = function (index) {

            if (!hasSessionWithIndex(index)) {
                return;
            }

            previousIndex = currentEditorIndex;
            currentEditorIndex = index;
            currentEditorSession = getSessionByIndex(currentEditorIndex);
            currentAceEditor = currentEditorSession.editor;
            clearVariablesCache();
            editor.val(currentAceEditor.session.getValue());
            selectionTextMemo.val(currentEditorSession.selectedText);
            breakpointsMemo.val(currentEditorSession.breakpoints);
            scriptItemIdMemo.val(currentEditorSession.scriptId);
            scriptItemDbMemo.val(currentEditorSession.scriptDb);
            spe.applyWindowTitle(currentEditorIndex);

            editorSessions.forEach(function (editorSession) {
                if (editorSession.index === index) {
                    $("#" + editorSession.editorContainerId).show();
                } else {
                    $("#" + editorSession.editorContainerId).hide();
                }
            });

            if (perTabResults) {
                $("#ScriptResultCode").text("");
                $("#ScriptResultCode").append(currentEditorSession.results);
                $("#Result").scrollTop($("#Result")[0].scrollHeight);
            }
        }

        spe.createEditor = function (index) {
            previousIndex = currentEditorIndex;

            currentEditorIndex = editorSessions.length + 1;

            var editorContainerId = "CodeEditor" + index;
            $("#CodeEditors").append("<div id='" + editorContainerId + "' class='aceCodeEditor'></div>");
            currentAceEditor = ace.edit(editorContainerId);

            const newSession = new EditorSession("", currentAceEditor, "", editorContainerId, "", currentEditorIndex);
            editorSessions.push(newSession);
            currentEditorSession = newSession;

            currentAceEditor.setTheme("ace/theme/powershellise");
            currentAceEditor.session.setMode("ace/mode/powershell");
            currentAceEditor.setShowPrintMargin(false);
            currentAceEditor.session.setValue(editor.val());
            currentAceEditor.session.on("change", function () {
                editor.val(currentAceEditor.session.getValue());
            });
            currentAceEditor.tokenTooltip = new TokenTooltip(currentAceEditor);
            currentAceEditor.setOption("fontFamily", editorFontFamily);
            currentAceEditor.setOption("fontSize", editorFontSize);
            currentAceEditor.setOptions({
                enableLiveAutocompletion: editorLiveAutocompletion
            });

            addProxy(currentAceEditor, "onPaste", function () {
                spe.updateRibbon();
            });

            $("#" + editorContainerId).on("keyup mousedown", function () {
                var position = currentAceEditor.getCursorPosition();
                posx.text(position.column);
                posy.text((position.row + 1));
                spe.updateRibbonNeeded();
            });

            $("#" + editorContainerId).on("keyup mouseup", function () {
                var range = currentAceEditor.getSelectionRange();
                currentEditorSession.selectedText = currentAceEditor.session.getTextRange(range);
                selectionTextMemo.val(currentEditorSession.selectedText);
            });

            ace.config.loadModule("ace/ext/emmet", function () {
                ace.require("ace/lib/net").loadScript("/sitecore modules/PowerShell/Scripts/ace/emmet-core/emmet.js", function () {
                    currentAceEditor.setOption("enableEmmet", true);
                });

                currentAceEditor.setOptions({
                    enableSnippets: true,
                    enableBasicAutocompletion: true
                });
            });

            ace.config.loadModule("ace/ext/language_tools", function (module) {
                currentAceEditor.setOptions({
                    enableSnippets: true,
                    enableBasicAutocompletion: true
                });

                var keyWordCompleter = {
                    insertMatch: function (editor) {

                        var data = editor.completer.popup.getData(editor.completer.popup.getRow());

                        var ranges = editor.selection.getAllRanges();
                        for (var i = 0, range; range = ranges[i]; i++) {
                            if (data.meta === "Signature") {
                                data.value = data.fullValue;
                            } else if (data.meta === "Type") {
                                while (range.start.column > 0 && currentAceEditor.session.getTextRange(range).lastIndexOf("[") !== 0) {
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
                                var replacedText = currentAceEditor.session.getTextRange(range);
                                var charStart = replacedText.charAt(0);
                                var charEnd = replacedText.charAt(replacedText.length - 1);
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
                    getCompletions: function (editor, session, pos, prefix, callback) {
                        session.$mode.$keywordList = [];

                        var range = currentAceEditor.getSelectionRange();
                        range.start.column = 0;
                        var line = currentAceEditor.session.getTextRange(range);

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
                                module.showErrorMarker(currentAceEditor, -1);
                            });
                            return;
                        }

                        var psCompleter = this;
                        callback(null, keywords.map(function (word) {
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


            currentAceEditor.setAutoScrollEditorIntoView(true);

            currentAceEditor.on("guttermousedown", function (editor) {
                var target = editor.domEvent.target;
                if (target.className.indexOf("ace_gutter-cell") === -1)
                    return;

                if (editor.clientX > 25 + target.getBoundingClientRect().left)
                    return;

                var currRow = editor.getDocumentPosition().row;
                spe.breakpointSet(currRow, "toggle");
                editor.stop();
                var sparseKeys = Object.keys(editor.editor.session.getBreakpoints());
                currentEditorSession.breakpoints = sparseKeys.toString();
                breakpointsMemo.val(currentEditorSession.breakpoints);
            });

            currentAceEditor.on("input", function () {
                spe.updateModificationFlag(false);
            });

            var codeeeditorcommands = [
                {
                    name: "help",
                    bindKey: {win: "ctrl-enter|shift-enter", mac: "ctrl-enter|command-enter", sender: "codeeditor|cli"},
                    exec: function (env, args, request) {
                        var range = currentAceEditor.getSelectionRange();
                        if (range.start.row === range.end.row && range.start.column === range.end.column) {
                            range.start.column = 0;
                        }
                        var command = currentAceEditor.session.getTextRange(range);
                        if (command) {
                            spe.showCommandHelp(command);
                        }
                    },
                    readOnly: true
                }, {
                    name: "fontSizeIncrease",
                    bindKey: {win: "Ctrl-Alt-Shift-=|Ctrl-Alt-Shift-+", mac: "Ctrl-Alt-Shift-=|Ctrl-Alt-Shift-+"},
                    exec: function (editor) {
                        spe.changeFontSize(editor.getFontSize() + 1);
                    },
                    readOnly: true
                }, {
                    name: "fontSizeDecrease",
                    bindKey: {win: "Ctrl-Alt-Shift--", mac: "Ctrl-Alt-Shift--"},
                    exec: function (editor) {
                        spe.changeFontSize(Math.max(editor.getFontSize() - 1, 8));
                    },
                    readOnly: true
                }, {
                    name: "setDebugPoint",
                    bindKey: {win: "F8", mac: "F8"},
                    exec: function (editor) {
                        var currRow = editor.selection.getCursor().row;
                        spe.breakpointSet(currRow, "toggle");
                    },
                    readOnly: true
                }
            ];

            currentAceEditor.commands.addCommands(codeeeditorcommands);

        }

        spe.changeLiveAutocompletion = function (liveAutocompletion) {
            editorLiveAutocompletion = liveAutocompletion;
            editorSessions.forEach(function (editor) {
                editor.editor.setOptions({
                    enableLiveAutocompletion: liveAutocompletion
                });
            });
        };


        $("#CopyResultsToClipboard").on("click", function () {
            clipboard.copy(spe.getOutput());
        });


        spe.getOutput = function () {
            return $("#ScriptResultCode")[0].innerText;
        };

        spe.clearOutput = function () {
            $("#ScriptResultCode").text("");
            $("#Result").scrollTop($("#Result")[0].scrollHeight);
            currentEditorSession.results = "";
            clearVariablesCache();
        };

        spe.appendOutput = function (outputToAppend) {
            var decoded = $("<div/>").html(outputToAppend).text();
            $("#ScriptResultCode").append(decoded);
            $("#Result").scrollTop($("#Result")[0].scrollHeight);
            currentEditorSession.results = currentEditorSession.results + decoded;
            clearVariablesCache();
        };

        spe.changeFontFamily = function (setting) {
            setting = setting || "Monaco";
            editorFontFamily = setting;
            editorSessions.forEach(function (editor) {
                editor.editor.setOption("fontFamily", setting);
            });

            document.getElementById("ScriptResult").style.fontFamily = setting;
        };

        spe.changeFontSize = function (setting) {
            setting = parseInt(setting) || 12;
            editorFontSize = setting;
            editorSessions.forEach(function (editor) {
                editor.editor.setOption("fontSize", setting);
            });
            $("#ScriptResult").css({"font-size": setting + "px"});
        };


        spe.changeBackgroundColor = function (setting) {
            $("#ScriptResult").css({"background-color": setting});
            $("#Result").css({"background-color": setting});
        };

        spe.changeSettings = function (fontFamily, fontSize, backgroundColor, bottomOffset, liveAutocompletion, perTabOutput) {
            spe.changeBackgroundColor(backgroundColor);
            spe.changeFontFamily(fontFamily);
            spe.changeFontSize(fontSize);
            if (liveAutocompletion !== undefined) {
                spe.changeLiveAutocompletion(liveAutocompletion);
            }
            resultsBottomOffset = bottomOffset;
            perTabResults = perTabOutput;
        };

        spe.changeSessionId = function (sessionId) {
            guid = sessionId;
        };

        spe.debugStart = function (sessionId) {
            currentAceEditor.setReadOnly(true);
        };

        spe.debugStop = function (sessionId) {
            setTimeout(spe.breakpointHandled, 100);
            currentAceEditor.setReadOnly(false);
        };

        spe.toggleBreakpoint = function (row, set) {
            if (set) {
                currentAceEditor.session.setBreakpoint(row);
            } else {
                currentAceEditor.session.clearBreakpoint(row);
            }
            scForm.postRequest("", "", "", "ise:togglebreakpoint(line=" + row + ",state=" + set + ")");
        };

        spe.breakpointSet = function (row, action) {
            if (action === "toggle") {
                if (currentAceEditor.session.getBreakpoints()[row] === "ace_breakpoint") {
                    spe.toggleBreakpoint(row, false);
                } else {
                    spe.toggleBreakpoint(row, true);
                }
            } else if (action === "Set" || action === "Enabled") {
                if (currentAceEditor.session.getBreakpoints()[row] !== "ace_breakpoint") {
                    spe.toggleBreakpoint(row, true);
                }
            } else if (action === "Removed" || action === "Disabled") {
                if (currentAceEditor.session.getBreakpoints()[row] === "ace_breakpoint") {
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
                debugMarkers.push(currentAceEditor.session.addMarker(new Range(line, column, endLine, endColumn + 1), "breakpoint", "text"));
            }, 100);
            if (line < currentAceEditor.getFirstVisibleRow() || line > currentAceEditor.getLastVisibleRow()) {
                currentAceEditor.gotoLine(line);
            }
        };

        spe.breakpointHandled = function () {
            while (debugMarkers.length > 0) {
                currentAceEditor.session.removeMarker(debugMarkers.shift());
            }
            scContent.ribbonNavigatorButtonClick(this, event, "PowerShellRibbon_Strip_ImageStrip");
        };

        spe.updateEditor = function () {
            currentAceEditor.getSession().setValue(editor.val());
        };

        spe.insertEditorContent = function (text) {
            var position = currentAceEditor.getCursorPosition();
            currentAceEditor.getSession().insert(position, text);
            spe.clearBreakpoints();
        };

        spe.scriptExecutionEnded = function () {
            if (spe.preventCloseWhenRunning) {
                spe.preventCloseWhenRunning(false);
            }
            clearVariablesCache();
            if (!resultsVisibilityIntent) {
                setTimeout(spe.closeResults, 2000);
            }
        };

        spe.clearBreakpoints = function () {
            var breakPoints = Object.keys(currentAceEditor.session.getBreakpoints());
            var bpCount = breakPoints.length;
            for (var i = 0; i < bpCount; i++) {
                currentAceEditor.session.clearBreakpoint(breakPoints[i]);
            }
        };

        function escapeRegExp(string) {
            return string.replace(/([.*+?^=!:${}()|\[\]\/\\])/g, "\\$1");
        }

        function replaceAll(string, find, replace) {
            return string.replace(new RegExp(escapeRegExp(find), "g"), replace);
        }

        spe.changeTabDetails = function (codeEditorIndex, newTitle, startBarHtml, scriptId, scriptDb) {

            var editorSession = getSessionByIndex(codeEditorIndex);
            var itemName = newTitle.split("/").last();
            editorSession.name = itemName
            editorSession.path = newTitle;
            const newTabTitle = replaceAll(editorSession.path, "/", "</i> / <i style=\"font-style: italic; color: #bbb;\">");
            editorSession.tabTitleInnerHTML = "<span title='" + editorSession.path + "'><span class=\"scriptModified ModifiedMark#tabindex#\">⬤</span> " + editorSession.name +
                "</i>" +
                "<span class=\"closeTab\" onclick=\"javascript:spe.closeTab(event, #tabindex#)\">&nbsp;</span>";
            const newWindowTitle = replaceAll(editorSession.path, "/", "</i> / <i style=\"font-style: italic; color: #bbb;\">");
            editorSession.windowTitleInnerHTML = "<span class=\"scriptModified ModifiedMark#tabindex#\" style='color:#fc2929;#styles#'>⬤</span> <i style=\"font-style: italic; color: #bbb;\">" + newWindowTitle + "</i> - ";
            editorSession.tabTitleHtml = replaceAll(editorSession.path, "/", "</i> / <i style=\"font-style: italic; color: #bbb;\">");
            editorSession.scriptId = scriptId;
            editorSession.scriptDb = scriptDb;
            editorSession.startBarHtml = startBarHtml;
            var openedScripts = editorSessions.map(session => session.path + ":" + session.index).join('\n');
            openedScriptsMemo.val(openedScripts);
        }

        spe.applyWindowTitle = function (codeEditorIndex) {

            var editorSession = getSessionByIndex(codeEditorIndex);
            var windowCaption = $("#WindowCaption", window.parent.document);
            if (windowCaption.length > 0) {
                windowCaption[0].innerHTML =
                    editorSession.windowTitleInnerHTML.replace("#tabindex#", editorSession.index).replace("#styles#", editorSession.isModified ? "display:inline;" : "display:none;");

            }

            var styles = $("#ModifiedStatusStyles");
            if (styles.length === 0) {
                var head = $("head");
                head.append("<style id='ModifiedStatusStyles'>.ModifiedMark" + codeEditorIndex + "{color:#fc2929;}</style>");
                styles = $("#ModifiedStatusStyles");
            }
            var stylesinnerHTML = "";
            editorSessions.forEach(function (currEditor) {
                var tabHeader = $("#Tabs_tab_" + (currEditor.index - 1))
                if (tabHeader.length > 0) {
                    tabHeader[0].innerHTML = currEditor.tabTitleInnerHTML.replaceAll("#tabindex#", currEditor.index);
                    if (currEditor.isModified) {
                        stylesinnerHTML = stylesinnerHTML + ".ModifiedMark" + currEditor.index + "{display:inline;}";
                    }
                }
            });
            styles[0].innerHTML = stylesinnerHTML;


            if (window.parent && window.parent.frameElement) {
                var frameId = window.parent.frameElement.id;
                var startbar = window.parent.parent.document.getElementById('startbar_application_' + frameId);
                $(startbar).find('span').html(editorSession.startBarHtml);
            }
        };


        spe.closeTab = function (event, index) {

            event.stopPropagation();
            var closingEditorSession = getSessionByIndex(index);
            var selectIndex = currentEditorIndex;
            if (index === currentEditorIndex) {
                selectIndex = previousIndex;
            }
            if (selectIndex > index) {
                selectIndex--;
            }
            selectIndex = Math.min(selectIndex, editorSessions.length - 1);
            scForm.postRequest("", "", "", "ise:closetab(index=" + index + ",modified=" + closingEditorSession.isModified + ",selectIndex=" + selectIndex + ")");
        };

        spe.closeScript = function (index) {

            // Remove the session with the specified index
            var closingEditorSession = getSessionByIndex(index);

            editorSessions = editorSessions.filter(session => session.index !== index);

            // cleanup the DOM and script content
            var editorContainer = $(closingEditorSession.editor.container)
            closingEditorSession.editor.destroy();
            editorContainer.remove();
            editor.val("");

            // Update the indices of the remaining sessions
            editorSessions.forEach((session, i) => {
                session.index = i + 1;
            });
        }

        spe.resizeEditor = function () {
            if (currentAceEditor !== undefined) {
                currentAceEditor.resize();
            }
            var resultsHeight = $(window).height() - $("#ResultsSplitter").offset().top - $("#ResultsSplitter").height() - $("#StatusBar").height() - resultsBottomOffset - 10;
            $("#Result").height(resultsHeight);
            $("#Result").width($(window).width() - $("#Result").offset().left * 2);
            $("#Result").width($("#ResultsPane").width());
            $("#ProgressOverlay").css("top", ($("#Result").offset().top + 4) + "px");
            $("#TreeViewToggle").css("top", ($("#TabsPanel").offset().top + 8) + "px");
            $("#TreeViewToggle").css("left", ($("#TabsPanel").offset().left + 6) + "px");
            if ($("#TreeView").is(":visible")) {
                    
            }
            $("#CodeEditors").css("height", "calc(100% - " + ($("#TabsPanel").height()+5) + "px");

        };

        function isEmpty(val) {
            return (val === undefined || val == null || val.length <= 0) ? true : false;
        }

        spe.showInfoPanel = function (showPanel, updateFromMessage) {
            if (showPanel) {
                $("#InfoPanel").removeClass("scEditorWarningHidden");
            } else {
                $("#InfoPanel").addClass("scEditorWarningHidden");
            }
            spe.resizeEditor();
            if (!isEmpty(updateFromMessage)) {
                scForm.invoke(updateFromMessage + "(elevationResult=1)");
            }
        };

        spe.requestElevation = function () {
            scForm.postRequest("", "", "", "ise:requestelevation");
        };

        spe.restoreResults = function () {
            $("#ResultsSplitter").show();
            $("#ResultsRow").show();
            spe.resizeEditor();
            $("#ResultsStatusBarAction").removeClass("status-bar-results-hidden")
        };

        spe.closeResults = function () {
            $("#ResultsSplitter").hide();
            $("#ResultsRow").hide("slow", function () {
                currentAceEditor.resize();
            });
            $("#ResultsStatusBarAction").addClass("status-bar-results-hidden")
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

        var ctrlPressed = false;
        $(window).keydown(function(evt) {
            if (evt.which == 17) { // ctrl
                ctrlPressed = true;
            }
        }).keyup(function(evt) {
            if (evt.which == 17) { // ctrl
                ctrlPressed = false;
            }
        });
        
        spe.variableValue = function (variableName) {
            var data;
            var sessionId = debugSessionId;
            if (!debugSessionId || 0 === debugSessionId.length) {
                sessionId = guid;
            }

            var storageKey = speVariablesCacheKey + sessionId;
            if(ctrlPressed === false) {
                var cachedValue = getCachedVariableValue(storageKey, variableName)
                if (cachedValue) {
                    return cachedValue;
                }
            }

            getPowerShellResponse({"guid": sessionId, "variableName": variableName}, "GetVariableValue",
                function (json) {
                    data = json.d;
                });

            setCachedVariableValue(storageKey, variableName, data);
            return data;
        };

        spe.getAutocompletionPrefix = function (text) {
            var data;
            getPowerShellResponse({"guid": guid, "command": text}, "GetAutoCompletionPrefix",
                function (json) {
                    data = JSON.parse(json.d);
                });
            return data;
        };

        spe.showCommandHelp = function (command) {
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
                close: function (event, ui) {
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


        var tipIndex = Math.floor(Math.random() * tips.length);
        var tip = tips[tipIndex];

        $("#TipText").html(tip);
        $("#StatusTip").html(tip);

        $("#TipOfTheSession").position({
            my: "left bottom",
            at: "left bottom-30px",
            within: $("body"),
            of: $("body")
        }).css({
            right: 0,
            left: 0
        }).hide()
            .show("drop", {direction: "down"}, "200")
            .delay(5000)
            .hide("drop", {direction: "down"}, "200",
                function () {
                    $(".status-bar-text").animate({backgroundColor: "#fcefa1"}).animate({backgroundColor: "#fff"});
                });

        $(".status-bar-text").click(function () {
            tipIndex++;
            if (tipIndex >= tips.length) {
                tipIndex = 0;
            }
            var nextTip = tips[tipIndex];
            $(".status-bar-text").animate({backgroundColor: "#fcefa1"},
                function () {
                    $("#TipText").html(nextTip);
                    $("#StatusTip").html(nextTip);
                }).animate({backgroundColor: "#fff"});

        });


        $("#ShowHideResults").click(function () {
            if ($("#ResultsRow").is(":visible")) {
                resultsVisibilityIntent = false;
                spe.closeResults();
            } else {
                resultsVisibilityIntent = true;
                spe.restoreResults();
            }
        });
        $("#TreeViewToggle").click(function () {
            if ($("#TreeViewPanel").is(":visible")) {
                $("#TreeViewPanel").hide();
                $("#TreeSplitterPanel").hide();
                spe.resizeEditor();
            } else {
                $("#TreeViewPanel").show();
                $("#TreeSplitterPanel").show();
                spe.resizeEditor();
            }
        });
        
        function _getCommandHelp(str) {
            getPowerShellResponse({"guid": guid, "command": str}, "GetHelpForCommand",
                function (json) {
                    var data = JSON.parse(json.d);
                    $.commandHelp = data[0];
                });
        }

        function _getTabCompletions(str) {
            getPowerShellResponse({"guid": guid, "command": str}, "CompleteAceCommand",
                function (json) {
                    var data = JSON.parse(json.d);
                    $.tabCompletions = data;
                });
        }

        function getPowerShellResponse(callData, remotefunction, doneFunction, errorFunction) {
            if (remotefunction != "GetVariableValue") {
                spe.requestElevation();
            }
            ;
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

        $(window).on('resize', function () {
            spe.resizeEditor();
        }).trigger('resize');
        
        setTimeout(function () {
            scForm.postRequest("", "", "", "ise:updatesettings");
            scForm.postRequest("", "", "", "ise:loadinitialscript");
            spe.resizeEditor();
        }, 100);

        const resizeObserver = new ResizeObserver((entries) => {
            spe.resizeEditor();
        })
        resizeObserver.observe(document.getElementById("EditingPanel"));        
    });
}(jQuery, window, window.spe = window.spe || {}, window.ace = window.ace || {}));
