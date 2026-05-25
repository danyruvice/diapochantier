/* Diaspo-Chantier — site.js */
(function () {
    'use strict';

    /* ── Carousel ─────────────────────────────────────────── */
    function initCarousel() {
        var slides = document.querySelectorAll('.carousel-slide');
        var dots = document.querySelectorAll('.carousel-dot');
        var btns = document.querySelectorAll('.carousel-btn');
        if (!slides.length) return;
        var current = 0;
        var timer;

        function goTo(index) {
            slides[current].classList.remove('is-active');
            if (dots[current]) dots[current].classList.remove('active');
            current = (index + slides.length) % slides.length;
            slides[current].classList.add('is-active');
            if (dots[current]) dots[current].classList.add('active');
        }

        function startAuto() {
            timer = setInterval(function () { goTo(current + 1); }, 4500);
        }
        function resetAuto() {
            clearInterval(timer);
            startAuto();
        }

        if (btns[0]) btns[0].addEventListener('click', function () { goTo(current - 1); resetAuto(); });
        if (btns[1]) btns[1].addEventListener('click', function () { goTo(current + 1); resetAuto(); });
        dots.forEach(function (dot, i) {
            dot.addEventListener('click', function () { goTo(i); resetAuto(); });
        });

        startAuto();
    }

    /* ── Reveal au scroll ─────────────────────────────────── */
    function initReveal() {
        var els = document.querySelectorAll('.reveal');
        if (!els.length) return;

        var obs = new IntersectionObserver(function (entries) {
            entries.forEach(function (e) {
                if (e.isIntersecting) {
                    e.target.classList.add('is-visible');
                    obs.unobserve(e.target);
                }
            });
        }, { threshold: 0.12 });

        els.forEach(function (el) { obs.observe(el); });
    }

    /* ── Subnav lien actif ────────────────────────────────── */
    function initSubnav() {
        var links = document.querySelectorAll('.subnav-link');
        var path = window.location.pathname;
        links.forEach(function (link) {
            var href = link.getAttribute('href');
            if (href && href === path) {
                links.forEach(function (l) { l.classList.remove('active'); });
                link.classList.add('active');
            }
        });
    }

    /* ── Navbar ombre au scroll ───────────────────────────── */
    function initNavbarScroll() {
        var header = document.querySelector('.header-sticky');
        if (!header) return;
        window.addEventListener('scroll', function () {
            if (window.scrollY > 8) {
                header.style.boxShadow = '0 4px 16px rgba(13,43,26,.22)';
            } else {
                header.style.boxShadow = 'none';
            }
        }, { passive: true });
    }

    document.addEventListener('DOMContentLoaded', function () {
        initCarousel();
        initReveal();
        initSubnav();
        initNavbarScroll();
    });
})();