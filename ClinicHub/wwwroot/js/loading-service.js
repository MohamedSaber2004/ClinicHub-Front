(function () {
    'use strict';

    var activeRequests = 0;
    var overlayEl = null;

    function getOverlay() {
        if (!overlayEl) {
            overlayEl = document.getElementById('globalLoadingOverlay');
        }
        return overlayEl;
    }

    function showLoader() {
        var el = getOverlay();
        if (el) {
            el.classList.add('loading-active');
            el.style.display = 'flex';
        }
    }

    function hideLoader() {
        var el = getOverlay();
        if (el) {
            el.classList.remove('loading-active');
            el.style.display = '';
        }
    }

    window.loadingService = {
        show: function () {
            activeRequests++;
            showLoader();
        },
        hide: function () {
            activeRequests = Math.max(0, activeRequests - 1);
            if (activeRequests === 0) {
                hideLoader();
            }
        },
        getActiveCount: function () {
            return activeRequests;
        }
    };

    var origFetch = window.fetch;
    window.fetch = function (input, init) {
        activeRequests++;
        showLoader();

        return origFetch.call(window, input, init).then(function (response) {
            activeRequests--;
            if (activeRequests <= 0) {
                activeRequests = 0;
                hideLoader();
            }
            return response;
        }).catch(function (err) {
            activeRequests--;
            if (activeRequests <= 0) {
                activeRequests = 0;
                hideLoader();
            }
            throw err;
        });
    };

    $(document).ajaxStart(function () {
        activeRequests++;
        showLoader();
    });

    $(document).ajaxStop(function () {
        activeRequests--;
        if (activeRequests <= 0) {
            activeRequests = 0;
            hideLoader();
        }
    });

    $(document).on('submit', 'form:not([data-ajax="true"])', function () {
        showLoader();
    });

    $(document).on('click', 'a:not([href^="#"]):not([href^="javascript"]):not([target="_blank"])', function (e) {
        if (e.ctrlKey || e.metaKey || e.shiftKey) return;
        var href = this.getAttribute('href');
        if (!href || href === '#' || href.startsWith('javascript:')) return;

        e.preventDefault();
        showLoader();
        void document.body.offsetHeight;
        window.location.href = href;
    });

    window.addEventListener('beforeunload', function () {
        showLoader();
    });
})();
