//self.registration.showNotification(
//        "Debug", {
//        body: "Service Worker Fired Up",
//        data: { url: "http://google.com" }
//    });

// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
self.addEventListener('fetch', () => { });

self.addEventListener('install', async event => {
    console.log('Installing service worker...');
    self.skipWaiting();
});

self.addEventListener('notificationclick', (e) => {
    console.log("notificationclick");

    var notification = e.notification;
    notification.close();

    var url = notification.data.url;

    e.waitUntil(
        navToUrl(url)
        /*clients.matchAll({ type: 'window' }).then(windowClients => {
            // Check if there is already a window/tab open with the target URL
            for (var i = 0; i < windowClients.length; i++) {
                var client = windowClients[i];
                // If so, just focus it.

                console.log('window - ' + client.url);

                if ((client.url.startsWith('https://meritcasting.github.io') || client.url.startsWith('https://localhost:5001')) && 'navigate' in client) {
                    return client.navigate(url);
                }
            }
            // If not, then open the target URL in a new window/tab.
            if (clients.openWindow) {
                //return clients.openWindow(url);
            }
        }) */
    );
});

addEventListener('message', event => {
    navToUrl(event.data);
});

self.navToUrl = (url) => {
    return clients.matchAll({ type: 'window' }).then(windowClients => {
        // Check if there is already a window/tab open with the target URL
        for (var i = 0; i < windowClients.length; i++) {
            var client = windowClients[i];
            // If so, just focus it.

            console.log('window - ' + client.url);

            if ((client.url.startsWith('https://meritcasting.github.io') || client.url.startsWith('https://localhost:5001')) && 'navigate' in client) {
                return client.navigate(url);
            }
        }
        // If not, then open the target URL in a new window/tab.
        if (clients.openWindow) {
            //return clients.openWindow(url);
        }
    });
}

self.addEventListener('notificationclose', (e) => {
    console.log("notificationclose");

    var notification = e.notification;

    var url = notification.data.url;

    console.log("notificationclise - notification click: " + url);

    //clients.openWindow(url);
    //notification.close();
});