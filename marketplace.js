(function () {
    'use strict';

    const packageNamePattern = /^[a-z0-9](?:[a-z0-9-]{0,62}[a-z0-9])?$/;

    function layoutForCount(count) {
        if (count <= 0) return 'empty';
        if (count === 1) return 'solo';
        if (count === 2) return 'duo';
        if (count === 3) return 'trio';
        if (count === 4) return 'quad';
        return 'catalog';
    }

    function safeStrings(value, limit) {
        if (!Array.isArray(value)) return [];
        return value
            .filter(item => typeof item === 'string')
            .map(item => item.trim().toLowerCase())
            .filter(item => item.length > 0 && item.length <= 40)
            .slice(0, limit);
    }

    function normalizePackage(value) {
        if (!value || typeof value !== 'object') return null;
        const name = typeof value.name === 'string' ? value.name.trim().toLowerCase() : '';
        if (!packageNamePattern.test(name)) return null;

        const description = typeof value.description === 'string'
            ? value.description.trim().slice(0, 320)
            : 'No package description has been provided yet.';
        const version = typeof value.version === 'string' ? value.version.trim().slice(0, 32) : 'unversioned';

        return {
            name,
            version,
            description,
            permissions: safeStrings(value.permissions, 6),
            keywords: safeStrings(value.keywords, 8)
        };
    }

    function titleForName(name) {
        const known = {
            'block-web': 'Block Web',
            'block-work': 'Block Work',
            'gblock-d': 'Gblock:D'
        };
        if (known[name]) return known[name];
        return name.split('-').map(part => part.charAt(0).toUpperCase() + part.slice(1)).join(' ');
    }

    function categoryForPackage(pkg) {
        const categoryMap = [
            ['concurrency', 'Concurrency'],
            ['web', 'Web'],
            ['game', 'Games'],
            ['workspace', 'Work'],
            ['drawing', 'Geometry'],
            ['graphics', 'Graphics'],
            ['data', 'Data'],
            ['testing', 'Testing']
        ];
        const terms = pkg.keywords.concat(pkg.permissions);
        const match = categoryMap.find(entry => terms.includes(entry[0]));
        return match ? match[1] : 'Package';
    }

    function symbolForPackage(pkg) {
        const symbolMap = {
            concurrency: 'OCT',
            web: 'WEB',
            game: 'G:D',
            workspace: 'WRK',
            drawing: 'DRW',
            graphics: 'GFX',
            testing: 'TST'
        };
        const terms = pkg.keywords.concat(pkg.permissions);
        const key = Object.keys(symbolMap).find(term => terms.includes(term));
        if (key) return symbolMap[key];
        const compact = pkg.name.replace(/(^|-)block(-|$)/g, '-').replace(/[^a-z0-9]/g, '');
        return (compact || pkg.name).slice(0, 3).toUpperCase();
    }

    function setRegistryStatus(element, label, failed) {
        if (!element) return;
        element.textContent = '';
        const indicator = document.createElement('i');
        if (failed) indicator.className = 'is-error';
        element.append(indicator, document.createTextNode(' ' + label));
    }

    function createState(kind, title, detail) {
        const state = document.createElement('div');
        state.className = 'market-state market-' + kind;
        state.setAttribute('role', kind === 'error' ? 'alert' : 'status');

        const mark = document.createElement('span');
        mark.className = 'market-state-mark';
        mark.setAttribute('aria-hidden', 'true');
        mark.textContent = kind === 'empty' ? '+' : '!';

        const copy = document.createElement('div');
        const eyebrow = document.createElement('p');
        eyebrow.className = 'eyebrow';
        eyebrow.textContent = kind === 'empty' ? 'The registry is ready' : 'Registry connection';
        const heading = document.createElement('h3');
        heading.textContent = title;
        const paragraph = document.createElement('p');
        paragraph.textContent = detail;
        copy.append(eyebrow, heading, paragraph);

        const action = document.createElement('a');
        action.className = 'market-state-action';
        action.target = '_blank';
        action.rel = 'noopener noreferrer';
        if (kind === 'empty') {
            action.href = 'https://github.com/O-O1112/Block_lang/blob/main/CONTRIBUTING.md';
            action.textContent = 'Publish the first package ↗';
        } else {
            action.href = 'https://github.com/O-O1112/Block_lang/tree/main/registry';
            action.textContent = 'Inspect registry source ↗';
        }

        state.append(mark, copy, action);
        return state;
    }

    function enableCardMotion(card) {
        const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        const finePointer = window.matchMedia('(hover: hover) and (pointer: fine)').matches;
        if (!finePointer || reducedMotion) return;
        card.addEventListener('pointermove', event => {
            const rect = card.getBoundingClientRect();
            const x = event.clientX - rect.left;
            const y = event.clientY - rect.top;
            card.style.setProperty('--mouse-x', x + 'px');
            card.style.setProperty('--mouse-y', y + 'px');
            card.style.transform = 'perspective(1000px) rotateX(' + (((y / rect.height) - .5) * -4) + 'deg) rotateY(' + (((x / rect.width) - .5) * 4) + 'deg)';
        });
        card.addEventListener('pointerleave', () => { card.style.transform = ''; });
    }

    function createCard(pkg, index, template) {
        const fragment = template.content.cloneNode(true);
        const card = fragment.querySelector('.market-card');
        const title = titleForName(pkg.name);
        const category = categoryForPackage(pkg);
        const tags = Array.from(new Set(pkg.permissions.concat(pkg.keywords))).slice(0, 5);

        fragment.querySelector('[data-market-meta]').textContent = String(index + 1).padStart(2, '0') + ' / ' + category;
        fragment.querySelector('[data-market-symbol]').textContent = symbolForPackage(pkg);
        fragment.querySelector('[data-market-title]').textContent = title;
        fragment.querySelector('[data-market-description]').textContent = pkg.description;
        fragment.querySelector('[data-market-command]').textContent = 'block pkg install ' + pkg.name + ' --remote';

        const source = fragment.querySelector('[data-market-source]');
        source.href = 'https://github.com/O-O1112/Block_lang/tree/main/registry/packages/' + encodeURIComponent(pkg.name);
        source.setAttribute('aria-label', 'Inspect ' + title + ' version ' + pkg.version + ' source');

        const tagList = fragment.querySelector('[data-market-tags]');
        (tags.length ? tags : ['metadata']).forEach(tag => {
            const item = document.createElement('li');
            item.textContent = tag.replace(/_/g, ' ');
            tagList.appendChild(item);
        });

        card.classList.add('visible');
        enableCardMotion(card);
        return fragment;
    }

    async function mountMarketplace() {
        const grid = document.getElementById('market-grid');
        const template = document.getElementById('market-card-template');
        if (!grid || !template) return;

        const count = document.getElementById('market-package-count');
        const status = document.getElementById('market-registry-status');
        const registryUrl = grid.dataset.registryUrl || 'registry/index.json';

        try {
            const response = await fetch(registryUrl, { cache: 'no-store', credentials: 'same-origin' });
            if (!response.ok) throw new Error('Registry returned HTTP ' + response.status + '.');
            const registry = await response.json();
            if (!registry || registry.schema !== 'block-registry/v1' || !Array.isArray(registry.packages)) {
                throw new Error('Registry data does not match block-registry/v1.');
            }

            const packages = registry.packages.map(normalizePackage).filter(Boolean);
            if (registry.packages.length > 0 && packages.length === 0) {
                throw new Error('Registry contains no safe package records.');
            }

            grid.replaceChildren();
            grid.dataset.layout = layoutForCount(packages.length);
            grid.setAttribute('aria-busy', 'false');
            if (count) count.textContent = String(packages.length).padStart(2, '0');
            setRegistryStatus(status, 'Online', false);

            if (packages.length === 0) {
                grid.appendChild(createState('empty', 'No packages have been published yet.', 'The layout will expand automatically as reviewed packages are added to registry/index.json.'));
                return;
            }

            const packageFragment = document.createDocumentFragment();
            packages.forEach((pkg, index) => packageFragment.appendChild(createCard(pkg, index, template)));
            grid.appendChild(packageFragment);
        } catch (error) {
            console.error('Block marketplace registry:', error);
            grid.replaceChildren(createState('error', 'The catalog could not be loaded.', 'No package data was guessed or cached. Refresh this page, or inspect the registry source on GitHub.'));
            grid.dataset.layout = 'error';
            grid.setAttribute('aria-busy', 'false');
            if (count) count.textContent = '--';
            setRegistryStatus(status, 'Unavailable', true);
        }
    }

    const api = { layoutForCount, normalizePackage };
    if (typeof module !== 'undefined' && module.exports) module.exports = api;
    if (typeof document !== 'undefined') {
        if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', mountMarketplace, { once: true });
        else mountMarketplace();
    }
}());
