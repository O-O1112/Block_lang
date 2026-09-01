# Third-party validation for Block Language

The purpose of this document is to collect independent, reproducible evidence that Block is useful outside the maintainer's own environment. It is not a testimonial generator and it must not be used to invent users, results, benchmarks, or quotations.

## What counts as evidence?

| Level | Evidence | Public record |
| --- | --- | --- |
| 0 | Maintainer build or smoke test | CI log or release artifact |
| 1 | An external developer runs a documented example | GitHub issue or discussion with version and environment |
| 2 | An external developer uses Block for a real workflow | Public reproduction, repository, demo, or redacted report |
| 3 | An independent tutorial, review, talk, or comparison | Third-party URL with clear authorship |
| 4 | An external contribution, integration, plugin, or package | Pull request, repository, or release note |

Levels 1–4 are third-party evidence. Level 0 is valuable engineering evidence, but it should not be presented as community adoption.

## Minimum report format

Every public report should include:

- Block version and edition;
- operating system and relevant host runtime versions;
- the scenario or example that was tested;
- the command or smallest safe reproduction;
- expected and observed result;
- date of the test;
- a link to the public record, when one exists;
- permission to quote the report, if a quotation is requested.

Do not request or publish passwords, API keys, private source code, personal addresses, private logs, or unredacted filesystem paths. A report can be useful without exposing private data.

## External tester packet

Send testers this short path:

1. Read the [security policy](../SECURITY.md) and only run scripts they trust.
2. Download a release from the [official download page](https://o-o1112.github.io/Block_lang/downloads.html) or use the published repository.
3. Start with one of the [copy-ready examples](../examples/README.md).
4. Record the exact edition, runtime versions, command, output, and any failure.
5. Submit the result through the [validation report form](https://github.com/O-O1112/Block_lang/issues/new?template=validation_report.yml), a GitHub Discussion, or an independent article.

For a Windows release checkout, the maintainer smoke test is:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\Test-BlockEngine.ps1 -EngineDirectory .
```

External testers should not be expected to build from source to provide useful feedback. A clean release artifact and a small example are the preferred first experience.

## What to ask, and what not to ask

Good questions are specific and falsifiable:

- Could you run `hello-polyglot.blk` on your environment?
- Which edition and host runtimes did you use?
- Did the documented output match what you observed?
- What was confusing or unexpectedly difficult?
- Would this solve a real workflow you have today?

Avoid asking people to say that Block is “amazing,” to repeat marketing language, or to hide limitations. Negative feedback is evidence too; it should become a regression test, documentation fix, or clearly recorded limitation.

## 30-day evidence plan

### Week 1 — Reproducible first run

- Recruit three developers with different Python/Node experience levels.
- Give each the same three examples and no private walkthrough.
- Record setup time, first successful command, and the first point of confusion.

### Week 2 — Real workflows

- Ask two testers to adapt an example to a small local automation or data task.
- Capture what they changed, which runtime they needed, and where the state boundary helped or failed.
- Turn repeated failures into tests or documentation issues.

### Week 3 — Independent explanation

- Invite one tester to write their own short tutorial, review, or code sample.
- Do not rewrite their conclusion or require positive wording.
- Link the independent work from the project only with the author's permission.

### Week 4 — Publish the evidence, not a slogan

- Summarize the number of testers, editions, platforms, successful runs, and known failures.
- Link the original public records.
- Separate maintainer test results from external user results.
- Update the roadmap based on what testers actually requested.

## Evidence register

Maintain a small table in release notes or project discussions:

| Date | Level | Edition | Scenario | Platform | Result | Public link | Quote permission |
| --- | --- | --- | --- | --- | --- | --- | --- |
| YYYY-MM-DD | 1 | Standard | `hello-polyglot.blk` | Windows 11 | Pass / Fail | URL | Yes / No |

Do not fill this table with planned tests. An empty row is better than an invented success.

## Independence and conflict of interest

Maintainers may explain the project and invite testing, but they should not present their own statements as independent reviews. When a tester is a friend, contractor, sponsor, or contributor, disclose that relationship. The most credible evidence includes the original context, reproducible steps, and limitations as well as the positive result.

The long-term goal is not to collect praise. It is to make Block easier to evaluate, easier to criticize, and easier for a new maintainer or user to trust.
