mergeInto(LibraryManager.library, {
  Luxodd_RegisterHostMessageListener: function (goNamePtr) {
        
        var S = window.__SessionBridgeState || (window.__SessionBridgeState = {
            listenerAdded: false,
            expectedOrigin: null,
            targetGO: "SessionOptionsBridge"
        });

        try {
            var go = UTF8ToString(goNamePtr || 0);
            if (go) S.targetGO = go;
        } catch(e) {}

        if (S.listenerAdded) return;

        function sendToUnity(method, payload) {
            var arg = (typeof payload === "string") ? payload : JSON.stringify(payload || {});
            if (typeof unityInstance !== "undefined") {
                unityInstance.SendMessage(S.targetGO, method, arg);
            } else {
                console.warn("[jslib] unityInstance is not defined, drop", method, arg);
            }
        }

        function onMessage(e) {
            if (S.expectedOrigin && e.origin !== S.expectedOrigin) return;
            var d = e.data;
            if (!d || typeof d !== "object") return;

            // Host sends { action: "restart|continue|end" }
            if (typeof d.action === "string") {
                sendToUnity("OnHostSessionAction", d.action);
            }

            // Host sends { jwt: "..." } at start
            if (typeof d.jwt === "string") {
                sendToUnity("OnHostJwt", d.jwt);
            }

            // Future: if typed response added
            // { type:"session_options_result", ... }
            if (d.type === "session_options_result") {
                sendToUnity("OnSessionOptionsResult", d);
            }
        }

        window.addEventListener("message", onMessage);
        S.listenerAdded = true;
        console.log("[jslib] Host message listener registered for GO:", S.targetGO);
    },
	
	Luxodd_SetExpectedHostOrigin: function (originPtr) {
        var S = window.__SessionBridgeState || (window.__SessionBridgeState = {
            listenerAdded: false,
            expectedOrigin: null,
            targetGO: "SessionOptionsBridge"
        });
        try {
            var o = UTF8ToString(originPtr || 0);
            S.expectedOrigin = o || null;
        } catch(e) {
            S.expectedOrigin = null;
        }
        console.log("[jslib] expectedOrigin =", S.expectedOrigin);
    }
});