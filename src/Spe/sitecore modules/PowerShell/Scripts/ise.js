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
            "You can contribute code and ideas to the project at <br/><a href='https://github.com/SitecorePowerShell/Console/' target='_blank'>Sitecore PowerShell Extensions GitHub repository</a>.",
            "You can toggle the results pane between a <strong>horizontal</strong> and <strong>vertical</strong> split using the toggle icon in the status bar. Double-click the splitter to reset to 50/50."
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
                this.breakpoints = "";            // string: breakpoints
                this.resultsHistory = [];         // array: Script execution results for terminal replay
                this.scriptId = "";               // string: Sitecore item ID
                this.scriptDb = "";               // string: Sitecore database name
                this.isDeleted = false;           // bool: source item was deleted
            }
        }

        var TokenTooltip = ace.require("tooltip").TokenTooltip;
        var guid = "ISE_Editing_Session";
        const speVariablesCacheKey = "spe::variables";
        var debugLine = 0;
        var debugSessionId = "";
        var debugMarkers = [];
        var resultsBottomOffset = 10;
        var maxResultsHistoryLines = 9001;

        function trimResultsHistory() {
            var history = currentEditorSession.resultsHistory;
            if (!history) return;
            var totalLines = 0;
            for (var i = 0; i < history.length; i++) {
                var text = typeof history[i] === "string" ? history[i] : history[i].text;
                totalLines += (text || "").split("\n").length;
            }
            while (totalLines > maxResultsHistoryLines && history.length > 1) {
                var removed = history.shift();
                var removedText = typeof removed === "string" ? removed : removed.text;
                totalLines -= (removedText || "").split("\n").length;
            }
        }
        var typingTimer;
        var resultsVisibilityIntent = true;
        var splitOrientation = localStorage.getItem("spe::ise.splitOrientation") || "horizontal";
        var splitPosition = JSON.parse(localStorage.getItem("spe::ise.splitPosition") || '{}');

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
        var activeTabsMemo = $($("#ActiveTabsMemo")[0]);
        var terminalCommandMemo = $($("#TerminalCommand")[0]);
        // Setup the ace code editor.

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

        spe.updateModificationFlag = function (clear, targetSession) {
            // Called from two very different places:
            //   1. The Ace "input" event handler (clear === false), which
            //      fires asynchronously ~31ms after any editor change
            //      because Ace debounces input via lang.delayedCall. A
            //      programmatic setValue (restore, reload, plugin update)
            //      therefore schedules a deferred event that could
            //      incorrectly flag the session as modified. We
            //      distinguish "user edit" from "programmatic load" by
            //      checking the session's undo bookmark - callers who
            //      load content programmatically call
            //      session.getUndoManager().markClean() immediately after
            //      setValue, and user edits advance the revision past
            //      that bookmark.
            //   2. The server-side save handler (clear === true), which
            //      clears the modified flag and re-anchors the clean
            //      bookmark to the current content.
            // targetSession lets the deferred input handler pass the
            // session it was registered for, avoiding a race where the
            // globally-active session is read instead of the editor that
            // actually raised the event.
            var session = targetSession || currentEditorSession;

            if (clear) {
                session.isModified = false;
                session.isDeleted = false;
                if (session.editor && session.editor.session) {
                    session.editor.session.getUndoManager().markClean();
                }
                spe.applyWindowTitle(session.index);
                return;
            }

            var aceSession = session.editor && session.editor.session;
            if (aceSession && aceSession.getUndoManager().isClean()) {
                return;
            }
            if (!session.isModified) {
                session.isModified = true;
                spe.applyWindowTitle(session.index);
            }
        };

        spe.collectTabState = function () {
            var tabs = [];
            var adjustedActiveIndex = 1;

            editorSessions.forEach(function (session) {
                var tab = {
                    db: session.scriptDb || "",
                    id: session.scriptId || "",
                    path: session.path || "",
                    modified: session.isModified
                };

                if (session.isModified || !tab.id) {
                    tab.content = session.editor.session.getValue();
                }

                if (!tab.id && !tab.content) return;
                tabs.push(tab);
                if (session.index === currentEditorIndex) {
                    adjustedActiveIndex = tabs.length;
                }
            });

            var state = {
                activeIndex: adjustedActiveIndex,
                tabs: tabs
            };

            activeTabsMemo.val(JSON.stringify(state));
        };

        spe.saveActiveTabs = function () {
            spe.collectTabState();
            scForm.postRequest("", "", "", "ise:savetabstate");
        };

        spe.applyRestoredTabs = function (tabsData) {
            if (!tabsData || !tabsData.tabs) return;

            var prevSession = currentEditorSession;
            var prevEditor = currentAceEditor;
            var prevIndex = currentEditorIndex;

            tabsData.tabs.forEach(function (tab, idx) {
                var session = editorSessions[idx];
                if (!session) return;

                // Temporarily swap to this session so Ace's session
                // "change" handler (which reads global currentAceEditor
                // to mirror content into the Editor memo) runs against
                // the correct session during setValue. The delayed
                // "input" event is handled separately via the
                // session-scoped handler registered in createEditor.
                currentEditorSession = session;
                currentAceEditor = session.editor;
                currentEditorIndex = session.index;

                var aceSession = session.editor.session;
                aceSession.setValue(tab.content || "");
                // Anchor the undo bookmark so the deferred Ace input
                // event (scheduled ~31ms after this setValue) does not
                // spuriously flag the tab as modified. Any subsequent
                // user edit advances the revision past this bookmark
                // and the flag logic marks the tab dirty correctly.
                aceSession.getUndoManager().markClean();

                session.isModified = !!tab.modified;
                session.isDeleted = !!tab.deleted;
                spe.applyWindowTitle(session.index);
            });

            currentEditorSession = prevSession;
            currentAceEditor = prevEditor;
            currentEditorIndex = prevIndex;

            if (currentAceEditor) {
                editor.val(currentAceEditor.session.getValue());
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

            if (perTabResults && iseTerminal) {
                iseTerminal.clear();
                var history = currentEditorSession.resultsHistory || [];
                history.forEach(function (entry) {
                    if (typeof entry === "string") {
                        // Legacy format - treat as raw HTML
                        iseTerminal.echo(entry, { raw: true });
                    } else {
                        var opts = { raw: entry.raw };
                        if (!entry.raw) opts.finalize = finalizeGuidLinks;
                        iseTerminal.echo(entry.text, opts);
                    }
                });
                // Ensure terminal is interactive after tab switch (unless a
                // script is currently running in the shared ISE session).
                if (!iseScriptRunning) {
                    iseTerminal.find(".cmd").css("visibility", "visible");
                    iseTerminal.resume();
                }
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
            // Anchor the undo bookmark to the initial content so the
            // modification-flag logic can tell later whether the session
            // has diverged from its loaded baseline.
            currentAceEditor.session.getUndoManager().markClean();
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

            // Capture newSession in the closure so the deferred Ace
            // input event always fires against the editor that actually
            // raised it, not whichever tab happens to be globally active
            // when the 31ms delayedCall timer elapses.
            currentAceEditor.on("input", function () {
                spe.updateModificationFlag(false, newSession);
            });

            // Ace keybinding overrides for ISE shortcuts are registered
            // as explicit commands below (#1457); removeCommand calls from
            // #1458 were redundant and have been dropped.
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
                }, {
                    name: "debugScript",
                    bindKey: {win: "Ctrl-D", mac: "Ctrl-D"},
                    exec: function () {
                        scForm.postRequest("", "", "", "ise:debug");
                    },
                    readOnly: true
                }, {
                    name: "executeSelection",
                    bindKey: {win: "Alt-E", mac: "Alt-E"},
                    exec: function () {
                        scForm.postRequest("", "", "", "ise:executeselection");
                    },
                    readOnly: true
                }, {
                    name: "runPlugin",
                    bindKey: {win: "Ctrl-Shift-Alt-I", mac: "Ctrl-Shift-Alt-I"},
                    exec: function () { },
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


        // Linkify Sitecore GUIDs in terminal output so they open the item in Content Editor
        var guidRegex = /\b([A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12})\b/gi;
        function finalizeGuidLinks(div) {
            div.find("span").each(function () {
                var span = $(this);
                var html = span.html();
                if (guidRegex.test(html)) {
                    guidRegex.lastIndex = 0;
                    span.html(html.replace(guidRegex, function (match) {
                        return "<a href='#' onclick=\"javascript:return scForm.postEvent(this,event,'item:load(id={" + match + "})')\">" + match + "</a>";
                    }));
                }
            });
        }

        // Initialize jQuery Terminal in the results pane
        var iseTerminal = null;
        var iseScriptRunning = false;
        var iseLastPrompt = "PS >";
        var iseDebugActive = false;

        function getIsePrompt() {
            // jQuery Terminal format syntax: [[STYLE;FG;BG]text]
            // Avoid [ and ] characters in the text since they conflict with the tag delimiters.
            // White when ready for commands, yellow during debug inspection.
            if (iseDebugActive) {
                return "[[;yellow;]DBG " + iseLastPrompt + "]";
            }
            return "[[;white;]" + iseLastPrompt + "]";
        }

        function getPowerShellResponseAsync(callData, remotefunction, doneFunction, errorFunction) {
            var datastring = JSON.stringify(callData);
            var ajax = $.ajax({
                type: "POST",
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                url: "/sitecore modules/PowerShell/Services/PowerShellWebService.asmx/" + remotefunction,
                data: datastring,
                processData: false,
                cache: false,
                async: true
            }).done(doneFunction);
            if (typeof errorFunction !== "undefined") {
                ajax.fail(errorFunction);
            }
        }

        // Server-pushed prompt update. Called from PushTerminalPrompt on the
        // server after session priming and after script execution completes.
        // Replaces the old round-trip prompt refresh that was routed through
        // PowerShellWebService.ExecuteCommand.
        spe.setTerminalPrompt = function (prompt) {
            if (prompt) {
                iseLastPrompt = prompt;
            }
            if (iseTerminal && !speBusyInterval) {
                iseTerminal.set_prompt(getIsePrompt());
            }
        };

        // Animated "busy" indicator that takes over the terminal prompt line
        // while a script is executing (editor Run or terminal command). Replaces
        // the floating bottom-right PleaseWait overlay, which was easy to miss
        // for terminal-issued commands since it appeared in a different spot
        // than the user's focus. Now the indicator appears exactly where the
        // prompt normally lives.
        //
        // Spinner is the classic cli-spinners "dots" pattern; ~80ms per frame
        // gives the characteristic rolling motion.
        var speSpinnerFrames = ["\u280B", "\u2819", "\u2839", "\u2838", "\u283C",
                                "\u2834", "\u2826", "\u2827", "\u2807", "\u280F"];
        var speBusyInterval = null;
        var speBusyMessage = "";
        var speBusyFrame = 0;

        function getBusyPrompt() {
            var frame = speSpinnerFrames[speBusyFrame];
            // jquery.terminal color format: [[STYLE;FG;BG]text]. Teal spinner
            // next to a white message line keeps the indicator distinct from
            // the normal white prompt without being noisy.
            return "[[;#4ec9b0;]" + frame + "] [[;#d4d4d4;]" + speBusyMessage + "]";
        }

        spe.showBusy = function (message) {
            speBusyMessage = message || "";
            speBusyFrame = 0;
            if (!iseTerminal) return;
            iseTerminal.set_prompt(getBusyPrompt());
            if (speBusyInterval) return;
            speBusyInterval = setInterval(function () {
                speBusyFrame = (speBusyFrame + 1) % speSpinnerFrames.length;
                if (iseTerminal) {
                    iseTerminal.set_prompt(getBusyPrompt());
                }
            }, 80);
        };

        spe.hideBusy = function () {
            if (speBusyInterval) {
                clearInterval(speBusyInterval);
                speBusyInterval = null;
            }
            speBusyMessage = "";
            if (iseTerminal) {
                iseTerminal.set_prompt(getIsePrompt());
            }
        };

        var iseTabCompletions = null;
        var iseTabCycleIndex = -1;
        var iseTabCycleActive = false;

        function iseCompletion(command, callback) {
            var term = this;
            var fullCommand = term.get_command();
            if (iseTabCycleActive && iseTabCompletions && iseTabCompletions.length > 0) {
                iseTabCycleIndex = (iseTabCycleIndex + 1) % iseTabCompletions.length;
                term.set_command(iseTabCompletions[iseTabCycleIndex]);
                return;
            }
            console.log("[spe] initializing tab completion");
            getPowerShellResponseAsync({ "guid": guid, "command": fullCommand }, "CompleteCommand",
                function (json) {
                    var data = JSON.parse(json.d);
                    iseTabCompletions = data.filter(function (hint) {
                        return hint.indexOf("Signature|") !== 0;
                    }).map(function (hint) {
                        var hintParts = hint.split("|");
                        if (hintParts[0] === "Type") {
                            return "[" + hintParts[3];
                        }
                        return hint;
                    });
                    console.log("[spe] setting tabCompletions to: ", iseTabCompletions);
                    if (iseTabCompletions.length === 0) {
                        // no completions
                    } else if (iseTabCompletions.length === 1) {
                        term.set_command(iseTabCompletions[0]);
                    } else {
                        iseTabCycleIndex = 0;
                        iseTabCycleActive = true;
                        term.set_command(iseTabCompletions[0]);
                    }
                }
            );
        }

        function initIseTerminal() {
            var target = $("#ScriptResultCode");
            if (!target.length) {
                console.error("[spe] initIseTerminal: #ScriptResultCode not found in DOM");
                return;
            }
            iseTerminal = target.terminal(function (command, term) {
                if (/^\s*(cls|clear-host)\s*$/i.test(command)) {
                    spe.clearOutput();
                    return;
                }
                if (command.length > 0) {
                    // Route the terminal command through the same Sheer UI
                    // message pipeline as the ribbon Run button. The server
                    // reads the command from the TerminalCommand memo and
                    // calls JobExecuteScript, giving the terminal the full
                    // editor execution pipeline (ScriptRunning flag, ribbon
                    // disable, progress overlay, Abort, PromptForChoice via
                    // the job message queue).
                    terminalCommandMemo.val(command);
                    scForm.postRequest("", "", "", "ise:termexecute");
                }
            }, {
                greetings: false,
                prompt: getIsePrompt(),
                enabled: true,
                onClear: function () {},
                completion: iseCompletion,
                caseSensitiveAutocomplete: false,
                keydown: function (e) {
                    if (e.which !== 9) {
                        iseTabCycleActive = false;
                        iseTabCompletions = null;
                    }
                }
            });
        }

        // Silently updates the terminal prompt without pausing the UI.
        // Used during debug breakpoints where we want to refresh the prompt
        // but keep the command line visible and usable while waiting for the response.
        function silentRefreshPrompt() {
            if (!iseTerminal) return;
            getPowerShellResponseAsync(
                { "guid": guid, "command": "", "stringFormat": "jsterm" },
                "ExecuteCommand",
                function (json) {
                    try {
                        var data = JSON.parse(json.d);
                        if (data && data["prompt"]) {
                            iseLastPrompt = data["prompt"];
                            if (iseTerminal) {
                                iseTerminal.set_prompt(getIsePrompt());
                            }
                        }
                    } catch (e) { /* ignore parse errors */ }
                },
                function () { /* silently ignore network errors */ }
            );
        }

        initIseTerminal();

        $.terminal.defaults.formatters.push(function (string) {
            return string.split(/((?:\s|&nbsp;)+)/).map(function (string) {
                if (/^[a-zA-Z]{1,}[\-][a-zA-Z]*\b$/g.test(string)) {
                    return '[[;yellow;]' + string + ']';
                } else if (/^([\-])([a-zA-Z]{1,})\b$/g.test(string)) {
                    return '[[;magenta;]' + string + ']';
                } else if (/^[$][a-zA-Z_][a-zA-Z0-9_]*\b/g.test(string)) {
                    return '[[;lightgreen;]' + string + ']';
                } else {
                    return string;
                }
            }).join('');
        });

        $("#CopyResultsToClipboard").on("click", function () {
            clipboard.copy(spe.getOutput());
        });


        spe.getOutput = function () {
            if (!iseTerminal) return "";
            return iseTerminal.find(".terminal-output").text();
        };

        spe.clearOutput = function () {
            if (iseTerminal) {
                iseTerminal.clear();
            }
            clearVariablesCache();
        };

        // Script output in jquery.terminal's native jsterm format. Text
        // contains color wrappers like [[;#fg;#bg]text]; jquery.terminal
        // parses them natively via echo (no `raw` flag).
        //
        // To support Write-Host -NoNewline correctly (inline-append
        // streaming), three functions cooperate:
        //
        //   spe.appendOutput(jsterm)          - commit a new line. Resets
        //                                       any pending partial.
        //   spe.updatePartialOutput(jsterm)   - create or update an
        //                                       in-progress "partial" line
        //                                       that may grow on subsequent
        //                                       polls. Uses jquery.terminal
        //                                       update() to replace the
        //                                       line in place.
        //   spe.commitPartialOutput(jsterm)   - replace the pending line
        //                                       with final text and release
        //                                       the pending-partial state.
        //   spe.finalizePartial()             - release the pending-partial
        //                                       state without updating text
        //                                       (used at script end).
        //
        // pendingPartialLineIndex is the jquery.terminal line index of the
        // current in-progress partial (-1 if none). Updates are applied via
        // iseTerminal.update(index, text, options).
        var pendingPartialLineIndex = -1;

        function appendHistoryEntry(text, partial) {
            if (!currentEditorSession.resultsHistory) {
                currentEditorSession.resultsHistory = [];
            }
            currentEditorSession.resultsHistory.push({ text: text, raw: false, partial: !!partial });
            trimResultsHistory();
        }

        function replacePartialHistoryEntry(text) {
            var history = currentEditorSession.resultsHistory;
            if (!history || history.length === 0) {
                appendHistoryEntry(text, true);
                return;
            }
            var last = history[history.length - 1];
            if (last && typeof last === "object" && last.partial) {
                last.text = text;
            } else {
                appendHistoryEntry(text, true);
            }
        }

        function commitPartialHistoryEntry(text) {
            var history = currentEditorSession.resultsHistory;
            if (!history || history.length === 0) {
                appendHistoryEntry(text, false);
                return;
            }
            var last = history[history.length - 1];
            if (last && typeof last === "object" && last.partial) {
                last.text = text;
                last.partial = false;
            } else {
                appendHistoryEntry(text, false);
            }
        }

        // Strip a trailing CR/LF from jsterm text before handing it to
        // jquery.terminal's echo/update. The server's GetTerminalLine adds
        // \r\n to terminated lines, and a trailing newline in echo/update
        // input causes jquery.terminal to split into [content, ""] and
        // produce a phantom empty row inside the block. echo/update already
        // implicitly start a new logical line per call, so we never want
        // the trailing newline.
        function stripTrailingNewline(text) {
            if (!text) return text;
            if (text.length >= 2 && text.charAt(text.length - 2) === "\r" && text.charAt(text.length - 1) === "\n") {
                return text.substring(0, text.length - 2);
            }
            if (text.charAt(text.length - 1) === "\n") {
                return text.substring(0, text.length - 1);
            }
            return text;
        }

        spe.appendOutput = function (outputToAppend) {
            if (!iseTerminal) {
                console.error("[spe] appendOutput: terminal not initialized, output lost");
                return;
            }
            outputToAppend = stripTrailingNewline(outputToAppend);
            iseTerminal.echo(outputToAppend, { finalize: finalizeGuidLinks });
            // Any previous pending partial is now closed; the new echo is
            // the current "last line" and is fully committed.
            pendingPartialLineIndex = -1;
            appendHistoryEntry(outputToAppend, false);
            clearVariablesCache();
        };

        spe.updatePartialOutput = function (outputToAppend) {
            if (!iseTerminal) {
                console.error("[spe] updatePartialOutput: terminal not initialized, output lost");
                return;
            }
            outputToAppend = stripTrailingNewline(outputToAppend);
            if (pendingPartialLineIndex >= 0) {
                iseTerminal.update(pendingPartialLineIndex, outputToAppend, { finalize: finalizeGuidLinks });
                replacePartialHistoryEntry(outputToAppend);
            } else {
                iseTerminal.echo(outputToAppend, { finalize: finalizeGuidLinks });
                pendingPartialLineIndex = iseTerminal.last_index();
                appendHistoryEntry(outputToAppend, true);
            }
            clearVariablesCache();
        };

        spe.commitPartialOutput = function (outputToAppend) {
            if (!iseTerminal) {
                console.error("[spe] commitPartialOutput: terminal not initialized, output lost");
                return;
            }
            outputToAppend = stripTrailingNewline(outputToAppend);
            if (pendingPartialLineIndex >= 0) {
                iseTerminal.update(pendingPartialLineIndex, outputToAppend, { finalize: finalizeGuidLinks });
                commitPartialHistoryEntry(outputToAppend);
            } else {
                iseTerminal.echo(outputToAppend, { finalize: finalizeGuidLinks });
                appendHistoryEntry(outputToAppend, false);
            }
            pendingPartialLineIndex = -1;
            clearVariablesCache();
        };

        spe.finalizePartial = function () {
            // Close the pending partial without updating its text - the
            // currently rendered content is considered final. Used at
            // script end so subsequent appends start a new visual line.
            if (pendingPartialLineIndex >= 0) {
                // Mark the last partial history entry as committed.
                var history = currentEditorSession.resultsHistory;
                if (history && history.length > 0) {
                    var last = history[history.length - 1];
                    if (last && typeof last === "object" && last.partial) {
                        last.partial = false;
                    }
                }
            }
            pendingPartialLineIndex = -1;
        };

        // Append literal HTML fragments (error spans, deferred-message
        // blocks with bespoke CSS classes, etc.) via jquery.terminal's raw
        // echo. Used for the small set of non-output-buffer messages the
        // ISE emits for error display and deferred-action reporting.
        spe.appendHtmlOutput = function (htmlToAppend) {
            if (!iseTerminal) {
                console.error("[spe] appendHtmlOutput: terminal not initialized, output lost");
                return;
            }
            var decoded = $("<div/>").html(htmlToAppend).text();
            iseTerminal.echo(decoded, { raw: true });
            // An HTML echo also closes the pending partial visually.
            pendingPartialLineIndex = -1;
            appendHistoryEntry(decoded, false);
            // Mark the last history entry as raw since we used raw echo.
            var history = currentEditorSession.resultsHistory;
            if (history && history.length > 0) {
                history[history.length - 1].raw = true;
            }
            clearVariablesCache();
        };

        spe.changeFontFamily = function (setting) {
            setting = setting || "Monaco";
            editorFontFamily = setting;
            editorSessions.forEach(function (editor) {
                editor.editor.setOption("fontFamily", setting);
            });
        };

        spe.changeFontSize = function (setting) {
            setting = parseInt(setting) || 12;
            editorFontSize = setting;
            editorSessions.forEach(function (editor) {
                editor.editor.setOption("fontSize", setting);
            });
        };

        spe.changeTerminalSettings = function (fontFamily, fontSize, backgroundColor, foregroundColor) {
            fontFamily = fontFamily || "Monaco";
            fontSize = parseInt(fontSize) || 12;
            $("#ScriptResult").css({
                "font-family": fontFamily,
                "font-size": fontSize + "px",
                "background-color": backgroundColor,
                "color": foregroundColor
            });
            $("#Result").css({"background-color": backgroundColor});
            $("#ScriptResultCode").css({
                "font-family": fontFamily,
                "font-size": fontSize + "px",
                "background-color": backgroundColor,
                "color": foregroundColor
            });
        };

        spe.changeSettings = function (fontFamily, fontSize, bottomOffset, liveAutocompletion, perTabOutput) {
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
            iseDebugActive = false;
            // Only hide terminal if the script is still running (stepping),
            // not when the debug session has ended (scriptExecutionEnded handles that)
            if (iseTerminal && iseScriptRunning) {
                iseTerminal.find(".cmd").css("visibility", "hidden");
                iseTerminal.pause();
            }
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
            // Enable terminal for debug inspection
            iseDebugActive = true;
            if (iseTerminal) {
                iseTerminal.find(".cmd").css("visibility", "visible");
                iseTerminal.resume();
                iseTerminal.set_prompt(getIsePrompt());
            }
            // Silently refresh the prompt from the paused session without blocking
            // the command line - the path may have changed since the last command
            // (especially if the script cd'd before the breakpoint).
            silentRefreshPrompt();
            // Refresh the Variables panel when the breakpoint is hit so the user
            // can inspect the current variable state without manually clicking refresh.
            if (spe.refreshVariables) {
                spe.refreshVariables();
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

        spe.reloadCurrentEditor = function (content) {
            var aceSession = currentAceEditor.getSession();
            aceSession.setValue(content);
            // Anchor the clean bookmark so the deferred Ace input event
            // does not mark the tab dirty right after the reload.
            aceSession.getUndoManager().markClean();
            currentEditorSession.isModified = false;
            currentEditorSession.isDeleted = false;
            spe.applyWindowTitle(currentEditorIndex);
            editor.val(content);
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
            iseScriptRunning = false;
            iseDebugActive = false;
            // Stop the busy spinner; this also restores the normal prompt via
            // getIsePrompt() using the prompt text the server pushed during
            // MonitorOnJobFinished.
            spe.hideBusy();
            // Restore the terminal command line now that the script has finished.
            if (iseTerminal) {
                iseTerminal.resume();
            }
            // Refresh the Variables panel - the script may have created or modified variables
            if (spe.refreshVariables) {
                spe.refreshVariables();
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

        function escapeHtml(str) {
            var div = document.createElement("div");
            div.appendChild(document.createTextNode(str));
            return div.innerHTML;
        }

        spe.changeTabDetails = function (codeEditorIndex, newTitle, startBarHtml, scriptId, scriptDb) {

            var editorSession = getSessionByIndex(codeEditorIndex);
            var itemName = newTitle.split("/").last();
            editorSession.name = itemName;
            editorSession.path = newTitle;
            var safePath = escapeHtml(editorSession.path);
            var safeName = escapeHtml(editorSession.name);
            const newTabTitle = replaceAll(safePath, "/", "</i> / <i style=\"font-style: italic; color: #bbb;\">");
            editorSession.tabTitleInnerHTML = "<span title=\"" + safePath + "\"><span class=\"scriptModified ModifiedMark#tabindex#\">⬤</span> " + safeName +
                "</i>" +
                "<span class=\"closeTab\" onclick=\"javascript:spe.closeTab(event, #tabindex#)\">&nbsp;</span>";
            const newWindowTitle = replaceAll(safePath, "/", "</i> / <i style=\"font-style: italic; color: #bbb;\">");
            editorSession.windowTitleInnerHTML = "<span class=\"scriptModified ModifiedMark#tabindex#\" style='color:#fc2929;#styles#'>⬤</span> <i style=\"font-style: italic; color: #bbb;\">" + newWindowTitle + "</i> - ";
            editorSession.tabTitleHtml = replaceAll(safePath, "/", "</i> / <i style=\"font-style: italic; color: #bbb;\">");
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
                var windowStyles = editorSession.isModified || editorSession.isDeleted ? "display:inline;" : "display:none;";
                var windowHtml = editorSession.windowTitleInnerHTML.replace("#tabindex#", editorSession.index).replace("#styles#", windowStyles);
                if (editorSession.isDeleted) {
                    windowHtml = windowHtml.replace("\u2B24", "\u2716");
                }
                windowCaption[0].innerHTML = windowHtml;
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
                    var tabHtml = currEditor.tabTitleInnerHTML.replaceAll("#tabindex#", currEditor.index);
                    if (currEditor.isDeleted) {
                        tabHtml = tabHtml.replace("\u2B24", "\u2716");
                    }
                    tabHeader[0].innerHTML = tabHtml;
                    if (currEditor.isModified || currEditor.isDeleted) {
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
            var openedScripts = editorSessions.map(session => session.path + ":" + session.index).join('\n');
            openedScriptsMemo.val(openedScripts);
            
        }

        spe.resizeEditor = function () {
            if (currentAceEditor !== undefined) {
                currentAceEditor.resize();
            }
            if (splitOrientation === "vertical") {
                $("#Result").css({ height: "100%", width: "100%" });
            } else {
                var resultsHeight = $(window).height() - $("#ResultsSplitter").offset().top - $("#ResultsSplitter").height() - $("#StatusBar").height() - resultsBottomOffset - 10;
                $("#Result").height(resultsHeight);
                $("#Result").width($(window).width() - $("#Result").offset().left * 2);
                $("#Result").width($("#ResultsPane").width());
            }
            // ProgressOverlay positioning is handled entirely by CSS now -
            // it's position:absolute relative to #Result, which is position:relative.
            // The old JS below used $.offset() (document coordinates) which was
            // correct when the overlay had no positioned ancestor, but now it
            // double-offsets the overlay off-screen.
            $("#TreeViewToggle").css("top", ($("#TabsPanel").offset().top + 8) + "px");
            $("#TreeViewToggle").css("left", ($("#TabsPanel").offset().left + 6) + "px");
            $("#VariablesToggle").css("top", ($("#TabsPanel").offset().top + 8) + "px");
            $("#VariablesToggle").css("left", ($("#TabsPanel").offset().left + 28) + "px");
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
            iseScriptRunning = true;
            // Pause the terminal so the user can't submit a concurrent command
            // against the busy session. The `visible` arg (pause(true)) keeps
            // the prompt element visible - without it jquery.terminal calls
            // command_line.find('.prompt').hidden() and our spe.showBusy
            // set_prompt updates are invisible.
            if (iseTerminal) {
                iseTerminal.pause(true);
            }
            if (splitOrientation === "vertical") {
                // Restore left pane flex from saved splitter position
                var leftPane = document.getElementById("VerticalLeftPane");
                if (leftPane && splitPosition.vertical) {
                    var containerWidth = leftPane.parentNode.offsetWidth;
                    var minWidth = Math.round(containerWidth * 0.25);
                    var restoredWidth = Math.round(splitPosition.vertical * containerWidth);
                    restoredWidth = Math.max(minWidth, Math.min(restoredWidth, containerWidth - minWidth - 10));
                    leftPane.style.flexBasis = restoredWidth + "px";
                    leftPane.style.flexGrow = "0";
                    leftPane.style.flexShrink = "0";
                } else if (leftPane) {
                    leftPane.style.flexBasis = "";
                    leftPane.style.flexGrow = "";
                    leftPane.style.flexShrink = "";
                }
                $("#VerticalSplitter").show();
                $("#VerticalRightPane").show();
            } else {
                $("#ResultsSplitter").show();
                $("#ResultsRow").show();
            }
            spe.resizeEditor();
            $("#ResultsStatusBarAction").removeClass("status-bar-results-hidden")
        };

        spe.closeResults = function () {
            if (splitOrientation === "vertical") {
                $("#VerticalSplitter").hide();
                $("#VerticalRightPane").hide("slow", function () {
                    // Let the left pane fill the entire container width
                    var leftPane = document.getElementById("VerticalLeftPane");
                    if (leftPane) {
                        leftPane.style.flexBasis = "100%";
                        leftPane.style.flexGrow = "1";
                        leftPane.style.flexShrink = "1";
                    }
                    spe.resizeEditor();
                });
            } else {
                $("#ResultsSplitter").hide();
                $("#ResultsRow").hide("slow", function () {
                    spe.resizeEditor();
                });
            }
            $("#ResultsStatusBarAction").addClass("status-bar-results-hidden")
        };

        function applySplitOrientation(orientation) {
            var editingPanel = document.getElementById("EditingPanel");
            if (!editingPanel) return;
            var table = editingPanel.querySelector("table");
            var container = document.getElementById("VerticalSplitContainer");
            var toggleBtn = document.getElementById("ToggleSplitOrientation");

            if (orientation === "vertical") {
                if (container) return;

                var editingArea = document.getElementById("EditingArea");
                var result = document.getElementById("Result");
                if (!editingArea || !result) return;

                // Save original inline height set by GridPanel
                editingArea.dataset.originalHeight = editingArea.style.height;

                // Create flex container with left pane, splitter, right pane
                container = document.createElement("div");
                container.id = "VerticalSplitContainer";

                var leftPane = document.createElement("div");
                leftPane.id = "VerticalLeftPane";

                var splitter = document.createElement("div");
                splitter.id = "VerticalSplitter";
                splitter.title = "Drag to resize, double-click to reset";

                var rightPane = document.createElement("div");
                rightPane.id = "VerticalRightPane";

                // Move content into panes
                leftPane.appendChild(editingArea);
                rightPane.appendChild(result);

                container.appendChild(leftPane);
                container.appendChild(splitter);
                container.appendChild(rightPane);

                // Hide table, insert container
                table.style.display = "none";
                editingPanel.appendChild(container);

                // Override the GridPanel inline height
                editingArea.style.height = "100%";

                // Set up vertical splitter drag
                initVerticalSplitter(splitter, leftPane);

                if (toggleBtn) {
                    toggleBtn.parentNode.classList.add("split-orientation-active");
                    toggleBtn.src = "/~/icon/office/16x16/mirror_horizontally.png";
                    toggleBtn.title = "Switch to horizontal split";
                    toggleBtn.alt = "Switch to horizontal split";
                }
            } else {
                if (!container) return;

                var editingArea = document.getElementById("EditingArea");
                var result = document.getElementById("Result");
                var scriptPaneTd = document.getElementById("ScriptPane");
                var resultsPaneTd = document.getElementById("ResultsPane");

                // Restore original inline height
                if (editingArea) {
                    editingArea.style.height = editingArea.dataset.originalHeight || "";
                }

                // Move content back to original table positions
                if (editingArea && scriptPaneTd) scriptPaneTd.appendChild(editingArea);
                if (result && resultsPaneTd) resultsPaneTd.appendChild(result);

                // Show table, remove container
                table.style.display = "";
                editingPanel.removeChild(container);

                if (toggleBtn) {
                    toggleBtn.parentNode.classList.remove("split-orientation-active");
                    toggleBtn.src = "/~/icon/office/16x16/mirror_vertically.png";
                    toggleBtn.title = "Switch to vertical split";
                    toggleBtn.alt = "Switch to vertical split";
                }
            }
        }

        spe.saveSplitPosition = function (orientation, value) {
            splitPosition[orientation] = value;
            localStorage.setItem("spe::ise.splitPosition", JSON.stringify(splitPosition));
        };

        function initVerticalSplitter(splitter, leftPane) {
            var dragging = false;
            var startX, startWidth;
            var minPanePct = 0.25; // 25% minimum pane width

            // Restore saved position
            if (splitPosition.vertical) {
                var containerWidth = leftPane.parentNode.offsetWidth;
                var minWidth = Math.round(containerWidth * minPanePct);
                var restoredWidth = Math.round(splitPosition.vertical * containerWidth);
                restoredWidth = Math.max(minWidth, Math.min(restoredWidth, containerWidth - minWidth - 10));
                leftPane.style.flexBasis = restoredWidth + "px";
                leftPane.style.flexGrow = "0";
                leftPane.style.flexShrink = "0";
            }

            splitter.addEventListener("mousedown", function (e) {
                dragging = true;
                startX = e.clientX;
                startWidth = leftPane.offsetWidth;
                document.body.style.cursor = "col-resize";
                document.body.style.userSelect = "none";
                e.preventDefault();
            });

            splitter.addEventListener("dblclick", function (e) {
                leftPane.style.flexBasis = "";
                leftPane.style.flexGrow = "";
                leftPane.style.flexShrink = "";
                spe.saveSplitPosition("vertical", 0.5);
                spe.resizeEditor();
                e.preventDefault();
            });

            document.addEventListener("mousemove", function (e) {
                if (!dragging) return;
                var dx = e.clientX - startX;
                var containerWidth = leftPane.parentNode.offsetWidth;
                var minWidth = Math.round(containerWidth * minPanePct);
                var maxWidth = containerWidth - minWidth - 10;
                var newWidth = Math.max(minWidth, Math.min(startWidth + dx, maxWidth));
                leftPane.style.flexBasis = newWidth + "px";
                leftPane.style.flexGrow = "0";
                leftPane.style.flexShrink = "0";
                e.preventDefault();
            });

            document.addEventListener("mouseup", function (e) {
                if (!dragging) return;
                dragging = false;
                document.body.style.cursor = "";
                document.body.style.userSelect = "";
                var containerWidth = leftPane.parentNode.offsetWidth;
                spe.saveSplitPosition("vertical", leftPane.offsetWidth / containerWidth);
                spe.resizeEditor();
            });
        }

        spe.toggleSplitOrientation = function () {
            splitOrientation = splitOrientation === "horizontal" ? "vertical" : "horizontal";
            localStorage.setItem("spe::ise.splitOrientation", splitOrientation);
            applySplitOrientation(splitOrientation);
            // Defer resize to allow the DOM to reflow after layout changes
            setTimeout(function () {
                spe.resizeEditor();
            }, 0);
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
            var resultsVisible = splitOrientation === "vertical"
                ? $("#VerticalRightPane").is(":visible")
                : $("#ResultsRow").is(":visible");
            if (resultsVisible) {
                resultsVisibilityIntent = false;
                spe.closeResults();
            } else {
                resultsVisibilityIntent = true;
                spe.restoreResults();
            }
        });
        $("#ToggleSplitOrientation").click(function () {
            spe.toggleSplitOrientation();
        });
        $("#ResultsSplitter").on("dblclick", function (e) {
            var img = $("#ResultsSplitter img")[0];
            if (img) {
                scHSplit.dblClick(img, e, "IDEXsltBottom", "top");
            }
        });
        // Tree view and Variables panel share the same sidebar cell (#SidebarPanel)
        // and are mutually exclusive. The sidebar uses the existing #TreeSplitter
        // for resizing - since both panels live in the same cell, they inherently
        // share the same user-chosen width. Clicking a panel's toggle:
        //  - if that panel is already visible   -> hide the sidebar
        //  - if the *other* panel is visible    -> switch to this one (sidebar stays)
        //  - if nothing is visible              -> open sidebar showing this panel

        function isSidebarVisible() {
            return $("#SidebarPanel").is(":visible");
        }
        function currentSidebarContent() {
            if ($("#VariablesView").is(":visible")) return "variables";
            if ($("#TreeView").is(":visible")) return "tree";
            return null;
        }
        function openSidebar(which) {
            $("#SidebarPanel").show();
            $("#TreeSplitterPanel").show();
            if (which === "variables") {
                $("#TreeView").hide();
                $("#VariablesView").show();
                spe.refreshVariables();
            } else {
                $("#VariablesView").hide();
                $("#TreeView").show();
                scForm.postRequest("", "", "", "ise:updatetreeview");
            }
        }
        function closeSidebar() {
            $("#SidebarPanel").hide();
            $("#TreeSplitterPanel").hide();
        }

        $("#TreeViewToggle").click(function () {
            if (isSidebarVisible() && currentSidebarContent() === "tree") {
                closeSidebar();
            } else {
                openSidebar("tree");
            }
            spe.resizeEditor();
        });

        // Variables panel - shows PowerShell session variables from the current
        // ISE session, refreshing automatically after script execution and when
        // terminal commands complete. Values use the same GetVariableValue style
        // tooltip source as the inline variable hover tooltip, but show the
        // entire user-defined variable set at once.
        function escapeVarHtml(s) {
            return $("<div/>").text(s == null ? "" : String(s)).html();
        }

        // Persist the collapsed state of each Variables section and individual
        // expanded variables across refreshes so the user's choices aren't reset
        // every time variables are reloaded.
        // Section defaults: user expanded, builtin collapsed.
        var iseVariablesSectionCollapsed = {
            "user": false,
            "builtin": true
        };
        // Set of variable names (without $) that the user has expanded.
        var iseExpandedVariables = {};
        // Cached per-variable detail HTML so re-expanding is instant.
        var iseVariableDetailCache = {};

        function fetchVariableDetails(name, container) {
            if (iseVariableDetailCache[name] !== undefined) {
                container.html(iseVariableDetailCache[name]);
                return;
            }
            container.html("<div class='spe-variable-loading'>Loading…</div>");
            getPowerShellResponseAsync(
                { "guid": guid, "variableName": name },
                "GetVariableValue",
                function (json) {
                    // GetVariableValue returns the HTML fragment directly as json.d (a string,
                    // not a JSON object), matching what the inline hover tooltip consumes.
                    var html = (json && typeof json.d === "string") ? json.d : "";
                    iseVariableDetailCache[name] = html;
                    container.html(html);
                },
                function (xhr, status, err) {
                    container.html("<div class='spe-variable-loading'>Error: " + (err || status) + "</div>");
                }
            );
        }

        function renderVariablesSection(category, title, entries) {
            var section = $("<div/>").addClass("spe-variables-section").attr("data-category", category);
            var header = $("<div/>").addClass("spe-variables-section-header");
            var caret = $("<span/>").addClass("spe-variables-section-caret").text(iseVariablesSectionCollapsed[category] ? "▶" : "▼");
            header.append(caret);
            header.append($("<span/>").addClass("spe-variables-section-title").text(title));
            header.append($("<span/>").addClass("spe-variables-section-count").text("(" + entries.length + ")"));
            section.append(header);

            var body = $("<div/>").addClass("spe-variables-section-body");
            if (iseVariablesSectionCollapsed[category]) {
                body.hide();
            }
            if (entries.length === 0) {
                body.append($("<div/>").addClass("spe-variables-empty").text("None"));
            } else {
                entries.forEach(function (v) {
                    var entry = $("<div/>").addClass("spe-variable");
                    if (v.expandable) {
                        entry.addClass("spe-variable-expandable");
                    }

                    var nameRow = $("<div/>").addClass("spe-variable-name-row");
                    // Chevron placeholder keeps horizontal alignment consistent between
                    // expandable and non-expandable entries.
                    var varCaret = $("<span/>").addClass("spe-variable-caret");
                    if (v.expandable) {
                        varCaret.text(iseExpandedVariables[v.name] ? "▼" : "▶");
                    } else {
                        varCaret.html("&nbsp;");
                    }
                    nameRow.append(varCaret);
                    nameRow.append($("<span/>").addClass("spe-variable-name").text("$" + v.name));
                    nameRow.append($("<span/>").addClass("spe-variable-type").text(v.type));
                    entry.append(nameRow);
                    entry.append($("<span/>").addClass("spe-variable-value").text(v.value));

                    if (v.expandable) {
                        var detail = $("<div/>").addClass("spe-variable-detail");
                        entry.append(detail);
                        if (iseExpandedVariables[v.name]) {
                            detail.show();
                            fetchVariableDetails(v.name, detail);
                        } else {
                            detail.hide();
                        }
                        // Prevent clicks inside the rendered detail content from
                        // bubbling up and collapsing the entry.
                        detail.on("click", function (e) { e.stopPropagation(); });
                        // Clicking anywhere on the entry (name row or value line)
                        // toggles expansion.
                        entry.on("click", function () {
                            var willExpand = !iseExpandedVariables[v.name];
                            if (willExpand) {
                                iseExpandedVariables[v.name] = true;
                                varCaret.text("▼");
                                detail.show();
                                fetchVariableDetails(v.name, detail);
                            } else {
                                delete iseExpandedVariables[v.name];
                                varCaret.text("▶");
                                detail.hide();
                            }
                        });
                    }

                    body.append(entry);
                });
            }
            section.append(body);

            // Toggle collapse on header click.
            header.on("click", function () {
                iseVariablesSectionCollapsed[category] = !iseVariablesSectionCollapsed[category];
                body.toggle(!iseVariablesSectionCollapsed[category]);
                caret.text(iseVariablesSectionCollapsed[category] ? "▶" : "▼");
            });

            return section;
        }

        spe.refreshVariables = function () {
            if (!$("#VariablesView").is(":visible")) {
                return;
            }
            var list = $("#VariablesList");
            if (list.length === 0) {
                console.error("[spe] refreshVariables: #VariablesList element not found in DOM");
                return;
            }
            // Invalidate cached detail HTML so expanded variables re-fetch on refresh.
            iseVariableDetailCache = {};
            getPowerShellResponseAsync(
                { "guid": guid },
                "GetSessionVariables",
                function (json) {
                    var payload;
                    try { payload = JSON.parse(json.d); } catch (e) {
                        list.empty().append($("<div/>").addClass("spe-variables-empty").text("Error parsing response: " + e.message));
                        return;
                    }
                    list.empty();
                    if (!payload || (payload.status !== "ok" && payload.status !== "no-session")) {
                        list.append($("<div/>").addClass("spe-variables-empty").text("Unexpected response: " + (payload ? payload.status : "null")));
                        return;
                    }
                    if (payload.status === "no-session") {
                        list.append($("<div/>").addClass("spe-variables-empty").text("No session in memory. Run a script first."));
                        return;
                    }
                    var vars = payload.variables || [];
                    var userVars = vars.filter(function (v) { return v.category !== "builtin"; });
                    var builtInVars = vars.filter(function (v) { return v.category === "builtin"; });

                    // User section first (expanded by default), built-in second (collapsed by default).
                    list.append(renderVariablesSection("user", "User variables", userVars));
                    list.append(renderVariablesSection("builtin", "Built-in variables", builtInVars));
                },
                function (xhr, status, err) {
                    list.empty().append($("<div/>").addClass("spe-variables-empty").text("Error: " + status + " " + err));
                }
            );
        };

        $("#VariablesToggle").click(function () {
            if (isSidebarVisible() && currentSidebarContent() === "variables") {
                closeSidebar();
            } else {
                openSidebar("variables");
            }
            spe.resizeEditor();
        });

        $(document).on("click", "#VariablesRefresh", function (e) {
            // Clicking the refresh icon inside the Variables header should
            // only refresh - not bubble up to any parent click handlers.
            e.stopPropagation();
            spe.refreshVariables();
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

        applySplitOrientation(splitOrientation);

        // Restore saved horizontal splitter position
        if (splitPosition.horizontal && splitOrientation === "horizontal") {
            var scriptPane = document.getElementById("ScriptPane");
            if (scriptPane) {
                scriptPane.style.height = splitPosition.horizontal + "px";
            }
        }

        $(window).on('resize', function () {
            spe.resizeEditor();
        }).trigger('resize');

        setTimeout(function () {
            scForm.postRequest("", "", "", "ise:updatesettings");
            scForm.postRequest("", "", "", "ise:restoreactivetabs");
            spe.resizeEditor();
            // Prime the ISE PowerShell session on the server with the correct
            // application type (ISE) and context item location. The server
            // pushes the current prompt back via spe.setTerminalPrompt.
            scForm.postRequest("", "", "", "ise:initterminal");
        }, 100);

        const resizeObserver = new ResizeObserver((entries) => {
            spe.resizeEditor();
        })
        resizeObserver.observe(document.getElementById("EditingPanel"));        
    });
}(jQuery, window, window.spe = window.spe || {}, window.ace = window.ace || {}));
