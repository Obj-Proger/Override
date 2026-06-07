(function () {
    'use strict';

    // Constants
    const THEME_KEY = 'docs-theme';
    const SCROLL_KEY = 'docs-scroll-position';
    const MOBILE_QUERY = '(max-width: 720px)';

    // Helpers
    const isMobile = () => window.matchMedia(MOBILE_QUERY).matches;

    // Returns a per-page localStorage key for open sections
    function getSectionsKey() {
        const path = window.location.pathname;
        const pageName = path.substring(path.lastIndexOf('/') + 1).replace(/\.html$/, '') || 'index';
        return `docs-open-sections-${pageName}`;
    }

    // Menu
    function initMenu(overlay) {
        const btn = document.getElementById('toggle');
        const nav = document.getElementById('menu');

        const closeMenu = () => {
            document.body.classList.remove('open', 'mobile-open');
            if (btn) btn.setAttribute('aria-expanded', 'false');
        };

        const openMenu = () => {
            if (isMobile()) {
                document.body.classList.add('mobile-open');
                document.body.classList.remove('open');
            } else {
                document.body.classList.add('open');
                document.body.classList.remove('mobile-open');
            }
            if (btn) btn.setAttribute('aria-expanded', 'true');
        };

        if (!btn || !nav) return;

        btn.addEventListener('click', () => {
            const isOpen = document.body.classList.contains('open')
                || document.body.classList.contains('mobile-open');
            isOpen ? closeMenu() : openMenu();
        });

        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') closeMenu();
        });

        // Close menu when user clicks a nav link
        nav.addEventListener('click', (e) => {
            if (e.target.closest('a')) closeMenu();
        });

        overlay.addEventListener('click', closeMenu);

        // Switch between desktop/mobile open classes on resize
        window.addEventListener('resize', () => {
            const bodyOpen = document.body.classList.contains('open');
            const mobileOpen = document.body.classList.contains('mobile-open');
            if (!bodyOpen && !mobileOpen) return;

            if (isMobile() && bodyOpen) {
                document.body.classList.replace('open', 'mobile-open');
            } else if (!isMobile() && mobileOpen) {
                document.body.classList.replace('mobile-open', 'open');
            }
        });
    }

    // Theme
    function initTheme() {
        const themeBtn = document.getElementById('theme-switch');
        if (!themeBtn) return;

        // Build toggle switch UI (sun + moon icons)
        themeBtn.innerHTML = `
            <span class="theme-toggle-track">
                <span class="theme-toggle-thumb"></span>
                <span class="theme-toggle-icon theme-toggle-icon--sun" aria-hidden="true">
                    <svg viewBox="0 0 53.85 53.85" xmlns="http://www.w3.org/2000/svg">
                        <path d="M26.93,40.39c-7.44,0-13.46-6.03-13.46-13.46s6.03-13.46,13.46-13.46,13.46,6.03,13.46,13.46-6.03,13.46-13.46,13.46ZM26.93,35.34c4.65,0,8.41-3.77,8.41-8.41s-3.77-8.41-8.41-8.41-8.41,3.77-8.41,8.41,3.77,8.41,8.41,8.41ZM45.97,7.89c.99.99.99,2.58,0,3.57,0,0,0,0,0,0l-3.57,3.57c-.95,1.02-2.54,1.08-3.56.13-.36-.33-.61-.76-.73-1.23-.23-.89.05-1.84.72-2.47l3.57-3.57c.99-.98,2.58-.98,3.57,0h0ZM15.02,38.83c.98.99.98,2.58,0,3.57l-3.57,3.57c-.97,1-2.57,1.03-3.57.06-1-.97-1.03-2.57-.06-3.57.02-.02.04-.04.06-.06l3.57-3.57c.99-.99,2.58-.99,3.57,0,0,0,0,0,0,0ZM26.93,0c1.39,0,2.52,1.13,2.52,2.52v5.05c0,1.39-1.13,2.52-2.52,2.52s-2.52-1.13-2.52-2.52V2.52c0-1.39,1.13-2.52,2.52-2.52ZM10.1,26.93c0,1.39-1.13,2.52-2.52,2.52H2.52c-1.39,0-2.52-1.13-2.52-2.52s1.13-2.52,2.52-2.52h5.05c1.39,0,2.52,1.13,2.52,2.52ZM53.85,26.93c0,1.39-1.13,2.52-2.52,2.52h-5.05c-1.39,0-2.52-1.13-2.52-2.52s1.13-2.52,2.52-2.52h5.05c1.39,0,2.52,1.13,2.52,2.52ZM26.93,43.76c1.39,0,2.52,1.13,2.52,2.52v5.05c0,1.39-1.13,2.52-2.52,2.52s-2.52-1.13-2.52-2.52v-5.05c0-1.39,1.13-2.52,2.52-2.52ZM38.83,38.83c.99-.98,2.58-.98,3.57,0l3.57,3.57c.97,1,.94,2.6-.06,3.57-.98.95-2.53.95-3.51,0l-3.57-3.57c-.99-.99-.99-2.58,0-3.57,0,0,0,0,0,0ZM7.89,7.89c.99-.99,2.58-.99,3.57,0h0s3.57,3.57,3.57,3.57c.93,1,.9,2.54-.06,3.51-.96.96-2.51.99-3.51.06l-3.57-3.57c-.98-.99-.98-2.58,0-3.57h0Z"/>
                    </svg>
                </span>
                <span class="theme-toggle-icon theme-toggle-icon--moon" aria-hidden="true">
                    <svg viewBox="0 0 50.04 50.04" xmlns="http://www.w3.org/2000/svg">
                        <path d="M30.2.81c.75-.76,1.88-1.01,2.88-.64,13.35,4.83,20.24,19.57,15.41,32.92-4.83,13.35-19.57,20.24-32.92,15.41-7.17-2.6-12.81-8.24-15.41-15.41-.52-1.43.22-3.01,1.65-3.53.61-.22,1.27-.22,1.87,0,10.48,3.8,22.06-1.61,25.87-12.09,1.61-4.45,1.61-9.32,0-13.77-.36-1-.11-2.13.64-2.89ZM36.13,7.95c1.45,14.12-8.82,26.74-22.94,28.19-1.74.18-3.5.18-5.25,0,6.53,9.04,19.15,11.07,28.19,4.54,9.04-6.53,11.07-19.15,4.54-28.19-1.26-1.74-2.79-3.27-4.54-4.54Z"/>
                    </svg>
                </span>
            </span>
        `;

        const applyTheme = (isDark) => {
            document.body.classList.toggle('dark', isDark);
            document.documentElement.classList.toggle('dark', isDark);
            themeBtn.classList.toggle('is-dark', isDark);
            themeBtn.setAttribute('aria-label', isDark ? 'Переключить на светлую тему' : 'Переключить на тёмную тему');
            localStorage.setItem(THEME_KEY, isDark ? 'dark' : 'light');
        };

        const saved = localStorage.getItem(THEME_KEY);
        const preferDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        applyTheme(saved === 'dark' || (!saved && preferDark));

        themeBtn.addEventListener('click', () => {
            applyTheme(!document.body.classList.contains('dark'));
        });
    }

    // Sections persistence

    // Set to true to temporarily suppress saving (e.g. during hash navigation)
    window.__docsSavePaused = false;

    function saveOpenSections() {
        if (window.__docsSavePaused) return;
        const openIds = Array.from(
            document.querySelectorAll('details[open][id^="section-"]')
        ).map(el => el.id);
        localStorage.setItem(getSectionsKey(), JSON.stringify(openIds));
    }

    function restoreOpenSections() {
        const saved = localStorage.getItem(getSectionsKey());
        if (!saved) return;
        try {
            JSON.parse(saved).forEach(id => {
                const el = document.getElementById(id);
                if (el?.tagName === 'DETAILS') el.open = true;
            });
        } catch (_) { /* ignore malformed data */ }
    }

    function saveScrollPosition() {
        localStorage.setItem(SCROLL_KEY, window.scrollY);
    }

    function restoreScrollPosition() {
        const saved = localStorage.getItem(SCROLL_KEY);
        if (saved) window.scrollTo({ top: parseInt(saved, 10), behavior: 'auto' });
    }

    function initSectionsPersistence() {
        restoreOpenSections();
        restoreScrollPosition();

        // Save on toggle (capture phase — fires before details opens/closes)
        document.addEventListener('toggle', (e) => {
            if (e.target.tagName === 'DETAILS') saveOpenSections();
        }, true);

        window.addEventListener('beforeunload', () => {
            saveScrollPosition();
            saveOpenSections();
        });

        let scrollTimer;
        window.addEventListener('scroll', () => {
            clearTimeout(scrollTimer);
            scrollTimer = setTimeout(saveScrollPosition, 100);
        }, { passive: true });
    }

    // Collapse all button 
    function initCollapseAll() {
        const btn = document.getElementById('collapse-all');
        if (!btn) return;

        btn.addEventListener('click', () => {
            // Section ids: "section-1" = parent, "section-1.1" = child (contains dot)
            const all = document.querySelectorAll('details[id^="section-"]');
            const anyOpenSub = Array.from(all).some(d => d.open && d.id.includes('.'));

            all.forEach(d => {
                if (d.id.includes('.') || !anyOpenSub) d.open = false;
            });

            saveOpenSections();
        });
    }

    // Init
    function init() {
        const overlay = document.createElement('div');
        overlay.className = 'overlay';
        document.body.appendChild(overlay);

        initMenu(overlay);
        initTheme();
        initSectionsPersistence();
        initCollapseAll();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();