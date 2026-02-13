#if USE_DATA_CACHING
const cacheName = {{{JSON.stringify(COMPANY_NAME + "-" + PRODUCT_NAME + "-" + PRODUCT_VERSION )}}};
const contentToCache = [
    "Build/{{{ LOADER_FILENAME }}}",
    "Build/{{{ FRAMEWORK_FILENAME }}}",
#if USE_THREADS
    "Build/{{{ WORKER_FILENAME }}}",
#endif
    "Build/{{{ DATA_FILENAME }}}",
    "Build/{{{ CODE_FILENAME }}}",
    "TemplateData/style.css"

];
#endif

self.addEventListener('install', function (e) {
    console.log('[Service Worker] Install');

#if USE_DATA_CACHING
    e.waitUntil((async function () {
      const cache = await caches.open(cacheName);
      console.log('[Service Worker] Caching all: app shell and content');
      await cache.addAll(contentToCache);
    })());
#endif

    // Activate immediately without waiting for old tabs to close
    self.skipWaiting();
});

self.addEventListener('activate', function (e) {
    // Take control of all pages immediately so the new fetch handler is used
    e.waitUntil(self.clients.claim());
});

#if USE_DATA_CACHING
self.addEventListener('fetch', function (e) {
    // Only handle GET requests over HTTP/HTTPS.
    // POST requests and non-HTTP schemes (chrome-extension://, blob:, data:, etc.)
    // cannot be stored in the Cache API and must pass through to the browser.
    if (e.request.method !== 'GET' || !e.request.url.startsWith('http')) {
        return;
    }

    e.respondWith((async function () {
      // Serve from cache if already stored (build files pre-cached on install)
      let response = await caches.match(e.request);
      if (response) { return response; }

      // Not in cache — fetch from network
      response = await fetch(e.request);

      // Only cache successful same-origin responses (avoid caching errors or
      // cross-origin opaque responses that waste cache quota)
      if (response && response.status === 200 && response.type === 'basic') {
          try {
              const cache = await caches.open(cacheName);
              cache.put(e.request, response.clone());
          } catch (err) {
              // Cache storage full or request not cacheable — not critical
          }
      }
      return response;
    })());
});
#endif