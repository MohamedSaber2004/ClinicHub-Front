// Swaps a photo that fails to load (missing attachment, 404, bad URL) for a
// neutral placeholder instead of the browser's broken-image glyph.
// Usage: <img src="…" data-fallback="avatar|clinic" />
(function () {
    var placeholders = {
        avatar: '/img/avatar-placeholder.svg',
        clinic: '/img/clinic-placeholder.svg'
    };

    function apply(img) {
        if (!img || img.tagName !== 'IMG' || img.dataset.fallbackApplied === '1') return;
        var placeholder = placeholders[img.getAttribute('data-fallback')];
        if (!placeholder) return;
        img.dataset.fallbackApplied = '1';
        img.classList.add('is-img-placeholder');
        img.src = placeholder;
    }

    // Images above this script can already be broken before it is parsed.
    function sweep() {
        var images = document.querySelectorAll('img[data-fallback]:not([data-fallback-applied])');
        for (var i = 0; i < images.length; i++) {
            if (images[i].complete && images[i].naturalWidth === 0) apply(images[i]);
        }
    }

    document.addEventListener('error', function (event) { apply(event.target); }, true);
    sweep();
    document.addEventListener('DOMContentLoaded', sweep);
})();
