(function () {
    var cfg = window.ClinicHubConfig || {};
    var VAPID_PUBLIC_KEY = cfg.vapidKey || "";
    var FIREBASE_CONFIG = cfg.firebaseConfig || {};

    var SW_PATH = "/firebase-messaging-sw.js";
    var LOGIN_FORM_ID = "loginForm";

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

    if (document.getElementById("notificationsCenter")) {
        initNotificationsPage();
        return;
    }

    if (!FIREBASE_CONFIG.apiKey || !VAPID_PUBLIC_KEY) {
        console.warn("[FCM] disabled - missing keys in settings:", { hasConfig: !!FIREBASE_CONFIG.apiKey, hasVapid: !!VAPID_PUBLIC_KEY });
        return;
    }

    function apiBaseUrl() {
        return cfg.apiBaseUrl || "";
    }

    function notificationsUrl(page, pageSize) {
        return apiBaseUrl() + "/api/v1/notifications/pagginated?pageNumber=" + (page || 1) + "&pageSize=" + (pageSize || 10);
    }

    function notificationsCountUrl() {
        return apiBaseUrl() + "/api/v1/notifications/count";
    }

    function authHeaders() {
        var token = localStorage.getItem("accessToken");
        return token ? { "Authorization": "Bearer " + token } : {};
    }

    function fetchJson(url) {
        return fetch(url, { headers: authHeaders() })
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
        if (!("serviceWorker" in navigator)) return Promise.resolve();
        return navigator.serviceWorker.register(SW_PATH)
            .then(function () {
                return Promise.race([
                    navigator.serviceWorker.ready,
                    new Promise(function (resolve) { setTimeout(resolve, 2000); })
                ]);
            })
            .catch(function () {
                console.warn("[FCM] service worker registration failed");
            });
    }

    function handleLoginPage(app) {
        var form = document.getElementById(LOGIN_FORM_ID);
        if (!form) return;

        var fcmInput = document.getElementById("fcmToken");
        var platformInput = document.getElementById("devicePlatform");
        var fillPromise = null;
        var tokenWaitBusy = false;
        var waitAttempted = false;

        function acquireToken() {
            var messaging;
            try {
                if (typeof firebase === "undefined" || !firebase.messaging) return Promise.resolve(null);
                messaging = getMessaging(app);
            } catch (err) {
                console.warn("[FCM] messaging init failed:", err);
                return Promise.resolve(null);
            }
            if (!("Notification" in window) || Notification.permission !== "granted") return Promise.resolve(null);
            return registerServiceWorker()
                .then(function () { return getToken(messaging, { vapidKey: VAPID_PUBLIC_KEY }); })
                .then(function (token) {
                    if (!token) return null;
                    if (fcmInput) fcmInput.value = token;
                    if (platformInput) platformInput.value = "0";
                    console.log("[FCM] token set on login");
                    return token;
                })
                .catch(function (err) {
                    console.warn("[FCM] getToken failed:", err && err.message ? err.message : err);
                    return null;
                });
        }

        function startFill() {
            if (!fillPromise) fillPromise = acquireToken();
            return fillPromise;
        }

        // First interaction is a user gesture: ask permission and start the
        // token fetch in the background, before the user finishes typing.
        document.addEventListener("pointerdown", function () {
            if (!("Notification" in window) || !("serviceWorker" in navigator)) return;
            if (Notification.permission === "default") {
                var req = Notification.requestPermission();
                if (req && typeof req.then === "function") req.then(function (p) { if (p === "granted") startFill(); });
            } else if (Notification.permission === "granted") {
                startFill();
            }
        }, { once: true, capture: true });

        // The form is data-ajax: error-service.js submits it via fetch. We only
        // intercept briefly when a token is genuinely imminent (permission already
        // granted and the token fetch is in flight). We never wait for the
        // permission prompt — the login goes through immediately.
        document.addEventListener("submit", function (e) {
            if (e.target !== form) return;

            if (tokenWaitBusy) {
                e.preventDefault();
                e.stopPropagation();
                return;
            }

            var tokenReady = !!fcmInput && !!fcmInput.value;
            var canWait = ("Notification" in window) && ("serviceWorker" in navigator)
                && Notification.permission === "granted"
                && !!fillPromise
                && !tokenReady
                && !waitAttempted;

            if (canWait) {
                waitAttempted = true;
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

                setTimeout(finish, 1500);
                fillPromise.then(finish);
                return;
            }

            if (typeof showLoader === "function") showLoader();
        }, true);
    }

    function roleAppointmentsPath() {
        var role = (localStorage.getItem("role") || "").toLowerCase();
        if (role.indexOf("clinic") !== -1) return "/Clinic/DoctorAppointments";
        if (role.indexOf("doctor") !== -1) return "/Doctor/Appointments";
        if (role.indexOf("staff") !== -1) return "/Staff/Appointments";
        return "/Admin/Index";
    }

    function notificationsPagePath() {
        var role = (localStorage.getItem("role") || "").toLowerCase();
        if (role.indexOf("clinic") !== -1) return "/Clinic/Notifications";
        if (role.indexOf("doctor") !== -1) return "/Doctor/Notifications";
        if (role.indexOf("staff") !== -1) return "/Staff/Notifications";
        return "/Admin/Notifications";
    }

    function navigateByType(type) {
        var appointmentTypes = ["AppointmentReminder", "AppointmentConfirmation", "AppointmentCancellation", "PaymentConfirmation", "CancellationWindowClosed", "RefundProcessed"];
        if (appointmentTypes.indexOf(type) !== -1) {
            window.location.href = roleAppointmentsPath();
        }
    }

    function handleForeground(app) {
        var messaging = getMessaging(app);
        registerServiceWorker();

        onMessage(messaging, function (payload) {
            var title = payload.notification && payload.notification.title ? payload.notification.title : "Doctory";
            var body = (payload.notification && payload.notification.body) || (payload.data && payload.data.body) || "";
            var type = (payload.data && payload.data.type) || "";

            if (typeof showSuccessModal === "function") {
                showSuccessModal(title + (body ? "\n" + body : ""));
            }
            navigateByType(type);
        });

        injectBell();
        refreshBell();
        setInterval(refreshBell, 60000);
    }

    function injectBell() {
        var header = document.querySelector(".top-header");
        var user = document.querySelector(".header-user");
        if (!header) return;
        if (document.getElementById("chBell")) return;

        var bell = document.createElement("div");
        bell.className = "ch-bell";
        bell.id = "chBell";
        bell.innerHTML =
            '<button class="ch-bell-btn" type="button" title="الإشعارات" aria-label="الإشعارات">' +
            '<i class="bi bi-bell"></i>' +
            '<span class="ch-bell-badge" id="chBellBadge" style="display:none;">0</span>' +
            '</button>' +
            '<div class="ch-bell-dropdown" id="chBellDropdown">' +
            '<div class="ch-bell-header">الإشعارات</div>' +
            '<div class="ch-bell-list" id="chBellList"><div class="ch-bell-empty">لا توجد إشعارات</div></div>' +
            '<a class="ch-bell-footer" href="' + notificationsPagePath() + '">عرض كل الإشعارات</a>' +
            '</div>';

        if (user) header.insertBefore(bell, user);
        else header.appendChild(bell);

        var btn = bell.querySelector(".ch-bell-btn");
        btn.addEventListener("click", function (e) {
            e.stopPropagation();
            var dd = document.getElementById("chBellDropdown");
            dd.classList.toggle("open");
            if (dd.classList.contains("open")) loadBellList();
        });

        document.addEventListener("click", function (e) {
            if (!e.target.closest("#chBell")) {
                document.getElementById("chBellDropdown").classList.remove("open");
            }
        });
    }

    function bellItemHtml(item) {
        var title = item.title || item.Title || "";
        var body = item.body || item.Body || item.message || item.Message || "";
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
        }).catch(function () {});
    }

    function refreshBell() {
        fetchJson(notificationsCountUrl()).then(function (data) {
            var count = typeof data === "number" ? data : (data && (data.count !== undefined ? data.count : data.unreadCount)) || 0;
            var badge = document.getElementById("chBellBadge");
            if (!badge) return;
            if (count > 0) {
                badge.textContent = count > 99 ? "99+" : count;
                badge.style.display = "flex";
            } else {
                badge.style.display = "none";
            }
        }).catch(function () {});
    }

    function typeIcon(type) {
        var map = {
            AppointmentReminder: "bi-bell",
            NewMessage: "bi-chat-dots",
            PaymentConfirmation: "bi-credit-card",
            AppointmentConfirmation: "bi-calendar-check",
            AppointmentCancellation: "bi-calendar-x",
            SystemAnnouncement: "bi-megaphone",
            CancellationWindowClosed: "bi-hourglass-split",
            SubscriptionExpiring: "bi-hourglass-split",
            RefundProcessed: "bi-cash-stack",
            AdExpiring: "bi-megaphone"
        };
        return map[type] || "bi-bell";
    }

    function formatDate(value) {
        if (!value) return "";
        var d = new Date(value);
        if (isNaN(d.getTime())) return "";
        return d.toLocaleString("ar", { day: "numeric", month: "short", hour: "2-digit", minute: "2-digit" });
    }

    function notifItemHtml(item) {
        var title = item.title || item.Title || "";
        var body = item.body || item.Body || item.message || item.Message || "";
        var date = formatDate(item.createdAt || item.createdDate || item.CreatedAt || item.date || item.Date);
        var type = item.type || item.Type || "";
        var unread = item.isRead === false || item.isUnread === true;
        return '<div class="notif-item' + (unread ? " unread" : "") + '" data-type="' + type + '">' +
            '<div class="notif-item-icon"><i class="bi ' + typeIcon(type) + '"></i></div>' +
            '<div class="notif-item-content">' +
            '<div class="notif-item-title">' + title + (unread ? '<span class="notif-item-dot"></span>' : "") + '</div>' +
            (body ? '<div class="notif-item-body">' + body + '</div>' : "") +
            (date ? '<div class="notif-item-time">' + date + '</div>' : "") +
            '</div></div>';
    }

    function initNotificationsPage() {
        var page = parseInt((new URLSearchParams(window.location.search)).get("page") || "1", 10) || 1;
        var list = document.getElementById("notifList");
        var empty = document.getElementById("notifEmpty");
        var loading = document.getElementById("notifLoading");
        var pagination = document.getElementById("notifPagination");
        var pageInfo = document.getElementById("notifPageInfo");
        var prevBtn = document.getElementById("notifPrev");
        var nextBtn = document.getElementById("notifNext");
        if (!list) return;

        fetchJson(notificationsUrl(page, 20)).then(function (data) {
            var items = data && data.items ? data.items : (Array.isArray(data) ? data : []);
            var totalPages = (data && data.totalPages) || 1;
            var hasPrev = !!(data && data.hasPreviousPage) || page > 1;
            var hasNext = !!(data && data.hasNextPage) || page < totalPages;

            loading.style.display = "none";
            if (!items.length) {
                empty.hidden = false;
                return;
            }
            list.innerHTML = items.map(notifItemHtml).join("");
            list.querySelectorAll(".notif-item").forEach(function (el) {
                el.addEventListener("click", function () {
                    navigateByType(el.getAttribute("data-type"));
                });
            });

            pagination.hidden = false;
            pageInfo.textContent = "صفحة " + page + " من " + totalPages;
            prevBtn.disabled = !hasPrev;
            nextBtn.disabled = !hasNext;
            prevBtn.onclick = function () {
                if (hasPrev) window.location.href = window.location.pathname + "?page=" + (page - 1);
            };
            nextBtn.onclick = function () {
                if (hasNext) window.location.href = window.location.pathname + "?page=" + (page + 1);
            };
        }).catch(function () {
            loading.style.display = "none";
            empty.hidden = false;
            empty.textContent = "تعذر تحميل الإشعارات";
        });
    }

    function init() {
        if (typeof firebase === "undefined" || typeof firebase.messaging !== "function") {
            console.warn("[FCM] disabled - Firebase messaging compat SDK unavailable");
            return;
        }
        var app = firebase.initializeApp(FIREBASE_CONFIG);
        if (isLoginPage()) handleLoginPage(app);
        else handleForeground(app);
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
                console.warn("[FCM] SDK script failed to load:", s.src);
                i += 1;
                loadNext();
            };
            document.head.appendChild(s);
        }

        loadNext();
    }

    loadSdk(init);
})();
