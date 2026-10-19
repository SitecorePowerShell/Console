// Keeps SPE's interactive script jobs reachable when the browser throttles,
// freezes or discards the tab. Loaded on every Sheer page through
// <clientscripts><everypage> in Spe.config, so it stays dependency free.
//
//  - Tab identity: a GUID kept in the top window's sessionStorage (survives
//    reload and discard restore) and posted with every Sheer request in a
//    hidden __SPETABID input, which is how the server knows which tab started
//    or owns a job.
//  - Watchdog: SpeJobMonitor arms it on every poll. If the next poll response
//    does not arrive (the one-shot Sheer timer chain died while the tab was
//    frozen, or a response was an error page) it re-posts the poll, and on
//    visibilitychange / pageshow / resume it re-posts at once.
//  - Discovery: the top window asks the server once per page load for jobs
//    this tab should take over; the server opens a re-attached Runner.
(function (window) {
    "use strict";

    if (window.speJobWatchdog) {
        return;
    }

    var doc = window.document;
    var TAB_KEY = "spe.tabId";
    var FIELD = "__SPETABID";
    var PAGE_FIELD = "__SPEPAGEID";
    var GRACE_MS = 10000;
    var BACKOFF_MS = [2000, 5000, 15000, 30000];
    var ESCALATE_AFTER = 3;
    var DISCOVERY_ATTEMPTS = 4;

    function newId() {
        var id = "";
        for (var i = 0; i < 32; i++) {
            id += Math.floor(Math.random() * 16).toString(16);
        }
        return id;
    }

    var tabId = (function () {
        try {
            var store = window.top.sessionStorage;
            var id = store.getItem(TAB_KEY);
            if (!id) {
                id = newId();
                store.setItem(TAB_KEY, id);
            }
            return id;
        } catch (e) {
            // Cross-origin top or storage blocked: a per-page id only loses the
            // "reclaim my own tab's job right after reload" shortcut.
            return newId();
        }
    })();

    // One id per top-level page load, shared by its frames (the Runner
    // dialog reports its host page). A reload starts a new one.
    var pageId = (function () {
        try {
            return window.top.__spePageId || (window.top.__spePageId = newId());
        } catch (e) {
            return newId();
        }
    })();

    function setHidden(name, value) {
        var field = doc.getElementById(name);
        if (!field) {
            field = doc.createElement("input");
            field.type = "hidden";
            field.id = name;
            field.name = name;
            doc.body.appendChild(field);
        }
        field.value = value;
    }

    function ensureTabField() {
        if (!doc.body) {
            return;
        }
        setHidden(FIELD, tabId);
        setHidden(PAGE_FIELD, pageId);
    }

    function post(message) {
        if (!window.scForm || typeof window.scForm.postRequest !== "function") {
            return false;
        }
        ensureTabField();
        window.scForm.postRequest("", "", "", message);
        return true;
    }

    function now() {
        return new Date().getTime();
    }

    var monitors = {};

    function monitor(id) {
        if (!monitors[id]) {
            monitors[id] = { armed: false, seq: 0, handle: "", due: 0, deadline: 0, failures: 0, heartbeat: null };
        }
        return monitors[id];
    }

    function stopHeartbeat(m) {
        if (m.heartbeat) {
            window.clearInterval(m.heartbeat);
            m.heartbeat = null;
        }
    }

    function lost(id, m) {
        m.armed = false;
        if (window.console) {
            console.warn("SPE: lost contact with script job " + m.handle + " (" + id + "), recovering");
        }
        if (typeof window.speJobWatchdogLost === "function") {
            window.speJobWatchdogLost(id, m.handle);
        } else {
            window.location.reload();
        }
    }

    function kick(id, m) {
        if (m.failures >= ESCALATE_AFTER) {
            lost(id, m);
            return;
        }
        var wait = BACKOFF_MS[Math.min(m.failures, BACKOFF_MS.length - 1)];
        m.failures++;
        m.due = now() + wait;
        m.deadline = m.due;
        post("pstaskmonitor:check(id=" + id + ",seq=" + m.seq + ")");
    }

    // True while a Sheer response of this window is parked behind a modal it
    // opened (e.g. Show-ListView -Modal): Sitecore resumes the rest of that
    // response - including our arm/disarm and the Runner's "Done!" - when the
    // modal closes. Posting a poll meanwhile would orphan those commands, so
    // the watchdog waits. A request created in this window is an instance of
    // this window's scRequest; requests of parent or child frames are not.
    function waitingOnOwnModal() {
        try {
            var dialogs = window.top._scDialogs || [];
            for (var i = 0; i < dialogs.length; i++) {
                var request = dialogs[i].request;
                if (request && request.suspend && window.scRequest && request instanceof window.scRequest) {
                    return true;
                }
            }
        } catch (e) {
            return false;
        }
        return false;
    }

    // waking: the tab just became visible / resumed, so poll every monitor
    // whose scheduled poll is already overdue instead of waiting out the
    // grace period. A monitor armed moments ago (the Runner's first poll,
    // with focus/pageshow firing as its page loads) is left alone.
    function sweep(waking) {
        if (waitingOnOwnModal()) {
            return;
        }
        var time = now();
        for (var id in monitors) {
            if (monitors.hasOwnProperty(id)) {
                var m = monitors[id];
                if (m.armed && (time > m.deadline || (waking && time > m.due + 250))) {
                    kick(id, m);
                }
            }
        }
    }

    var lastForced = 0;

    function wake() {
        if (doc.visibilityState === "hidden") {
            return;
        }
        var time = now();
        if (time - lastForced < 1000) {
            return;
        }
        lastForced = time;
        sweep(true);
    }

    window.speJobWatchdog = {
        tabId: tabId,

        // A poll for monitor `id` with sequence `seq` is scheduled in `delay` ms.
        arm: function (id, seq, delay, handle) {
            var m = monitor(id);
            stopHeartbeat(m);
            m.armed = true;
            m.seq = seq;
            m.handle = handle || m.handle;
            m.failures = 0;
            m.due = now() + delay;
            m.deadline = m.due + GRACE_MS;
        },

        // The monitor is paused on purpose (a dialog is open): no polls, but
        // keep the server-side lease alive.
        hold: function (id, heartbeatMs) {
            var m = monitor(id);
            m.armed = false;
            stopHeartbeat(m);
            m.heartbeat = window.setInterval(function () {
                post("pstaskmonitor:heartbeat(id=" + id + ")");
            }, heartbeatMs);
        },

        disarm: function (id) {
            var m = monitor(id);
            m.armed = false;
            stopHeartbeat(m);
        }
    };

    // Re-attaching may show a dialog with scForm.showModalDialog, which needs
    // top._scDialogs; Sitecore creates it only once its JqueryModalDialogs
    // iframe (added on window load) has loaded itself. Runs fn once it exists
    // (or after ~15s regardless).
    function whenDialogsReady(fn, waited) {
        waited = waited || 0;
        var ready = false;
        try {
            ready = !!window.top._scDialogs;
        } catch (e) {
            ready = false;
        }
        if (!ready && waited < 15000) {
            window.setTimeout(function () {
                whenDialogsReady(fn, waited + 250);
            }, 250);
            return;
        }
        fn();
    }

    window.speJobRecovery = {
        retryDiscovery: function (delay, attempt) {
            if (attempt + 1 < DISCOVERY_ATTEMPTS) {
                window.setTimeout(function () {
                    discover(attempt + 1);
                }, delay);
            }
        },

        // Posts a Sheer message once dialogs can be shown (ISE adopting a job
        // that waits on a dialog).
        postWhenDialogsReady: function (message) {
            whenDialogsReady(function () {
                post(message);
            });
        }
    };

    function discover(attempt) {
        whenDialogsReady(function () {
            post("spe:claimorphanedjobs(attempt=" + attempt + ")");
        });
    }

    window.setInterval(function () {
        sweep(false);
    }, 2000);

    doc.addEventListener("visibilitychange", wake);
    window.addEventListener("pageshow", wake);
    window.addEventListener("focus", wake);
    doc.addEventListener("resume", wake);

    doc.addEventListener("DOMContentLoaded", ensureTabField);

    window.addEventListener("load", function () {
        ensureTabField();
        var isTop = false;
        try {
            isTop = window.top === window;
        } catch (e) {
            isTop = false;
        }
        if (isTop) {
            window.setTimeout(function () {
                discover(0);
            }, 300);
        }
    });
})(window);
