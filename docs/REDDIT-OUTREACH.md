# Reddit outreach plan

Reddit posts must be community-specific. This plan intentionally uses different topics and wording for each subreddit instead of cross-posting one advertisement.

## Ready for the first batch

### 1. r/opensource — independent validation

**Title:** `Seeking independent validation for a local-first polyglot engine`

```text
I am looking for a few independent Windows users to test Block, an open-source local-first polyglot execution engine.

The practical idea is simple: one readable .blk document can combine native control flow with local runtimes such as Python and Node.js, passing serializable state from one stage to the next. The repository includes Windows builds, an installer, examples, source code, and a validation process.

This is not a request for stars. I want to learn where installation, runtime discovery, error messages, or state transfer still fail in real environments. The first test takes 10–15 minutes and does not require building from source.

Tester guide: https://github.com/O-O1112/Block_lang/blob/main/docs/TESTER-PACKET.md
Repository: https://github.com/O-O1112/Block_lang

If you try it, please report the edition, Windows/runtime versions, exact command, expected result, and observed result. Please remove any secrets or private data before sharing logs or screenshots.
```

### 2. r/SideProject — build story and feedback

**Title:** `I built a single-file engine for combining Python, Node.js, and other runtimes`

```text
I have been building Block, a local-first polyglot engine for people who are tired of stitching small scripts together with temporary files and shell glue.

The current v2.7.1 build lets a .blk document contain separate language blocks and pass serializable state between them. The project has Lite, Standard, and Plus editions, Windows binaries, an installer, examples, and editor plugins.

The part I am unsure about is the first-run experience. Can a developer understand the editions, install the right package, and run an example without asking me for help?

I am looking for a few people to spend 10–15 minutes trying it on Windows and telling me exactly where the experience becomes confusing. A native example is enough; Python/Node.js and installer tests are optional.

Test instructions: https://github.com/O-O1112/Block_lang/blob/main/docs/TESTER-PACKET.md
Source and releases: https://github.com/O-O1112/Block_lang

Specific feedback about friction or missing pieces is more useful than a general positive reaction.
```

## Technical post to hold until rules are checked again

### 3. r/ProgrammingLanguages — language/runtime design discussion

**Title:** `Design review: should a polyglot document use tags as an explicit execution boundary?`

```text
I am working on Block, a small polyglot execution engine where a document can contain explicit blocks such as <py>, <js>, <json>, and native control-flow sections.

The design question is about the boundary between languages. Each block runs in its own local runtime, then serializable values are passed to the next block. This makes the orchestration readable, but it also raises questions about type loss, error locations, runtime startup cost, and whether implicit shared state is too surprising.

For people who have designed or implemented languages: what invariants would you require for a system like this to remain predictable? Would you prefer explicit input/output declarations, a typed intermediate representation, or a smaller set of built-in values?

The implementation is open source if code is useful for context, but I am mainly looking for design criticism rather than promotion: https://github.com/O-O1112/Block_lang
```

Do not publish this until the current community rules and recent posts confirm that the topic is allowed. Do not hide the project's human–AI collaboration history if a community asks about authorship.

## Do not publish yet

- **r/Blazor:** wait for a real, tested Blazor integration example. The previous link-only post was correctly criticized as off-topic.
- **r/programming:** avoid a launch/link post unless there is a substantial technical article that meets the community's current self-promotion rules.
- **r/learnprogramming:** the project is not a beginner question or tutorial by itself; use the tester packet only if a moderator-approved testing thread exists.
- **Runtime-specific communities:** only post when the body contains a genuine example for that runtime, not merely a list of supported tags.

## Posting discipline

- Space posts out; do not publish all communities within a few minutes.
- Read the current rules and recent posts immediately before submitting.
- Never ask for stars as a condition of testing.
- Answer criticism instead of arguing with moderators or commenters.
- Record the URL, timestamp, score, comments, and tester conversions for each post.
