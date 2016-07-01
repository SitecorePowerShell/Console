window.onload = function() {
    scForm.postRequest("", "", "", "psr:execute");
};

window.onfocus = function() {
    if ($ise("#Closed")[0].innerHTML === "close") {
        scForm.postRequest("", "", "", "psr:delayedclose");
    }
};

jQuery(document).ready(function($) {
    var progressWidth = $("#Progress").width();
    if ($("#progressbar").length > 0) {
        $("#progressbar").empty().VistaProgressBar({
            mode: "indeterminate",
            width: progressWidth,
            highlightspeed: 3000
        });
        $("#progressbar").VistaProgressBar("start");
    }
    $("#Copyright").each(function() { // Notice the .each() loop, discussed below
        $(this).qtip({
            content: {
                text: "Copyright &copy; 2010-2016 Adam Najmanowicz - Cognifide, Michael West. All rights Reserved.\r\n",
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

function undeterminateProgress(id) {
    var progressWidth = $("#Progress").width();
    var widget = $ise(id);
    widget.empty().VistaProgressBar({
        mode: "indeterminate",
        width: progressWidth,
        highlightspeed: 3000
    }).VistaProgressBar("start");
}

function updateProgress(id, progress) {
    var widget = $ise(id);
    var mode = widget.VistaProgressBar("getMode");
    if (mode != "determinate") {
        widget.empty().VistaProgressBar({
            mode: "determinate",
            highlight: true,
            highlightspeed: 1000,
            smooth: true,
            smoothdelta: 1,
            smoothsteps: 10, // &gt; 0 exponent easing, == 0 linear
            smoothdelay: 25 // in milliseconds
        }).VistaProgressBar("setProgress", progress);
    } else {
        widget.VistaProgressBar("setProgress", progress);
    }
}

function scriptFinished(id, hasResults, hasErrors) {
    var progress = $ise(id);
    progress.empty().VistaProgressBar({
        mode: "determinate",
        highlight: true,
        highlightspeed: 1000,
        smooth: false
    }).VistaProgressBar("setProgress", 100);
    progress.addClass("done");
    if (hasResults || hasErrors) {
        var button;
        if (hasErrors) {
            button = $ise("#ViewErrorsButton");
        } else {
            button = $ise("#ViewButton");
        }
        button
            .fadeIn("slow")
            .css("display", "block")
            .effect("shake", { times: 2, distance: 5 }, 1000);
    }
}
