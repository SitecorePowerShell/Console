(function($, window, spe, undefined) {
    var defaults = {
        initialPoll: 100,
        maxPoll: 2500,
        keepAliveInterval: 60000, // 60 * 1000 - every minute
        keepAliveCheck: 2000, // 2 * 1000 - every 2 seconds
        monitorActive: true
    };

    var settings = defaults;
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
        term.pause();
        $("#working").show();
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
                                        term.resume();
                                        $("#working").hide();
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
        if (data["status"] != "partial" && data["status"] != "working") {
            term.resume();
            $("#working").hide();
            spe.preventCloseWhenRunning(false);
            term.set_prompt(data["prompt"]);
            var background = data["background"];
            if (background !== undefined && background !== "null") {
                $("#terminal").css({ "background-color": background });
            }
            var color = data["color"];
            if (color !== undefined && color !== "null") {
                $("#terminal").css({ "color": color });
            }
        }

        term.echo(data["result"]);
        $("html").animate({ scrollTop: $(document).height() }, "slow");
    }

    var sigHint = "";

    function completion(command, tab_count, callback) {
        if (tab_count < 0 || !tabCompletions) {
            tabCompletionInit(command, function () {
                if (tabCompletions) {
                    callback(tabCompletions[0]);

                    if (tabCompletions.length === 0) {
                        tabCompletionNoHints();
                    }
                }
            });
        } else if (tabCompletions) {
            callback(tabCompletions[tab_count]);

            if (tabCompletions.length === 0) { 
                tabCompletionNoHints();
            }
        }
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
                        sigHint += hintParts[1] + "<br/>";
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
                if (!!console) {
                    console.log("[spe] setting tabCompletions to: ", tabCompletions);
                }
            });
        if (!!console) {
            console.log("[spe] initializing tab completion");
        }
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
                caseSensitiveAutocomplete: false
                });
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
        terminal.resume();
        $ise("#working").hide();
        terminal.set_prompt("unelevated >");
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
          terminal.resume();
          $("#working").hide();
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
