// a convenience function for parsing string namespaces and
// automatically generating nested namespaces
function extend(e, t) {
    var n = t.split("."), r = e, i, s;
    if (n[0] == "spe") {
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

var spe = spe || {};
extend(spe, "powershell");

(function ($, window, spe, undefined) {
    var messages = {
        "confirmQuit": "Script is running. Are you sure you want to quit?"
    };


    spe.preventCloseWhenRunning = function (isRunning) {
        if (isRunning) {
            if (!window.onbeforeunload) {
                window.onbeforeunload = function () { return messages["confirmQuit"]; };
            }
        }
        else {
            window.onbeforeunload = null;
        }
    };

    spe.DownloadReport = function (handle) {
        var iframe = document.createElement("iframe");
        iframe.src = "/-/script/handle/" + handle;
        iframe.width = "1";
        iframe.height = "1";
        iframe.style.position = "absolute";
        iframe.style.display = "none";
        document.body.appendChild(iframe);
    }
}(jQuery, window, window.spe = window.spe || {}));
