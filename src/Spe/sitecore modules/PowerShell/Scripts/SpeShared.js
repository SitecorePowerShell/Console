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

    // Resize grip at the bottom-right of #StatusBar. It does not resize anything
    // itself, it starts the host's own bottom-right border resize:
    // - Sitecore desktop window: scWindowManager, which captures the mouse in
    //   every frame and tracks screen coordinates.
    // - Modal dialog: the jQuery UI resizable "se" handle of the dialog. The
    //   browser keeps sending mouse events to this frame while the button is
    //   down, so they are forwarded to the dialog document in its coordinates.
    // A top-level page has no host window to resize, so no grip is added.
    function findDesktopWindow() {
        for (var w = window; w !== w.parent; w = w.parent) {
            try {
                if (w.parent.scManager && w.frameElement) return w;
            } catch (e) {
                return null;
            }
        }
        return null;
    }

    function findModalDialogHandle() {
        try {
            var pj = window.parent.jQuery;
            var frame = window.frameElement;
            if (!pj || !pj.ui || !pj.ui.dialog || !frame || !pj(frame).dialog("instance")) return null;
            var handle = pj(frame).dialog("widget").find(".ui-resizable-se");
            return handle.length ? handle : null;
        } catch (e) {
            return null;
        }
    }

    function startModalResize(handle, e) {
        var pj = window.parent.jQuery;
        var rect = window.frameElement.getBoundingClientRect();
        var dx = rect.left + window.parent.pageXOffset;
        var dy = rect.top + window.parent.pageYOffset;
        function forward(target, type, ev) {
            target.trigger(pj.Event(type, {
                which: 1, button: 0,
                pageX: ev.clientX + dx, pageY: ev.clientY + dy,
                clientX: ev.clientX + rect.left, clientY: ev.clientY + rect.top
            }));
        }
        var parentDoc = pj(window.parent.document);
        function move(ev) { forward(parentDoc, "mousemove", ev); ev.preventDefault(); }
        function up(ev) {
            forward(parentDoc, "mouseup", ev);
            document.removeEventListener("mousemove", move, true);
            document.removeEventListener("mouseup", up, true);
        }
        document.addEventListener("mousemove", move, true);
        document.addEventListener("mouseup", up, true);
        // resizable takes the resize axis from the last handle hovered.
        handle.trigger("mouseover");
        forward(handle, "mousedown", e);
    }

    // The dialog's own handles sit on top of this frame's corner, where the grip
    // is: the 7px "se" handle and the 2px "s" and "e" edge strips. An inward drag
    // from "se" loses the mouse to this frame, so nothing resizes, and the edge
    // strips resize one direction only. Let the grip own the corner
    // (#StatusBarGrip is 14x15px) while this page is shown. A style rule rather than
    // inline styles: maximize/restore recreates the handles.
    function yieldDialogCorner() {
        var doc = window.parent.document;
        var style = doc.createElement("style");
        var dialog = '.ui-dialog[aria-describedby="' + window.frameElement.id + '"] ';
        style.textContent =
            dialog + ".ui-resizable-se { pointer-events: none; }\n" +
            dialog + ".ui-resizable-s { width: auto; right: 14px; }\n" +
            dialog + ".ui-resizable-e { height: auto; bottom: 15px; }";
        doc.head.appendChild(style);
        window.addEventListener("pagehide", function () { style.remove(); });
    }

    // host defaults to #StatusBar; pages without one (Console) pass their own.
    spe.initResizeGrip = function (host) {
        host = host || document.getElementById("StatusBar");
        if (!host || document.getElementById("StatusBarGrip")) return;
        var desktopWindow = findDesktopWindow();
        if (!desktopWindow) {
            if (!findModalDialogHandle()) return;
            yieldDialogCorner();
        }

        var grip = document.createElement("div");
        grip.id = "StatusBarGrip";
        grip.className = "status-bar-grip";
        grip.title = "Drag to resize";
        host.appendChild(grip);

        grip.addEventListener("mousedown", function (e) {
            if (e.button !== 0) return;
            e.preventDefault();
            if (desktopWindow) {
                desktopWindow.parent.scManager.mouseDown(grip, e, desktopWindow.frameElement.id, "resize bottomright");
                return;
            }
            // Looked up per drag: the handle is recreated when the dialog is maximized/restored.
            var handle = findModalDialogHandle();
            if (handle) startModalResize(handle, e);
        });
    };

    $(function () { spe.initResizeGrip(); });
}(jQuery, window, window.spe = window.spe || {}));
