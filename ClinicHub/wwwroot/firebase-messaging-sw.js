importScripts("https://www.gstatic.com/firebasejs/12.17.1/firebase-app-compat.js");
importScripts("https://www.gstatic.com/firebasejs/12.17.1/firebase-messaging-compat.js");

firebase.initializeApp({
    apiKey: "AIzaSyBDxnZgDSspKUrcjdao39rfTL7PTgoW1DU",
    authDomain: "doctory-1aca1.firebaseapp.com",
    projectId: "doctory-1aca1",
    storageBucket: "doctory-1aca1.firebasestorage.app",
    messagingSenderId: "1077893614286",
    appId: "1:1077893614286:web:be3f460b60b1dd290c9e9f",
    measurementId: "G-FQGVDLQC6Q"
});

const messaging = firebase.messaging();

messaging.onBackgroundMessage((payload) => {
    const notificationTitle = payload.notification?.title || payload.data?.title || "Doctory";
    const notificationOptions = {
        body: payload.notification?.body || payload.data?.body || "",
        icon: "/notification_logo.png",
        badge: "/notification_logo.png",
        data: payload.data || {}
    };
    self.registration.showNotification(notificationTitle, notificationOptions);
});

const APPOINTMENT_TYPES = ["AppointmentReminder", "AppointmentConfirmation", "AppointmentCancellation", "PaymentConfirmation", "CancellationWindowClosed", "RefundProcessed"];
const NAV_CACHE = "ch-nav";
const NAV_KEY = "/__ch_nav_appointments__";

// The role-based appointments page is cached by fcm.js from the dashboard —
// service workers have no access to localStorage, so the click handler reads
// it from the Cache Storage API instead.
async function cachedAppointmentsPath() {
    try {
        const cache = await caches.open(NAV_CACHE);
        const res = await cache.match(NAV_KEY);
        if (res) return await res.text();
    } catch (e) {}
    return null;
}

async function resolveTargetUrl(notification) {
    const type = notification.data?.type || "";
    if (APPOINTMENT_TYPES.includes(type)) {
        const cached = await cachedAppointmentsPath();
        if (cached) return cached;
    }
    return notification.data?.link || "/";
}

self.addEventListener("notificationclick", (event) => {
    event.notification.close();
    event.waitUntil((async () => {
        const url = await resolveTargetUrl(event.notification);
        if (!url.startsWith("/")) return;

        const windows = await self.clients.matchAll({ type: "window", includeUncontrolled: true });
        if (windows.length > 0) {
            try {
                await windows[0].focus();
                await windows[0].navigate(url);
                return;
            } catch (e) {}
        }
        await self.clients.openWindow(url);
    })());
});
