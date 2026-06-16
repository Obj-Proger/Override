// search.js — expanded search logic for Override
// Depends on: search-index.js (must be loaded before this file)

(function () {
    'use strict';

    let searchInput, searchDropdown, searchWrapper, currentPageId;

    function getBasePath() {
        const baseTag = document.querySelector('base');
        if (baseTag) return baseTag.getAttribute('href') || '/';

        const path = window.location.pathname;
        if (path.includes('/Override/')) return '/Override/';
        return '/';
    }

    const BASE_PATH = getBasePath();

    function detectCurrentPage() {
        const path = window.location.pathname;
        return path.split('/').pop().replace('.html', '') || 'index';
    }

    function normalize(str) {
        return String(str || '')
            .toLowerCase()
            .replace(/ё/g, 'е')
            .replace(/c#/g, 'csharp')
            .replace(/f#/g, 'fsharp')
            .replace(/\.net/g, 'dotnet')
            .replace(/c\+\+/g, 'cpp')
            .replace(/[''`"]/g, '')
            .replace(/[^a-z0-9а-яіїєґ#+.]+/gi, ' ')
            .replace(/\s+/g, ' ')
            .trim();
    }

    function tokenize(str) {
        return normalize(str)
            .split(' ')
            .map(s => s.trim())
            .filter(Boolean)
            .filter(token => token.length > 1 || ['c', 'r', 'x', 'y', 'i', 'j', 'k'].includes(token));
    }

    function unique(arr) {
        return [...new Set(arr.filter(Boolean))];
    }

    function stemEnglish(token) {
        if (!token || /[а-я]/i.test(token)) return token;
        let t = token;
        if (t.length > 5 && t.endsWith('ing')) t = t.slice(0, -3);
        else if (t.length > 4 && t.endsWith('ed')) t = t.slice(0, -2);
        else if (t.length > 4 && t.endsWith('es')) t = t.slice(0, -2);
        else if (t.length > 3 && t.endsWith('s') && !t.endsWith('ss')) t = t.slice(0, -1);
        return t;
    }

    function ruToLat(str) {
        const map = {
            а: 'a', б: 'b', в: 'v', г: 'g', д: 'd', е: 'e', ё: 'e', ж: 'zh', з: 'z',
            и: 'i', й: 'y', к: 'k', л: 'l', м: 'm', н: 'n', о: 'o', п: 'p', р: 'r',
            с: 's', т: 't', у: 'u', ф: 'f', х: 'h', ц: 'ts', ч: 'ch', ш: 'sh', щ: 'sch',
            ъ: '', ы: 'y', ь: '', э: 'e', ю: 'yu', я: 'ya'
        };
        return String(str || '').toLowerCase().replace(/[а-яё]/g, ch => map[ch] ?? ch);
    }

    function latToRu(str) {
        const text = String(str || '').toLowerCase();
        const pairs = [
            ['shch', 'щ'],
            ['sch', 'щ'],
            ['yo', 'ё'],
            ['yu', 'ю'],
            ['ya', 'я'],
            ['zh', 'ж'],
            ['kh', 'х'],
            ['ch', 'ч'],
            ['sh', 'ш'],
            ['ts', 'ц'],
            ['ph', 'ф']
        ];

        let out = text;
        for (const [latin, cyr] of pairs) {
            out = out.replace(new RegExp(latin, 'g'), cyr);
        }

        const single = {
            a: 'а', b: 'б', v: 'в', g: 'г', d: 'д', e: 'е', z: 'з', i: 'и', j: 'й', y: 'й',
            k: 'к', l: 'л', m: 'м', n: 'н', o: 'о', p: 'п', r: 'р', s: 'с', t: 'т', u: 'у',
            f: 'ф', h: 'х', c: 'к', q: 'к', w: 'в', x: 'кс'
        };
        out = out.replace(/[abvgdezijyklmnoprstufhcqwx]/g, ch => single[ch] || ch);
        return out;
    }

    function levenshtein(a, b) {
        const s = String(a || '');
        const t = String(b || '');
        const m = s.length;
        const n = t.length;

        if (m === 0) return n;
        if (n === 0) return m;
        if (s === t) return 0;

        let prev = Array.from({ length: n + 1 }, (_, i) => i);
        let curr = new Array(n + 1);

        for (let i = 1; i <= m; i++) {
            curr[0] = i;
            const sc = s.charCodeAt(i - 1);

            for (let j = 1; j <= n; j++) {
                const cost = sc === t.charCodeAt(j - 1) ? 0 : 1;
                curr[j] = Math.min(prev[j] + 1, curr[j - 1] + 1, prev[j - 1] + cost);
            }

            [prev, curr] = [curr, prev];
        }

        return prev[n];
    }

    function similarity(a, b) {
        const s = normalize(a);
        const t = normalize(b);
        if (!s || !t) return 0;
        if (s === t) return 1;

        const maxLen = Math.max(s.length, t.length);
        const dist = levenshtein(s, t);
        return Math.max(0, 1 - dist / maxLen);
    }

    function resolveUrl(url) {
        if (url.startsWith('/') || url.startsWith('http')) {
            if (url.startsWith('/') && BASE_PATH !== '/') {
                return BASE_PATH.replace(/\/$/, '') + url;
            }
            return url;
        }

        const a = document.createElement('a');
        a.href = url;
        let pathname = a.pathname;

        if (BASE_PATH !== '/' && !pathname.startsWith(BASE_PATH)) {
            pathname = BASE_PATH.replace(/\/$/, '') + pathname;
        }
        return pathname;
    }

    function safeArray(v) {
        return Array.isArray(v) ? v : [];
    }

    function getExtras(section) {
        return [
            ...safeArray(section.aliases),
            ...safeArray(section.synonyms),
            ...safeArray(section.tags),
            ...safeArray(section.phrases),
            ...safeArray(section.concepts)
        ];
    }

    const SYNONYMS = {
        'класс': ['class', 'classes', 'object', 'objects', 'instance', 'oop', 'объект', 'объекты'],
        'метод': ['method', 'methods', 'function', 'functions', 'функция', 'функции'],
        'переменная': ['variable', 'variables'],
        'константа': ['const', 'constant', 'constants', 'readonly'],
        'свойство': ['property', 'properties', 'get', 'set', 'init'],
        'конструктор': ['constructor', 'constructors', 'ctor', 'new'],
        'цикл': ['loop', 'loops', 'for', 'foreach', 'while', 'do-while', 'итерация'],
        'условие': ['if', 'switch', 'ternary', 'тернарный'],
        'массив': ['array', 'arrays'],
        'кортеж': ['tuple', 'tuples'],
        'тип': ['type', 'types', 'struct', 'class', 'interface', 'delegate'],
        'рекурсия': ['recursion', 'recursive'],
        'параметр': ['parameter', 'parameters', 'argument', 'arguments', 'params'],
        'возврат': ['return', 'returns'],
        'перегрузка': ['overload', 'overloading', 'overloads'],
        'сборка мусора': ['gc', 'garbage collection'],
        '.net': ['dotnet', 'clr', 'runtime', 'sdk', 'jit', 'aot', 'il', 'msil'],
        'cli': ['command line', 'dotnet cli', 'командная строка']
    };

    function expandToken(token) {
        const base = normalize(token);
        if (!base) return [];

        const out = new Set([base, stemEnglish(base)]);

        if (/^[a-z0-9 .#+-]+$/.test(base)) {
            out.add(normalize(latToRu(base)));
        }

        if (/^[а-я0-9 .#+-]+$/.test(base)) {
            out.add(normalize(ruToLat(base)));
        }

        if (SYNONYMS[base]) {
            for (const t of SYNONYMS[base]) out.add(normalize(t));
        }

        return [...out].filter(Boolean);
    }

    function buildQueryInfo(query) {
        const raw = normalize(query);
        const tokens = tokenize(raw);
        const expanded = new Set();

        for (const token of tokens) {
            expanded.add(token);
            for (const variant of expandToken(token)) expanded.add(variant);
        }

        const phrases = new Set([raw, normalize(ruToLat(raw)), normalize(latToRu(raw))].filter(Boolean));

        return {
            raw,
            tokens: [...expanded].filter(Boolean),
            phrases: [...phrases].filter(Boolean)
        };
    }

    function collectCorpus(page, section) {
        const parts = [
            page.title,
            page.page,
            ...(safeArray(page.aliases)),
            ...(safeArray(page.tags)),
            section.title,
            ...(safeArray(section.keywords)),
            ...getExtras(section)
        ];

        return normalize(parts.join(' '));
    }

    function scoreMatch(queryToken, corpusToken) {
        if (!queryToken || !corpusToken) return 0;

        const q = normalize(queryToken);
        const c = normalize(corpusToken);

        if (!q || !c) return 0;
        if (q === c) return 1.0;

        const qs = stemEnglish(q);
        const cs = stemEnglish(c);
        if (qs === cs) return 0.95;

        if (q.startsWith(c) || c.startsWith(q)) return 0.9;
        if (q.includes(c) || c.includes(q)) return 0.78;

        if (q.length >= 4 && c.length >= 4) {
            const sim = similarity(q, c);
            if (sim >= 0.82) return 0.72;
            if (sim >= 0.72) return 0.55;
        }

        return 0;
    }

    function scoreSection(queryInfo, page, section) {
        const title = normalize(section.title);
        const pageTitle = normalize(page.title);
        const sectionKeywords = normalize((safeArray(section.keywords).join(' ')));
        const corpus = collectCorpus(page, section);
        const corpusTokens = tokenize(corpus);

        let score = 0;
        let matched = 0;

        for (const phrase of queryInfo.phrases) {
            if (!phrase) continue;
            if (title === phrase) score += 240;
            if (title.startsWith(phrase)) score += 160;
            if (title.includes(phrase)) score += 120;
            if (sectionKeywords.includes(phrase)) score += 90;
            if (corpus.includes(phrase)) score += 20;
        }

        for (const token of queryInfo.tokens) {
            let best = 0;

            for (const ct of corpusTokens) {
                const s = scoreMatch(token, ct);
                if (s > best) best = s;
                if (best === 1) break;
            }

            if (best > 0) {
                matched += 1;
                score += 55 * best;
                if (title.includes(token)) score += 25 * best;
                if (sectionKeywords.includes(token)) score += 20 * best;
                if (pageTitle.includes(token)) score += 8 * best;
            }
        }

        if (queryInfo.tokens.length > 1) {
            const coverage = matched / queryInfo.tokens.length;
            if (coverage === 1) score += 60;
            else if (coverage >= 0.75) score += 30;
            score += Math.round(coverage * 30);
        }

        if (typeof section.boost === 'number') score += section.boost;
        if (typeof page.boost === 'number') score += page.boost;

        return Math.round(score);
    }

    function search(query) {
        const q = buildQueryInfo(query);
        if (!q.raw || q.raw.length < 2) return { current: [], other: [], queryInfo: q };

        const current = [];
        const other = [];

        for (const page of SEARCH_INDEX) {
            for (const section of (page.sections || [])) {
                const score = scoreSection(q, page, section);
                if (score <= 0) continue;

                const result = {
                    pageTitle: page.title,
                    sectionTitle: section.title,
                    url: section.id ? `${resolveUrl(page.url)}#section-${section.id}` : resolveUrl(page.url),
                    score
                };

                if (page.page === currentPageId) current.push(result);
                else other.push(result);
            }
        }

        const sortFn = (a, b) => {
            if (b.score !== a.score) return b.score - a.score;
            return a.sectionTitle.localeCompare(b.sectionTitle, 'ru');
        };

        current.sort(sortFn);
        other.sort(sortFn);

        return { current, other, queryInfo: q };
    }

    function highlight(text, queryInfo) {
        const terms = unique([
            ...tokenize(queryInfo.raw),
            ...queryInfo.tokens
        ]).filter(t => t.length >= 2).sort((a, b) => b.length - a.length);

        if (!terms.length) return text;

        const escaped = terms
            .map(t => t.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'))
            .filter(Boolean);

        if (!escaped.length) return text;

        return String(text).replace(
            new RegExp(`(${escaped.join('|')})`, 'gi'),
            '<mark>$1</mark>'
        );
    }

    function buildResultItem(r, queryInfo, modifierClass) {
        const item = document.createElement('a');
        item.className = `search-item ${modifierClass}`;
        item.setAttribute('role', 'option');
        item.href = r.url;

        if (modifierClass === 'search-item--other') {
            item.innerHTML = `
                <span class="search-item-page">${r.pageTitle}</span>
                <span class="search-item-title">${highlight(r.sectionTitle, queryInfo)}</span>
            `;
        } else {
            item.innerHTML = `<span class="search-item-title">${highlight(r.sectionTitle, queryInfo)}</span>`;
        }

        item.addEventListener('click', modifierClass === 'search-item--current' ? closeSearch : closeDropdown);
        return item;
    }

    function buildGroup(label, results, queryInfo, modifierClass) {
        const group = document.createElement('div');
        group.className = 'search-group';
        group.innerHTML = `<div class="search-group-label">${label}</div>`;
        results.forEach(r => group.appendChild(buildResultItem(r, queryInfo, modifierClass)));
        return group;
    }

    function renderDropdown(results) {
        const { queryInfo } = results;
        const currentSlice = results.current.slice(0, 15);
        const otherSlice = results.other.slice(0, 15);

        searchDropdown.innerHTML = '';

        if (!currentSlice.length && !otherSlice.length) {
            searchDropdown.innerHTML = '<div class="search-empty">Ничего не найдено</div>';
            searchDropdown.classList.add('open');
            return;
        }

        if (currentSlice.length) {
            searchDropdown.appendChild(buildGroup('На этой странице', currentSlice, queryInfo, 'search-item--current'));
        }

        if (currentSlice.length && otherSlice.length) {
            const sep = document.createElement('div');
            sep.className = 'search-separator';
            searchDropdown.appendChild(sep);
        }

        if (otherSlice.length) {
            searchDropdown.appendChild(buildGroup('На других страницах', otherSlice, queryInfo, 'search-item--other'));
        }

        searchDropdown.classList.add('open');
    }

    function closeDropdown() {
        searchDropdown.classList.remove('open');
        searchDropdown.innerHTML = '';
    }

    function closeSearch() {
        closeDropdown();
        searchInput.value = '';
        searchInput.blur();
        searchWrapper.classList.remove('search-expanded');
        document.body.classList.remove('search-open');
    }

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

    function scrollToSection() {
        const hash = window.location.hash;
        if (!hash.startsWith('#section-')) return;

        const sectionId = hash.replace('#section-', '');
        const target = document.getElementById(`section-${sectionId}`);
        if (!target) return;

        localStorage.removeItem('docs-scroll-position');

        if (typeof window.__docsSavePaused !== 'undefined') window.__docsSavePaused = true;

        let el = target;
        while (el) {
            if (el.tagName === 'DETAILS') el.open = true;
            el = el.parentElement;
        }

        history.replaceState(null, '', window.location.pathname + window.location.search);

        setTimeout(() => {
            target.scrollIntoView({ behavior: 'smooth', block: 'start' });
            if (typeof window.__docsSavePaused !== 'undefined') window.__docsSavePaused = false;
        }, 100);
    }

    function createSearchUI() {
        const header = document.querySelector('header');
        if (!header) return;

        const wrapper = document.createElement('div');
        wrapper.className = 'search-wrapper';
        searchWrapper = wrapper;

        const inputWrap = document.createElement('div');
        inputWrap.className = 'search-input-wrap';

        const searchIcon = document.createElement('span');
        searchIcon.className = 'search-input-icon';
        searchIcon.setAttribute('aria-hidden', 'true');
        searchIcon.innerHTML = `<svg viewBox="0 0 49.94 49.94" xmlns="http://www.w3.org/2000/svg"><path d="M34.48,38.26c-9.33,7.24-22.75,5.55-29.99-3.78C-2.11,25.97-1.36,13.88,6.26,6.26c8.35-8.35,21.88-8.35,30.23,0,7.62,7.62,8.37,19.71,1.77,28.22l10.83,10.83c1.08,1,1.14,2.69.14,3.77-.35.38-.8.65-1.3.77-.95.24-1.95-.05-2.61-.77l-10.83-10.83ZM37.4,21.37c.13-8.85-6.94-16.13-15.79-16.27-8.85-.13-16.13,6.94-16.27,15.79,0,.16,0,.32,0,.48.13,8.85,7.41,15.92,16.27,15.79,8.66-.13,15.66-7.12,15.79-15.79Z"/></svg>`;

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

        const themeBtn = document.getElementById('theme-switch');
        if (themeBtn) wrapper.appendChild(themeBtn);

        const collapseBtn = document.getElementById('collapse-all');
        if (collapseBtn?.nextSibling) {
            header.insertBefore(wrapper, collapseBtn.nextSibling);
        } else {
            header.appendChild(wrapper);
        }
    }

    function bindEvents() {
        let debounceTimer;

        searchInput.addEventListener('input', () => {
            clearTimeout(debounceTimer);
            const q = searchInput.value.trim();

            if (q.length < 2) {
                closeDropdown();
                return;
            }

            debounceTimer = setTimeout(() => {
                renderDropdown(search(q));
            }, 120);
        });

        searchInput.addEventListener('focus', () => {
            const q = searchInput.value.trim();
            if (q.length >= 2) renderDropdown(search(q));
        });

        searchInput.addEventListener('keydown', handleKeyboardNav);

        document.addEventListener('mousedown', (e) => {
            if (!e.target.closest('.search-wrapper')) {
                searchWrapper.classList.remove('search-expanded');
                document.body.classList.remove('search-open');
                closeDropdown();
            }
        });
    }

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