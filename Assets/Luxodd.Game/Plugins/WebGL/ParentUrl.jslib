mergeInto(LibraryManager.library, {
    Luxodd_GetParentHost: function () {
        var host;

        try {
            host = window.parent.location.host;
        } catch (e) {
            if (document.referrer) {
                var ref = new URL(document.referrer);
                host = ref.host;
            } else {
                host = window.location.hostname + ":8080";
            }
        }

        var length = lengthBytesUTF8(host) + 1;
        var buffer = _malloc(length);
        stringToUTF8(host, buffer, length);
        return buffer;
    },

    Luxodd_GetWebSocketProtocol: function () {
        var proto = (window.location.protocol === "https:") ? "wss:" : "ws:";

        var length = lengthBytesUTF8(proto) + 1;
        var buffer = _malloc(length);
        stringToUTF8(proto, buffer, length);
        return buffer;
    }
});