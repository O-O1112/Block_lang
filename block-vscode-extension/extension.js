const vscode = require('vscode');
const { spawn } = require('child_process');

let statusBarItem;
let currentPanel = undefined;
let outputChannel;
let extensionVersion = 'unknown';
const MAX_PROCESS_OUTPUT_CHARS = 1024 * 1024;
const PROCESS_TIMEOUT_MS = 120000;

function terminateProcessTree(child) {
    if (!child || child.killed || !child.pid) return;
    if (process.platform === 'win32') {
        const killer = spawn('taskkill', ['/T', '/F', '/PID', String(child.pid)], {
            shell: false,
            windowsHide: true
        });
        killer.on('error', () => child.kill());
    } else {
        child.kill();
    }
}

function runChildProcess(command, args, cwd, label) {
    if (!vscode.workspace.isTrusted) {
        vscode.window.showErrorMessage('Block execution is disabled until this workspace is trusted.');
        return null;
    }

    const child = spawn(command, args, {
        cwd,
        shell: false,
        windowsHide: true
    });
    let outputChars = 0;
    let outputTruncated = false;
    const appendOutput = data => {
        const text = data.toString();
        const remaining = MAX_PROCESS_OUTPUT_CHARS - outputChars;
        if (remaining <= 0) {
            if (!outputTruncated) {
                outputTruncated = true;
                outputChannel.appendLine('\n[Block] output truncated after 1 MiB.');
            }
            return;
        }
        const chunk = text.slice(0, remaining);
        outputChars += chunk.length;
        outputChannel.append(chunk);
        if (chunk.length < text.length && !outputTruncated) {
            outputTruncated = true;
            outputChannel.appendLine('\n[Block] output truncated after 1 MiB.');
        }
    };

    const timeout = setTimeout(() => {
        if (!child.killed) {
            terminateProcessTree(child);
            outputChannel.appendLine('\n[' + label + '] timed out after ' + PROCESS_TIMEOUT_MS + 'ms.');
        }
    }, PROCESS_TIMEOUT_MS);

    outputChannel.show(true);
    outputChannel.appendLine('[' + label + '] ' + command + ' ' + args.join(' '));
    child.stdout.on('data', appendOutput);
    child.stderr.on('data', appendOutput);
    child.on('error', err => {
        clearTimeout(timeout);
        vscode.window.showErrorMessage('Block process failed to start: ' + err.message);
    });
    child.on('close', code => {
        clearTimeout(timeout);
        outputChannel.appendLine('\n[' + label + '] exited with code ' + code);
    });
    return child;
}

function activate(context) {
    extensionVersion = context.extension && context.extension.packageJSON
        ? context.extension.packageJSON.version
        : 'unknown';
    // 1. Create Status Bar Item
    statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
    outputChannel = vscode.window.createOutputChannel('Block Engine');
    context.subscriptions.push(outputChannel);
    statusBarItem.command = 'block.runScript';
    context.subscriptions.push(statusBarItem);

    // 2. Register CodeLens Provider for Block Tags
    context.subscriptions.push(
        vscode.languages.registerCodeLensProvider({ language: 'block' }, new BlockCodeLensProvider())
    );

    // 3. Register Commands
    const runScriptCmd = vscode.commands.registerCommand('block.runScript', () => runEngineCommand('auto'));
    const runLiteCmd = vscode.commands.registerCommand('block.runLite', () => runEngineCommand('block-lite'));
    const runPlusCmd = vscode.commands.registerCommand('block.runPlus', () => runEngineCommand('block-plus'));
    
    const formatCmd = vscode.commands.registerCommand('block.formatScript', formatBlockCode);
    const diagCmd = vscode.commands.registerCommand('block.checkDiagnostics', runDiagnostics);
    const previewHtmlCmd = vscode.commands.registerCommand('block.previewHtml', previewHtmlRender);
    const initProjectCmd = vscode.commands.registerCommand('block.initProject', () => runEcosystemCommand(['init']));
    const listPackagesCmd = vscode.commands.registerCommand('block.listPackages', () => runEcosystemCommand(['list']));
    const addPackageCmd = vscode.commands.registerCommand('block.addPackage', addLocalPackage);

    context.subscriptions.push(runScriptCmd, runLiteCmd, runPlusCmd, formatCmd, diagCmd, previewHtmlCmd,
        initProjectCmd, listPackagesCmd, addPackageCmd);

    // 4. Register Editor Active File Change Listener for Status Bar
    context.subscriptions.push(
        vscode.window.onDidChangeActiveTextEditor(updateStatusBar)
    );
    updateStatusBar();
}

