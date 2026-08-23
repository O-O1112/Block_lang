# Teach Block to Gemini, ChatGPT, and other assistants

The Block AI pack is intentionally provider-neutral. Teach every assistant the
same facts and test the same failure cases; only the upload surface changes.

## Files to use

Use these files in this order:

1. `SYSTEM-PROMPT.md` — behavior, terminology, safety boundaries, and response
   rules.
2. `block-knowledge.md` — canonical v2.2.2 reference facts.
3. `docs/wiki/` and `examples/` — deeper reference material and copy-ready code.
4. `training.jsonl` — few-shot or supervised examples, not a replacement for
   the policy.
5. `eval-cases.jsonl` — private tests. Do not treat the expected answers as
   public documentation.

Keep `SYSTEM-PROMPT.md` separate from reference files when a platform exposes
separate instruction and knowledge fields. Rules belong in instructions; facts,
examples, and manuals belong in knowledge.

## ChatGPT

### Custom GPT or workspace GPT

If the account or workspace can create a GPT:

1. Put the contents of `SYSTEM-PROMPT.md` in the GPT's **Instructions** field.
2. Upload `block-knowledge.md` and the most relevant files from `docs/wiki/`
   and `examples/` as **Knowledge**.
3. Add conversation starters such as:
   - `Explain why <block> is not a valid Block tag.`
   - `Review this .blk file for syntax and state-boundary mistakes.`
   - `Diagnose this Windows path-discovery error in Block v2.2.2.`
4. Test the GPT in Preview with every case in `eval-cases.jsonl`.
5. Require the assistant to cite the relevant Block file or section when it
   makes a version-sensitive claim.

OpenAI distinguishes instructions (how the GPT behaves) from knowledge files
(reference material), and recommends testing in Preview after uploading files:
[Creating and editing GPTs](https://help.openai.com/en/articles/8554397-creating-a-gpt).

As of the current official documentation, new GPT creation and publishing may
be unavailable on personal ChatGPT accounts. If the Create option is missing,
use Custom Instructions for a short version of the policy, upload the knowledge
files in a conversation when needed, or use an API/RAG integration instead. Do
not upload private keys, credentials, or unpublished release material.

### API or another hosted assistant

Use `SYSTEM-PROMPT.md` as the system/developer instruction, index
`block-knowledge.md` and the maintained wiki as retrieval documents, and run the
same `eval-cases.jsonl` in CI or a private evaluation job. Keep the v2.2.2
version label in the retrieval metadata so a future release can be evaluated
without silently mixing syntax.

## Gemini

### Gem

In the Gemini web app, create a Gem and:

1. Paste `SYSTEM-PROMPT.md` into the Gem instructions.
2. Add `block-knowledge.md` under Knowledge.
3. Add only the relevant wiki pages and examples; start small so incorrect or
   obsolete material is easy to find.
4. Preview with `eval-cases.jsonl`, especially the `<block>` confusion case,
   native `block` terminators, edition extensions, and security boundaries.
5. Save the Gem only after it answers the cases without inventing syntax.

Google's official Gemini help documents creating a Gem, entering instructions,
adding files under Knowledge, and previewing before saving:
[Use Gems in Gemini Apps](https://support.google.com/gemini/answer/15146780?co=GENIE.Platform%3DDesktop&hl=en).

### One-off Gemini chat

Attach `SYSTEM-PROMPT.md` and `block-knowledge.md` to a new chat, then begin
with this prompt:

> You are learning Block Language v2.2.2. Use the attached policy as behavior
> rules and the attached knowledge file as the canonical reference. Do not
> invent a `<block>` tag. Before answering code questions, distinguish runtime
> tags from native Block control flow and state the edition assumptions.

## Other models

For local or hosted models without a custom-assistant UI:

- put `SYSTEM-PROMPT.md` in the system prompt;
- index `block-knowledge.md`, `docs/wiki/`, and `examples/` in the model's RAG
  store;
- include 2–4 relevant `training.jsonl` examples as few-shot context;
- run all `eval-cases.jsonl` prompts with a fixed temperature and record the
  model, version, date, and pass/fail result;
- never call a behavior “supported” unless the repository documentation or
  tests confirm it.

## Cross-platform acceptance checklist

An assistant is ready to describe Block only when it can:

- reject the nonexistent `<block>` tag and show the standalone `block` terminator;
- write a matching `<py>...</py>` and `<js>...</js>` example;
- explain serializable state and reject sockets or open handles as shared state;
- distinguish `.blkl`, `.blk`, and `.blkp`;
- separate a missing host runtime from a Block parser error;
- explain workspace/project discovery without claiming that Block scans the whole
  drive;
- state that local language blocks can start native programs and are not a full
  operating-system sandbox;
- identify v2.2.2 as the knowledge version.

