# Block external tester packet

Thank you for helping test Block. This is a short, practical test: you do not need to build the engine from source, and honest negative feedback is as useful as a successful run.

## What we are testing

Block is a local-first polyglot engine. A `.blk` file can combine Block-native control flow with supported runtimes such as Python and Node.js, while passing state through one pipeline.

The current target is the v2.2.6 experience on Windows. Please record the exact edition and version you used; Block Lite, Block, and Block+ may have different capabilities.

## Before you start

- Use a release package or an official repository copy from [Block_lang](https://github.com/O-O1112/Block_lang).
- Only run scripts you have inspected and trust. Do not place passwords, tokens, private files, or personal data in test programs.
- A native smoke test needs no optional runtime. The polyglot examples need the runtimes named in the example.
- If Windows blocks the executable, record the exact Windows message instead of bypassing the protection.

## Ten-minute smoke test

Open PowerShell in the extracted or installed Block directory. Replace `block` with the actual executable path if it is not on `PATH`.

```powershell
block --version
block .\examples\native-control-flow.blk
```

If Python and Node.js are installed, continue with:

```powershell
block .\examples\hello-polyglot.blk
block .\examples\local-data-pipeline.blk
```

If you are testing the installer, also try a path containing spaces, for example `C:\Users\Public\Block Test`, and then run the same native example from that installation.

## Choose one deeper test

| Test | What to check | Useful evidence |
| --- | --- | --- |
| Native | A simple Block program runs and prints the expected result | command, output, elapsed time |
| Polyglot | Python and Node.js stages receive the expected values | runtime versions, output, stage order |
| Installer | Selected edition is installed and launches after a clean install | edition, install path, installer message |
| Error handling | A missing file or malformed tag produces a clear, non-crashing error | exact command and full error text |
| Real workflow | A small data, automation, or teaching task works end to end | short description, files used, result |

## What to report

Please include only what is needed to reproduce the result:

1. Block version and edition.
2. Windows version and architecture.
3. Python, Node.js, Ruby, PHP, or other runtime versions used.
4. The scenario and the exact command.
5. Expected result and observed result.
6. Whether the test was a success, limitation, usability issue, or crash.
7. A screenshot or public link only if it contains no private information.

Use the [Volunteer tester issue form](https://github.com/O-O1112/Block_lang/issues/new?template=tester_application.yml) to volunteer, or the [validation report form](https://github.com/O-O1112/Block_lang/issues/new?template=validation_report.yml) to submit results. You can also open a normal bug report when the problem is clearly reproducible.

## What makes a good test report

```text
Block version / edition:
Windows version:
Other runtime versions:
Scenario:
Command:
Expected:
Observed:
Result: success / limitation / usability issue / crash
Reproduction steps:
Public evidence link (optional):
May the project quote this report? yes / no
```

Do not feel pressure to describe Block positively. A clear failure, confusing message, slow step, or missing feature is valuable evidence and will be tracked separately from testimonials.

## Definition of a useful first round

The first round is complete when we have:

- three testers who are not maintainers;
- two different Windows/runtime environments;
- one native test and one polyglot or installer test from each tester;
- at least one real usability observation, whether positive or negative; and
- permission recorded separately before any report is quoted publicly.
