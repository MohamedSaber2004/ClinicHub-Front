function showSuccessModal(message) {
    var msgEl = document.getElementById('globalSuccessMsg');
    if (!msgEl) return;
    msgEl.innerHTML = message;
    var modalEl = document.getElementById('globalSuccessModal');
    if (!modalEl) return;
    var modal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
    modal.show();
}

function showErrorModal(message) {
    var msgEl = document.getElementById('globalErrorMsg');
    if (!msgEl) return;
    msgEl.innerHTML = message;
    var modalEl = document.getElementById('globalErrorModal');
    if (!modalEl) return;
    var modal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
    modal.show();
}

(function () {
    var origFetch = window.fetch;

    function getMsg(text, status) {
        var msg = '';
        if (status >= 500) {
            msg = 'الخدمة غير متاحة الآن. يرجى المحاولة لاحقاً أو الاتصال بفريق الدعم الفني.';
        }
        try {
            var resp = JSON.parse(text);
            if (resp.message) msg = resp.message;
            else if (resp.title) msg = resp.title;
        } catch (e) {}
        return msg || 'عذراً، حدث خطأ غير متوقع. يرجى المحاولة لاحقاً.';
    }

    window.fetch = function () {
        var args = arguments;
        var method = 'GET';
        if (args[1] && args[1].method) {
            method = args[1].method.toUpperCase();
        }

        return origFetch.apply(this, args).then(function (response) {
            if (response.status >= 400) {
                response.clone().text().then(function (text) {
                    setTimeout(function () { showErrorModal(getMsg(text, response.status)); }, 100);
                });
            } else if (method !== 'GET' && method !== 'HEAD') {
                response.clone().text().then(function (text) {
                    var msg = '';
                    try {
                        var resp = JSON.parse(text);
                        if (resp.message) msg = resp.message;
                    } catch (e) {}
                    if (msg) {
                        setTimeout(function () { showSuccessModal(msg); }, 100);
                    }
                });
            }
            return response;
        });
    };

    $(document).ajaxError(function (event, jqxhr) {
        var msg = 'عذراً، حدث خطأ غير متوقع. يرجى المحاولة لاحقاً.';
        if (jqxhr.status >= 500) {
            msg = 'الخدمة غير متاحة الآن. يرجى المحاولة لاحقاً أو الاتصال بفريق الدعم الفني.';
        }
        try {
            var resp = JSON.parse(jqxhr.responseText);
            if (resp.message) msg = resp.message;
            else if (resp.title) msg = resp.title;
        } catch (e) {}
        showErrorModal(msg);
    });
})();
