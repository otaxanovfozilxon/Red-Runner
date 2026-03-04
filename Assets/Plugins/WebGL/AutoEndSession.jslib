mergeInto(LibraryManager.library, {

    // ── Persistent diagnostic logger (survives page navigation) ──
    DiagLog: function (msgPtr) {
        var msg = UTF8ToString(msgPtr);
        var ts = new Date().toISOString().substr(11, 12);
        var entry = "[" + ts + "] " + msg;
        console.log(entry);
        // Send to parent window so it's visible in host console
        try { window.parent.postMessage({ type: "game_debug", log: entry }, "*"); } catch(e) {}
        try {
            var prev = localStorage.getItem("redrunner_diag") || "";
            // Keep last 80 lines
            var lines = prev ? prev.split("\n") : [];
            lines.push(entry);
            if (lines.length > 80) lines = lines.slice(lines.length - 80);
            localStorage.setItem("redrunner_diag", lines.join("\n"));
        } catch (e) {}
    },

    DiagLogJS: function (msg) {
        // Internal JS helper (not called from C#)
        var ts = new Date().toISOString().substr(11, 12);
        var entry = "[" + ts + "] " + msg;
        console.log(entry);
        try { window.parent.postMessage({ type: "game_debug", log: entry }, "*"); } catch(e) {}
        try {
            var prev = localStorage.getItem("redrunner_diag") || "";
            var lines = prev ? prev.split("\n") : [];
            lines.push(entry);
            if (lines.length > 80) lines = lines.slice(lines.length - 80);
            localStorage.setItem("redrunner_diag", lines.join("\n"));
        } catch (e) {}
    },

    GetDiagLog: function () {
        var json = "";
        try { json = localStorage.getItem("redrunner_diag") || ""; } catch(e) {}
        var bufferSize = lengthBytesUTF8(json) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(json, buffer, bufferSize);
        return buffer;
    },

    ClearDiagLog: function () {
        try { localStorage.removeItem("redrunner_diag"); } catch(e) {}
    },

    // ── Save/Restore game state (survives iframe removal) ──
    SaveGameState: function (jsonPtr) {
        var json = UTF8ToString(jsonPtr);
        try { localStorage.setItem("redrunner_continue_state", json); } catch(e) {}
        console.log("[SaveState] Saved: " + json);
    },

    HasSavedGameState: function () {
        try { return localStorage.getItem("redrunner_continue_state") !== null ? 1 : 0; } catch(e) { return 0; }
    },

    GetSavedGameState: function () {
        var json = "";
        try { json = localStorage.getItem("redrunner_continue_state") || ""; } catch(e) {}
        var bufferSize = lengthBytesUTF8(json) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(json, buffer, bufferSize);
        return buffer;
    },

    ClearSavedGameState: function () {
        try { localStorage.removeItem("redrunner_continue_state"); } catch(e) {}
    },

    // ── Auto-end session timer ──
    AutoEndSession_Start: function (seconds) {
        // Helper for persistent logging from JS
        function dlog(msg) {
            var ts = new Date().toISOString().substr(11, 12);
            var entry = "[" + ts + "] " + msg;
            console.log(entry);
            try {
                var prev = localStorage.getItem("redrunner_diag") || "";
                var lines = prev ? prev.split("\n") : [];
                lines.push(entry);
                if (lines.length > 80) lines = lines.slice(lines.length - 80);
                localStorage.setItem("redrunner_diag", lines.join("\n"));
            } catch (e) {}
        }

        // Cancel any existing timer and listener
        if (window.__autoEndTimerId) {
            clearTimeout(window.__autoEndTimerId);
            window.__autoEndTimerId = null;
        }

        // Flags
        window.__autoEndHandled = false;
        window.__continueAllowed = false;

        // Listen for host responses directly in JS
        // This runs SYNCHRONOUSLY when the host postMessage arrives,
        // BEFORE the host can remove the iframe — critical for state management.
        if (!window.__autoEndListener) {
            window.__autoEndListener = function (e) {
                var d = e.data;
                if (!d || typeof d !== "object") return;
                if (typeof d.action === "string") {
                    var actionLower = d.action.toLowerCase();
                    dlog("[AutoEnd] Host responded with action: '" + d.action + "' (lower: '" + actionLower + "')");

                    if (actionLower === "continue" || actionLower === "restart") {
                        window.__continueAllowed = true;
                        window.__autoEndHandled = true;
                        // KEEP the saved state — game will reload and auto-continue
                        dlog("[AutoEnd] " + actionLower.toUpperCase() + " detected - timer cancelled, saved state PRESERVED for reload");
                    } else if (actionLower === "end") {
                        window.__autoEndHandled = true;
                        // CLEAR the saved state — session is ending, no auto-continue
                        try { localStorage.removeItem("redrunner_continue_state"); } catch(ex) {}
                        dlog("[AutoEnd] END detected - timer cancelled, saved state CLEARED");
                    }

                    if (window.__autoEndTimerId) {
                        clearTimeout(window.__autoEndTimerId);
                        window.__autoEndTimerId = null;
                    }
                    window.removeEventListener("message", window.__autoEndListener);
                    window.__autoEndListener = null;
                }
            };
            window.addEventListener("message", window.__autoEndListener);
        }

        // Add beforeunload handler to catch page navigation
        if (!window.__beforeUnloadAdded) {
            window.__beforeUnloadAdded = true;
            window.addEventListener("beforeunload", function () {
                dlog("[NAVIGATION] Page is being unloaded! continueAllowed=" + window.__continueAllowed + " autoEndHandled=" + window.__autoEndHandled);
                // Dump all diagnostics to parent so they survive iframe removal
                try {
                    var allDiag = localStorage.getItem("redrunner_diag") || "";
                    var hasSaved = localStorage.getItem("redrunner_continue_state") || "none";
                    window.parent.postMessage({
                        type: "game_debug_dump",
                        diagnostics: allDiag,
                        savedState: hasSaved,
                        continueAllowed: window.__continueAllowed,
                        autoEndHandled: window.__autoEndHandled
                    }, "*");
                } catch(e) {}
            });
        }

        var ms = seconds * 1000;
        dlog("[AutoEnd] Timer started: " + seconds + "s");

        window.__autoEndTimerId = setTimeout(function () {
            window.__autoEndTimerId = null;

            if (window.__autoEndHandled) {
                dlog("[AutoEnd] Timer fired but already handled - skipping");
                return;
            }

            dlog("[AutoEnd] Timer fired - sending end + session_end to host");
            window.__autoEndHandled = true;

            if (window.__autoEndListener) {
                window.removeEventListener("message", window.__autoEndListener);
                window.__autoEndListener = null;
            }

            // No button was pressed within 10s — clear saved state so next game starts fresh
            try { localStorage.removeItem("redrunner_continue_state"); } catch(ex) {}

            window.parent.postMessage({
                type: "session_options",
                action: "end"
            }, "*");

            setTimeout(function () {
                window.parent.postMessage({
                    type: "session_end"
                }, "*");
                dlog("[AutoEnd] session_end sent (from timer)");
            }, 500);
        }, ms);
    },

    AutoEndSession_Cancel: function () {
        window.__autoEndHandled = true;
        if (window.__autoEndTimerId) {
            clearTimeout(window.__autoEndTimerId);
            window.__autoEndTimerId = null;
        }
        if (window.__autoEndListener) {
            window.removeEventListener("message", window.__autoEndListener);
            window.__autoEndListener = null;
        }
        // Log
        var ts = new Date().toISOString().substr(11, 12);
        var entry = "[" + ts + "] [AutoEnd] Timer cancelled from Unity, continueAllowed=" + (window.__continueAllowed || false);
        console.log(entry);
        try {
            var prev = localStorage.getItem("redrunner_diag") || "";
            var lines = prev ? prev.split("\n") : [];
            lines.push(entry);
            if (lines.length > 80) lines = lines.slice(lines.length - 80);
            localStorage.setItem("redrunner_diag", lines.join("\n"));
        } catch (e) {}
    }
});
