(function($, window, spe, undefined) {
    window.onload = function () {
        scForm.postRequest("", "", "", "psr:execute");
    };

    // SpeJobRecovery.js gave up reviving the poll (page state lost after a
    // long freeze): reload this Runner re-attached to the same job.
    window.speJobWatchdogLost = function (id, handle) {
        if (!handle) {
            window.location.reload();
            return;
        }
        window.location.href = "/sitecore/shell/default.aspx?xmlcontrol=PowerShellRunner&reattach=" +
            encodeURIComponent(handle);
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
            progressBar.progressbar({ value: false });
            var timeout = setTimeout(function() {
                if (!animate) {
                    clearTimeout(timeout);
                    return;
                }
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

        // Built here rather than in PowerShellRunner.xml: Sheer renders empty
        // elements self-closing, which swallows the markup that follows.
        $("<div id='RunnerOutput' role='log' aria-label='Script output'><div id='RunnerTerminal'></div></div>")
            .insertBefore($(".scFlexColumnContainer > .scFormDialogFooter").first());
        $("#OutputToggle")
            .empty()
            .append("<span class='st'></span>", $("<span class='lbl'></span>").text(texts().showOutput), "<span class='chev'></span>");
        $("#OutputToggle").on("click", function () {
            setOutputOpen(!outputOpen);
        });
        $(document).on("keydown", function (e) {
            if (e.ctrlKey && (e.key === "`" || e.keyCode === 192)) {
                e.preventDefault();
                setOutputOpen(!outputOpen);
            }
        });
    });

    spe.updateProgress = function(id, progress) {
        animate = false;
        var widget = $(id);
        if(progress === false || progress === -1){
            widget.progressbar("option", "value", false);
            widget.addClass("ui-progressbar-indeterminate");

        } else {
            widget.progressbar("value", Math.max(progress, 1));
        }
    };

    // status: "ok" | "problems" | "aborted" | "stopped". The output toggle
    // turns into the run's verdict.
    spe.scriptFinished = function(id, status, warnings, errors) {
        animate = false;
        $(id).progressbar("value", 100);
        showVerdict(status, warnings || 0, errors || 0);
    };

    // ---- Embedded output terminal --------------------------------------
    //
    // Hidden until the user opens it. While closed nothing streams and the
    // terminal assets are not even loaded; the server starts streaming when
    // told the output is visible (psr:outputvisible).

    var ASSETS = "/sitecore modules/PowerShell/";
    var GROW_WIDTH = 360;    // 400x260 caller size -> 760x560
    var GROW_HEIGHT = 300;
    var RESIZE_MS = 220;

    var outputOpen = false;
    var term = null;
    var loading = null;
    var queued = [];         // streaming calls that arrived before the terminal existed
    var partialIndex = -1;   // terminal line holding the current unterminated output
    var collapsedSize = null;

    function texts() {
        return spe.runnerTexts || {
            showOutput: "Show output",
            hideOutput: "Hide output",
            success: "Success",
            aborted: "Aborted",
            warning: "{0} warning",
            warnings: "{0} warnings",
            error: "{0} error",
            errors: "{0} errors"
        };
    }

    // A trailing newline in echo/update input makes jquery.terminal add a
    // phantom empty row; each call already starts its own line (see ise.js).
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

    function apply(op, text) {
        switch (op) {
            case "append":
                term.echo(stripTrailingNewline(text));
                partialIndex = -1;
                break;
            case "partial":
                text = stripTrailingNewline(text);
                if (partialIndex >= 0) {
                    term.update(partialIndex, text);
                } else {
                    term.echo(text);
                    partialIndex = term.last_index();
                }
                break;
            case "commit":
                text = stripTrailingNewline(text);
                if (partialIndex >= 0) {
                    term.update(partialIndex, text);
                } else {
                    term.echo(text);
                }
                partialIndex = -1;
                break;
            case "finalize":
                partialIndex = -1;
                break;
            case "clear":
                term.clear();
                partialIndex = -1;
                break;
        }
    }

    function stream(op, text) {
        if (term) {
            apply(op, text);
            return;
        }
        if (op === "clear") {
            queued = [];
            return;
        }
        var last = queued[queued.length - 1];
        if (op === "partial" && last && last.op === "partial") {
            last.text = text;
            return;
        }
        queued.push({ op: op, text: text });
    }

    spe.appendOutput = function (text) { stream("append", text); };
    spe.updatePartialOutput = function (text) { stream("partial", text); };
    spe.commitPartialOutput = function (text) { stream("commit", text); };
    spe.finalizePartial = function () { stream("finalize"); };
    spe.clearOutput = function () { stream("clear"); };

    function loadTerminal() {
        if (loading) return loading;
        $("<link rel='stylesheet' type='text/css'>")
            .attr("href", ASSETS + "Styles/jquery.terminal.css")
            .appendTo("head");
        loading = $.ajax({ url: ASSETS + "Scripts/jquery.terminal.js", dataType: "script", cache: true })
            .then(function () {
                term = $("#RunnerTerminal").terminal($.noop, {
                    greetings: false,
                    enabled: false,
                    exit: false,
                    history: false
                });
                term.disable();
                var pending = queued;
                queued = [];
                for (var i = 0; i < pending.length; i++) {
                    apply(pending[i].op, pending[i].text);
                }
            });
        return loading;
    }

    function setOutputOpen(open) {
        if (open === outputOpen) return;
        outputOpen = open;
        var toggle = $("#OutputToggle");
        toggle.attr("aria-expanded", String(open));
        toggle.find(".lbl").text(open ? texts().hideOutput : texts().showOutput);

        if (open) {
            // The progress panel keeps its natural height (Runner.css); the
            // terminal takes whatever the grown dialog adds, inset to the
            // panel's own gutter so it lines up in both Sitecore themes.
            var gutter = $("#ValuePanel").css("padding-left");
            $("#RunnerTerminal").css({ left: gutter, right: gutter });
            $("body").addClass("runnerOutputOpen");
            growHost();
            loadTerminal().then(function () {
                // closed again while the assets loaded: visible=0 already went out
                if (!outputOpen) return;
                fitTerminal();
                scForm.postRequest("", "", "", "psr:outputvisible(visible=1)");
            });
        } else {
            $("body").removeClass("runnerOutputOpen");
            shrinkHost();
            scForm.postRequest("", "", "", "psr:outputvisible(visible=0)");
        }
    }

    function fitTerminal() {
        if (!term) return;
        term.resize();
        term.scroll_to_bottom();
    }

    // ---- Growing the host (Sheer modal or desktop window) --------------

    // The jQuery UI dialog hosting this Runner, but only while it is the
    // topmost one: a dialog the script opened on top must never be resized.
    function hostDialog() {
        try {
            var dialogs = top._scDialogs;
            var record = dialogs && dialogs[0];
            if (record && record.contentIframe && record.contentIframe[0] === window.frameElement) {
                return record.contentIframe;
            }
        } catch (e) {
            // cross-frame access denied: not hosted in a Sheer modal
        }
        return null;
    }

    function setDialogSize(dialog, width, height) {
        dialog.dialog("option", { width: width, height: height });
        var widget = dialog.dialog("widget");
        if (widget.scCenter) {
            widget.scCenter({ animate: false });
        }
    }

    // The Sitecore Desktop window hosting this Runner (Start-ScriptSession
    // -Interactive), when it floats; a docked window keeps its zone's size.
    // The Desktop manages an Application frame that hosts the Runner page in
    // an inner frame, so walk up to the frame the Desktop's scManager owns.
    function desktopWindow() {
        try {
            var current = window;
            while (current.frameElement && current.parent !== current) {
                var frame = current.frameElement;
                var desktop = current.parent;
                if (desktop.scManager && desktop.scRect && desktop.scManager.getWindow(frame.id) === frame) {
                    return desktop.scManager.isDocked(frame) ? null : { frame: frame, desktop: desktop };
                }
                current = desktop;
            }
        } catch (e) {
            // cross-frame access denied: not hosted on the Desktop
        }
        return null;
    }

    // Same as the Desktop's own resize (scWindowManager.resizeEnd), kept on
    // screen by moving the window up/left when it would overflow.
    function setWindowSize(host, width, height) {
        var view = host.desktop.document.documentElement;
        var rect = new host.desktop.scRect();
        rect.getControlRect(host.frame);
        rect.width = Math.min(width, view.clientWidth - 40);
        rect.height = Math.min(height, view.clientHeight - 40);
        rect.left = Math.max(0, Math.min(rect.left, view.clientWidth - rect.width - 20));
        rect.top = Math.max(0, Math.min(rect.top, view.clientHeight - rect.height - 20));
        rect.apply(host.frame);
        host.frame.floatBounds = new host.desktop.scRect();
        host.frame.floatBounds.assign(rect);
    }

    // Resizes the host from one size to another with an ease-out, or at once
    // when the user prefers reduced motion. The terminal is re-fitted at the end.
    function animateSize(from, to, apply) {
        var reduced = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        if (reduced || !window.requestAnimationFrame) {
            apply(to.width, to.height);
            fitTerminal();
            return;
        }
        var start = null;
        function frame(now) {
            if (start === null) start = now;
            var progress = Math.min(1, (now - start) / RESIZE_MS);
            var eased = 1 - Math.pow(1 - progress, 4);
            apply(Math.round(from.width + (to.width - from.width) * eased),
                Math.round(from.height + (to.height - from.height) * eased));
            if (progress < 1) {
                window.requestAnimationFrame(frame);
            } else {
                fitTerminal();
            }
        }
        window.requestAnimationFrame(frame);
    }

    function hostResizer() {
        var dialog = hostDialog();
        if (dialog) {
            return {
                size: { width: dialog.dialog("option", "width"), height: dialog.dialog("option", "height") },
                max: { width: top.innerWidth - 80, height: top.innerHeight - 80 },
                apply: function (width, height) { setDialogSize(dialog, width, height); }
            };
        }
        var desktop = desktopWindow();
        if (desktop) {
            return {
                size: { width: desktop.frame.offsetWidth, height: desktop.frame.offsetHeight },
                max: { width: Infinity, height: Infinity },   // setWindowSize keeps it on the Desktop
                apply: function (width, height) { setWindowSize(desktop, width, height); }
            };
        }
        return null;
    }

    function growHost() {
        var host = hostResizer();
        if (!host) {
            setTimeout(fitTerminal, 50);
            return;
        }
        collapsedSize = collapsedSize || host.size;
        animateSize(host.size, {
            width: Math.min(collapsedSize.width + GROW_WIDTH, host.max.width),
            height: Math.min(collapsedSize.height + GROW_HEIGHT, host.max.height)
        }, host.apply);
    }

    function shrinkHost() {
        var host = hostResizer();
        if (host && collapsedSize) {
            animateSize(host.size, collapsedSize, host.apply);
        }
    }

    // ---- Verdict ---------------------------------------------------------

    function escapeHtml(text) {
        return $("<span>").text(text).html();
    }

    function count(n, one, many) {
        return escapeHtml((n === 1 ? one : many).replace("{0}", n));
    }

    function showVerdict(status, warnings, errors) {
        var toggle = $("#OutputToggle");
        if (!toggle.length) return;
        var t = texts();
        var verdict = "";
        if (status === "ok") {
            verdict = escapeHtml(t.success);
        } else if (status === "aborted") {
            verdict = escapeHtml(t.aborted);
        } else if (status === "problems") {
            var parts = [];
            if (warnings) parts.push("<span class='w'>" + count(warnings, t.warning, t.warnings) + "</span>");
            if (errors) parts.push("<span class='e'>" + count(errors, t.error, t.errors) + "</span>");
            verdict = parts.join("<span class='sep'> · </span>");
        }

        var button = toggle[0];
        var from = button.getBoundingClientRect().width;
        toggle.attr("data-status", status);
        toggle.toggleClass("warnOnly", status === "problems" && !errors);
        toggle.find(".st").html(verdict);
        morph(button, from);
    }

    // Grow or shrink the button from its old width to the new one while the
    // new label fades in.
    function morph(button, from) {
        var to = button.getBoundingClientRect().width;
        button.classList.remove("morph");
        void button.offsetWidth;
        button.classList.add("morph");
        var reduced = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        if (reduced || !from || !to || from === to) return;
        button.style.width = from + "px";
        void button.offsetWidth;
        button.style.width = to + "px";
        setTimeout(function () { button.style.width = ""; }, 300);
    }

}($ise, window, window.spe = window.spe || {}));
