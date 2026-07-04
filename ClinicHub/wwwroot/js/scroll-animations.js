(function() {
    var style = document.createElement('style');
    style.textContent =
        '[data-scroll]{' +
            'opacity:0;' +
            'transform:translateY(30px);' +
            'transition:opacity 0.5s ease-out,transform 0.5s ease-out' +
        '}' +
        '[data-scroll].visible{' +
            'opacity:1;' +
            'transform:translateY(0)' +
        '}';
    document.head.appendChild(style);

    var observer = new IntersectionObserver(function(entries) {
        entries.forEach(function(entry) {
            if (entry.isIntersecting) {
                entry.target.classList.add('visible');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.08, rootMargin: '0px 0px -40px 0px' });

    function isInViewport(el) {
        var rect = el.getBoundingClientRect();
        return rect.top < window.innerHeight && rect.bottom > 0;
    }

    function init() {
        var selectors = 'section, .table-card, .stat-card, .plan-card, .benefit-card, .benefits-grid, .subscription-cta-section, .register-form-card, .submitted-card, .submitted-step, .register-form-section';
        document.querySelectorAll(selectors).forEach(function(el) {
            if (!el.hasAttribute('data-scroll')) {
                el.setAttribute('data-scroll', '');
                if (isInViewport(el)) {
                    el.classList.add('visible');
                } else {
                    observer.observe(el);
                }
            }
        });
    }

    document.addEventListener('DOMContentLoaded', init);
    new MutationObserver(init).observe(document.body, { childList: true, subtree: true });
})();
