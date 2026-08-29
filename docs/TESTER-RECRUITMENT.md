# Block tester recruitment

This page is a ready-to-post campaign for finding a small first group of independent Block testers. The goal is reproducible feedback, not a request for stars or promotional praise.

## The offer

We are looking for 3–5 developers, teachers, students, or automation builders who can spend 10–15 minutes testing Block v2.2.6 on Windows. No source build is required. Testers may choose a native example, a Python/Node.js example, the installer, or a small real workflow.

In return, testers get a direct route to report bugs and confusing behavior, acknowledgement if they want it, and the chance to influence the next release. We will not publish a name, screenshot, quote, or repository link without permission.

## English post

```text
Looking for 3–5 independent testers for Block v2.2.6.

Block is a local-first polyglot programming engine: one readable .blk file can combine native Block control flow with runtimes such as Python and Node.js.

The test takes about 10–15 minutes on Windows. No source build is needed. You can try one of the included examples, the installer, or a small workflow of your own. We want honest reports about setup friction, runtime compatibility, error messages, and missing features—positive or negative.

Tester packet: https://github.com/O-O1112/Block_lang/blob/main/docs/TESTER-PACKET.md
Project: https://github.com/O-O1112/Block_lang

Interested? Comment with your Windows/runtime environment, or open a tester request through the repository. Please do not include secrets or private data.
```

## Traditional Chinese post

```text
正在招募 3–5 位獨立測試者協助測試 Block v2.2.6。

Block 是 local-first 的多語言程式引擎，可以在同一個可讀的 .blk 檔案中，混合 Block 原生控制流程與 Python、Node.js 等 runtime。

測試約需 10–15 分鐘，Windows 使用者即可參加，不需要自行編譯原始碼。你可以跑內建範例、測試安裝器，或用自己的小型資料/自動化流程。我們特別希望知道安裝是否順利、runtime 是否相容、錯誤訊息是否清楚，以及哪些功能還缺少；正面與負面回報都歡迎。

測試說明：https://github.com/O-O1112/Block_lang/blob/main/docs/TESTER-PACKET.md
專案：https://github.com/O-O1112/Block_lang

有興趣可以留言你的 Windows/runtime 環境，或直接在儲存庫提出測試申請。請勿放入密碼、token 或私人資料。
```

## Where to share

Use a small, relevant mix instead of posting everywhere at once:

- GitHub Discussions or the tester issue form: best for structured, reproducible reports.
- DEV.to: best for a longer technical invitation and follow-up report.
- X: best for reaching language/tooling developers with the short English post.
- Relevant programming-language, open-source, education, and automation communities: read each community's rules first and adapt the message to the community.
- Direct invitations: contact a few people whose projects genuinely match Block; personalize the request and give them an easy way to decline.

Do not ask people to star the repository as a condition of testing. Do not mass-message unrelated users. A small number of independent, reproducible reports is more useful than a large number of unverified reactions.

## Recommended publishing order

1. GitHub Discussions: publish the full tester call and keep all structured reports connected to the repository.
2. DEV.to: publish a technical article explaining the problem, the polyglot pipeline, and the ten-minute test.
3. X: publish a short English announcement linking to the tester packet and the repository.
4. Reddit `r/opensource`: publish a project-specific request for independent validation, with enough technical context to start a discussion.

Do not repost the same link-only announcement to every subreddit. `r/Blazor` should wait until Block has a real, tested Blazor integration example. A programming-language community should receive a technical design discussion, not a generic advertisement. Always read the current rules and recent posts immediately before submitting.

## Platform-ready drafts

### GitHub Discussions

Title: `Call for independent Block v2.2.6 testers`

```text
Block v2.2.6 is looking for 3–5 independent testers on Windows.

Block is a local-first polyglot engine. A readable .blk file can combine native Block control flow with local runtimes such as Python and Node.js, passing serializable state between stages.

The first test takes 10–15 minutes and does not require building from source. You can test a native example, a Python/Node.js pipeline, the installer, error handling, or a small workflow of your own.

We are looking for honest evidence about setup friction, runtime compatibility, error messages, and missing features. Positive and negative reports are equally useful, and no star or promotional statement is required.

Tester packet:
https://github.com/O-O1112/Block_lang/blob/main/docs/TESTER-PACKET.md

Please reply with your Windows/runtime environment and the test you would like to try. Do not include secrets or private data.
```

### X

```text
Looking for 3–5 independent testers for Block v2.2.6 on Windows.

Block runs native Block, Python, Node.js and other local runtimes in one readable .blk file. The first test takes 10–15 minutes; no source build required. Honest bug and UX feedback welcome.

Tester packet: https://github.com/O-O1112/Block_lang/blob/main/docs/TESTER-PACKET.md
```

### Reddit `r/opensource`

Title: `Seeking independent validation for a local-first polyglot engine (Block v2.2.6)`

```text
I am looking for 3–5 independent Windows users to test Block v2.2.6, an open-source local-first polyglot execution engine.

The project lets one readable .blk document combine native Block control flow with local runtimes such as Python and Node.js, passing serializable state between stages. The current release includes Lite, Standard, and Plus editions, Windows binaries, examples, an installer, and validation documentation.

The first test takes 10–15 minutes and does not require building from source. I am specifically looking for reproducible feedback about installation, runtime compatibility, error messages, and missing features—not just stars.

Tester packet: https://github.com/O-O1112/Block_lang/blob/main/docs/TESTER-PACKET.md
Repository: https://github.com/O-O1112/Block_lang

If you try it, please report the edition, Windows/runtime versions, exact command, expected result, and observed result. Please do not include secrets or private data.
```

### DEV.to

Suggested title: `I’m looking for independent testers for a local-first polyglot engine`

Use the longer technical introduction from the existing DEV.to draft series, then end with the [external tester packet](TESTER-PACKET.md). Suggested tags are `programming`, `tutorial`, and one runtime-specific tag only when the article contains a real example for that runtime.

## Screening and assignment

Ask each volunteer for only:

- Windows version and architecture;
- installed runtimes, if any;
- preferred test: native, polyglot, installer, error handling, or real workflow;
- approximate time available; and
- whether their report may be summarized anonymously.

Assign the first round across different environments. For example, one tester can run native-only, one can test Python plus Node.js, and one can focus on a clean installer path. Keep maintainer tests separate from external evidence.

## Success criteria

Close the first recruitment round after three independent test reports, not after a fixed number of views or stars. Summarize the results in the validation register, convert reproducible failures into issues, and update the roadmap with what remains unresolved.
