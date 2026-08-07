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

self.addEventListener("notificationclick", (event) => {
    event.notification.close();
    const link = event.notification.data?.link || "/";
    if (link.startsWith("/")) {
        event.waitUntil(clients.openWindow(link));
    }
});
