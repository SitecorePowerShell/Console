// a convenience function for parsing string namespaces and
// automatically generating nested namespaces
function extend(e, t) {
    var n = t.split("."), r = e, i, s;
    if (n[0] == "cognifide") {
        n = n.slice(1);
    }
    i = n.length;
    for (s = 0; s < i; s++) {
        if (typeof r[n[s]] == "undefined") {
            r[n[s]] = {};
        }
        r = r[n[s]];
    }
    return r;
}

var cognifide = cognifide || {};
extend(cognifide, "powershell");

(function($, window, cognifide, undefined) {
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

    cognifide.powershell.setOptions = function (options) {
        $.extend(settings, options);
    };

    cognifide.powershell.resetAttempts = function () {
        attempts = 0;
    };

    var checkInterval = setInterval(function() {
        if (new Date().getTime() - lastUpdate > settings.keepAliveInterval) {
            getPowerShellResponse({ "guid": guid }, "KeepAlive");
        }
    }, settings.keepAliveCheck);

    function getParam(name) {
        if (name = (new RegExp("[?&]" + encodeURIComponent(name) + "=([^&]*)")).exec(location.search))
            return decodeURIComponent(decodeURIComponent(name[1]));
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
                                        term.echo("Communication error: " + textStatus + "; " + errorThrown);
                                    }
                                );
                            } else {
                                poll(initialWait);
                            }
                        }, wait);
                    })(initialWait);
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
            term.set_prompt(data["prompt"]);
        }

        term.echo(data["result"]);
        $("html").animate({ scrollTop: $(document).height() }, "slow");
    }

    function tabCompletionInit(command) {
        getPowerShellResponse({ "guid": guid, "command": command }, "CompleteCommand",
            function(json) {
                var data = JSON.parse(json.d);
                if (!!console) {
                    console.log("setting tabCompletions to: " + data.toString());
                }
                tabCompletions = data;
            });
        if (!!console) {
            console.log("initializing tab completion");
        }
        return (tabCompletions) ? tabCompletions.length : 0;
    }

    function tabCompletion(term, tabCount) {
        if (tabCompletions) {
            term.set_command(tabCompletions[tabCount]);
        }
    }

    function tabCompletionEnd() {
        if (!!console) {
            console.log("ending tab completion");
        }
    }

    function tabCompletionNoHints() {

        var tip = $(".tip_no_hints");

        //Absolute position the tooltip according to mouse position
        tip.css({ top: 10, left: 10 });

        tip.fadeIn(function() {
            window.setTimeout(function() {
                tip.fadeOut("slow");
            }, 1000);
        });
    }

    var guid = getSessionId();

    $(function() {

        var terminal =
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
                greetings: "Sitecore PowerShell Extensions\r\nCopyright &copy; 2010-2015 Adam Najmanowicz - Cognifide, Michael West. All rights Reserved.\r\n",
                name: "mainConsole",
                tabcompletion: true,
                onTabCompletionInit: tabCompletionInit,
                onTabCompletion: tabCompletion,
                onTabCompletionEnd: tabCompletionEnd,
                onTabCompletionNoHints: tabCompletionNoHints
            });

        if (!isBlank(getUrlParameter("item") && getUrlParameter("item") != "null")) {
            callPowerShellHost(terminal, guid, "cd \"" + getUrlParameter("db") + ":\\" + myUnescape(getUrlParameter("item")) + "\"");
        } else {
            callPowerShellHost(terminal, guid, "cd master:\\");
        }

        window.parent.focus();
        window.focus();

        function setFocusOnConsole() {
            $("body").focus();
        }

        setTimeout(setFocusOnConsole, 1000);
    });
}(jQuery, window, window.cognifide = window.cognifide || {}));