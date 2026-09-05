const pluginId = "com.blocklang.acode";

const MAX_CODE_CHARS = 2 * 1024 * 1024;
const MAX_RESPONSE_BYTES = 4 * 1024 * 1024;
const BLOCK_PLUGIN_VERSION = '2.7.0';

const svgIconUrl = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'%3E%3Crect width='100' height='100' rx='20' fill='%232a2a2a'/%3E%3Cg fill='none' stroke='%23ffffff' stroke-width='5' stroke-linejoin='round'%3E%3Crect x='25' y='20' width='15' height='60'/%3E%3Cpolygon points='45,20 70,20 75,32.5 70,45 45,45'/%3E%3Cpolygon points='45,55 75,55 80,67.5 75,80 45,80'/%3E%3C/g%3E%3C/svg%3E";

const BLOCK_LANGUAGE_CATALOG = [
    { tag: 'py', label: 'Python', edition: 'Lite / Standard / Plus', defaultCode: 'print("Hello from Block")' },
    { tag: 'js', label: 'JavaScript', edition: 'Lite / Standard / Plus', defaultCode: 'console.log("Hello from Block");' },
    { tag: 'php', label: 'PHP', edition: 'Standard / Plus', defaultCode: 'echo "Hello from Block";' },
    { tag: 'lua', label: 'Lua', edition: 'Standard / Plus', defaultCode: 'print("Hello from Block")' },
    { tag: 'ruby', label: 'Ruby', edition: 'Standard / Plus', defaultCode: 'puts "Hello from Block"' },
    { tag: 'sql', label: 'SQL', edition: 'Standard / Plus', defaultCode: 'SELECT 1 AS block_ready;' },
    { tag: 'html', label: 'HTML', edition: 'Standard / Plus', defaultCode: '<h2>Block Preview</h2>' },
    { tag: 'rust', label: 'Rust', edition: 'Plus', defaultCode: 'fn main() { println!("Hello from Block"); }' },
    { tag: 'go', label: 'Go', edition: 'Plus', defaultCode: 'package main\n\nimport "fmt"\n\nfunc main() { fmt.Println("Hello from Block") }' },
    { tag: 'cpp', label: 'C++', edition: 'Plus', defaultCode: '#include <iostream>\nint main() { std::cout << "Hello from Block"; }' },
    { tag: 'c', label: 'C', edition: 'Plus', defaultCode: '#include <stdio.h>\nint main(void) { puts("Hello from Block"); }' },
    { tag: 'ts', label: 'TypeScript', edition: 'Plus', defaultCode: 'console.log("Hello from Block");' },
    { tag: 'cs', label: 'C#', edition: 'Plus', defaultCode: 'Console.WriteLine("Hello from Block");' },
    { tag: 'zig', label: 'Zig', edition: 'Plus', defaultCode: 'const std = @import("std");\npub fn main() void { std.debug.print("Hello from Block", .{}); }' },
    { tag: 'dart', label: 'Dart', edition: 'Plus', defaultCode: 'void main() { print("Hello from Block"); }' },
    { tag: 'perl', label: 'Perl', edition: 'Plus', defaultCode: 'print "Hello from Block\\n";' },
    { tag: 'r', label: 'R', edition: 'Plus', defaultCode: 'cat("Hello from Block\\n")' }
];

const BLOCK_ENGINE_OPTIONS = [
    { id: 'lite', label: 'Lite', extension: '.blkl' },
    { id: 'standard', label: 'Standard', extension: '.blk' },
    { id: 'plus', label: 'Plus', extension: '.blkp' }
];

const BLOCK_DEFAULT_RUNTIME_CONFIG = {
    language: 'py',
    engine: 'standard',
    targetProfileIndex: 0,
    timeoutMs: 10000,
    networkBlocked: true
};

