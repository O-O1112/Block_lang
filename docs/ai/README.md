# Block AI Knowledge Pack

This directory is the machine-oriented teaching pack for assistants that need
to explain, generate, review, or troubleshoot Block Language.

## Files

- [`SYSTEM-PROMPT.md`](SYSTEM-PROMPT.md) — behavior and accuracy rules for a
  Block-aware assistant.
- [`block-knowledge.md`](block-knowledge.md) — compact canonical facts for
  retrieval-augmented generation (RAG) or prompt context.
- [`training.jsonl`](training.jsonl) — provider-neutral chat examples in JSONL
  format. Each line is one supervised conversation example.
- [`eval-cases.jsonl`](eval-cases.jsonl) — adversarial and regression prompts
  that test whether an assistant has learned the real syntax and boundaries.
- [`PLATFORM-GUIDE.md`](PLATFORM-GUIDE.md) — setup guidance for Gemini, ChatGPT,
  API/RAG systems, and other assistants.

## Recommended teaching order

1. Load `SYSTEM-PROMPT.md` as the assistant policy.
2. Index `block-knowledge.md` and the maintained documentation under `docs/wiki/`.
3. Use `training.jsonl` for few-shot examples or supervised fine-tuning.
4. Run the prompts in `eval-cases.jsonl` against the model and manually review
   the required facts before publishing an answer.

The knowledge pack describes the tested v2.7.1 release. It must be regenerated
or reviewed when the version, syntax, edition behavior, or security model
changes. It is not a substitute for running the Block test suite.

## What this pack deliberately teaches

- Block is a local-first polyglot programming language and execution engine.
- Native-language tags such as `<py>` and `<js>` keep their host syntax.
- There is no `<block>` language tag. Native Block control flow uses a standalone
  `block` terminator.
- Cross-runtime state must be serializable and should be validated again at
  every boundary.
- Host runtimes are installed and managed separately from Block.
- Block files execute local programs; they must not be treated as trusted merely
  because the extension is `.blk`, `.blkl`, or `.blkp`.
