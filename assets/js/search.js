//  search.js — search logic for Override
//  Depends on: search-index.js (must be loaded before this file)

(function () {
    'use strict';

    // State
    let searchInput, searchDropdown, currentPageId;

    // Page detection

    // Returns the current page identifier (filename without extension)
    function detectCurrentPage() {
        const path = window.location.pathname;
        return path.split('/').pop().replace('.html', '') || 'index';
    }

    // Search

    // Normalises a string for case-insensitive comparison
    function normalize(str) {
        return str.toLowerCase().trim();
    }

    // Converts a relative URL from the index to an absolute path
    function resolveUrl(url) {
        if (url.startsWith('/') || url.startsWith('http')) return url;
        // Build absolute URL using an <a> element — browser handles the resolution
        const a = document.createElement('a');
        a.href = url;
        return a.pathname;
    }

    // Searches SEARCH_INDEX and returns results split by page
    function search(query) {
        if (!query || query.length < 2) return { current: [], other: [] };

        const q = normalize(query);
        const current = [];
        const other = [];

        for (const page of SEARCH_INDEX) {
            for (const section of page.sections) {
                const inTitle = normalize(section.title).includes(q);
                const inKeywords = section.keywords.some(k => normalize(k).includes(q));

                if (!inTitle && !inKeywords) continue;

                const result = {
                    pageTitle: page.title,
                    sectionTitle: section.title,
                    url: section.id ? `${resolveUrl(page.url)}#section-${section.id}` : resolveUrl(page.url),
                };

                if (page.page === currentPageId) {
                    current.push(result);
                } else {
                    other.push(result);
                }
            }
        }

        return { current, other };
    }

    // Rendering

    // Wraps matched query text in <mark> tags
    function highlight(text, query) {
        const escaped = query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        return text.replace(new RegExp(`(${escaped})`, 'gi'), '<mark>$1</mark>');
    }

    // Builds a single result <a> element
    function buildResultItem(r, query, modifierClass) {
        const item = document.createElement('a');
        item.className = `search-item ${modifierClass}`;
        item.href = r.url;

        if (modifierClass === 'search-item--other') {
            item.innerHTML = `
                <span class="search-item-page">${r.pageTitle}</span>
                <span class="search-item-title">${highlight(r.sectionTitle, query)}</span>
            `;
        } else {
            item.innerHTML = `<span class="search-item-title">${highlight(r.sectionTitle, query)}</span>`;
        }

        item.addEventListener('click', closeDropdown);
        return item;
    }

    // Builds a labelled group of results
    function buildGroup(label, results, query, modifierClass) {
        const group = document.createElement('div');
        group.className = 'search-group';
        group.innerHTML = `<div class="search-group-label">${label}</div>`;
        results.forEach(r => group.appendChild(buildResultItem(r, query, modifierClass)));
        return group;
    }

    // Renders the dropdown with prioritised results
    function renderDropdown(results, query) {
        searchDropdown.innerHTML = '';

        const { current, other } = results;

        if (current.length === 0 && other.length === 0) {
            searchDropdown.innerHTML = '<div class="search-empty">Ничего не найдено</div>';
            searchDropdown.classList.add('open');
            return;
        }

        if (current.length > 0) {
            searchDropdown.appendChild(
                buildGroup('На этой странице', current, query, 'search-item--current')
            );
        }

        if (current.length > 0 && other.length > 0) {
            const sep = document.createElement('div');
            sep.className = 'search-separator';
            searchDropdown.appendChild(sep);
        }

        if (other.length > 0) {
            searchDropdown.appendChild(
                buildGroup('На других страницах', other, query, 'search-item--other')
            );
        }

        searchDropdown.classList.add('open');
    }

    // Dropdown control

    function closeDropdown() {
        searchDropdown.classList.remove('open');
        searchDropdown.innerHTML = '';
    }

    // Keyboard navigation

    function handleKeyboardNav(e) {
        const items = searchDropdown.querySelectorAll('.search-item');
        if (!items.length) return;

        const active = searchDropdown.querySelector('.search-item--active');
        let idx = Array.from(items).indexOf(active);

        if (e.key === 'ArrowDown') {
            e.preventDefault();
            if (active) active.classList.remove('search-item--active');
            idx = (idx + 1) % items.length;
            items[idx].classList.add('search-item--active');
            items[idx].scrollIntoView({ block: 'nearest' });
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            if (active) active.classList.remove('search-item--active');
            idx = (idx - 1 + items.length) % items.length;
            items[idx].classList.add('search-item--active');
            items[idx].scrollIntoView({ block: 'nearest' });
        } else if (e.key === 'Enter' && active) {
            e.preventDefault();
            active.click();
        } else if (e.key === 'Escape') {
            closeDropdown();
            searchInput.blur();
        }
    }

    // Hash navigation

    // Opens the target <details> and scrolls to it when URL contains #section-*
    function scrollToSection() {
        const hash = window.location.hash;
        if (!hash.startsWith('#section-')) return;

        const sectionId = hash.replace('#section-', '');
        const target = document.getElementById(`section-${sectionId}`);
        if (!target) return;

        // Clear saved scroll so it doesn't fight with our scroll-to
        localStorage.removeItem('docs-scroll-position');

        // Pause saving so opening details here doesn't get persisted
        if (typeof window.__docsSavePaused !== 'undefined') window.__docsSavePaused = true;

        // Open all ancestor <details> elements
        let el = target;
        while (el) {
            if (el.tagName === 'DETAILS') el.open = true;
            el = el.parentElement;
        }

        // Remove hash from URL so refresh doesn't reopen sections
        history.replaceState(null, '', window.location.pathname + window.location.search);

        setTimeout(() => {
            target.scrollIntoView({ behavior: 'smooth', block: 'start' });
            // Resume saving after toggle events have fired
            if (typeof window.__docsSavePaused !== 'undefined') window.__docsSavePaused = false;
        }, 100);
    }

    // UI construction

    function createSearchUI() {
        const header = document.querySelector('header');
        if (!header) return;

        const wrapper = document.createElement('div');
        wrapper.className = 'search-wrapper';

        // Wrapper for input + permanent icon
        const inputWrap = document.createElement('div');
        inputWrap.className = 'search-input-wrap';

        // Permanent search icon (always visible inside input)
        const searchIcon = document.createElement('span');
        searchIcon.className = 'search-input-icon';
        searchIcon.setAttribute('aria-hidden', 'true');
        searchIcon.innerHTML = `<svg viewBox="0 0 49.94 49.94" xmlns="http://www.w3.org/2000/svg"><path d="M34.48,38.26c-9.33,7.24-22.75,5.55-29.99-3.78C-2.11,25.97-1.36,13.88,6.26,6.26c8.35-8.35,21.88-8.35,30.23,0,7.62,7.62,8.37,19.71,1.77,28.22l10.83,10.83c1.08,1,1.14,2.69.14,3.77-.35.38-.8.65-1.3.77-.95.24-1.95-.05-2.61-.77l-10.83-10.83ZM37.4,21.37c.13-8.85-6.94-16.13-15.79-16.27-8.85-.13-16.13,6.94-16.27,15.79,0,.16,0,.32,0,.48.13,8.85,7.41,15.92,16.27,15.79,8.66-.13,15.66-7.12,15.79-15.79Z"/></svg>`;

        // Mobile icon button (replaces input on small screens)
        const searchIconBtn = document.createElement('button');
        searchIconBtn.className = 'search-icon-btn';
        searchIconBtn.setAttribute('aria-label', 'Поиск');
        searchIconBtn.innerHTML = `<svg viewBox="0 0 49.94 49.94" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"><path d="M34.48,38.26c-9.33,7.24-22.75,5.55-29.99-3.78C-2.11,25.97-1.36,13.88,6.26,6.26c8.35-8.35,21.88-8.35,30.23,0,7.62,7.62,8.37,19.71,1.77,28.22l10.83,10.83c1.08,1,1.14,2.69.14,3.77-.35.38-.8.65-1.3.77-.95.24-1.95-.05-2.61-.77l-10.83-10.83ZM37.4,21.37c.13-8.85-6.94-16.13-15.79-16.27-8.85-.13-16.13,6.94-16.27,15.79,0,.16,0,.32,0,.48.13,8.85,7.41,15.92,16.27,15.79,8.66-.13,15.66-7.12,15.79-15.79Z"/></svg>`;

        searchInput = document.createElement('input');
        searchInput.type = 'search';
        searchInput.className = 'search-input';
        searchInput.placeholder = 'Поиск...';
        searchInput.autocomplete = 'off';
        searchInput.spellcheck = false;
        searchInput.setAttribute('aria-label', 'Поиск по документации');

        searchDropdown = document.createElement('div');
        searchDropdown.className = 'search-dropdown';
        searchDropdown.setAttribute('role', 'listbox');

        inputWrap.appendChild(searchIcon);
        inputWrap.appendChild(searchInput);

        wrapper.appendChild(searchIconBtn);
        wrapper.appendChild(inputWrap);
        wrapper.appendChild(searchDropdown);

        // Mobile: icon btn opens input
        searchIconBtn.addEventListener('click', () => {
            wrapper.classList.toggle('search-expanded');
            if (wrapper.classList.contains('search-expanded')) {
                document.body.classList.add('search-open');
                searchInput.focus();
            } else {
                document.body.classList.remove('search-open');
                searchInput.blur();
                closeDropdown();
            }
        });

        // Collapse on outside click
        document.addEventListener('mousedown', (e) => {
            if (!e.target.closest('.search-wrapper')) {
                wrapper.classList.remove('search-expanded');
                document.body.classList.remove('search-open');
            }
        });

        // Move theme button into the wrapper so they sit together on the right
        const themeBtn = document.getElementById('theme-switch');
        if (themeBtn) wrapper.appendChild(themeBtn);

        // Insert after the collapse-all button
        const collapseBtn = document.getElementById('collapse-all');
        if (collapseBtn?.nextSibling) {
            header.insertBefore(wrapper, collapseBtn.nextSibling);
        } else {
            header.appendChild(wrapper);
        }
    }

    // Event binding

    function bindEvents() {
        let debounceTimer;

        searchInput.addEventListener('input', () => {
            clearTimeout(debounceTimer);
            const q = searchInput.value.trim();

            if (q.length < 2) { closeDropdown(); return; }

            debounceTimer = setTimeout(() => {
                renderDropdown(search(q), q);
            }, 150);
        });

        // Re-show dropdown on focus if query is still present
        searchInput.addEventListener('focus', () => {
            const q = searchInput.value.trim();
            if (q.length >= 2) renderDropdown(search(q), q);
        });

        searchInput.addEventListener('keydown', handleKeyboardNav);

        // Close dropdown when clicking outside the search wrapper
        document.addEventListener('mousedown', (e) => {
            if (!e.target.closest('.search-wrapper')) closeDropdown();
        });
    }

    // Init

    function init() {
        if (typeof SEARCH_INDEX === 'undefined') {
            console.warn('search.js: SEARCH_INDEX not found. Make sure search-index.js is loaded first.');
            return;
        }

        currentPageId = detectCurrentPage();
        createSearchUI();
        bindEvents();

        if (window.location.hash) scrollToSection();
        window.addEventListener('hashchange', scrollToSection);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();