function updateStatusBar() {
    const editor = vscode.window.activeTextEditor;
    if (editor && editor.document.languageId === 'block') {
        const fileName = editor.document.fileName;
        let edition = 'Standard';
        if (fileName.endsWith('.blkl')) edition = 'Lite';
        if (fileName.endsWith('.blkp')) edition = 'Plus (Flagship)';
        
        statusBarItem.text = `$(rocket) Block v${extensionVersion} [${edition}]`;
        statusBarItem.tooltip = 'Click to run current Block script';
        statusBarItem.show();
    } else {
        statusBarItem.hide();
    }
}

function runEngineCommand(targetMode) {
    const editor = vscode.window.activeTextEditor;
    if (!editor) {
        vscode.window.showErrorMessage('No active Block script to run!');
        return;
    }

    const document = editor.document;
    if (document.languageId !== 'block') {
        vscode.window.showErrorMessage('This is not a Block script!');
        return;
    }

    document.save().then(() => {
        const filePath = document.fileName;
        const config = vscode.workspace.getConfiguration('block');
        let customExe = config.get('customEnginePath');

        let cmd = customExe && customExe.trim() ? customExe.trim() : 'block';
        
        if (targetMode === 'block-lite') {
            cmd = 'block-lite';
        } else if (targetMode === 'block-plus') {
            cmd = 'block-plus';
        } else if (targetMode === 'auto') {
            const lowerPath = filePath.toLowerCase();
            if (lowerPath.endsWith('.blkl') || lowerPath.endsWith('.blocklite')) {
                cmd = 'block-lite';
            } else if (lowerPath.endsWith('.blkp') || lowerPath.endsWith('.blockplus')) {
                cmd = 'block-plus';
            }
        }

        runChildProcess(cmd, [filePath], require('path').dirname(filePath), 'Block');
    }).catch(err => vscode.window.showErrorMessage('Unable to save Block script: ' + err.message));
}

function resolveEngineCommand(document) {
    const config = vscode.workspace.getConfiguration('block');
    const customExe = config.get('customEnginePath');
    if (customExe && customExe.trim()) return customExe.trim();
    const lowerPath = document.fileName.toLowerCase();
    return (lowerPath.endsWith('.blkp') || lowerPath.endsWith('.blockplus')) ? 'block-plus' :
        (lowerPath.endsWith('.blkl') || lowerPath.endsWith('.blocklite')) ? 'block-lite' : 'block';
}

function runEcosystemCommand(ecosystemArgs) {
    const editor = vscode.window.activeTextEditor;
    if (!editor) {
        vscode.window.showErrorMessage('Open a Block script inside a workspace first.');
        return;
    }
    const document = editor.document;
    const workspaceFolder = vscode.workspace.getWorkspaceFolder(document.uri);
    const projectRoot = workspaceFolder ? workspaceFolder.uri.fsPath : require('path').dirname(document.fileName);
    const cmd = resolveEngineCommand(document);
    runChildProcess(cmd, ['ecosystem'].concat(ecosystemArgs || []).concat([projectRoot]), projectRoot, 'Block Ecosystem');
}

async function addLocalPackage() {
    if (!vscode.workspace.isTrusted) {
        vscode.window.showErrorMessage('Block package operations are disabled until this workspace is trusted.');
        return;
    }
    const selected = await vscode.window.showOpenDialog({
        canSelectFiles: false,
        canSelectFolders: true,
        canSelectMany: false,
        openLabel: 'Add Block Package'
    });
    if (!selected || selected.length === 0) return;

    const editor = vscode.window.activeTextEditor;
    if (!editor) return;
    const workspaceFolder = vscode.workspace.getWorkspaceFolder(editor.document.uri);
    const projectRoot = workspaceFolder ? workspaceFolder.uri.fsPath : require('path').dirname(editor.document.fileName);
    runEcosystemCommand(['add', selected[0].fsPath, projectRoot]);
}

