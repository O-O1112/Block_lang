# Block Language organic growth playbook

This project is aimed at developers, automation teams, and technical educators who need one readable entry point for work that crosses language boundaries. The goal is qualified adoption: a visitor should understand the use case, run a small example, and know where to ask for help.

## Positioning

Use one sentence consistently:

> Block is a local-first polyglot engine that lets Python, JavaScript, and other native runtimes share one readable program and state pipeline.

Lead with the problem it solves rather than the runtime count:

- **Data and automation builders:** stop maintaining a chain of glue scripts for one local workflow.
- **Full-stack and tooling developers:** keep each ecosystem where it is strongest while sharing state explicitly.
- **Teachers and learners:** demonstrate several languages in one file without hiding the runtime boundary.

Avoid promising that Block replaces a mature language or bundles every runtime. The host runtime still needs to be installed locally, and untrusted scripts must not be executed.

## The activation funnel

| Stage | Visitor question | Project asset | Success signal |
| --- | --- | --- | --- |
| Discovery | “What is this for?” | Homepage headline and use cases | Qualified page visit |
| Proof | “Can it solve my small problem?” | [`examples/`](../examples/) and the demo GIF | Example opened or copied |
| Activation | “Can I run it now?” | [Downloads](https://o-o1112.github.io/Block_lang/downloads.html), installer, and one-minute README | Download plus first run |
| Trust | “Is it maintained and safe?” | CI badge, release manifest, security policy, and source | GitHub visit, issue, or star |
| Retention | “Where do I go next?” | Documentation, changelog, and issue templates | Repeat visit or contribution |

Keep every announcement linked to one example and one clear next action. A feature list alone rarely creates a first successful run.

## Repeatable content loop

For each release or meaningful fix:

1. Publish one short before/after example showing the user-visible result.
2. Link to the exact file in [`examples/`](../examples/) or add a new focused example.
3. Point readers to the matching documentation chapter and download page.
4. Record the question or failure that users report; turn repeated questions into a troubleshooting entry or test.
5. Update the changelog and README only after the example has been verified.

Good topics include Python-to-JavaScript state transfer, replacing a fragile local automation chain, and comparing Lite, Standard, and Plus. Keep posts technical, reproducible, and useful even when the reader does not install Block.

## Maintainer checklist

- Keep the homepage, README, download page, and release version aligned.
- Verify every public download link after a release.
- Keep at least three examples runnable with the documented commands.
- Add a regression test for every reported installation or execution failure.
- Enable GitHub Issues and Discussions when the project is ready for public support.
- Pin one “start here” discussion or issue that links to the demo, examples, install page, and security policy.
- Review incoming questions monthly and promote the most common answer into the documentation.

## Lightweight metrics

Track only numbers that inform a decision:

- website visits that reach Downloads or Documentation;
- GitHub repository visitors, clones, and stars;
- downloads by edition and extension package;
- first-run failures reported by users;
- time from an issue report to a documented answer.

The first milestone is not raw traffic. It is a new developer completing one example and returning with a real workflow or a useful issue.
