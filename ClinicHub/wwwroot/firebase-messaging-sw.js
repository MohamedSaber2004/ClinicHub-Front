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

const APPOINTMENT_TYPES = ["AppointmentOutsideAvailability", "AppointmentOutsideWorkingHours", "NewBookingRequest", "PaymentReceived", "RevenueIncreased", "AppointmentAccepted"];
const CLINIC_TYPES = ["ClinicRegistered", "ClinicApproved", "ClinicRejected"];
const NOTIFICATION_TYPES = ["SubscriptionExpiring", "AdExpiring"];
const NAV_CACHE = "ch-nav";
const NAV_KEY_APPOINTMENTS = "/__ch_nav_appointments__";
const NAV_KEY_NOTIFICATIONS = "/__ch_nav_notifications__";
const NAV_KEY_CLINICS = "/__ch_nav_clinics__";

// The role-based pages are cached by fcm.js from the dashboard — service
// workers have no access to localStorage, so the click handler reads them
// from the Cache Storage API instead.
async function cachedValue(key) {
    try {
        const cache = await caches.open(NAV_CACHE);
        const res = await cache.match(key);
        if (res) return await res.text();
    } catch (e) {}
    return null;
}

// First path segment of a deep link, lowercase. Handles both full URLs
// ("https://your-frontend/appointments/3fa8…") and relative links
// ("AppointmentDetails/123").
function firstSegment(link) {
    if (!link) return "";
    try {
        return new URL(link).pathname.split("/").filter(Boolean)[0].toLowerCase() || "";
    } catch (e) {
        return link.split("/").filter(Boolean)[0].toLowerCase() || "";
    }
}

async function resolveTargetUrl(notification) {
    const type = notification.data?.type || "";
    const link = notification.data?.link || "";
    const first = firstSegment(link);

    if (APPOINTMENT_TYPES.includes(type) || first === "appointments" || first === "appointmentdetails") {
        const path = await cachedValue(NAV_KEY_APPOINTMENTS);
        if (path) return path;
    }
    if (CLINIC_TYPES.includes(type) || first === "clinics") {
        const path = await cachedValue(NAV_KEY_CLINICS);
        if (path) return path;
    }
    if (NOTIFICATION_TYPES.includes(type) || first === "notifications" || first === "chat" || first === "conversations") {
        const path = await cachedValue(NAV_KEY_NOTIFICATIONS);
        if (path) return path;
    }
    if (link.startsWith("/")) return link;
    return "/";
}

// Replace the old service worker immediately on the next page load — without
// skipWaiting a new SW only activates after ALL tabs are closed, so clicks
// would keep using the previous (broken) handler.
self.addEventListener("install", () => {
    self.skipWaiting();
});

self.addEventListener("activate", (event) => {
    event.waitUntil(self.clients.claim());
});

self.addEventListener("notificationclick", (event) => {
    event.notification.close();
    event.waitUntil((async () => {
        const url = await resolveTargetUrl(event.notification);
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
