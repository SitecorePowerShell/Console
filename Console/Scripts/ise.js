
function onExecute() {
    document.getElementById("Result").innerHTML = "&lt;div align='Center' style='padding:32px 0px 32px 0px'&gt;Please wait... Executing script...&lt;/div&gt;&lt;img src='../../../../Console/Assets/working.gif' alt='Working'/&gt;";
}

jQuery(document).ready(function($) {

    var tips = [
            "You can press <strong>Ctrl+Space</strong> to show the Auto Suggest drop down that will show you all the matching comands/parameters/files depending on your caret position",
            "If you press <strong>Shift+Space</strong> ISE sill attempt to auto-complete your statement in-line.",
            "By pressing <strong>Ctrl+Enter</strong> you can see help for the command you've just typed.",
            "Your script will start in the location/folder picked by the <strong>Content Item</strong> dropdown.",
            "You can change the color of the results dialog shown to your script users using the <strong>Console</strong> ribbon button.",
            "If you save your script in the <strong>Content Editor Context Menu</strong> it will automatically show as a context menu option for items that users Right Click in the tree and will start in the location of that item.",
            "All your scripts that share the same <strong>Persistent Session ID</strong> can re-use variables that were created by the scripts with the same session id that were run before.",
            "<strong>Runtime</strong> ribbon button is active only if you're editong a script from library. Save your script in script library to enable it.",
            "<strong>Script Library</strong> comes with a wealth of samples and useful scripts that you can base your scripts upon.",
            "You can execute your script with the <strong>Ctrl+E</strong> hotkey.",
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

    // editor caret position in the status bar
    $("#Editor").keyup(function() {
        var ctrl = this;
        var val = ctrl.value;
        var pos = $(this).getSelection().start;
        var spl = val.substr(0, pos).split("\n");
        $("#PosX").text(spl[spl.length - 1].length);
        $("#PosY").text(spl.length);
    }).mousedown(function() {
        var ctrl = this;
        var val = ctrl.value;
        var pos = $(this).getSelection().start;
        var spl = val.substr(0, pos).split("\n");
        $("#PosX").text(spl[spl.length - 1].length);
        $("#PosY").text(spl.length);
    });

    var guid = "ECBC33D9-A623-4A97-888B-375B627B4189";

    // Defines for the example the match to take which is any word (with Umlauts!!).

    function _leftMatch(string, area) {
        var selectionStart = ('context' in area) ? area.context.selectionStart : area.selectionStart;
        var preCursor = string.substring(0, selectionStart);
        var allMatches = preCursor.match(/[^\n].*$/);
        var substr = allMatches[0];
        return substr;
    }

    function _setCursorPosition(area, pos) {
        if (area.setSelectionRange) {
            area.setSelectionRange(pos, pos);
        } else if (area.createTextRange) {
            var range = area.createTextRange();
            range.collapse(true);
            range.moveEnd('character', pos);
            range.moveStart('character', pos);
            range.select();
        }
    }

    var editor = $($('#Editor')[0]);

    editor.keypress(function(event) {
        if (event.which === 32 && event.ctrlKey) {
            event.preventDefault();
            editor.autocomplete("enable");
            editor.autocomplete("search");
        } else if ((event.which === 32 && event.shiftKey) ||event.which === 198) {
            var str = _leftMatch(editor[0].value, editor);
            _getTabCompletions(str);
            event.preventDefault();
        } else if (event.which === 13 && (event.shiftKey || event.ctrlKey)) {
            var command = _leftMatch(editor[0].value, editor);
            _getCommandHelp(command);
            event.preventDefault();
            var ajaxDialog = $('<div id="ajax-dialog"/>').html($.commandHelp).appendTo('body');
            ajaxDialog.dialog({
                modal: true,
                close: function(event, ui) {
                    $(this).remove();
                },
                height: $(window).height() - 20,
                width: $(window).width() * 2 / 3,
                show: "slow",
                hide: "slow"
            });
            $('#ajax-dialog').scrollTop("0");
        }

    });

    $.tabCompletions = [];
    $.commandHelp = "";
    $("#Help").dialog({ autoOpen: false });
    editor.asuggest($.tabCompletions);
    
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
            function() {
                $(".status-bar-text").animate({ backgroundColor: "#012456" }).animate({ backgroundColor: "#fff" });
            });

    $(".status-bar-text").click(function () {
        tipIndex++;
        if (tipIndex >= tips.length) {
            tipIndex = 0;
        }
        var tip = tips[tipIndex];
        $(".status-bar-text").animate({ backgroundColor: "#012456" },
        function() {
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
        getPowerShellResponse({ "guid": guid, "command": str }, "CompleteCommand",
            function (json) {
                var data = JSON.parse(json.d);
                $.tabCompletions = data;
            });
    }
    
    editor.autocomplete({
        appendTo: "#Tip",
        position: { my: "left top", at: "middle center" },
        source: function (request, response) {
            var str = _leftMatch(request.term, editor);
            _getTabCompletions(str);
            str = (str !== null) ? str : "";
            response($.ui.autocomplete.filter($.tabCompletions, str));
        },
        //minLength: 2,  // does have no effect, regexpression is used instead
        focus: function () {
            // prevent value inserted on focus
            return false;
        },
        open: function() {
            var position = $("#Editor").position();
            var left = position.left;
            var top = position.top;
            var pos = $("#Editor").getCaretPosition();

            $("#Tip > ul").css({
                left: (left-1) + "px",
                top: (top + pos.top + 2) + "px",
                width: "auto",
                "margin-right": "6px",
                "margin-bottom": "30px",
                "max-height": ($(window).height() - top - pos.top - 40) + "px",
                "overflow-y": "auto",
        });
        },
        close: function () {
            // prevent value inserted on focus
            editor.autocomplete("disable");
            return false;
        },
        // Insert the match inside the ui element at the current position by replacing the matching substring
        select: function (event, ui) {
            //alert("completing "+ui.item.value);},
            var m = _leftMatch(this.value, this);
            var editor1 = $("#Editor");
            var selectionStart = editor1[0].selectionStart;
            var beg = editor1[0].value.substring(0, selectionStart - m.length);
            this.value = beg + ui.item.value + editor1[0].value.substring(selectionStart, this.value.length);
            var pos = beg.length + ui.item.value.length;
            _setCursorPosition(this, pos);
            editor.autocomplete("disable");
            return false;
        },
        search: function (event, ui) {
            var m = _leftMatch(this.value, this);
            return (m !== null);
        }
    });
    
    editor.autocomplete("disable");

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