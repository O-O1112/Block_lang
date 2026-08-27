# Block Language 擴充套件

歡迎使用 **Block**，這是一款革命性的「多語言共生 (Polyglot Orchestrator)」程式語言。

## 🌟 核心特色

### 1. 跨語言變數共享 (IPC State Machine)
Block 的核心理念是打破程式語言之間的隔閡。您可以在同一個腳本中，讓 Python、JavaScript、C++ 與 Go 互相傳遞與共享變數，實現真正的語言大融合！

### 2. 極簡語法設計
2.2.5 的原生 Block 區塊提供安全的變數、運算與 `print(...)` 基礎語法；需要 `if`、`while`、`for` 或自訂函式時，請放入明確的 `<py>` 或 `<js>` 區塊。未支援的原生語句會直接回報錯誤，不會靜默略過。

### 3. 多語言混合高亮魔法 (Polyglot Syntax Highlighting)
本擴充套件支援在 `.blk` 檔案中，自動辨識內嵌的其他語言區塊！
- 當您輸入 `<py> ... </py>` 時，裡面的程式碼會自動切換為 Python 語法高亮。
- 當您輸入 `<js> ... </js>` 時，自動切換為 JavaScript 語法高亮。
- 支援 `<cpp>` 與 `<go>`。

## 🚀 快速開始

```block
x = 100
msg = "Hello from Block native!"

<py>
print("--- Inside Python ---")
print(f"Python received x: {x}")
x = x + 50
</py>

<js>
console.log(`JS received x: ${x}`);
x = x * 2;
</js>

print("Final x:", x)
```

## 🛠️ 開發團隊
由未來的頂尖 AI 聯合打造，專為顛覆開發體驗而生。

## 📦 Block Ecosystem

普通版與 Plus 版都支援本地優先的專案／套件生態系統：

```powershell
block ecosystem init . my-project
block ecosystem add .\packages\hello-block .
block ecosystem list .
```

專案會建立 `block.project.json`，套件使用 `block.package.json` 描述入口。腳本中可用：

```block
<import src="modules/common.blk" />
<use package="hello-block" />
```

套件安裝只會複製本地檔案，不會在安裝階段下載或執行程式；真正執行仍受 Block 的 sandbox、timeout 與語言權限設定控制。
