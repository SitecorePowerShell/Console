(function ($, window, spe, undefined) {
    var pathKinds = { "Item": true, "ProviderItem": true, "ProviderContainer": true };
    var tokenTerminators = " \t;|)]},";

    function isQuote(ch) {
        return ch === "'" || ch === '"';
    }

    function parseHints(raw) {
        var result = { entries: [], signatures: [] };
        var rows;
        try {
            rows = raw ? JSON.parse(raw) : [];
        } catch (e) {
            return result;
        }
        if (!$.isArray(rows)) {
            return result;
        }
        $.each(rows, function (i, row) {
            var parts = String(row).split("|");
            if (parts[0] === "Signature") {
                result.signatures.push(parts[3] || parts[1]);
            } else if (parts.length >= 2) {
                result.entries.push({
                    kind: parts[0],
                    text: parts[1],
                    position: parseInt(parts[2], 10) || 0,
                    value: parts.length > 3 ? parts[3] : parts[1]
                });
            }
        });
        return result;
    }

    function wordEnd(command, cursor, pattern) {
        while (cursor < command.length && pattern.test(command.charAt(cursor))) {
            cursor++;
        }
        return cursor;
    }

    // Mirrors the ISE editor's insertMatch rules so both completion surfaces replace the same span.
    function tokenRange(entry, command, cursor) {
        if (entry.kind === "Type") {
            var afterBracket = command.charAt(entry.position) === "[" ? entry.position + 1 : entry.position;
            return { start: afterBracket, end: wordEnd(command, cursor, /[\w.]/), quote: "" };
        }
        if (!pathKinds[entry.kind]) {
            return { start: entry.position, end: wordEnd(command, cursor, /[\w\-]/), quote: "" };
        }
        var start = entry.position;
        var openPos = -1;
        var active = "";
        for (var i = 0; i < start; i++) {
            var ch = command.charAt(i);
            if (!active && isQuote(ch)) {
                active = ch;
                openPos = i;
            } else if (active && ch === active) {
                active = "";
                openPos = -1;
            }
        }
        var quote = "";
        if (openPos >= 0) {
            quote = command.charAt(openPos);
            start = openPos;
        } else if (isQuote(command.charAt(start))) {
            quote = command.charAt(start);
        }
        var end = cursor;
        if (quote) {
            var close = command.indexOf(quote, Math.max(cursor, start + 1));
            if (close >= 0) {
                return { start: start, end: close + 1, quote: quote };
            }
            quote = "";
        }
        while (end < command.length && tokenTerminators.indexOf(command.charAt(end)) < 0) {
            end++;
        }
        return { start: start, end: end, quote: quote };
    }

    function applyCompletion(command, cursor, entry) {
        var range = tokenRange(entry, command, cursor);
        var value = entry.value;
        if (range.quote && value.charAt(0) !== range.quote) {
            value = range.quote + value + range.quote;
        }
        return {
            command: command.substring(0, range.start) + value + command.substring(range.end),
            cursor: range.start + value.length
        };
    }

    function stripQuote(text) {
        return isQuote(text.charAt(0)) ? text.substring(1) : text;
    }

    function filterEntries(entries, command, cursor) {
        return $.grep(entries, function (entry) {
            var range = tokenRange(entry, command, cursor);
            if (cursor < range.start) {
                return false;
            }
            var typed = stripQuote(command.substring(range.start, cursor)).toLowerCase();
            // Types match on the short name and relative paths on the leaf, which is what the row shows.
            var leaf = typed.substring(typed.lastIndexOf("\\") + 1);
            return stripQuote(entry.value).toLowerCase().indexOf(typed) === 0 ||
                entry.text.toLowerCase().indexOf(leaf) === 0;
        });
    }

    var serviceUrl = "/sitecore modules/PowerShell/Services/PowerShellWebService.asmx/";
    var pageRows = 10;
    var noHintsMs = 1000;
    var signatureMs = 5000;
    var notElevatedMs = 3000;
    var liveDelayMs = 300;
    var loadingDelayMs = 150;
    var keys = { backspace: 8, tab: 9, enter: 13, esc: 27, space: 32, pageUp: 33, pageDown: 34, up: 38, down: 40, del: 46 };

    function editsText(e) {
        if (e.ctrlKey || e.altKey || e.metaKey) {
            return false;
        }
        return (e.key && e.key.length === 1) || e.which === keys.backspace || e.which === keys.del;
    }

    // Terminal containers are often transparent; take the first painted ancestor's colour.
    function paintedBackground(el) {
        for (; el && el.nodeType === 1; el = el.parentNode) {
            var bg = window.getComputedStyle(el).backgroundColor;
            if (bg && bg !== "transparent" && bg !== "rgba(0, 0, 0, 0)") {
                return bg;
            }
        }
        return "#000";
    }

    function attachCompletionMenu(term, options) {
        var $menu = $("<ul class='spe-completion-menu'></ul>").hide().appendTo(document.body);
        var mode = null; // null, "list", "info" or "loading"
        var entries = [];
        var shown = [];
        var selected = 0;
        var seq = 0;
        var infoTimer = null;
        var liveTimer = null;
        var loadingTimer = null;

        function close() {
            clearTimeout(infoTimer);
            clearTimeout(loadingTimer);
            mode = null;
            shown = [];
            $menu.hide().empty();
        }

        function position() {
            var cursor = term.cmd().find(".cursor")[0];
            if (!cursor) {
                return;
            }
            var rect = cursor.getBoundingClientRect();
            var height = $menu.outerHeight();
            var top = rect.bottom;
            if (top + height > window.innerHeight && rect.top - height >= 0) {
                top = rect.top - height;
            }
            var left = Math.max(0, Math.min(rect.left, window.innerWidth - $menu.outerWidth()));
            $menu.css({ top: top + "px", left: left + "px" });
        }

        function render() {
            var style = window.getComputedStyle(term[0]);
            $menu.empty();
            $.each(shown, function (i, row) {
                var $li = $("<li></li>");
                if (mode !== "list") {
                    $li.addClass("info").toggleClass("loading", mode === "loading")
                        .append($("<span class='text'></span>").text(row));
                } else {
                    $li.addClass("kind-" + row.kind).toggleClass("selected", i === selected)
                        .append($("<span class='text'></span>").text(row.text))
                        .append($("<span class='kind'></span>").text(row.kind));
                }
                $menu.append($li);
            });
            $menu[0].style.setProperty("--spe-cm-bg", paintedBackground(term[0]));
            $menu[0].style.setProperty("--spe-cm-fg", (options.foreground && options.foreground()) || style.color);
            $menu.css({ fontFamily: style.fontFamily, fontSize: style.fontSize }).show();
            position();
            var li = $menu.children()[selected];
            if (li && li.scrollIntoView) {
                li.scrollIntoView({ block: "nearest" });
            }
        }

        function open(list) {
            mode = "list";
            shown = list;
            selected = 0;
            render();
        }

        function showInfo(lines, ms) {
            mode = "info";
            shown = lines;
            render();
            infoTimer = setTimeout(close, ms);
        }

        function accept(entry) {
            var result = applyCompletion(term.get_command(), term.get_position(), entry);
            close();
            term.set_command(result.command);
            term.set_position(result.cursor);
        }

        function move(delta, wrap) {
            var count = shown.length;
            selected = wrap ? (selected + delta + count) % count : Math.max(0, Math.min(count - 1, selected + delta));
            render();
        }

        function refilter() {
            if (mode !== "list") {
                return;
            }
            var list = filterEntries(entries, term.get_command(), term.get_position());
            if (!list.length) {
                close();
                if (options.live && options.live()) {
                    liveTimer = setTimeout(liveComplete, liveDelayMs);
                }
                return;
            }
            var current = shown[selected];
            shown = list;
            selected = Math.max(0, $.inArray(current, list));
            render();
        }

        function fetch(done) {
            var mySeq = ++seq;
            var sent = term.before_cursor();
            clearTimeout(loadingTimer);
            loadingTimer = setTimeout(function () {
                if (mySeq === seq && mode !== "list") {
                    clearTimeout(infoTimer);
                    mode = "loading";
                    shown = ["Loading..."];
                    render();
                }
            }, loadingDelayMs);
            $.ajax({
                type: "POST",
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                url: serviceUrl + options.endpoint,
                data: JSON.stringify({ guid: options.guid(), command: term.before_cursor() }),
                processData: false,
                cache: false
            }).done(function (json) {
                if (mySeq !== seq) {
                    return;
                }
                endLoading();
                // A reply for a line that has since changed would replace text typed after the request.
                if (!term.paused() && term.enabled() && term.before_cursor() === sent) {
                    var raw = json && json.d;
                    var hints = parseHints(raw);
                    // The service answers an empty string, not an empty list, when the session is not elevated.
                    hints.notElevated = raw === "";
                    done(hints);
                }
            }).fail(function () {
                if (mySeq === seq) {
                    endLoading();
                }
            });
        }

        function endLoading() {
            clearTimeout(loadingTimer);
            if (mode === "loading") {
                close();
            }
        }

        function complete() {
            fetch(function (hints) {
                if (hints.notElevated) {
                    showInfo(["Session not elevated"], notElevatedMs);
                    if (options.onNotElevated) {
                        options.onNotElevated();
                    }
                } else if (hints.signatures.length) {
                    showInfo(hints.signatures, signatureMs);
                } else if (!hints.entries.length) {
                    showInfo(["No hints found"], noHintsMs);
                } else if (hints.entries.length === 1) {
                    accept(hints.entries[0]);
                } else {
                    entries = hints.entries;
                    open(entries);
                }
            });
        }

        function liveComplete() {
            if (!/\S$/.test(term.before_cursor())) {
                return;
            }
            fetch(function (hints) {
                if (hints.signatures.length) {
                    showInfo(hints.signatures, signatureMs);
                } else if (hints.entries.length) {
                    entries = hints.entries;
                    open(entries);
                }
            });
        }

        function keydown(e) {
            var key = e.which;
            clearTimeout(liveTimer);
            if ((key === keys.enter || key === keys.esc) && mode !== "list") {
                seq++; // drop any reply still in flight for the line being submitted or cancelled
                endLoading();
            }
            if (mode === "list") {
                if (key === keys.space && e.ctrlKey) {
                    return false;
                }
                switch (key) {
                    case keys.up: move(-1, true); return false;
                    case keys.down: move(1, true); return false;
                    case keys.pageUp: move(-pageRows, false); return false;
                    case keys.pageDown: move(pageRows, false); return false;
                    case keys.tab:
                    case keys.enter: accept(shown[selected]); return false;
                    case keys.esc: close(); return false;
                }
                // Let the terminal apply the keystroke first, then narrow.
                setTimeout(refilter, 0);
                return undefined;
            }
            if (mode === "info") {
                close();
            }
            var ctrlSpace = key === keys.space && e.ctrlKey && !e.altKey && !e.metaKey;
            if (ctrlSpace || (key === keys.tab && !e.ctrlKey && !e.altKey && !e.metaKey)) {
                complete();
                return false;
            }
            if (options.live && options.live() && editsText(e)) {
                liveTimer = setTimeout(liveComplete, liveDelayMs);
            }
            return undefined;
        }

        $menu.on("mousedown", "li:not(.info)", function (e) {
            e.preventDefault();
            accept(shown[$(this).index()]);
        });
        $(document).on("mousedown", function (e) {
            if (!$.contains($menu[0], e.target) && e.target !== $menu[0]) {
                close();
            }
        });
        $(window).on("resize blur", close);
        window.addEventListener("scroll", function (e) {
            if (e.target !== $menu[0]) {
                close();
            }
        }, true);

        return {
            keydown: keydown,
            close: close,
            isOpen: function () { return mode === "list"; }
        };
    }

    spe.attachCompletionMenu = attachCompletionMenu;

    spe.terminalCompletion = {
        parseHints: parseHints,
        applyCompletion: applyCompletion,
        filterEntries: filterEntries
    };
}(jQuery, window, window.spe = window.spe || {}));
