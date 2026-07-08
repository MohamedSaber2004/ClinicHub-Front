(function () {
    'use strict';

    function extractErrors(body) {
        var errors = [];

        if (!body) return ['حدث خطأ غير متوقع'];

        try {
            var resp = JSON.parse(body);

            var message = resp.message || resp.Message || resp.title || resp.Title || '';

            if (resp.errors && typeof resp.errors === 'object') {
                if (Array.isArray(resp.errors)) {
                    resp.errors.forEach(function (e) { if (e) errors.push(e); });
                } else {
                    for (var key in resp.errors) {
                        if (resp.errors.hasOwnProperty(key)) {
                            var val = resp.errors[key];
                            if (Array.isArray(val)) {
                                val.forEach(function (msg) { if (msg) errors.push(msg); });
                            } else if (typeof val === 'string' && val) {
                                errors.push(val);
                            }
                        }
                    }
                }
            }

            if (resp.error && typeof resp.error === 'object') {
                if (Array.isArray(resp.error)) {
                    resp.error.forEach(function (e) { if (e) errors.push(e); });
                } else if (typeof resp.error === 'string') {
                    errors.push(resp.error);
                }
            } else if (resp.error && typeof resp.error === 'string') {
                errors.push(resp.error);
            }

            if (message) {
                if (errors.length === 0) {
                    errors.push(message);
                } else {
                    errors.unshift(message);
                }
            }
        } catch (e) {
            if (typeof body === 'string' && body.length > 0 && body !== '{}') {
                errors.push(body.length > 200 ? body.substring(0, 200) + '...' : body);
            }
        }

        return errors.length > 0 ? errors : ['حدث خطأ غير متوقع'];
    }

    window.showBackendErrors = function (body) {
        var errors = extractErrors(body);
        var modal = $('#globalErrorModal');
        var msgEl = $('#globalErrorMsg');

        if (errors.length === 1) {
            msgEl.html(errors[0]);
        } else {
            var list = '<ul style="text-align: right; padding-right: 20px; margin: 0;">';
            errors.forEach(function (e) { list += '<li>' + e + '</li>'; });
            list += '</ul>';
            msgEl.html(list);
        }

        modal.modal('show');
    };

    $.ajaxSetup({
        headers: { 'Accept-Language': 'ar' }
    });

    var origFetch = window.fetch;
    var isRefreshing = false;
    var failedQueue = [];

    function getUrl(input) {
        return typeof input === 'string' ? input : (input && input.url ? input.url : '');
    }

    window.fetch = function (input, init) {
        init = init || {};
        init.headers = init.headers || {};
        init.headers['Accept-Language'] = 'ar';

        var accessToken = localStorage.getItem('accessToken');
        if (accessToken) {
            init.headers['Authorization'] = 'Bearer ' + accessToken;
        }

        var requestUrl = getUrl(input);

        return origFetch.call(window, input, init).then(function (response) {
            if (response.status === 401
                && requestUrl.indexOf('/Account/RefreshToken') === -1
                && requestUrl.indexOf('/Account/Login') === -1)
            {
                if (isRefreshing) {
                    return new Promise(function (resolve) {
                        failedQueue.push(function (newToken) {
                            init.headers['Authorization'] = 'Bearer ' + newToken;
                            resolve(origFetch.call(window, input, init));
                        });
                    });
                }

                isRefreshing = true;
                var refreshToken = localStorage.getItem('refreshToken');

                return origFetch('/Account/RefreshToken', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ refreshToken: refreshToken })
                }).then(function (refreshRes) {
                    return refreshRes.json().then(function (data) {
                        if (refreshRes.ok && data.accessToken) {
                            localStorage.setItem('accessToken', data.accessToken);
                            if (data.refreshToken) localStorage.setItem('refreshToken', data.refreshToken);

                            isRefreshing = false;
                            failedQueue.forEach(function (cb) { cb(data.accessToken); });
                            failedQueue = [];

                            init.headers['Authorization'] = 'Bearer ' + data.accessToken;
                            return origFetch.call(window, input, init);
                        }

                        isRefreshing = false;
                        failedQueue = [];
                        localStorage.removeItem('accessToken');
                        localStorage.removeItem('refreshToken');
                        localStorage.removeItem('userId');
                        localStorage.removeItem('role');
                        localStorage.removeItem('clinicId');
                        window.location.href = '/Account/Login';
                        throw new Error('Session expired');
                    });
                }).catch(function () {
                    isRefreshing = false;
                    failedQueue = [];
                    localStorage.removeItem('accessToken');
                    localStorage.removeItem('refreshToken');
                    localStorage.removeItem('userId');
                    localStorage.removeItem('role');
                    localStorage.removeItem('clinicId');
                    window.location.href = '/Account/Login';
                });
            }

            if (response.status >= 400 && !isRefreshing) {
                response.clone().text().then(function (text) {
                    setTimeout(function () { window.showBackendErrors(text); }, 100);
                });
            }
            return response;
        });
    };

    $(document).ajaxError(function (event, jqxhr) {
        window.showBackendErrors(jqxhr.responseText);
    });

    $(document).on('submit', 'form[data-ajax="true"]', function (e) {
        e.preventDefault();
        var $form = $(this);
        var formData = new FormData($form[0]);

        fetch($form.attr('action'), {
            method: $form.attr('method') || 'POST',
            body: new URLSearchParams(formData),
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'Content-Type': 'application/x-www-form-urlencoded',
                'Accept-Language': 'ar'
            }
        }).then(function (response) {
            var contentType = response.headers.get('Content-Type') || '';
            if (contentType.indexOf('application/json') >= 0) {
                return response.json().then(function (data) {
                    if (response.ok && data.redirectUrl) {
                        window.location.href = data.redirectUrl;
                    } else if (!response.ok && data.errors) {
                        window.showBackendErrors(JSON.stringify(data));
                    } else if (!response.ok) {
                        window.showBackendErrors(response.statusText);
                    }
                });
            } else {
                if (response.redirected) {
                    window.location.href = response.url;
                } else if (!response.ok) {
                    response.text().then(function (text) { window.showBackendErrors(text); });
                }
            }
        }).catch(function () {
            window.showBackendErrors(null);
        });
    });

})();
