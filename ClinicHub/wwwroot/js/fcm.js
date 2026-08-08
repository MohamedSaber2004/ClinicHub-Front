(function () {
    var cfg = window.ClinicHubConfig || {};
    var VAPID_PUBLIC_KEY = cfg.vapidKey || "";
    var FIREBASE_CONFIG = cfg.firebaseConfig || {};

    var SW_PATH = "/firebase-messaging-sw.js";
    var LOGIN_FORM_ID = "loginForm";
    var REGISTER_FORM_ID = "clinicRegisterForm";
    var TOKEN_CACHE_KEY = "ch_fcm_token";

    // Compat-SDK helpers: firebase-messaging-compat exposes these as methods on
    // the messaging instance (firebase.messaging(app)), not as global functions.
    function getMessaging(app) {
        return firebase.messaging(app);
    }

    function getToken(messaging, options) {
        return messaging.getToken(options);
    }

    function onMessage(messaging, callback) {
        return messaging.onMessage(callback);
    }

    if (!FIREBASE_CONFIG.apiKey || !VAPID_PUBLIC_KEY) {
        return;
    }

    // The bell badge/list now flow through the MVC controllers (server-side
    // bearer token via the shared HttpClient) instead of the raw API with a
    // localStorage token — so the endpoints are role-relative MVC actions.
    function notificationsBasePath() {
        var role = (localStorage.getItem("role") || "").toLowerCase();
        if (role.indexOf("clinic") !== -1) return "/Clinic";
        if (role.indexOf("doctor") !== -1) return "/Doctor";
        if (role.indexOf("staff") !== -1) return "/Staff";
        return "/Admin";
    }

    function notificationsUrl(page, pageSize) {
        return notificationsBasePath() + "/NotificationsList?pageNumber=" + (page || 1) + "&pageSize=" + (pageSize || 10);
    }

    function notificationsCountUrl() {
        return notificationsBasePath() + "/NotificationsCount";
    }

    function fetchJson(url) {
        return fetch(url)
            .then(function (r) { return r.json().catch(function () { return null; }); })
            .then(function (json) {
                if (json && json.data !== undefined && json.data !== null) return json.data;
                return json;
            });
    }

    function isLoginPage() {
        return !!document.getElementById(LOGIN_FORM_ID);
    }

    function registerServiceWorker() {
        if (!("serviceWorker" in navigator)) {
            return Promise.resolve();
        }
        return navigator.serviceWorker.register(SW_PATH)
            .then(function (reg) {
                // Force the update check on every load: combined with
                // skipWaiting() in the SW file, a freshly deployed SW takes
                // over immediately instead of waiting for all tabs to close.
                if (reg && typeof reg.update === "function") reg.update();
                return Promise.race([
                    navigator.serviceWorker.ready,
                    new Promise(function (resolve) { setTimeout(resolve, 2000); })
                ]);
            });
    }

    function tokenSupported() {
        return ("Notification" in window) && ("serviceWorker" in navigator);
    }

    // Shared token-attach machinery for auth forms (login + clinic
    // registration): pre-fill the cached token, warm up the service worker,
    // request permission on the first user gesture, and briefly wait at
    // submit time for an imminent token. The registration page needs this too
    // because a pending clinic owner cannot log in until approval — the token
    // must travel with the registration request or ClinicApproved/ClinicRejected
    // pushes can never be delivered.
    function attachTokenFill(app, form, fcmInput, platformInput) {
        if (!form || !fcmInput) return;

        var TOKEN_WAIT_MS = 4000;
        var fillPromise = null;
        var tokenWaitBusy = false;
        var tokenWaitAttempted = false;
        var permissionPending = false;

        function cachedToken() {
            try { return localStorage.getItem(TOKEN_CACHE_KEY) || ""; } catch (e) { return ""; }
        }

        // Pre-fill with the last known web token so even a fast or Enter-key
        // submit still carries one; the background refresh below replaces it.
        if (cachedToken()) {
            fcmInput.value = cachedToken();
            if (platformInput) platformInput.value = "0";
        }

        function acquireToken() {
            var messaging;
            try {
                if (typeof firebase === "undefined" || !firebase.messaging) {
                    return Promise.resolve(null);
                }
                messaging = getMessaging(app);
            } catch (err) {
                return Promise.resolve(null);
            }
            if (!tokenSupported()) {
                return Promise.resolve(null);
            }
            if (Notification.permission !== "granted") {
                return Promise.resolve(null);
            }
            return registerServiceWorker()
                .then(function () { return getToken(messaging, { vapidKey: VAPID_PUBLIC_KEY }); })
                .then(function (token) {
                    if (!token) return null;
                    fcmInput.value = token;
                    if (platformInput) platformInput.value = "0";
                    try { localStorage.setItem(TOKEN_CACHE_KEY, token); } catch (e) {}
                    return token;
                })
                .catch(function (err) {
                    return null;
                });
        }

        function startFill() {
            if (!fillPromise) fillPromise = acquireToken();
            return fillPromise;
        }

        // Warm-up on page load: registering the service worker needs no
        // permission, so do it immediately; fetch the token right away too
        // when permission is already granted.
        function warmUp() {
            if (!tokenSupported()) return;
            registerServiceWorker().then(function () {
                if (Notification.permission === "granted") startFill();
            });
        }
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", warmUp);
        } else {
            warmUp();
        }

        // First interaction is a user gesture: ask permission and start the
        // token fetch in the background, before the user finishes typing.
        document.addEventListener("pointerdown", function () {
            if (!tokenSupported()) return;
            if (Notification.permission === "default") {
                var req = Notification.requestPermission();
                permissionPending = true;
                if (req && typeof req.then === "function") {
                    req.then(function (p) {
                        permissionPending = false;
                        if (p === "granted") startFill();
                    });
                } else {
                    permissionPending = false;
                }
            } else if (Notification.permission === "granted") {
                startFill();
            }
        }, { once: true, capture: true });

        // Intercept submit briefly when a token is genuinely imminent
        // (permission already granted, or a permission prompt is pending and
        // the token fetch is in flight). The form is never blocked for more
        // than TOKEN_WAIT_MS — without a token it proceeds and push stays off.
        document.addEventListener("submit", function (e) {
            if (e.target !== form) return;

            if (tokenWaitBusy) {
                e.preventDefault();
                e.stopPropagation();
                return;
            }

            var tokenReady = !!fcmInput.value;
            var canWait = tokenSupported()
                && (Notification.permission === "granted" || permissionPending)
                && !!fillPromise
                && !tokenReady
                && !tokenWaitAttempted;

            if (canWait) {
                tokenWaitAttempted = true;
                e.preventDefault();
                e.stopPropagation();
                tokenWaitBusy = true;
                if (typeof showLoader === "function") showLoader();

                var forced = false;
                function finish() {
                    if (forced) return;
                    forced = true;
                    tokenWaitBusy = false;
                    try { form.requestSubmit(); } catch (err) { form.submit(); }
                }

                setTimeout(finish, TOKEN_WAIT_MS);
                fillPromise.then(finish);
                return;
            }

            if (typeof showLoader === "function") showLoader();
        }, true);
    }

    function handleLoginPage(app) {
        attachTokenFill(
            app,
            document.getElementById(LOGIN_FORM_ID),
            document.getElementById("fcmToken"),
            document.getElementById("devicePlatform")
        );
    }

    function handleRegisterPage(app) {
        attachTokenFill(
            app,
            document.getElementById(REGISTER_FORM_ID),
            document.getElementById("fcmToken"),
            document.getElementById("devicePlatform")
        );
    }

    function roleAppointmentsPath() {
        var role = (localStorage.getItem("role") || "").toLowerCase();
        if (role.indexOf("clinic") !== -1) return "/Clinic/DoctorAppointments";
        if (role.indexOf("doctor") !== -1) return "/Doctor/Appointments";
        if (role.indexOf("staff") !== -1) return "/Staff/Appointments";
        return "/Admin/Index";
    }

    // Service workers cannot read localStorage, so the role-based pages are
    // cached in Cache Storage for the notification click handler in
    // firebase-messaging-sw.js.
    function cacheNavPaths() {
        if (!("caches" in window) || !localStorage.getItem("role")) return;
        caches.open("ch-nav").then(function (cache) {
            cache.put(new Request("/__ch_nav_appointments__"), new Response(roleAppointmentsPath()));
            cache.put(new Request("/__ch_nav_notifications__"), new Response(notificationsPagePath()));
            cache.put(new Request("/__ch_nav_clinics__"), new Response(clinicsPath() || ""));
            cache.put(new Request("/__ch_nav_support__"), new Response(supportPath() || ""));
        }).catch(function () {});
    }

    function notificationsPagePath() {
        var role = (localStorage.getItem("role") || "").toLowerCase();
        if (role.indexOf("clinic") !== -1) return "/Clinic/Notifications";
        if (role.indexOf("doctor") !== -1) return "/Doctor/Notifications";
        if (role.indexOf("staff") !== -1) return "/Staff/Notifications";
        return "/Admin/Notifications";
    }

    // NotificationType enum values (docs/WEB_DASHBOARD_NOTIFICATIONS_README.md
    // + docs/NOTIFICATIONS_README.md): the list endpoint returns them as
    // numbers, push payloads carry the enum name. Map numbers → names so both
    // paths share one resolution logic.
    var TYPE_NAMES = {
        "0": "AppointmentReminder",
        "1": "NewMessage",
        "2": "PaymentConfirmation",
        "3": "AppointmentConfirmation",
        "4": "AppointmentCancellation",
        "5": "SystemAnnouncement",
        "6": "CancellationWindowClosed",
        "7": "SubscriptionExpiring",
        "8": "RefundProcessed",
        "9": "AdExpiring",
        "10": "AppointmentOutsideAvailability",
        "11": "AppointmentOutsideWorkingHours",
        "12": "NewBookingRequest",
        "13": "ClinicRegistered",
        "14": "ClinicApproved",
        "15": "ClinicRejected",
        "16": "SupportTicketUpdate",
        "17": "PaymentReceived",
        "18": "RevenueIncreased"
    };

    function typeName(value) {
        var name = String(value || "");
        return TYPE_NAMES[name] || name;
    }

    // Dashboard target groups — types 10-18 are dashboard-only; the rest are
    // shared with the mobile catalogue. The web dashboard has no
    // appointment-detail or chat pages, so each group lands on the role's hub
    // page (appointments / clinics / support / notifications).
    var APPOINTMENT_TYPES = ["AppointmentReminder", "PaymentConfirmation", "AppointmentConfirmation", "AppointmentCancellation", "CancellationWindowClosed", "RefundProcessed", "NewBookingRequest", "AppointmentOutsideAvailability", "AppointmentOutsideWorkingHours", "PaymentReceived", "RevenueIncreased"];
    var CLINIC_TYPES = ["ClinicRegistered", "ClinicApproved", "ClinicRejected"];
    var SUPPORT_TYPES = ["SupportTicketUpdate"];
    var NOTIFICATION_TYPES = ["NewMessage", "SystemAnnouncement", "SubscriptionExpiring", "AdExpiring"];

    function clinicsPath() {
        var role = (localStorage.getItem("role") || "").toLowerCase();
        if (role.indexOf("clinic") !== -1) return "/Clinic/Index";
        if (role.indexOf("super") !== -1) return "/Admin/Clinics";
        return null;
    }

    function supportPath() {
        var role = (localStorage.getItem("role") || "").toLowerCase();
        if (role.indexOf("clinic") !== -1) return "/Clinic/Support";
        if (role.indexOf("super") !== -1) return "/Admin/Support";
        return null;
    }

    // Mirrors firebase-messaging-sw.js: each NotificationType group lands on
    // the role-appropriate hub page; unknown types do nothing.
    function navigateByType(type) {
        var name = typeName(type);
        var target = null;
        if (APPOINTMENT_TYPES.indexOf(name) !== -1) {
            target = roleAppointmentsPath();
        } else if (CLINIC_TYPES.indexOf(name) !== -1) {
            target = clinicsPath();
        } else if (SUPPORT_TYPES.indexOf(name) !== -1) {
            target = supportPath();
        } else if (NOTIFICATION_TYPES.indexOf(name) !== -1) {
            target = notificationsPagePath();
        }
        if (target && window.location.pathname !== target) {
            window.location.href = target;
        }
    }

    function handleForeground(app) {
        var messaging = getMessaging(app);
        registerServiceWorker();

        onMessage(messaging, function (payload) {
            var title = payload.notification && payload.notification.title ? payload.notification.title : "Doctory";
            var body = (payload.notification && payload.notification.body) || (payload.data && payload.data.body) || "";
            var type = (payload.data && payload.data.type) || "";

            // Foreground pushes do not reach the service worker — show the
            // notification natively so the push content is actually visible.
            if (("Notification" in window) && Notification.permission === "granted") {
                new Notification(title, { body: body, icon: "/notification_logo.png", badge: "/notification_logo.png" });
            }
            navigateByType(type);
            refreshBell();
        });

        attachBellEvents();
        refreshBell();
        setInterval(refreshBell, 60000);
        cacheNavPaths();

        // Registers the web token with the backend through the MVC server —
        // same-origin, authenticated by the HttpOnly session cookie, so it
        // works on every page load (no localStorage token required, no CORS).
        function registerTokenOnBackend(token) {
            if (!token) return;

            fetch("/Account/RegisterFcmToken", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ fcmToken: token, devicePlatform: 0 })
            })
            .catch(function () {});
        }

        // Token rotation: browsers rotate FCM tokens over time. Refresh the
        // cached web token in the background and register it on the backend
        // NOW (README §3.6, docs/FCM_TOKEN_ENDPOINT_FRONTEND.md).
        function syncCachedToken() {
            if (!("Notification" in window) || Notification.permission !== "granted") return;
            registerServiceWorker()
                .then(function () { return getToken(messaging, { vapidKey: VAPID_PUBLIC_KEY }); })
                .then(function (token) {
                    if (!token) return;
                    try { localStorage.setItem(TOKEN_CACHE_KEY, token); } catch (e) {}
                    registerTokenOnBackend(token);
                })
                .catch(function () {});
        }

        // Re-register the last known token on every dashboard load so the
        // backend keeps pushing even after refreshes and TempData is gone.
        var cachedToken = "";
        try { cachedToken = localStorage.getItem(TOKEN_CACHE_KEY) || ""; } catch (e) {}
        if (cachedToken && ("Notification" in window) && Notification.permission === "granted") {
            registerServiceWorker().then(function () { registerTokenOnBackend(cachedToken); });
        }

        if (("Notification" in window) && Notification.permission === "granted") {
            syncCachedToken();
        }

        // Visible banner prompting the user to enable browser notifications —
        // a silent auto-prompt is easy to miss (Edge shows a quiet bell icon in
        // the address bar instead). The button click is a strong user gesture,
        // so the permission prompt reliably appears; the fetched token is then
        // cached and registered with the backend immediately via the
        // POST /api/v1/auth/fcm-token endpoint.
        var ENABLE_TIMEOUT_MS = 8000;

        function closeBanner() {
            var b = document.getElementById("chNotifBanner");
            if (b) b.remove();
        }

        function instructionsHtml() {
            return '<div class="ch-notif-banner-text"><i class="bi bi-bell-slash"></i> الإشعارات معطّلة من المتصفح. فعّلها يدوياً: اضغط على أيقونة الجرس أو القفل بجانب شريط العنوان ← «الإشعارات» ← «السماح»، ثم عُد إلى الصفحة.</div>' +
                '<button type="button" class="ch-notif-banner-btn" id="chNotifRetryBtn">أعد التحقق</button>' +
                '<button type="button" class="ch-notif-banner-close" id="chNotifCloseBtn" aria-label="إغلاق" title="إغلاق">&times;</button>';
        }

        // Requests the permission once. On grant: produce the token and
        // register it on the backend immediately. If the browser suppresses
        // the prompt (Edge quiet mode resolves "default", or the promise never
        // settles), the banner switches to manual instructions so the user
        // knows the prompt was not shown.
        function requestPermissionAndEnable() {
            var req = Notification.requestPermission();
            if (!req || typeof req.then !== "function") return;
            var settled = false;

            var timeout = setTimeout(function () {
                if (settled || Notification.permission === "granted") return;
                var banner = document.getElementById("chNotifBanner");
                if (!banner) return;
                banner.innerHTML = instructionsHtml();
                document.getElementById("chNotifRetryBtn").addEventListener("click", recheckPermission);
                document.getElementById("chNotifCloseBtn").addEventListener("click", closeBanner);
            }, ENABLE_TIMEOUT_MS);

            req.then(function (p) {
                settled = true;
                clearTimeout(timeout);
                if (p === "granted") {
                    closeBanner();
                    syncCachedToken();
                } else if (p === "default") {
                    // The prompt was suppressed or ignored (quiet mode) —
                    // guide the user to the address-bar bell/lock icon.
                    var banner = document.getElementById("chNotifBanner");
                    if (!banner) return;
                    banner.innerHTML = instructionsHtml();
                    document.getElementById("chNotifRetryBtn").addEventListener("click", recheckPermission);
                    document.getElementById("chNotifCloseBtn").addEventListener("click", closeBanner);
                }
            });
        }

        function showEnableBanner() {
            if (!("Notification" in window) || Notification.permission === "granted") return;
            var main = document.querySelector(".content-body") || document.querySelector("main") || document.body;
            if (!main || document.getElementById("chNotifBanner")) return;

            var banner = document.createElement("div");
            banner.id = "chNotifBanner";
            banner.className = "ch-notif-banner";

            if (Notification.permission === "default") {
                banner.innerHTML =
                    '<div class="ch-notif-banner-text"><i class="bi bi-bell"></i> فعّل إشعارات المتصفح ليصلك تنبيه المواعيد والحجوزات</div>' +
                    '<button type="button" class="ch-notif-banner-btn" id="chNotifEnableBtn">تفعيل الإشعارات</button>' +
                    '<button type="button" class="ch-notif-banner-close" id="chNotifCloseBtn" aria-label="إغلاق" title="إغلاق">&times;</button>';

                main.insertBefore(banner, main.firstChild);

                document.getElementById("chNotifEnableBtn").addEventListener("click", requestPermissionAndEnable);
            } else {
                // Permission is "denied": the browser will NEVER show the prompt
                // again, so the only fix is the user enabling notifications from
                // the site settings. Show instructions + a re-check button that
                // re-reads the permission when the user comes back.
                banner.innerHTML = instructionsHtml();
                main.insertBefore(banner, main.firstChild);

                document.getElementById("chNotifRetryBtn").addEventListener("click", recheckPermission);
            }

            document.getElementById("chNotifCloseBtn").addEventListener("click", closeBanner);
        }

        // Re-checks the permission after the user returns from the browser
        // site settings (or clicks "أعد التحقق"): if granted now, produce the
        // token and register it on the backend immediately. Silent when still
        // not granted — no console noise on every focus event.
        function recheckPermission() {
            var banner = document.getElementById("chNotifBanner");
            if (!banner) return;
            if (!("Notification" in window) || Notification.permission !== "granted") return;
            closeBanner();
            syncCachedToken();
        }

        // When the user switches to the browser settings and comes back, the
        // page regains focus/visibility — re-check then instead of making them
        // reload manually.
        document.addEventListener("visibilitychange", function () {
            if (document.visibilityState === "visible") recheckPermission();
        });
        window.addEventListener("focus", recheckPermission);

        // First user interaction on the dashboard is a strong gesture: request
        // the permission automatically (same pattern as the login page) so
        // push activates without the user hunting for the banner. Clicks on
        // the banner are skipped — the banner button is its own single request
        // path, avoiding two simultaneous prompts for one click.
        document.addEventListener("pointerdown", function (e) {
            if (!("Notification" in window) || Notification.permission !== "default") return;
            if (e.target && e.target.closest && e.target.closest("#chNotifBanner")) return;
            requestPermissionAndEnable();
        }, { once: true, capture: true });

        showEnableBanner();
    }

    // The bell markup is server-rendered in every dashboard layout (badge
    // count from ViewBag.UnreadNotificationsCount). This only wires up the
    // toggle/dropdown behaviour and the AJAX list refresh.
    function attachBellEvents() {
        var bell = document.getElementById("chBell");
        if (!bell) return;

        var btn = bell.querySelector(".ch-bell-btn");
        if (!btn) return;

        btn.addEventListener("click", function (e) {
            e.stopPropagation();
            var dd = document.getElementById("chBellDropdown");
            dd.classList.toggle("open");
            if (dd.classList.contains("open")) loadBellList();
        });

        document.addEventListener("click", function (e) {
            if (!e.target.closest("#chBell")) {
                var dd = document.getElementById("chBellDropdown");
                if (dd) dd.classList.remove("open");
            }
        });
    }

    function bellItemHtml(item) {
        var title = item.titleAr || item.TitleAr || item.title || item.Title || "";
        var body = item.bodyAr || item.BodyAr || item.body || item.Body || item.message || item.Message || "";
        var date = item.createdAt || item.createdDate || item.CreatedAt || item.date || item.Date || "";
        if (date) {
            var d = new Date(date);
            if (!isNaN(d.getTime())) date = d.toLocaleString("ar", { day: "numeric", month: "short", hour: "2-digit", minute: "2-digit" });
        }
        var timeHtml = date ? '<div class="ch-bell-item-time">' + date + '</div>' : "";
        return '<div class="ch-bell-item">' +
            '<div class="ch-bell-item-title">' + title + '</div>' +
            (body ? '<div class="ch-bell-item-body">' + body + '</div>' : "") +
            timeHtml +
            '</div>';
    }

    function renderBellList(items) {
        var list = document.getElementById("chBellList");
        if (!list) return;
        if (!items || !items.length) {
            list.innerHTML = '<div class="ch-bell-empty">لا توجد إشعارات</div>';
            return;
        }
        list.innerHTML = items.slice(0, 10).map(bellItemHtml).join("");
    }

    function loadBellList() {
        fetchJson(notificationsUrl(1, 10)).then(function (data) {
            var items = data && data.items ? data.items : (Array.isArray(data) ? data : []);
            renderBellList(items);
            // The list endpoint marks returned items as read server-side —
            // refresh the badge AFTER the fetch (README §1.2).
            refreshBell();
        }).catch(function () {});
    }

    function refreshBell() {
        fetchJson(notificationsCountUrl()).then(function (data) {
            var count = (data && (typeof data.count === "number" ? data.count : data.unreadCount)) || 0;
            var badge = document.getElementById("chBellBadge");
            if (!badge) return;
            if (count > 0) {
                badge.textContent = count > 99 ? "99+" : count;
                badge.hidden = false;
            } else {
                badge.hidden = true;
            }
        }).catch(function () {});
    }

    function init() {
        if (typeof firebase === "undefined" || typeof firebase.messaging !== "function") {
            return;
        }
        var app = firebase.initializeApp(FIREBASE_CONFIG);
        if (isLoginPage()) {
            handleLoginPage(app);
        } else if (document.getElementById(REGISTER_FORM_ID)) {
            handleRegisterPage(app);
        } else {
            handleForeground(app);
        }
    }

    function loadSdk(callback) {
        // Load compat SDK scripts sequentially: firebase-messaging-compat
        // depends on firebase-app-compat having executed first. Loading them
        // in parallel (async=true) races — messaging-compat can run before
        // app-compat and crash with "Cannot read properties of undefined
        // (reading 'INTERNAL')", leaving firebase.messaging undefined.
        var scripts = [
            "https://www.gstatic.com/firebasejs/12.17.1/firebase-app-compat.js",
            "https://www.gstatic.com/firebasejs/12.17.1/firebase-messaging-compat.js"
        ];
        var i = 0;

        function loadNext() {
            if (i >= scripts.length) {
                if (typeof firebase !== "undefined") callback();
                return;
            }
            var s = document.createElement("script");
            s.src = scripts[i];
            s.async = false;
            s.onload = function () {
                i += 1;
                loadNext();
            };
            s.onerror = function () {
                i += 1;
                loadNext();
            };
            document.head.appendChild(s);
        }

        loadNext();
    }

    loadSdk(init);
})();
