# Governance

Block Language is currently a maintainer-led open-source project.

## Roles

- **Maintainer:** O-O1112 coordinates releases, reviews changes, manages the public
  repository, and is the final decision-maker while the project remains single-
  maintainer.
- **Contributors:** anyone who submits code, documentation, tests, examples, or
  reproducible feedback under the terms described in `CONTRIBUTING.md`.
- **Reviewers:** trusted contributors may be asked to review a focused change; review
  access does not by itself grant release or security authority.

## Decision process

Changes should start with a public issue or pull request unless they are small fixes.
The maintainer evaluates compatibility, security, testing evidence, documentation
impact, and maintenance cost. A change that affects file formats, runtime behavior,
installer behavior, or published URLs must document the compatibility impact.

Security reports follow `SECURITY.md` and are not discussed in public issues until a
coordinated disclosure decision is made.

## Releases

Release-facing changes should pass the Windows build, smoke tests, and release
verification documented in `CONTRIBUTING.md` and `docs/RELEASE-2.2.2.md`. Published
artifacts must retain their documented filenames or provide a compatibility alias.

The changelog is the public record of user-visible changes. Planned features must not
be presented as shipped functionality.

## Changes to governance

As the project gains regular contributors, this document can be updated to define
additional maintainers, review ownership, release delegation, and a formal decision
record. Governance changes should be made through a public pull request.
