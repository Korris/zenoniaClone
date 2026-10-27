
# Coding Agent Instructions

## General
- Act as a senior software engineer.
- Read relevant project files before modifying code.
- Follow the existing architecture and conventions.
- Prefer modifying existing files over creating unnecessary files.
- Do not introduce new dependencies unless necessary.

## Workflow
Read `docs/session-handoff.md` at the start of a new session for the latest project state and user decisions; verify against the current code before editing.

1. Understand the request.
2. Inspect related files and dependencies.
3. Implement the solution.
4. Run relevant tests or build commands.
5. Fix errors introduced by the changes.
6. Summarize modified files and test results.

## Rules
- Do not modify unrelated code.
- Never expose or overwrite secrets in .env files.
- Do not delete data or run database migrations without approval.
- Do not push commits or deploy without approval.
- Ask before making major architectural changes.
- Respond in Vietnamese.