function formatBlockCode() {
    const editor = vscode.window.activeTextEditor;
    if (!editor) return;

    const document = editor.document;
    let code = document.getText();

    const lines = code.split('\n');
    let formatted = [];
    let inBlock = false;

    lines.forEach(line => {
        let trimmed = line.trim();
        const isClosingTag = /^<\/\s*(py|python|js|javascript|ps|powershell|sql|lua|php|ruby|html|json|del|server|route|import|define)\s*>$/i.test(trimmed);
        const isOpeningTag = /^<\s*(py|python|js|javascript|ps|powershell|sql|lua|php|ruby|html|json|del|server|route|import|define)(?:\s+[^>]*)?>$/i.test(trimmed);

        if (isClosingTag) {
            inBlock = false;
            formatted.push(trimmed);
        } else if (isOpeningTag) {
            inBlock = true;
            formatted.push(trimmed);
        } else {
            // Inside code block: preserve relative indentation without destroying Python / YAML indentation
            formatted.push(line);
        }
    });

    const fullRange = new vscode.Range(
        document.positionAt(0),
        document.positionAt(document.getText().length)
    );

    editor.edit(editBuilder => {
        editBuilder.replace(fullRange, formatted.join('\n'));
    });

    vscode.window.showInformationMessage('Polyglot Block code structure formatted successfully!');
}

function runDiagnostics() {
    const editor = vscode.window.activeTextEditor;
    if (!editor) return;

    const document = editor.document;
    const text = document.getText();
    const lines = text.split('\n');
    let errors = [];
    let openTags = [];

    lines.forEach((line, idx) => {
        const trimmed = line.trim();
        const match = trimmed.match(/^<(\/)?\s*(py|python|js|javascript|ps|powershell|sql|lua|php|ruby|html|json|del|server|route)\b[^>]*>$/i);
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
        vscode.window.showInformationMessage('✅ Syntax Check Passed: All polyglot block tags are balanced!');
    } else {
        vscode.window.showErrorMessage(`⚠️ Syntax Warnings:\n${errors.join('\n')}`);
    }
}

function previewHtmlRender() {
    const editor = vscode.window.activeTextEditor;
    if (!editor) return;

    const text = editor.document.getText();
    const match = text.match(/<html>([\s\S]*?)<\/html>/i);
    if (!match) {
        vscode.window.showWarningMessage('No <html>...</html> block found in current document!');
        return;
    }

    const htmlContent = match[1];

    if (currentPanel) {
        currentPanel.reveal(vscode.ViewColumn.Beside);
    } else {
        currentPanel = vscode.window.createWebviewPanel(
            'blockHtmlPreview',
            'Block HTML Preview',
            vscode.ViewColumn.Beside,
            { enableScripts: false }
        );

        currentPanel.onDidDispose(() => {
            currentPanel = undefined;
        }, null);
    }

    currentPanel.webview.html = `<!doctype html><html><head><meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data:; style-src 'unsafe-inline';"></head><body>${htmlContent}</body></html>`;
}

// CodeLens Provider for Inline Run Buttons above Tags
class BlockCodeLensProvider {
    provideCodeLenses(document, token) {
        if (!vscode.workspace.getConfiguration('block').get('enableCodeLens', true)) {
            return [];
        }
        const lenses = [];
        const text = document.getText();
        const regex = /^<\s*(py|python|js|javascript|ps|powershell|sql|lua|php|ruby|html|json|del|server|route)\b[^>]*>$/gim;
        let match;

        while ((match = regex.exec(text)) !== null) {
            const line = document.positionAt(match.index).line;
            const range = new vscode.Range(line, 0, line, match[0].length);
            const tag = match[1];

            if (!tag.startsWith('/')) {
                lenses.push(
                    new vscode.CodeLens(range, {
                        title: `▶ Run Script (${tag.toUpperCase()})`,
                        command: 'block.runScript'
                    })
                );
            }
        }
        return lenses;
    }
}

function deactivate() {
    if (statusBarItem) statusBarItem.dispose();
    if (outputChannel) outputChannel.dispose();
}

module.exports = {
    activate,
    deactivate
};
