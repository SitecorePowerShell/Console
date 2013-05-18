<%@ Page Language="C#" CodeBehind="PowerShellTerminal.aspx.cs" Inherits="Cognifide.PowerShell.Console.Layouts.PowerShellTerminal" %>
<%@ Import Namespace="Cognifide.PowerShell" %><!DOCTYPE html>
<!-- !DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd" -->
<html>
    <head runat="server">
        <meta name="robots" content="noindex, nofollow" />
        <!-- meta http-equiv="X-UA-Compatible" content="IE=EmulateIE7" / -->
        <link href="~/sitecore/shell/themes/standard/default.css" type="text/css" rel="stylesheet" />

        <link href="~/Console/Styles/Console.css" type="text/css" rel="stylesheet" />
        <title>PowerShell for Sitecore</title>
        <script type="text/javascript" language="javascript" src="/Console/Scripts/jquery-1.7.1.min.js"> </script>
        <script type="text/javascript" language="javascript" src="/Console/Scripts/jquery.mousewheel-min.js"> </script>
        <script type="text/javascript" language="javascript" src="/Console/Scripts/jquery.terminal.js"> </script>
        <link href="/Console/Styles/jquery.terminal.css" type="text/css" rel="Stylesheet" />
        <script type="text/javascript" language="javascript" src="/Console/Scripts/split.js"> </script>
        <!--[if lt IE 9]>
            <script type="text/javascript" language="javascript" src="/Console/Scripts/json2.js"> </script>
        <![endif]-->

        <script>
            function guidGenerator() {
                var s4 = function() {
                    return (((1 + Math.random()) * 0x10000) | 0).toString(16).substring(1);
                };
                return (s4() + s4() + "-" + s4() + "-" + s4() + "-" + s4() + "-" + s4() + s4() + s4());
            }

            function getURLParameter(name) {
                return decodeURI(
                    (RegExp(name + '=' + '(.+?)(&|$)').exec(location.search) || [, null])[1]
                );
            }

            function myUnescape(str) {
                return unescape(str).replace(/[+]/g, " ");
            }

            function isBlank(str) {
                return (!str || /^\s*$/.test(str));
            }

            jQuery(document).ready(function($) {
                var guid = guidGenerator();
                var tabCompletions = null;

                var terminal =
                    $('body').terminal(function(command, term) {
                        var buffer = "";
                        if (command.length > 0 && command.lastIndexOf(' `') == command.length - 1) {
                            buffer = command;
                            term.push(function(subCommand) {
                                if (subCommand.length == 0) {
                                    term.pop();
                                    callPowerShellHost(term, guid, buffer);
                                    /*if (console) {
                              console.log("buffer executed");
                            }*/
                                    buffer = "";
                                } else {
                                    buffer += '\n' + subCommand;
                                }
                            }, {
                                prompt: '>>',
                                name: 'nested'
                            });
                        } else {
                            callPowerShellHost(term, guid, command);
                            /*if (console) {
                        console.log("command executed");
                    }*/
                        }
                    }, {
                        greetings: "Sitecore Powershell Console\r\nCopyright (c) 2010-2011 Cognifide Limited & Adam Najmanowicz. All rights Reserved\n\n",
                        name: "mainConsole",
                        tabcompletion: true,
                        onTabCompletionInit: tabCompletionInit,
                        onTabCompletion: tabCompletion,
                        onTabCompletionEnd: tabCompletionEnd
                    });

                if (!isBlank(getURLParameter("item") && getURLParameter("item") != "null")) {
                    callPowerShellHost(terminal, guid, "cd \"" + getURLParameter("db") + ":\\" + myUnescape(getURLParameter("item")) + "\"");
                } else {
                    callPowerShellHost(terminal, guid, "cd master:\\");
                }

                function tabCompletionInit(command) {
                    getPowerShellResponse({ "guid": guid, "command": command }, "CompleteCommand",
                        function(json) {
                            var data = JSON.parse(json.d);
                            /*if (console) {
                    console.log("setting tabCompletions to: " + data.toString());
                    }*/
                            tabCompletions = data;
                        });
                    /*if (console) {
            console.log("initializing tab completion");
            }*/
                    return (tabCompletions) ? tabCompletions.length : 0;
                }

                function tabCompletion(term, tabCount) {
                    /*if (console) {
                console.log("tabCount is: " + tabCount.toString());
                console.log("tabCompletions are: " + tabCompletions.toString());
            }*/
                    if (tabCompletions) {
                        term.set_command(tabCompletions[tabCount]);
                    }
                    /*if (console) {
                console.log("tab completing");
            }*/
                }

                function tabCompletionEnd() {
                    /*if (console) {
            console.log("ending tab completion");
            }*/
                }
            });

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
                        async: true
                    }).done(doneFunction);
                if (typeof(errorFunction) != "undefined") {
                    ajax.fail(errorFunction);
                }
            }

            function callPowerShellHost(term, guid, command) {
                term.pause();
                jQuery("#working").show();
                getPowerShellResponse({ "guid": guid, "command": command, "stringFormat": "jsterm" }, "ExecuteCommand",
                    function(json) {
                        var data = JSON.parse(json.d);
                        if (data["status"] == "working") {
                            var handle = data["handle"];
                            var attempts = 0;
                            var initialWait = <%= Settings.InitialPollMillis %>;
                            var maxWait = <%= Settings.MaxmimumPollMillis %>;
                            (function poll(wait) {
                                setTimeout(function() {
                                    getPowerShellResponse({ "guid": guid, "handle": handle, "stringFormat": "jsterm" }, "PollCommandOutput",
                                        function(pollJson) {
                                            var data = JSON.parse(pollJson.d);
                                            if (data["status"] == "working") {
                                                if (attempts >= 0) {
                                                    attempts++;
                                                    var newWait = Math.pow(initialWait, 1 + (attempts / 50));
                                                    if (newWait > maxWait) {
                                                        newWait = maxWait;
                                                        attempts = -1; //stop incrementing
                                                    }
                                                    poll(newWait);
                                                } else {
                                                    poll(maxWait);
                                                }
                                            } else if (data["status"] == "partial") {
                                                displayResult(term, data);
                                                poll(initialWait);
                                            } else {
                                                displayResult(term, data);
                                            }
                                        },
                                        function(jqXHR, textStatus, errorThrown) {
                                            term.resume();
                                            jQuery("#working").hide();
                                            term.echo("Communication error: " + textStatus + "; " + errorThrown);
                                        }
                                    );
                                }, wait);
                            })(initialWait);
                        } else {
                            displayResult(term, data);
                        }
                    }
                );
            }

            function displayResult(term, data) {
                if (data["status"] != "partial") {
                    term.resume();
                    jQuery("#working").hide();
                }

                term.set_prompt(data["prompt"]);
                term.echo(data["result"]);
                $("html").animate({ scrollTop: $(document).height() }, "slow");

                /*if (console) {
                    console.log(json);
        console.log(data["prompt"]);
        console.log(data["result"]);
                }*/
            }

        </script>
    </head>
    <body style="overflow: visible;">
        <div id="terminal" class="terminal2"></div>
        <div id="working" style="bottom: 0; display: none; float: right; position: fixed; right: 0;"><img src="/Console/Assets/working.gif" alt="Working"></div>
    </body>
    <script>
        jQuery(document).ready(function($) {

            window.parent.focus();
            window.focus();

            function setFocusOnConsole() {
                $('body').focus();
            }

            setTimeout(setFocusOnConsole, 1000);
        })
    </script>
</html>