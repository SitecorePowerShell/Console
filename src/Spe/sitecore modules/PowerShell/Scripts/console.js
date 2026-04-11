(function($, window, spe, undefined) {
    var defaults = {
        initialPoll: 100,
        maxPoll: 2500,
        keepAliveInterval: 60000, // 60 * 1000 - every minute
        keepAliveCheck: 2000, // 2 * 1000 - every 2 seconds
        monitorActive: true,
        busyMessages: []
    };

    var settings = defaults;

    // =====================================================================
    // Busy indicator - animated spinner in the terminal prompt line.
    // Same pattern as the ISE. Replaces the old bottom-right #working GIF,
    // which was easy to miss since it lived outside the terminal.
    // =====================================================================
    var speSpinnerFrames = ["\u280B", "\u2819", "\u2839", "\u2838", "\u283C",
                            "\u2834", "\u2826", "\u2827", "\u2807", "\u280F"];
    var speBusyInterval = null;
    var speBusyMessage = "";
    var speBusyFrame = 0;
    var consoleLastPrompt = "PS >";
    var consoleTerminal = null;

    function getConsolePrompt() {
        return "[[;white;]" + consoleLastPrompt + "]";
    }

    function getBusyPrompt() {
        var frame = speSpinnerFrames[speBusyFrame];
        return "[[;#4ec9b0;]" + frame + "] [[;#d4d4d4;]" + speBusyMessage + "]";
    }

    function pickBusyMessage() {
        var messages = settings.busyMessages;
        if (!messages || messages.length === 0) return "Working...";
        return messages[Math.floor(Math.random() * messages.length)];
    }

    spe.setTerminalPrompt = function (prompt) {
        if (prompt) {
            consoleLastPrompt = prompt;
        }
        if (consoleTerminal && !speBusyInterval) {
            consoleTerminal.set_prompt(getConsolePrompt());
        }
    };

    spe.showBusy = function (message) {
        speBusyMessage = message || "";
        speBusyFrame = 0;
        if (!consoleTerminal) return;
        consoleTerminal.set_prompt(getBusyPrompt());
        if (speBusyInterval) return;
        speBusyInterval = setInterval(function () {
            speBusyFrame = (speBusyFrame + 1) % speSpinnerFrames.length;
            if (consoleTerminal) {
                consoleTerminal.set_prompt(getBusyPrompt());
            }
        }, 80);
    };

    spe.hideBusy = function () {
        if (speBusyInterval) {
            clearInterval(speBusyInterval);
            speBusyInterval = null;
        }
        speBusyMessage = "";
        if (consoleTerminal) {
            consoleTerminal.set_prompt(getConsolePrompt());
        }
    };

    // =====================================================================
    // Streaming output - inline-append support for Write-Host -NoNewline.
    // The server emits a list of structured `emits` ({ op, text }) in the
    // PollCommandOutput response, and we dispatch each to the appropriate
    // spe function here.
    //
    // pendingPartialLineIndex is the jquery.terminal line index of the
    // current in-progress partial line (-1 if none). Partial updates use
    // iseTerminal.update(index, text, opts) to replace the line in place
    // so successive Write-Host -NoNewline calls render inline rather than
    // as separate terminal lines.
    // =====================================================================
    var pendingPartialLineIndex = -1;

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

    // Strip a trailing CR/LF from jsterm text before handing it to
    // jquery.terminal's echo/update. The server's GetTerminalLine adds
    // \r\n to terminated lines, and a trailing newline in echo/update
    // input causes jquery.terminal to split into [content, ""] and
    // produce a phantom empty row inside the block. echo/update already
    // implicitly start a new logical line per call, so we never want the
    // trailing newline.
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
        if (!consoleTerminal) return;
        outputToAppend = stripTrailingNewline(outputToAppend);
        consoleTerminal.echo(outputToAppend, { finalize: finalizeGuidLinks });
        pendingPartialLineIndex = -1;
    };

    spe.updatePartialOutput = function (outputToAppend) {
        if (!consoleTerminal) return;
        outputToAppend = stripTrailingNewline(outputToAppend);
        if (pendingPartialLineIndex >= 0) {
            consoleTerminal.update(pendingPartialLineIndex, outputToAppend, { finalize: finalizeGuidLinks });
        } else {
            consoleTerminal.echo(outputToAppend, { finalize: finalizeGuidLinks });
            pendingPartialLineIndex = consoleTerminal.last_index();
        }
    };

    spe.commitPartialOutput = function (outputToAppend) {
        if (!consoleTerminal) return;
        outputToAppend = stripTrailingNewline(outputToAppend);
        if (pendingPartialLineIndex >= 0) {
            consoleTerminal.update(pendingPartialLineIndex, outputToAppend, { finalize: finalizeGuidLinks });
        } else {
            consoleTerminal.echo(outputToAppend, { finalize: finalizeGuidLinks });
        }
        pendingPartialLineIndex = -1;
    };

    spe.finalizePartial = function () {
        pendingPartialLineIndex = -1;
    };

    function dispatchEmits(emits) {
        if (!emits || !emits.length) return;
        for (var i = 0; i < emits.length; i++) {
            var emit = emits[i];
            if (emit.op === "append") {
                spe.appendOutput(emit.text);
            } else if (emit.op === "partial") {
                spe.updatePartialOutput(emit.text);
            } else if (emit.op === "commit") {
                spe.commitPartialOutput(emit.text);
            }
        }
    }

    function escapeHtml(str) {
        var div = document.createElement("div");
        div.appendChild(document.createTextNode(str));
        return div.innerHTML;
    }

    var tabCompletions = null;
    var lastUpdate = 0;
    var attempts = 0;
    var pausedCommand = null;

    spe.setOptions = function (options) {
        $.extend(settings, options);

        var fontSize = parseInt(settings.fontSize);
        fontSize = Math.max(fontSize, 12);
        fontSize = Math.min(fontSize, 25);
        var fontFamily = settings.fontFamily;
        $("#terminal").css({ "font-size": fontSize + "px", "font-family":  fontFamily });
    };

    spe.resetAttempts = function () {
        attempts = 0;
    };

    var checkInterval = setInterval(function() {
        if (new Date().getTime() - lastUpdate > settings.keepAliveInterval) {
            getPowerShellResponse({ "guid": guid }, "KeepAlive");
        }
    }, settings.keepAliveCheck);

    function getParam(name) {
        name = (new RegExp("[?&]" + encodeURIComponent(name) + "=([^&]*)")).exec(location.search);
        if (name) {
            return decodeURIComponent(decodeURIComponent(name[1]));
        }
        return undefined;
    }

    function getSessionId() {
        var id = getParam("id");
        if (id !== undefined) {
            return id;
        }
        var s4 = function() {
            return (((1 + Math.random()) * 0x10000) | 0).toString(16).substring(1);
        };
        return (s4() + s4() + "-" + s4() + "-" + s4() + "-" + s4() + "-" + s4() + s4() + s4());
    }

    function getUrlParameter(name) {
        return decodeURI(
            (RegExp(name + "=" + "(.+?)(&|$)").exec(location.search) || [, null])[1]
        );
    }

    function myUnescape(str) {
        return unescape(str).replace(/[+]/g, " ");
    }

    function isBlank(str) {
        return (!str || /^\s*$/.test(str));
    }

    function getPowerShellResponse(callData, remotefunction, doneFunction, errorFunction) {
        var datastring = JSON.stringify(callData);
        lastUpdate = new Date().getTime();
        var ajax =
            $.ajax({
                type: "POST",
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                url: "/sitecore modules/PowerShell/Services/PowerShellWebService.asmx/" + remotefunction,
                data: datastring,
                processData: false,
                cache: false,
                async: false
            }).done(doneFunction);
        if (typeof(errorFunction) != "undefined") {
            ajax.fail(errorFunction);
        }
    }

    function callPowerShellHost(term, guid, command) {
        // pause(true) keeps the prompt line visible so the animated busy
        // indicator (spe.showBusy below) has somewhere to render.
        term.pause(true);
        spe.showBusy(pickBusyMessage());
        spe.preventCloseWhenRunning(true);
        getPowerShellResponse({ "guid": guid, "command": command, "stringFormat": "jsterm" }, "ExecuteCommand",
            function(json) {
                var data = JSON.parse(json.d);
                if (data["status"] === "working") {
                    displayResult(term, data);
                    var handle = data["handle"];
                    var initialWait = settings.initialPoll;
                    var maxWait = settings.maxPoll;
                    if (settings.monitorActive) {
                        scForm.postRequest("", "", "", "pstaskmonitor:check(guid=" + guid + ",handle=" + handle + ")");
                    };
                    (function poll(wait) {
                        setTimeout(function() {
                            if (settings.monitorActive) {
                                getPowerShellResponse({ "guid": guid, "handle": handle, "stringFormat": "jsterm" }, "PollCommandOutput",
                                    function(pollJson) {
                                        var jsonData = JSON.parse(pollJson.d);
                                        var finished = false;
                                        if (jsonData["status"] === "working") {
                                            displayResult(term, jsonData);
                                            var textResult = jsonData["result"];
                                            // value returned stop throttling
                                            if (textResult || textResult.length > 0) {
                                                attempts = 0;
                                            }
                                            if (attempts >= 0) {
                                                // no value returned start throttling
                                                attempts++;
                                                var newWait = Math.pow(initialWait, 1 + (attempts / 10));
                                                if (newWait > maxWait) {
                                                    newWait = maxWait;
                                                    attempts = -1; //stop incrementing
                                                }
                                                poll(newWait);
                                            } else {
                                                poll(maxWait);
                                            }
                                        } else if (jsonData["status"] === "partial") {
                                            displayResult(term, jsonData);
                                            poll(initialWait);
                                        } else {
                                            displayResult(term, jsonData);
                                            finished = true;
                                        }
                                        scForm.postRequest("", "", "", "pstaskmonitor:check(guid=" + guid + ",handle=" + handle + ",finished=" + finished + ")");
                                    },
                                    function(jqXHR, textStatus, errorThrown) {
                                        spe.hideBusy();
                                        term.resume();
                                        spe.preventCloseWhenRunning(false);
                                        term.echo("Communication error: " + textStatus + "; " + errorThrown);
                                    }
                                );
                            } else {
                                poll(initialWait);
                            }
                        }, wait);
                    })(initialWait);
                } else if (data["status"] === "unauthorized") {
                    // Store command to be executed once session has been evaluated
                    pausedCommand = function () { callPowerShellHost(term, guid, command); };
                    spe.elevateSession();
                } else {
                    displayResult(term, data);
                    var handle = data["handle"];
                    scForm.postRequest("", "", "", "pstaskmonitor:check(guid=" + guid + ",handle=" + handle + ")");
                }
            }
        );
    }

    function displayResult(term, data) {
        var terminated = data["status"] !== "partial" && data["status"] !== "working";

        // Clear-Host called during command execution: purge the terminal
        // output before rendering any new result from this poll cycle.
        if (data["clear"]) {
            term.clear();
            pendingPartialLineIndex = -1;
        }

        // Preferred path: server sent a structured list of streaming emits.
        // Each emit is one of { op: "append"|"partial"|"commit", text }
        // and dispatches to spe.appendOutput / updatePartialOutput /
        // commitPartialOutput which uses jquery.terminal's update(index)
        // API for inline-append support of Write-Host -NoNewline.
        if (data["emits"] && data["emits"].length > 0) {
            dispatchEmits(data["emits"]);
        } else if (data["result"]) {
            // Fallback to the legacy flat result field (e.g. error paths).
            term.echo(data["result"], { finalize: finalizeGuidLinks });
            pendingPartialLineIndex = -1;
        }

        if (terminated) {
            spe.hideBusy();
            term.resume();
            spe.preventCloseWhenRunning(false);
            // Finalize any pending partial so subsequent commands start a
            // fresh visual line. The current rendered text is considered
            // the final form of the partial.
            spe.finalizePartial();
            if (data["prompt"]) {
                spe.setTerminalPrompt(data["prompt"]);
            }
            var background = data["background"];
            if (background !== undefined && background !== "null") {
                $("#terminal").css({ "background-color": background });
            }
            var color = data["color"];
            if (color !== undefined && color !== "null") {
                $("#terminal").css({ "color": color });
            }
        }

        $("html").animate({ scrollTop: $(document).height() }, "slow");
    }

    var sigHint = "";
    var tabCycleIndex = -1;
    var tabCycleActive = false;

    function completion(command, callback) {
        var term = this;
        var fullCommand = term.get_command();
        if (tabCycleActive && tabCompletions && tabCompletions.length > 0) {
            tabCycleIndex = (tabCycleIndex + 1) % tabCompletions.length;
            term.set_command(tabCompletions[tabCycleIndex]);
            return;
        }
        tabCompletionInit(fullCommand, function () {
            if (tabCompletions) {
                if (tabCompletions.length === 0) {
                    tabCompletionNoHints();
                } else if (tabCompletions.length === 1) {
                    term.set_command(tabCompletions[0]);
                } else {
                    tabCycleIndex = 0;
                    tabCycleActive = true;
                    term.set_command(tabCompletions[0]);
                }
            }
        });
    }

    function tabCompletionInit(command, callback) {
        getPowerShellResponse({ "guid": guid, "command": command }, "CompleteCommand",
            function (json) {
                var data = JSON.parse(json.d);
                sigHint = "";
                tabCompletions = data.filter(function (hint) {
                    var isSignature = hint.indexOf("Signature|") === 0;
                    if (isSignature) {
                        var hintParts = hint.split("|");
                        sigHint += escapeHtml(hintParts[1]) + "<br/>";
                    }
                    return !isSignature;
                }).map(function (hint) {
                    var hintParts = hint.split("|");
                    if (hintParts[0] === "Type") {
                        return "[" + hintParts[3];
                    }                    
                    return hint;
                    });
                if (callback) {
                    callback();
                }
                console.log("[spe] setting tabCompletions to: ", tabCompletions);
            });
        console.log("[spe] initializing tab completion");
        return (tabCompletions) ? tabCompletions.length : 0;
    }

    function tabCompletionNoHints() {

        var tips = $(".tip_no_hints");
        var tip = tips[0];
        var tipInterval = 1000;
        if (sigHint === "") {
            tip.innerHTML = "No hints found";
            tips.addClass("no_hints").removeClass("signature");
        } else {
            tipInterval = 5000;
            tip.innerHTML = sigHint;
            tips.addClass("signature").removeClass("no_hints");
        }
        //Absolute position the tooltip according to mouse position
        tips.css({ top: 10, left: 10 });

        tips.fadeIn(function () {
            window.setTimeout(function () {
                tips.fadeOut("slow");
            }, tipInterval);
        });
    }

    var guid = getSessionId();
    var terminal;
    var currentYear = (new Date()).getFullYear();
    var greetings = "Sitecore PowerShell Extensions\r\nCopyright &copy; 2010-" + currentYear + " Adam Najmanowicz, Michael West. All rights Reserved.\r\n";
    $(function() {
        terminal =
            $("#terminal").terminal(function(command, term) {
                if (/^\s*(cls|clear-host)\s*$/i.test(command)) {
                    term.clear();
                    return;
                }
                var buffer;
                if (command.length > 0 && command.lastIndexOf(" `") == command.length - 1) {
                    buffer = command;
                    term.push(function(subCommand) {
                        if (subCommand.length == 0) {
                            term.pop();
                            callPowerShellHost(term, guid, buffer);
                            buffer = "";
                        } else {
                            buffer += subCommand;
                        }
                    }, {
                        prompt: ">>",
                        name: "nested"
                    });
                } else {
                    callPowerShellHost(term, guid, command);
                }
            }, {
                greetings: greetings,
                name: "mainConsole",
                completion: completion,
                caseSensitiveAutocomplete: false,
                keydown: function (e) {
                    if (e.which !== 9) {
                        tabCycleActive = false;
                        tabCompletions = null;
                    }
                }
                });
        // Expose the terminal reference to the spe streaming/busy helpers
        // defined at the top of this IIFE.
        consoleTerminal = terminal;
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
        spe.bootstrap(false);
    });

    
    spe.elevateSession = function() {
        scForm.postRequest("", "", "", "ise:elevatesession");
        spe.showInfoPanel(true);
    };

    spe.showUnelevated = function() {
        spe.hideBusy();
        terminal.resume();
        spe.setTerminalPrompt("unelevated >");
        spe.showInfoPanel(true);
    }

    spe.showInfoPanel = function(showPanel) {
        if (showPanel) {
            $ise("#InfoPanel").css("display", "block");
            $ise("#terminal").css({ "top": $("#InfoPanel").outerHeight()+"px" });
        } else {
            $ise("#InfoPanel").css("display", "none");
            $ise("#terminal").css({ "top": "0" });
        }
    }

    spe.bootstrap = function(elevationBlocked) {
	if(!elevationBlocked){
          if (pausedCommand) {
              var unpausedCommand = pausedCommand;
              pausedCommand = null;
              unpausedCommand();
          } else if (!isBlank(getUrlParameter("item") && getUrlParameter("item") != "null")) {
              callPowerShellHost(terminal, guid, "cd \"" + getUrlParameter("db") + ":\\" + myUnescape(getUrlParameter("item")) + "\"");
          } else if (!isBlank(getUrlParameter("debug") && getUrlParameter("debug") === "true")) {
              callPowerShellHost(terminal, guid, "Get-PSCallStack");
          } else if (!isBlank(getUrlParameter("suspend") && getUrlParameter("suspend") === "true")) {
              callPowerShellHost(terminal, guid, ""); //just initialize the prompt
          } else {
              callPowerShellHost(terminal, guid, "cd master:\\");
          }
        } else {
          spe.hideBusy();
          terminal.resume();
          terminal.echo("Script execution forbidden. Contact your Sitecore administrator if you need this functionality.");
        }

        window.parent.focus();
        window.focus();

        function setFocusOnConsole() {
            $("body").focus();
        }
        setTimeout(setFocusOnConsole, 1000);
    };

}(jQuery, window, window.spe = window.spe || {}));
