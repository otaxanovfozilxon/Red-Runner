mergeInto(LibraryManager.library, {

    /**
     * Patch HTMLMediaElement.playbackRate to prevent NotSupportedError in WebGL.
     *
     * Unity's audio bridge (JS_Sound_SetPitch) sets playbackRate = pitch * timeScale.
     * When timeScale is 0 or very small, this produces values like 1e-6 which browsers
     * reject with: "The provided playback rate (0.000001) is not in the supported
     * playback range."  This unhandled exception corrupts the WebGL audio context,
     * killing ALL subsequent audio.
     *
     * This patch clamps playbackRate to the browser-safe range [0.0625, 16]
     * and swallows any remaining exceptions, keeping the audio context alive.
     *
     * Must be called once, before any audio plays (e.g. in AudioManager.Awake).
     */
    WebGLPatchAudioPitch: function () {
        if (window.__audioPitchPatched) return;
        window.__audioPitchPatched = true;

        try {
            var desc = Object.getOwnPropertyDescriptor(
                HTMLMediaElement.prototype, 'playbackRate'
            );
            if (desc && desc.set) {
                var origSet = desc.set;
                Object.defineProperty(HTMLMediaElement.prototype, 'playbackRate', {
                    get: desc.get,
                    set: function (v) {
                        // Clamp to browser-safe range (Chrome: 0.0625–16)
                        if (!isFinite(v) || v < 0.0625) v = 0.0625;
                        if (v > 16) v = 16;
                        try {
                            origSet.call(this, v);
                        } catch (e) {
                            // Swallow — prevents audio context corruption
                        }
                    },
                    configurable: true,
                    enumerable: true
                });
            }
        } catch (e) {
            console.warn('[AudioPitchFix] Could not patch playbackRate:', e);
        }
    }

});