async function readResponseTextLimited(response) {
    const advertisedLength = Number(response.headers.get('content-length') || 0);
    if (Number.isFinite(advertisedLength) && advertisedLength > MAX_RESPONSE_BYTES) {
        throw new Error('Engine response is larger than the 4 MiB safety limit.');
    }

    if (!response.body || typeof response.body.getReader !== 'function') {
        const text = await response.text();
        if (text.length > MAX_RESPONSE_BYTES) {
            throw new Error('Engine response is larger than the 4 MiB safety limit.');
        }
        return text;
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let totalBytes = 0;
    let text = '';
    try {
        while (true) {
            const chunk = await reader.read();
            if (chunk.done) break;
            totalBytes += chunk.value.byteLength;
            if (totalBytes > MAX_RESPONSE_BYTES) {
                await reader.cancel();
                throw new Error('Engine response is larger than the 4 MiB safety limit.');
            }
            text += decoder.decode(chunk.value, { stream: true });
        }
        text += decoder.decode();
        return text;
    } finally {
        reader.releaseLock();
    }
}

async function readJsonResponseLimited(response) {
    return JSON.parse(await readResponseTextLimited(response));
}

class BlockPlugin {
    constructor() {
        this._onFileUpdate = this.onFileUpdate.bind(this);
        this._onEditorChange = this.onEditorChange.bind(this);
        this.statusCheckTimer = null;
        this.lastOutput = null;
        this.lastHtmlOutput = null;
        this.lastExecutionStats = null;
        this.sessionTokens = new Map();
        this.activeTheme = window.localStorage.getItem('block_ui_theme') || 'cyber';
        this.runtimeConfig = this.loadRuntimeConfig();
        try {
            this.serverProfiles = JSON.parse(window.localStorage.getItem('block_server_profiles') || '[]');
        } catch (e) {
            this.serverProfiles = [];
        }
        if (!Array.isArray(this.serverProfiles) || this.serverProfiles.length === 0) {
            this.serverProfiles = [
                { name: 'Localhost Engine', url: 'http://127.0.0.1:8080/api/run' }
            ];
            this.saveProfiles();
        } else {
            // Migrate legacy plaintext tokens out of persistent localStorage.
            this.serverProfiles = this.serverProfiles.map(profile => {
                const clean = { name: String(profile.name || 'Block Engine'), url: String(profile.url || '') };
                if (typeof profile.token === 'string' && profile.token) {
                    this.sessionTokens.set(this.profileKey(clean), profile.token);
                }
                return clean;
            });
            this.saveProfiles();
        }
    }

    profileKey(profile) {
        return `${profile.name}\n${profile.url}`;
    }

    saveProfiles() {
        const safeProfiles = this.serverProfiles.map(profile => ({ name: profile.name, url: profile.url }));
        window.localStorage.setItem('block_server_profiles', JSON.stringify(safeProfiles));
    }

    isAllowedServerUrl(value) {
        try {
            const url = new URL(value);
            if (url.username || url.password) return false;
            if (url.protocol === 'https:') return true;
            return url.protocol === 'http:' && (url.hostname === 'localhost' || url.hostname === '127.0.0.1');
        } catch (e) {
            return false;
        }
    }

    getActiveProfile() {
        const idx = Number.isInteger(this.runtimeConfig.targetProfileIndex)
            ? this.runtimeConfig.targetProfileIndex
            : parseInt(window.localStorage.getItem('block_active_profile_idx') || '0', 10);
        const profile = this.serverProfiles[idx] || this.serverProfiles[0];
        if (!profile) return null;
        return { ...profile, token: this.sessionTokens.get(this.profileKey(profile)) || '' };
    }

    loadRuntimeConfig() {
        let stored = {};
        try {
            stored = JSON.parse(window.localStorage.getItem('block_runtime_config') || '{}');
        } catch (_) {}

        const config = { ...BLOCK_DEFAULT_RUNTIME_CONFIG, ...(stored && typeof stored === 'object' ? stored : {}) };
        if (!BLOCK_LANGUAGE_CATALOG.some(item => item.tag === config.language)) config.language = BLOCK_DEFAULT_RUNTIME_CONFIG.language;
        if (!BLOCK_ENGINE_OPTIONS.some(item => item.id === config.engine)) config.engine = BLOCK_DEFAULT_RUNTIME_CONFIG.engine;
        config.targetProfileIndex = Number.isInteger(config.targetProfileIndex) ? Math.max(0, config.targetProfileIndex) : 0;
        config.timeoutMs = Number.isFinite(config.timeoutMs) ? Math.min(120000, Math.max(1000, Math.round(config.timeoutMs))) : BLOCK_DEFAULT_RUNTIME_CONFIG.timeoutMs;
        config.networkBlocked = config.networkBlocked !== false;
        delete config.maxParallel;
        delete config.cacheEnabled;
        return config;
    }

    saveRuntimeConfig() {
        window.localStorage.setItem('block_runtime_config', JSON.stringify(this.runtimeConfig));
        window.localStorage.setItem('block_active_profile_idx', String(this.runtimeConfig.targetProfileIndex));
        this.updateRuntimeConfigButton();
    }

    getLanguageConfig() {
        return BLOCK_LANGUAGE_CATALOG.find(item => item.tag === this.runtimeConfig.language) || BLOCK_LANGUAGE_CATALOG[0];
    }

    getEngineConfig() {
        return BLOCK_ENGINE_OPTIONS.find(item => item.id === this.runtimeConfig.engine) || BLOCK_ENGINE_OPTIONS[1];
    }

    getRuntimeSummary() {
        const language = this.getLanguageConfig();
        const engine = this.getEngineConfig();
        const profile = this.getActiveProfile();
        return `${language.label} <${language.tag}> · ${engine.label} · ${profile ? profile.name : 'No target'}`;
    }

    getRuntimeChipLabel() {
        const language = this.getLanguageConfig();
        const engine = this.getEngineConfig();
        return `${language.tag.toUpperCase()} · ${engine.label}`;
    }

    updateRuntimeConfigButton() {
        const button = document.getElementById('block-runtime-config-button');
        if (button) button.innerText = `⚙ ${this.getRuntimeChipLabel()}`;
        const chip = document.getElementById('block-runtime-chip');
        if (chip) chip.innerText = this.getRuntimeSummary();
    }

    async init($page) {
        // Inject comprehensive themes and responsive mobile styles
        const style = document.createElement('style');
        style.id = 'block-plugin-style';
        style.innerHTML = `
            .icon-block-lang {
                background-image: url("${svgIconUrl}");
                background-size: cover;
                background-repeat: no-repeat;
                background-position: center;
                width: 26px;
                height: 26px;
                display: inline-block;
                margin: 11px 8px 0 8px;
                cursor: pointer;
                border-radius: 4px;
                float: right;
            }
            .file_type_blk::before, .file_type_block::before, .file_type_blkl::before, .file_type_blkp::before {
                content: '' !important;
                display: inline-block !important;
                width: 1.1em;
                height: 1.1em;
                background-image: url("${svgIconUrl}");
                background-size: contain;
                background-repeat: no-repeat;
                background-position: center;
                vertical-align: middle;
            }

            /* Quick Mobile Action Bar */
            #block-quick-bar {
                display: flex;
                gap: 6px;
                overflow-x: auto;
                background: #121215;
                padding: 6px 10px;
                border-top: 1px solid #27272a;
                white-space: nowrap;
                z-index: 10;
            }
            .block-tag-btn {
                background: #27272a;
                color: #e4e4e7;
                border: 1px solid #3f3f46;
                padding: 4px 10px;
                border-radius: 6px;
                font-family: monospace;
                font-size: 12px;
                cursor: pointer;
                transition: all 0.2s;
            }
            .block-tag-btn:active {
                background: #3b82f6;
                color: #ffffff;
            }

            /* Floating Action Buttons (FAB) for Mobile */
            #block-fab-container {
                position: fixed;
                bottom: 80px;
                right: 20px;
                display: flex;
                flex-direction: column;
                gap: 10px;
                z-index: 999;
            }
            .block-fab {
                width: 44px;
                height: 44px;
                border-radius: 50%;
                background: #3b82f6;
                color: #fff;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 20px;
                box-shadow: 0 4px 15px rgba(0,0,0,0.5);
                border: none;
                cursor: pointer;
            }
            .block-fab.run { background: #10b981; }

            /* Persistent Modal Panel with Themes */
            .block-panel-modal {
                position: fixed;
                bottom: 0;
                left: 0;
                right: 0;
                height: 55vh;
                background: #09090b;
                color: #f4f4f5;
                border-top: 2px solid #3b82f6;
                z-index: 99999;
                display: flex;
                flex-direction: column;
                box-shadow: 0 -10px 30px rgba(0,0,0,0.85);
                font-family: monospace;
            }
            .block-panel-header {
                display: flex;
                justify-content: space-between;
                align-items: center;
                background: #18181b;
                padding: 8px 14px;
                border-bottom: 1px solid #27272a;
            }
            .block-panel-tabs {
                display: flex;
                gap: 10px;
            }
            .block-tab {
                padding: 4px 10px;
                font-size: 12px;
                color: #a1a1aa;
                cursor: pointer;
                border-radius: 4px;
            }
            .block-tab.active {
                color: #ffffff;
                background: #27272a;
                font-weight: bold;
            }
            .block-panel-body {
                flex: 1;
                padding: 12px;
                overflow-y: auto;
                font-size: 13px;
                line-height: 1.5;
                white-space: pre-wrap;
                word-break: break-all;
            }
            
            /* Performance Benchmark Bar */
            .block-perf-bar {
                display: flex;
                height: 4px;
                background: #27272a;
                border-radius: 2px;
                overflow: hidden;
                margin: 6px 14px;
            }
            .perf-net { background: #3b82f6; }
            .perf-exec { background: #10b981; }

            .block-panel-footer {
                display: flex;
                justify-content: space-between;
                align-items: center;
                padding: 6px 14px;
                background: #18181b;
                border-top: 1px solid #27272a;
                font-size: 11px;
                color: #a1a1aa;
            }
            .panel-act-btn {
                background: #27272a;
                color: #fff;
                border: 1px solid #3f3f46;
                padding: 2px 8px;
                border-radius: 4px;
                cursor: pointer;
                margin-left: 4px;
            }
            .block-runtime-chip {
                color: #a7f3d0;
                background: rgba(16,185,129,0.12);
                border: 1px solid rgba(16,185,129,0.35);
                border-radius: 999px;
                padding: 3px 8px;
                margin-left: 8px;
                font-size: 10px;
                white-space: nowrap;
            }
            .block-config-button {
                background: #312e81;
                color: #ede9fe;
                border-color: #4c1d95;
            }
            .block-runtime-summary {
                display: grid;
                grid-template-columns: repeat(2, minmax(0, 1fr));
                gap: 6px 12px;
                padding: 8px 14px;
                background: #111113;
                border-bottom: 1px solid #27272a;
                color: #a1a1aa;
                font-size: 10px;
            }
            .block-runtime-summary strong { color: #e4e4e7; font-weight: 600; }
        `;
        document.head.appendChild(style);

        // Action Buttons in Header
        this.$btn = document.createElement('span');
        this.$btn.className = 'icon action-btn';
        this.$btn.onclick = this.showMenu.bind(this);
        this.$btn.style.backgroundImage = `url("${svgIconUrl}")`;
        this.$btn.style.backgroundSize = '20px';
        this.$btn.style.backgroundRepeat = 'no-repeat';
        this.$btn.style.backgroundPosition = 'center';

        this.$runBtn = document.createElement('span');
        this.$runBtn.className = 'icon play_arrow action-btn';
        this.$runBtn.onclick = this.runBlockCode.bind(this);
        this.$runBtn.style.color = '#10b981';
        this.$runBtn.style.fontSize = '24px';

        // Initialize Ace Editor Snippets & Auto-Close Listener
        this.registerSnippets();
        this.initAutoCloseTags();

        // Listen for editor file events
        editorManager.on('switch-file', this._onFileUpdate);
        editorManager.on('rename-file', this._onFileUpdate);
        this._onFileUpdate();

        // Register Commands & Hotkeys
        editorManager.editor.commands.addCommand({
            name: 'Run Block Engine Code',
            bindKey: {win: 'Ctrl-R', mac: 'Command-R'},
            exec: this.runBlockCode.bind(this)
        });

        editorManager.editor.commands.addCommand({
            name: 'Format Block Code',
            bindKey: {win: 'Ctrl-Shift-F', mac: 'Command-Shift-F'},
            exec: this.formatCode.bind(this)
        });
    }

    initAutoCloseTags() {
        const editor = editorManager.editor;
        if (!editor) return;
        editor.on('change', this._onEditorChange);
    }

    onEditorChange(e) {
        if (e.action !== 'insert') return;
        const file = editorManager.activeFile;
        const isBlock = file && file.name && (
            file.name.endsWith('.blk') || file.name.endsWith('.blkp') || file.name.endsWith('.blkl') ||
            file.name.endsWith('.block') || file.name.endsWith('.blocklite') || file.name.endsWith('.blockplus')
        );

        const cursor = editorManager.editor.getCursorPosition();
        const line = editorManager.editor.session.getLine(cursor.row);
        const textBefore = line.substring(0, cursor.column);
        
        // Auto close tag on typing '>' (e.g. typing <py> -> inserts </py>)
        const match = textBefore.match(/<([a-zA-Z0-9_\-]+)>$/);
        if (match && !textBefore.includes('</')) {
            const tag = match[1];
            const closeTag = `</${tag}>`;
            // Check if closing tag isn't already immediately after
            const textAfter = line.substring(cursor.column);
            if (!textAfter.startsWith(closeTag)) {
                editorManager.editor.insert(`\n\n${closeTag}`);
                editorManager.editor.navigateUp(1);
            }
        }
    }

    registerSnippets() {
        if (!window.ace) return;
        ace.config.loadModule("ace/snippets/html", function(m) {
            const snippetManager = ace.require("ace/snippets").snippetManager;
            if (!m) m = { snippets: [] };
            m.snippets = m.snippets || [];
            const mySnippets = [
                { name: "server", content: "<server port=\"${1:8080}\">\n  $2\n</server>", tabTrigger: "server" },
                { name: "route", content: "<route path=\"${1:/}\">\n  $2\n</route>", tabTrigger: "route" },
                { name: "python", content: "<py>\n$1\n</py>", tabTrigger: "python" },
                { name: "javascript", content: "<js>\n$1\n</js>", tabTrigger: "js" },
                { name: "php", content: "<php>\n$1\n</php>", tabTrigger: "php" },
                { name: "powershell", content: "<ps>\n$1\n</ps>", tabTrigger: "ps" },
                { name: "lua", content: "<lua>\n$1\n</lua>", tabTrigger: "lua" },
                { name: "ruby", content: "<ruby>\n$1\n</ruby>", tabTrigger: "ruby" },
                { name: "sql", content: "<sql>\n$1\n</sql>", tabTrigger: "sql" },
                { name: "html", content: "<html>\n$1\n</html>", tabTrigger: "html" },
                { name: "json", content: "<json>\n$1\n</json>", tabTrigger: "json" },
                { name: "rust", content: "<rust>\n$1\n</rust>", tabTrigger: "rust" },
                { name: "go", content: "<go>\n$1\n</go>", tabTrigger: "go" },
                { name: "cpp", content: "<cpp>\n$1\n</cpp>", tabTrigger: "cpp" },
                { name: "c", content: "<c>\n$1\n</c>", tabTrigger: "c" },
                { name: "typescript", content: "<ts>\n$1\n</ts>", tabTrigger: "ts" },
                { name: "csharp", content: "<cs>\n$1\n</cs>", tabTrigger: "cs" },
                { name: "zig", content: "<zig>\n$1\n</zig>", tabTrigger: "zig" },
                { name: "dart", content: "<dart>\n$1\n</dart>", tabTrigger: "dart" },
                { name: "perl", content: "<perl>\n$1\n</perl>", tabTrigger: "perl" },
                { name: "r", content: "<r>\n$1\n</r>", tabTrigger: "r" }
            ];
            m.snippets.push.apply(m.snippets, mySnippets);
            snippetManager.register(m.snippets, "html");
        });
    }

    onFileUpdate() {
        const file = editorManager.activeFile;
        const exts = ['.blk', '.block', '.blkl', '.blocklite', '.blkp', '.blockplus'];
        const isBlock = file && file.name && exts.some(ext => file.name.endsWith(ext));
        
        if (isBlock) {
            editorManager.editor.getSession().setMode('ace/mode/html');
            const header = document.querySelector('#main-header') || document.querySelector('header');
            if (header && !this.$btn.parentElement) {
                header.appendChild(this.$btn);
                header.appendChild(this.$runBtn);
            }
            this.showQuickBar(true);
        } else {
            if (this.$btn.parentElement) this.$btn.parentElement.removeChild(this.$btn);
            if (this.$runBtn.parentElement) this.$runBtn.parentElement.removeChild(this.$runBtn);
            this.showQuickBar(false);
        }
    }

    showQuickBar(show) {
        let bar = document.getElementById('block-quick-bar');
        if (show) {
            if (!bar) {
                bar = document.createElement('div');
                bar.id = 'block-quick-bar';
                const tags = ['<py>', '<js>', '<html>', '<php>', '<sql>', '<rust>', '<go>', '<cpp>', '<use>', '</py>', '</js>', '</html>'];
                tags.forEach(tag => {
                    const btn = document.createElement('button');
                    btn.className = 'block-tag-btn';
                    btn.innerText = tag;
                    btn.onclick = () => {
                        editorManager.editor.insert(tag + '\n');
                        editorManager.editor.focus();
                    };
                    bar.appendChild(btn);
                });

                const runtimeBtn = document.createElement('button');
                runtimeBtn.id = 'block-runtime-config-button';
                runtimeBtn.className = 'block-tag-btn block-config-button';
                runtimeBtn.innerText = `⚙ ${this.getRuntimeChipLabel()}`;
                runtimeBtn.onclick = () => this.openRuntimeParameters();
                bar.appendChild(runtimeBtn);
                
                const wizardBtn = document.createElement('button');
                wizardBtn.className = 'block-tag-btn';
                wizardBtn.style.background = '#8b5cf6';
                wizardBtn.style.color = '#fff';
                wizardBtn.innerText = '⚡ Template Wizard';
                wizardBtn.onclick = () => this.insertTemplate();
                bar.appendChild(wizardBtn);

                const editorContainer = document.querySelector('#editor') || document.body;
                editorContainer.parentElement.insertBefore(bar, editorContainer);
            }
            bar.style.display = 'flex';
        } else if (bar) {
            bar.style.display = 'none';
        }
    }

    async openRuntimeParameters() {
        const profile = this.getActiveProfile();
        const options = [
            `🧩 Language · ${this.getLanguageConfig().label} <${this.runtimeConfig.language}>`,
            `⚡ Engine · ${this.getEngineConfig().label} (${this.getEngineConfig().extension})`,
            `🎯 Target · ${profile ? profile.name : 'No target'}`,
            `⏱ Timeout · ${this.runtimeConfig.timeoutMs}ms`,
            `🛡 Security · Advisory network guard ${this.runtimeConfig.networkBlocked ? 'ON' : 'OFF'}`,
            '↺ Reset runtime defaults',
            '✓ Done'
        ];
        const selected = await window.acode.select('Block Runtime Parameters', options);
        if (!selected || selected.includes('Done')) return;

        if (selected.includes('Language')) await this.configureLanguage();
        else if (selected.includes('Engine')) await this.configureEngine();
        else if (selected.includes('Target')) await this.configureTarget();
        else if (selected.includes('Timeout')) await this.configurePerformance();
        else if (selected.includes('Security')) await this.configureSecurity();
        else if (selected.includes('Reset')) this.runtimeConfig = { ...BLOCK_DEFAULT_RUNTIME_CONFIG };

        this.saveRuntimeConfig();
        if (window.toast) window.toast(`Runtime saved: ${this.getRuntimeSummary()}`, 1800);
        return this.openRuntimeParameters();
    }

    async configureLanguage() {
        const options = BLOCK_LANGUAGE_CATALOG.map(item =>
            `${item.tag === this.runtimeConfig.language ? '✔ ' : ''}${item.label} <${item.tag}> · ${item.edition}`
        );
        const selected = await window.acode.select('Block Language', options);
        if (!selected) return;
        const item = BLOCK_LANGUAGE_CATALOG.find(language => selected.includes(`${language.label} <${language.tag}>`));
        if (item) this.runtimeConfig.language = item.tag;
    }

    async configureEngine() {
        const options = BLOCK_ENGINE_OPTIONS.map(item =>
            `${item.id === this.runtimeConfig.engine ? '✔ ' : ''}${item.label} · ${item.extension}`
        );
        const selected = await window.acode.select('Execution Engine', options);
        if (!selected) return;
        const item = BLOCK_ENGINE_OPTIONS.find(engine => selected.includes(`${engine.label} · ${engine.extension}`));
        if (item) this.runtimeConfig.engine = item.id;
    }

    async configureTarget() {
        const options = this.serverProfiles.map((item, index) =>
            `${index === this.runtimeConfig.targetProfileIndex ? '✔ ' : ''}${item.name} · ${item.url}`
        );
        if (options.length === 0) {
            window.acode.alert('Execution Target', 'Add a server profile before selecting a target.');
            return;
        }
        const selected = await window.acode.select('Execution Target', options);
        if (!selected) return;
        const index = options.indexOf(selected);
        if (index >= 0) this.runtimeConfig.targetProfileIndex = index;
    }

    async configurePerformance() {
        const timeoutInput = await window.acode.prompt('Execution timeout (ms)', String(this.runtimeConfig.timeoutMs));
        if (timeoutInput === null || timeoutInput === undefined) return;
        const timeoutMs = Number(timeoutInput);
        if (!Number.isFinite(timeoutMs) || timeoutMs < 1000 || timeoutMs > 120000) {
            window.acode.alert('Invalid timeout', 'Use a value between 1000 and 120000 milliseconds.');
            return;
        }

        this.runtimeConfig.timeoutMs = Math.round(timeoutMs);
    }

    async configureSecurity() {
        const selected = await window.acode.select('Advisory Network Guard', [
            `${this.runtimeConfig.networkBlocked ? '✔ ' : ''}Best-effort guard · Block common networking APIs`,
            `${!this.runtimeConfig.networkBlocked ? '✔ ' : ''}Allow runtime network access`
        ]);
        if (!selected) return;
        this.runtimeConfig.networkBlocked = selected.includes('Best-effort');
    }

    insertTemplate() {
        const tpl = `<py>\n# Data Processing Block\nuser_name = "Acode Developer"\nscores = [95, 98, 100]\navg_score = sum(scores) / len(scores)\n</py>\n\n<js>\n// Interoperable JS Scope\nconsole.log("Welcome,", user_name);\nconsole.log("Average Score:", avg_score);\nstatus_badge = "Active";\n</js>\n\n<html>\n<div style="background:#18181b; color:#fff; padding:20px; border-radius:12px;">\n  <h2>🚀 {{user_name}}</h2>\n  <p>Status: {{status_badge}} | Average: {{avg_score}}</p>\n</div>\n</html>\n`;
        editorManager.editor.insert(tpl);
        editorManager.editor.focus();
    }

    async showMenu() {
        const activeProf = this.getActiveProfile();
        const opts = [
            `🚀 Run Code (Server: ${activeProf.name})`,
            '🌐 Preview Rendered HTML',
            '⚡ Quick Interactive REPL Playground',
            '🎨 Auto-Fix & Format Code',
            '🔍 Offline Syntax Diagnostics',
            '⚙ Runtime Parameter Studio',
            '🖥️ Server Profile Manager',
            '📥 Export Execution Output Log',
            `ℹ️ About Block Engine v${BLOCK_PLUGIN_VERSION}`
        ];
        const res = await window.acode.select(`⚡ Block Engine (${activeProf.name})`, opts);
        if (!res) return;
        if (res.includes('Run Code')) this.runBlockCode();
        else if (res.includes('Preview Rendered HTML')) this.previewHtml();
        else if (res.includes('Interactive REPL Playground')) this.openRepl();
        else if (res.includes('Auto-Fix & Format')) this.formatCode();
        else if (res.includes('Offline Syntax Diagnostics')) this.runDiagnostics();
        else if (res.includes('Runtime Parameter Studio')) this.openRuntimeParameters();
        else if (res.includes('Server Profile Manager')) this.openProfileManager();
        else if (res.includes('Export Execution Output Log')) this.exportLog();
        else if (res.includes('About Block Engine')) this.showAbout();
    }

    async openProfileManager() {
        const profNames = this.serverProfiles.map((p, i) => `${i === parseInt(window.localStorage.getItem('block_active_profile_idx') || '0', 10) ? '✔ ' : ''}${p.name} (${p.url})`);
        profNames.push('➕ Add New Server Profile');
        
        const res = await window.acode.select('Server Profile Manager', profNames);
        if (!res) return;
        if (res.includes('Add New Server Profile')) {
            const name = await window.acode.prompt('Profile Name', 'Dev Server');
            if (!name) return;
            const url = await window.acode.prompt('API URL', 'http://127.0.0.1:8080/api/run');
            if (!url) return;
            if (!this.isAllowedServerUrl(url)) {
                window.acode.alert('Invalid API URL', 'Use HTTPS for remote servers; HTTP is allowed only for localhost or 127.0.0.1.');
                return;
            }
            const token = await window.acode.prompt('X-Api-Token (required; copy it from block serve output)', '');
            const profile = { name, url };
            this.serverProfiles.push(profile);
            if (token) this.sessionTokens.set(this.profileKey(profile), token);
            this.saveProfiles();
            if (window.toast) window.toast('Profile added!', 1500);
        } else {
            const idx = profNames.indexOf(res);
            if (idx >= 0 && idx < this.serverProfiles.length) {
                this.runtimeConfig.targetProfileIndex = idx;
                this.saveRuntimeConfig();
                window.localStorage.setItem('block_active_profile_idx', idx.toString());
                if (window.toast) window.toast(`Switched profile to '${this.serverProfiles[idx].name}'`, 1500);
            }
        }
    }

    async openRepl() {
        const langs = BLOCK_LANGUAGE_CATALOG.map(item =>
            `${item.tag === this.runtimeConfig.language ? '✔ ' : ''}${item.label} (<${item.tag}>)`
        );
        const sel = await window.acode.select('Select REPL Language', langs);
        if (!sel) return;

        const selectedLanguage = BLOCK_LANGUAGE_CATALOG.find(item => sel.includes(`${item.label} (<${item.tag}>)`));
        const tag = selectedLanguage ? selectedLanguage.tag : this.runtimeConfig.language;
        const defaultCode = selectedLanguage ? selectedLanguage.defaultCode : BLOCK_LANGUAGE_CATALOG[0].defaultCode;

        const codeSnippet = await window.acode.prompt(`Enter code for <${tag}>`, defaultCode);
        if (!codeSnippet) return;

        const fullBlock = `<${tag}>\n${codeSnippet}\n</${tag}>`;
        this.executeCodeDirect(fullBlock);
    }

    runDiagnostics() {
        const code = editorManager.editor.getValue();
        const lines = code.split('\n');
        let errors = [];
        let openTags = [];
        const blockTagPattern = '(py|python|js|javascript|ps|powershell|sql|lua|php|ruby|rb|html|json|c|cpp|c\\+\\+|go|golang|rust|rs|java|ts|typescript|cs|csharp|kotlin|kt|dart|zig|perl|pl|bash|sh|r|del|server|route|import|use|define)';

        lines.forEach((line, idx) => {
            const trimmed = line.trim();
            const match = trimmed.match(new RegExp('^<(\\/)?' + blockTagPattern + '>$', 'i'));
            if (match) {
                const isClosing = match[1] !== undefined;
                const tag = match[2].toLowerCase();
                if (!isClosing) {
                    openTags.push({ tag, line: idx + 1 });
                } else {
                    if (openTags.length === 0) {
                        errors.push(`Line ${idx + 1}: Closing tag </${tag}> has no matching opening tag.`);
                    } else {
                        const last = openTags.pop();
                        if (last.tag !== tag) {
                            errors.push(`Line ${idx + 1}: Unmatched closing tag </${tag}> (expected </${last.tag}> opened at Line ${last.line}).`);
                        }
                    }
                }
            }
        });

        openTags.forEach(item => {
            errors.push(`Line ${item.line}: Unclosed opening tag <${item.tag}>.`);
        });

        if (errors.length === 0) {
            window.acode.alert('Syntax Check Passed', '✅ All polyglot block tags are balanced and valid!');
        } else {
            const html = `<div style="color:#ef4444; font-family:monospace; text-align:left;">${errors.join('<br>')}</div>`;
            window.acode.alert('Syntax Warnings', html);
        }
    }

    formatCode() {
        let code = editorManager.editor.getValue();
        // Auto fix legacy tag names
        code = code.replace(/<python>/gi, '<py>').replace(/<\/python>/gi, '</py>')
                   .replace(/<javascript>/gi, '<js>').replace(/<\/javascript>/gi, '</js>')
                   .replace(/<powershell>/gi, '<ps>').replace(/<\/powershell>/gi, '</ps>');

        const lines = code.split('\n');
        let formatted = [];
        let indent = 0;
        const blockTagPattern = '(py|python|js|javascript|ps|powershell|sql|lua|php|ruby|rb|html|json|c|cpp|c\\+\\+|go|golang|rust|rs|java|ts|typescript|cs|csharp|kotlin|kt|dart|zig|perl|pl|bash|sh|r|del|server|route|import|use|define)';

        lines.forEach(line => {
            let trimmed = line.trim();
            const closing = trimmed.match(new RegExp('^<\\/\\s*' + blockTagPattern + '\\s*>$', 'i'));
            const opening = trimmed.match(new RegExp('^<\\s*' + blockTagPattern + '(?:\\s+[^>]*)?\\s*>$', 'i'));
            if (closing) {
                indent = Math.max(0, indent - 1);
                formatted.push('  '.repeat(indent) + trimmed);
            } else if (opening) {
                formatted.push('  '.repeat(indent) + trimmed);
                indent++;
            } else {
                // Preserve the embedded language exactly. In particular, Python
                // indentation is semantic and must never be reconstructed here.
                formatted.push(line);
            }
        });

        editorManager.editor.setValue(formatted.join('\n'));
        if (window.toast) window.toast('Auto-fixed aliases & formatted!', 1500);
    }

    exportLog() {
        if (!this.lastOutput) {
            window.acode.alert('Export Log', 'No execution log available yet. Run a script first!');
            return;
        }
        window.acode.prompt('Log Output', this.lastOutput);
    }

    showAbout() {
        const html = `
            <div style="text-align: center;">
                <img src="${svgIconUrl}" style="width: 80px; height: 80px; border-radius: 16px; margin-bottom: 12px; box-shadow: 0 4px 12px rgba(0,0,0,0.4);">
                <h3 style="margin: 0; color: #fff; font-family: sans-serif;">Block Engine</h3>
                <p style="margin: 6px 0 12px 0; color: #10b981; font-size: 14px; font-weight: bold;">v${BLOCK_PLUGIN_VERSION}</p>
                <div style="color: #aaa; font-size: 12px; text-align: left; background: #18181b; padding: 12px; border-radius: 8px; border: 1px solid #27272a; line-height: 1.6;">
                    • <strong>Multi-Profile Manager</strong>: Switch between Dev / Cloud PCs<br>
                    • <strong>Interactive REPL Playground</strong>: Test code snippets on mobile<br>
                    • <strong>Auto-Close Tags & Keyboard Toolbar</strong>: Fast touch typing<br>
                    • <strong>Performance Visualizer</strong>: Latency & execution timing<br>
                    • <strong>HTML Live Renderer</strong>: Mobile web preview tab<br>
                    • <strong>Auto-Fixer & Diagnostics</strong>: Auto alias conversion
                </div>
            </div>
        `;
        window.acode.alert('About Extension', html);
    }

    async runBlockCode() {
        const code = editorManager.editor.getValue();
        this.executeCodeDirect(code);
    }

    async executeCodeDirect(code) {
        const prof = this.getActiveProfile();
        const startTime = Date.now();
        const language = this.getLanguageConfig();
        const engine = this.getEngineConfig();
        this.lastExecutionStats = {
            language: language.label,
            tag: language.tag,
            engine: engine.label,
            target: prof ? prof.name : 'No target',
            timeoutMs: this.runtimeConfig.timeoutMs,
            networkBlocked: this.runtimeConfig.networkBlocked,
            elapsed: 0
        };

        if (!prof || !this.isAllowedServerUrl(prof.url)) {
            this.showPanelOutput('❌ Security Error: use HTTPS, or HTTP only for localhost/127.0.0.1.', 'error', 0);
            return;
        }

        if (typeof code !== 'string' || code.length > MAX_CODE_CHARS) {
            this.showPanelOutput('❌ Security Error: code input exceeds the 2 MiB safety limit.', 'error', 0);
            return;
        }

        let timeout = null;
        try {
            let sessionToken = prof.token;
            if (!sessionToken) {
                sessionToken = await window.acode.prompt('X-Api-Token (kept for this session only)', '');
                if (!sessionToken) {
                    this.showPanelOutput('❌ Authentication required: enter the token printed by "block serve".', 'error', 0);
                    return;
                }
                this.sessionTokens.set(this.profileKey(prof), sessionToken);
            }
            const headers = {
                'Content-Type': 'text/plain',
                'X-Block-Engine': engine.id,
                'X-Block-Timeout-Ms': String(this.runtimeConfig.timeoutMs),
                'X-Block-Network-Blocked': this.runtimeConfig.networkBlocked ? '1' : '0'
            };
            headers['X-Api-Token'] = sessionToken;

            const controller = new AbortController();
            timeout = setTimeout(() => controller.abort(), this.runtimeConfig.timeoutMs);
            const response = await fetch(prof.url, {
                method: 'POST',
                headers: headers,
                body: code,
                signal: controller.signal
            });

            const elapsed = Date.now() - startTime;
            this.lastExecutionStats = {
                language: language.label,
                tag: language.tag,
                engine: engine.label,
                target: prof.name,
                timeoutMs: this.runtimeConfig.timeoutMs,
                networkBlocked: this.runtimeConfig.networkBlocked,
                elapsed
            };

            if (!response.ok) {
                let errorMsg = 'Server status ' + response.status;
                try {
                    const errResult = await readJsonResponseLimited(response);
                    if (errResult && errResult.error) errorMsg = errResult.error;
                } catch (_) {}
                if (response.status === 403) {
                    errorMsg += '\nThe session token was rejected. Copy the current token printed by "block serve".';
                }
                this.showPanelOutput(`❌ Execution Error (${response.status}):\n${errorMsg}`, 'error', elapsed);
                return;
            }

            const result = await readJsonResponseLimited(response);
            
            if (result.status === 'success') {
                const output = (result.output != null) ? result.output : '(Code executed successfully with zero stdout output)';
                this.lastOutput = output;
                this.showPanelOutput(output, 'success', elapsed);
            } else {
                const errorText = (result.error != null) ? result.error : 'Unknown execution failure';
                this.showPanelOutput(`❌ Execution Failed:\n${errorText}`, 'error', elapsed);
            }
        } catch (e) {
            this.showPanelOutput(`🔌 Connection Failed:\nCould not reach ${prof.url} (${prof.name}).\nIs the PC engine running ('block serve')?\n\nDetails: ${e.message}`, 'error', 0);
        } finally {
            if (timeout) clearTimeout(timeout);
        }
    }

    showPanelOutput(content, status, elapsed) {
        let panel = document.getElementById('block-console-panel');
        if (!panel) {
            panel = document.createElement('div');
            panel.id = 'block-console-panel';
            panel.className = 'block-panel-modal';
            panel.innerHTML = `
                <div class="block-panel-header">
                    <div class="block-panel-tabs">
                        <span class="block-tab active" id="tab-output" onclick="window.acodeBlockPlugin.switchTab('output')">Console Output</span>
                        <span class="block-tab" id="tab-html" onclick="window.acodeBlockPlugin.switchTab('html')">HTML Render</span>
                        <span class="block-runtime-chip" id="block-runtime-chip"></span>
                    </div>
                    <div>
                        <button class="panel-act-btn" onclick="window.acodeBlockPlugin.exportLog()">Export Log</button>
                        <button style="background:none; border:none; color:#ef4444; font-size:18px; cursor:pointer; margin-left:10px;" onclick="window.acodeBlockPlugin.closePanel()">✕</button>
                    </div>
                </div>
                <div class="block-perf-bar">
                    <div class="perf-net" id="block-perf-net" style="width:30%;"></div>
                    <div class="perf-exec" id="block-perf-exec" style="width:70%;"></div>
                </div>
                <div class="block-runtime-summary" id="block-runtime-summary"></div>
                <div class="block-panel-body" id="block-panel-body"></div>
                <div class="block-panel-footer">
                    <span id="block-panel-status">Status: Success</span>
                    <span id="block-panel-time">Time: 0ms</span>
                </div>
            `;
            document.body.appendChild(panel);
        }

        window.acodeBlockPlugin = this;
        panel.style.display = 'flex';

        const body = document.getElementById('block-panel-body');
        const statusSpan = document.getElementById('block-panel-status');
        const timeSpan = document.getElementById('block-panel-time');
        const perfNet = document.getElementById('block-perf-net');
        const perfExec = document.getElementById('block-perf-exec');
        const runtimeSummary = document.getElementById('block-runtime-summary');
        const stats = this.lastExecutionStats || {
            language: this.getLanguageConfig().label,
            tag: this.getLanguageConfig().tag,
            engine: this.getEngineConfig().label,
            target: this.getActiveProfile() ? this.getActiveProfile().name : 'No target',
            timeoutMs: this.runtimeConfig.timeoutMs,
            networkBlocked: this.runtimeConfig.networkBlocked
        };
        const networkShare = Math.min(70, Math.max(18, Math.round((elapsed || 0) / Math.max(stats.timeoutMs, elapsed || 1) * 100)));

        body.innerText = content;
        body.style.color = status === 'error' ? '#ef4444' : '#f4f4f5';
        statusSpan.innerText = status === 'error' ? '🔴 Execution Error' : '🟢 Success';
        timeSpan.innerText = `Total: ${elapsed}ms`;
        if (perfNet) perfNet.style.width = `${networkShare}%`;
        if (perfExec) perfExec.style.width = `${100 - networkShare}%`;
        if (runtimeSummary) {
            runtimeSummary.innerHTML = '';
            const rows = [
                ['Language', `${stats.language} <${stats.tag}>`],
                ['Engine', stats.engine],
                ['Target', stats.target],
                ['Budget', `${stats.timeoutMs}ms`],
                ['Network', stats.networkBlocked ? 'Advisory guard' : 'Allowed']
            ];
            rows.forEach(([label, value]) => {
                const cell = document.createElement('span');
                const labelNode = document.createElement('span');
                labelNode.innerText = `${label} `;
                const valueNode = document.createElement('strong');
                valueNode.innerText = value;
                cell.appendChild(labelNode);
                cell.appendChild(valueNode);
                runtimeSummary.appendChild(cell);
            });
        }
        this.updateRuntimeConfigButton();
    }

    switchTab(tab) {
        const body = document.getElementById('block-panel-body');
        const tabOut = document.getElementById('tab-output');
        const tabHtml = document.getElementById('tab-html');

        if (tab === 'output') {
            tabOut.classList.add('active');
            tabHtml.classList.remove('active');
            body.innerText = this.lastOutput || '(No console output)';
        } else if (tab === 'html') {
            tabHtml.classList.add('active');
            tabOut.classList.remove('active');
            
            const code = editorManager.editor.getValue();
            const match = code.match(/<html>([\s\S]*?)<\/html>/i);
            if (match) {
                const iframe = document.createElement('iframe');
                iframe.style.width = '100%';
                iframe.style.height = '100%';
                iframe.style.border = 'none';
                // Security Fix: Sandbox the iframe to isolate preview DOM from Acode host origin & localStorage
                iframe.setAttribute('sandbox', 'allow-scripts allow-modals');
                iframe.srcdoc = match[1];
                body.innerHTML = '';
                body.appendChild(iframe);
            } else {
                body.innerText = 'No <html>...</html> block found in current document to render.';
            }
        }
    }

    previewHtml() {
        this.showPanelOutput('', 'success', 0);
        this.switchTab('html');
    }

    closePanel() {
        const panel = document.getElementById('block-console-panel');
        if (panel) panel.style.display = 'none';
    }

    async destroy() {
        editorManager.off('switch-file', this._onFileUpdate);
        editorManager.off('rename-file', this._onFileUpdate);
        if (editorManager.editor) editorManager.editor.off('change', this._onEditorChange);
        
        try {
            editorManager.editor.commands.removeCommand('Run Block Engine Code');
            editorManager.editor.commands.removeCommand('Format Block Code');
        } catch (_) {}

        const style = document.getElementById('block-plugin-style');
        if (style) style.remove();
        const quickBar = document.getElementById('block-quick-bar');
        if (quickBar) quickBar.remove();
        this.closePanel();
        if (this.$btn.parentElement) this.$btn.parentElement.removeChild(this.$btn);
        if (this.$runBtn.parentElement) this.$runBtn.parentElement.removeChild(this.$runBtn);
    }
}

if (window.acode) {
    const acodePlugin = new BlockPlugin();
    acode.setPluginInit(pluginId, function(baseUrl, $page, options) {
        acodePlugin.baseUrl = baseUrl;
        acodePlugin.init($page);
    });
    
    acode.setPluginUnmount(pluginId, function() {
        acodePlugin.destroy();
    });
}
