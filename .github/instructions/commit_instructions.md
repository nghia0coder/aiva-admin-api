## GitHub Copilot Commit Message Instructions

You are writing **git commit messages** for this repository.

### 1. Always use Conventional Commits

Follow the [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/) specification exactly.  
Commit messages **must** have this structure:

```text
<type>[optional scope]: <short description>

[optional body]

[optional footer(s)]
```

### 2. Allowed commit types

Use one of these lowercase `type` values:

- `feat`: for new features
- `fix`: for bug fixes
- `docs`: for documentation-only changes
- `style`: for formatting, whitespace, or stylistic changes that do not affect behavior
- `refactor`: for code changes that neither fix a bug nor add a feature
- `perf`: for performance improvements
- `test`: for tests (adding, updating, or refactoring tests)
- `build`: for build system or dependency changes
- `ci`: for CI configuration changes
- `chore`: for other changes that don’t modify `src` or tests (e.g., tooling, configs, scripts)

If you are unsure, prefer:

- `feat` when adding visible functionality,
- `fix` when resolving a bug,
- `chore` for purely internal / maintenance work.

### 3. Scopes (recommended)

When possible, add a scope in parentheses after the type to clarify which area is affected.  
Examples of scopes for this repo (use them when they fit, or invent similar ones when needed):

- `api`, `web`, `infrastructure`, `core`, `conversations`, `configs`, `tests`

Format:

```text
feat(conversations): add streaming endpoint for chat responses
fix(web): correct null reference in history endpoint
```

### 4. Descriptions

- Use **imperative, present tense** (e.g., “add”, “fix”, “update”, not “added” or “fixes”).
- Do **not** start with a capital letter after the colon unless it’s a proper noun.
- Do **not** end the summary line with a period.
- Keep the first line short (ideally ≤ 72 characters).

Examples:

- `feat(api): add endpoint to retrieve conversation history`
- `fix(core): handle missing chat response type`
- `refactor(infrastructure): simplify application settings loading`

### 5. Bodies (optional but encouraged)

Add a body when more context is helpful:

- Wrap at roughly 72 characters per line.
- Explain **what** changed and **why**, not the obvious “what” from the diff.
- Mention any important design decisions, trade-offs, or side effects.

Example:

```text
fix(conversations): prevent duplicate messages in history

The query previously joined on an incorrect key, which could result
in duplicate rows for conversations with multiple participants.

Updated the query service to use the conversation id and message
sequence number as the composite key.
```

### 6. Breaking changes

If the change is backward-incompatible, explicitly mark it as a breaking change using **either**:

1. `!` after the type or type/scope, **and/or**
2. A `BREAKING CHANGE:` footer.

Examples:

```text
feat(api)!: require auth token for all history endpoints

BREAKING CHANGE: unauthenticated calls to conversation history
endpoints will now return 401 instead of the previous 200 with
partial data.
```

```text
refactor(core)!: remove legacy chat response type
```

### 7. Footers

Use footers for:

- `BREAKING CHANGE: <description>`
- Issue references (if applicable), e.g. `Refs: #123`, `Fixes: #456`
- Other metadata-style notes.

Format:

```text
<Token>: <value>
```

Example:

```text
fix(web): handle null conversation id on stream start

BREAKING CHANGE: streaming calls without a valid conversation id
will now be rejected instead of silently failing.
Refs: #123
```

### 8. Behavior for multiple kinds of changes

If the diff clearly contains **multiple conceptual changes**, prefer to:

- Assume the developer will split them into multiple commits, and
- Write the message for the **dominant / most important** change.

Do **not** try to describe every small edit in the summary line.

### 9. What you should output

When asked to generate a commit message, output **only** the commit message text, in this format:

```text
type(scope): short description

[optional body]

[optional footer(s)]
```

Do **not** include explanations, commentary, or code snippets in the commit message itself.
