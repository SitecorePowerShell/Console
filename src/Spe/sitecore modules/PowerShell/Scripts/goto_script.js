(function () {
    "use strict";

    var Tooltip = ace.require("ace/tooltip").Tooltip;

    var modifierDown = false;

    function syncModifierFromEvent(e) {
        modifierDown = !!(e.ctrlKey || e.metaKey);
    }
    document.addEventListener("keydown", syncModifierFromEvent, true);
    document.addEventListener("keyup", syncModifierFromEvent, true);
    window.addEventListener("blur", function () { modifierDown = false; });

    var TRIGGER_REGEXES = [
        {
            kind: "function",
            re: /\bImport-Function\b(?:\s+-Name)?\s+(?:"([^"$`]+)"|'([^']+)'|([A-Za-z0-9_\-]+))/gi
        },
        {
            kind: "script",
            re: /\bInvoke-Script\b(?:\s+-(?:Path|FullName|FileName))?\s+(?:"([^"$`]+)"|'([^']+)'|([A-Za-z0-9_\-\\\/. :]+))/gi
        }
    ];

    function GotoScript(editor) {
        this.editor = editor;
        this.session = editor.getSession();
        this.marker = null;
        this.markerRange = null;
        this.markerKind = null;
        this.tooltip = new Tooltip(editor.container);
        this.tooltip.setClassName("spe-goto-tooltip");
        this.pickerEl = null;
        this.pickerMatches = null;
        this.pickerSelectedIndex = 0;
        this.resolveCache = {};
        this.resolveTimer = null;
        this.currentHover = null;
        this._bindEvents();
    }

    GotoScript.prototype._bindEvents = function () {
        var self = this;
        var scroller = this.editor.renderer.scroller;
        this._onMouseMove = function (e) { self._handleMouseMove(e); };
        this._onMouseOut  = function (e) { self._handleMouseOut(e); };
        this._onMouseDown = function (e) { self._handleMouseDown(e); };
        scroller.addEventListener("mousemove", this._onMouseMove);
        scroller.addEventListener("mouseout", this._onMouseOut);
        scroller.addEventListener("mousedown", this._onMouseDown, true);
    };

    GotoScript.prototype._handleMouseOut = function () {
        this._clearMarker();
        this._hideTooltip();
    };

    GotoScript.prototype._clearMarker = function () {
        if (this.marker != null) {
            this.session.removeMarker(this.marker);
            this.marker = null;
            this.markerRange = null;
            this.markerKind = null;
        }
    };

    GotoScript.prototype._hideTooltip = function () {
        if (this.tooltip) this.tooltip.hide();
    };

    GotoScript.prototype._findTriggerAt = function (lineText, column) {
        for (var i = 0; i < TRIGGER_REGEXES.length; i++) {
            var spec = TRIGGER_REGEXES[i];
            var iterator = lineText.matchAll(spec.re);
            var step = iterator.next();
            while (!step.done) {
                var m = step.value;
                var target = m[1] || m[2] || m[3];
                var targetStart = m.index + m[0].lastIndexOf(target);
                var targetEnd = targetStart + target.length;
                if (column >= targetStart && column <= targetEnd) {
                    var extras = spec.kind === "function"
                        ? this._parseFunctionExtras(lineText)
                        : {};
                    return {
                        kind: spec.kind,
                        target: target,
                        start: targetStart,
                        end: targetEnd,
                        module: extras.module || null,
                        library: extras.library || null
                    };
                }
                step = iterator.next();
            }
        }
        return null;
    };

    GotoScript.prototype._parseFunctionExtras = function (lineText) {
        var out = {};
        var re = /-(Module|Library)\s+(?:"([^"$`]*)"|'([^']*)'|([A-Za-z0-9_\-]+))/gi;
        var iterator = lineText.matchAll(re);
        var step = iterator.next();
        while (!step.done) {
            var m = step.value;
            out[m[1].toLowerCase()] = m[2] || m[3] || m[4];
            step = iterator.next();
        }
        return out;
    };

    GotoScript.prototype._handleMouseMove = function (e) {
        if (!modifierDown) {
            this._clearMarker();
            this._hideTooltip();
            return;
        }
        var renderer = this.editor.renderer;
        var docPos = renderer.screenToTextCoordinates(e.clientX, e.clientY);
        if (!docPos) return;

        var line = this.session.getLine(docPos.row);
        var hit = this._findTriggerAt(line, docPos.column);
        if (!hit) {
            this.currentHover = null;
            this._clearMarker();
            this._hideTooltip();
            return;
        }
        hit.row = docPos.row;
        this.currentHover = hit;

        var self = this;
        if (this.resolveTimer) clearTimeout(this.resolveTimer);
        this.resolveTimer = setTimeout(function () {
            self._resolveAndRender(hit, e.clientX, e.clientY);
        }, 120);
    };

    GotoScript.prototype._cacheKey = function (hit) {
        return hit.kind + "|" + hit.target + "|" + (hit.module || "") + "|" + (hit.library || "");
    };

    GotoScript.prototype._resolveAndRender = function (hit, clientX, clientY) {
        var key = this._cacheKey(hit);
        var self = this;
        var render = function (matches) {
            if (!self.currentHover ||
                self.currentHover.start !== hit.start ||
                self.currentHover.row !== hit.row) return;
            self._renderMarker(hit, matches);
            self._renderTooltip(matches, clientX, clientY);
        };
        if (this.resolveCache[key]) { render(this.resolveCache[key]); return; }
        this._callResolveEndpoint(hit, function (matches) {
            self.resolveCache[key] = matches;
            render(matches);
        });
    };

    GotoScript.prototype._callResolveEndpoint = function (hit, cb) {
        var payload = {
            guid: (window.spe && window.spe.guid) || "",
            kind: hit.kind,
            target: hit.target,
            module: hit.module || "",
            library: hit.library || ""
        };
        var url = "/sitecore modules/PowerShell/Services/PowerShellWebService.asmx/ResolveScriptReference";
        var xhr = new XMLHttpRequest();
        xhr.open("POST", url, true);
        xhr.setRequestHeader("Content-Type", "application/json");
        xhr.onreadystatechange = function () {
            if (xhr.readyState !== 4) return;
            if (xhr.status !== 200) { cb([]); return; }
            try {
                var wrapped = JSON.parse(xhr.responseText);
                var inner = (wrapped && typeof wrapped.d === "string") ? JSON.parse(wrapped.d) : wrapped;
                cb((inner && inner.Matches) || []);
            } catch (_) { cb([]); }
        };
        xhr.send(JSON.stringify(payload));
    };

    var Range = ace.require("ace/range").Range;

    GotoScript.prototype._renderMarker = function (hit, matches) {
        this._clearMarker();
        var valid = matches && matches.length > 0;
        var cls = valid ? "spe-goto-underline" : "spe-goto-underline-missing";
        this.markerRange = new Range(hit.row, hit.start, hit.row, hit.end);
        this.marker = this.session.addMarker(this.markerRange, cls, "text");
        this.markerKind = valid ? "valid" : "missing";
    };

    GotoScript.prototype._renderTooltip = function (matches, clientX, clientY) {
        var el = this.tooltip.getElement();
        while (el.firstChild) el.removeChild(el.firstChild);

        var header = document.createElement("div");
        header.className = "spe-goto-tooltip-header";

        var body = document.createElement("div");
        body.className = "spe-goto-tooltip-body";

        if (!matches || matches.length === 0) {
            header.textContent = "Not found";
        } else if (matches.length === 1) {
            header.textContent = "Ctrl+Click or Ctrl+F12 to open";
            body.textContent = matches[0].Path;
        } else {
            header.textContent = "Ctrl+Click or Ctrl+F12 to choose";
            body.textContent = matches.length + " matches";
        }

        el.appendChild(header);
        if (body.textContent) el.appendChild(body);
        this.tooltip.show(null, clientX + 10, clientY + 20);
    };

    GotoScript.prototype._handleMouseDown = function (e) {
        if (!modifierDown) return;
        if (!this.currentHover) return;
        if (this.markerKind !== "valid") return;

        var renderer = this.editor.renderer;
        var docPos = renderer.screenToTextCoordinates(e.clientX, e.clientY);
        if (!docPos || docPos.row !== this.currentHover.row) return;
        if (docPos.column < this.currentHover.start || docPos.column > this.currentHover.end) return;

        var matches = this.resolveCache[this._cacheKey(this.currentHover)] || [];
        if (matches.length === 0) return;

        e.preventDefault();
        e.stopPropagation();
        if (matches.length === 1) this._openMatch(matches[0]);
        else this._showPicker(matches, e.clientX, e.clientY, "mouse");
    };

    GotoScript.prototype._openMatch = function (match) {
        if (!match || !match.Id || !match.Db) return;
        scForm.postRequest("", "", "", "ise:gotoscript(id=" + match.Id + ",db=" + match.Db + ")");
    };

    GotoScript.prototype.gotoAtCaret = function () {
        var pos = this.editor.getCursorPosition();
        var line = this.session.getLine(pos.row);
        var hit = this._findTriggerAt(line, pos.column);
        if (!hit) return;
        hit.row = pos.row;
        this.currentHover = hit;

        var self = this;
        this._callResolveEndpoint(hit, function (matches) {
            if (!matches || matches.length === 0) return;
            self.resolveCache[self._cacheKey(hit)] = matches;
            if (matches.length === 1) {
                self._openMatch(matches[0]);
            } else {
                var coords = self.editor.renderer.textToScreenCoordinates(hit.row, hit.end);
                self._showPicker(matches, coords.pageX, coords.pageY, "keyboard");
            }
        });
    };

    GotoScript.prototype._showPicker = function (matches, x, y, trigger) {
        this._hidePicker();
        var self = this;
        this.pickerEl = document.createElement("div");
        this.pickerEl.className = "spe-goto-picker";
        this.pickerEl.tabIndex = -1;

        matches.forEach(function (m, idx) {
            var row = document.createElement("div");
            row.className = "spe-goto-picker-row";

            var nameSpan = document.createElement("span");
            nameSpan.className = "spe-goto-picker-name";
            nameSpan.textContent = m.DisplayName || "";
            row.appendChild(nameSpan);

            if (m.Module) {
                row.appendChild(document.createTextNode(" "));
                var moduleSpan = document.createElement("span");
                moduleSpan.className = "spe-goto-picker-meta";
                moduleSpan.textContent = "[" + m.Module + "]";
                row.appendChild(moduleSpan);
            }
            if (m.Library) {
                row.appendChild(document.createTextNode(" "));
                var librarySpan = document.createElement("span");
                librarySpan.className = "spe-goto-picker-meta";
                librarySpan.textContent = m.Library;
                row.appendChild(librarySpan);
            }

            var pathDiv = document.createElement("div");
            pathDiv.className = "spe-goto-picker-path";
            pathDiv.textContent = m.Path || "";
            row.appendChild(pathDiv);

            row.addEventListener("click", function () { self._openMatch(m); self._hidePicker(); });
            row.addEventListener("mouseenter", function () { self._selectPickerRow(idx); });
            self.pickerEl.appendChild(row);
        });

        document.body.appendChild(this.pickerEl);
        this.pickerEl.style.left = x + "px";
        this.pickerEl.style.top  = (y + 6) + "px";
        this.pickerMatches = matches;
        this.pickerSelectedIndex = 0;
        this._selectPickerRow(0);

        this._pickerKeyHandler = function (ev) { self._handlePickerKey(ev); };
        this._pickerOutsideHandler = function (ev) {
            if (self.pickerEl && !self.pickerEl.contains(ev.target)) self._hidePicker();
        };
        document.addEventListener("keydown", this._pickerKeyHandler, true);
        document.addEventListener("mousedown", this._pickerOutsideHandler, true);
        if (trigger === "keyboard") this.pickerEl.focus();
    };

    GotoScript.prototype._selectPickerRow = function (idx) {
        if (!this.pickerEl) return;
        var rows = this.pickerEl.querySelectorAll(".spe-goto-picker-row");
        for (var i = 0; i < rows.length; i++) rows[i].classList.remove("selected");
        if (rows[idx]) { rows[idx].classList.add("selected"); this.pickerSelectedIndex = idx; }
    };

    GotoScript.prototype._handlePickerKey = function (ev) {
        if (!this.pickerEl) return;
        if (ev.key === "ArrowDown") {
            ev.preventDefault();
            this._selectPickerRow(Math.min(this.pickerMatches.length - 1, this.pickerSelectedIndex + 1));
        } else if (ev.key === "ArrowUp") {
            ev.preventDefault();
            this._selectPickerRow(Math.max(0, this.pickerSelectedIndex - 1));
        } else if (ev.key === "Enter") {
            ev.preventDefault();
            var m = this.pickerMatches[this.pickerSelectedIndex];
            if (m) this._openMatch(m);
            this._hidePicker();
        } else if (ev.key === "Escape") {
            ev.preventDefault();
            this._hidePicker();
        }
    };

    GotoScript.prototype._hidePicker = function () {
        if (this._pickerKeyHandler) {
            document.removeEventListener("keydown", this._pickerKeyHandler, true);
            this._pickerKeyHandler = null;
        }
        if (this._pickerOutsideHandler) {
            document.removeEventListener("mousedown", this._pickerOutsideHandler, true);
            this._pickerOutsideHandler = null;
        }
        if (this.pickerEl) { this.pickerEl.remove(); this.pickerEl = null; }
        this.pickerMatches = null;
        this.pickerSelectedIndex = 0;
    };

    window.spe = window.spe || {};
    window.spe.gotoScript = {
        attach: function (editor) {
            if (!editor) return null;
            editor.gotoScript = new GotoScript(editor);
            return editor.gotoScript;
        }
    };
})();
