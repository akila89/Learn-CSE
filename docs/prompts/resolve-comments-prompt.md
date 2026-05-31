# TASK

Resolve all open review comments on PR `{{PR_NUMBER}}` on branch `{{BRANCH}}`.

# CONTEXT

## Coding standards

@CODING_STANDARDS.md

## Open review threads

!`gh api graphql -f query='{ repository(owner: "{{OWNER}}", name: "{{REPO}}") { pullRequest(number: {{PR_NUMBER}}) { reviewThreads(first: 20) { nodes { id isResolved comments(first: 1) { nodes { body path } } } } } } }'`

## Current branch diff

!`git diff main...{{BRANCH}}`

# PROCESS

1. **Read the comments** — fetch all open review threads above. Understand what each comment is asking for before touching any code.

2. **Read the affected files** — for each comment, read the file at the path indicated. Understand the current state before making changes.

3. **Classify each comment**:
   - **Correctness** — bugs, null dereferences, unsafe assumptions, missing error paths. Always fix.
   - **Standards** — member ordering, blank lines, naming, formatting per CODING_STANDARDS.md. Always fix.
   - **Minor / optional** — flagged as such by the reviewer. Fix unless it introduces more complexity than it removes.
   - **Unclear** — ask the user before touching anything.

4. **Apply fixes** — make the minimal change that addresses the comment. Do not refactor surrounding code. Do not change behaviour unless the comment is a correctness fix.

5. **Add tests** — if a comment requests a new test, write it. Test names must state the expected outcome: `returns_typed_error_not_exception`, not `test_parse_null`.

6. **Verify** — run the full test suite and formatter after all fixes are applied:
   - .NET: run both in `scraper/` — all must pass:
     - `dotnet format --verify-no-changes` (run `dotnet format` to auto-fix)
     - `dotnet test` (0 failures)
   - Angular: run all three in `frontend/` — all must pass:
     - `npm run lint`
     - `npm run format:check`
     - `npm run test -- --watch=false`

7. **Commit** — stage only the files changed to resolve comments. Do not include unrelated modified files. Commit message format:
   ```
   Review: <short summary of what was fixed>
   ```
   Do **not** include a `Co-Authored-By` line.

8. **Push** the branch.

9. **Reply to each comment** — for every thread, post a reply describing exactly what was done. Be specific: name the fix, not just "done" or "fixed".

10. **Resolve each thread** — use the GraphQL mutation after replying:
    ```
    gh api graphql -f query='mutation { resolveReviewThread(input: {threadId: "<id>"}) { thread { isResolved } } }'
    ```

# RULES

- Never resolve a thread without first applying the fix and verifying tests pass.
- If a fix causes a test failure, diagnose and fix the root cause — do not work around it.
- If a comment is ambiguous or the suggested fix conflicts with CODING_STANDARDS.md, ask the user before proceeding.
- Stage only files that changed to address review comments. Do not bundle unrelated modifications.

# DONE

Once all threads are resolved and the branch is pushed, output:

<promise>COMPLETE</promise>
