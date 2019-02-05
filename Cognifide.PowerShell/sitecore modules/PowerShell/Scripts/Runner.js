(function($, window, cognifide, undefined) {
    window.onload = function () {
        scForm.postRequest("", "", "", "psr:execute");
    };

    window.onfocus = function () {
        if ($ise("#Closed")[0].innerHTML === "close") {
            scForm.postRequest("", "", "", "psr:delayedclose");
        }
    };

    var animate = true;

    $(function() {
        var progressBar = $("#progressbar");
        if (progressBar.length > 0) {
            progressBar.progressbar({ value: 1 });
            setTimeout(function() {
                if (!animate) {
                    clearInterval(interval);
                }
                progressBar.progressbar("option", "value", false);
                progressBar.addClass("ui-progressbar-indeterminate");
            }, 2000);
        }
        $("#Copyright").each(function () { // Notice the .each() loop, discussed below
            var currentYear = (new Date()).getFullYear();
            var greetings = "Copyright &copy; 2010-" + currentYear + " Adam Najmanowicz, Michael West. All rights Reserved.\r\n";

            $(this).qtip({
                content: {
                    text: greetings,
                    title: "Sitecore PowerShell Extensions"
                },
                position: {
                    my: "bottom left",
                    at: "top center"
                },
                style: {
                    width: 355,
                    "max-width": 355
                },
                hide: {
                    event: false,
                    inactive: 3000
                }
            });
        });
    });

    cognifide.powershell.undeterminateProgress = function(id) {
        animate = false;
        var widget = $(id);
        widget.progressbar("value", 1);
    }

    cognifide.powershell.updateProgress = function(id, progress) {
        animate = false;
        var widget = $(id);
        widget.progressbar("value", Math.max(progress, 1));
    }

    cognifide.powershell.scriptFinished = function(id, hasResults, hasErrors) {
        animate = false;
        var progress = $(id);
        progress.progressbar("value", 100);
        if (hasResults || hasErrors) {
            var button;
            if (hasErrors) {
                button = $("#ViewErrorsButton");
            } else {
                button = $("#ViewButton");
            }
            button
                .fadeIn("slow")
                .css("display", "block")
                .effect("shake", { times: 2, distance: 5 }, 1000);
        }
    }

}($ise, window, window.cognifide = window.cognifide || {}));
