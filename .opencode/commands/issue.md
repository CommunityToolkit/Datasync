# Issue Workflow

Use this workflow to implement a GitHub issue from `CommunityToolkit/Datasync` with a single-responsibility pull request.  You are operating in a fork: `adrianhall/CommunityToolkit-Datasync` that is used to create PRs.

## Inputs

Expected input is a GitHub issue number, for example:

```text
42
```

If no issue number is provided, ask for one before doing any work.

## Workflow

1. Load project context.

   Review the entire project, including [README.md](./README.md), and documentation in the `docs` before continuing.

2. Inspect the issue.

   Run:

   ```sh
   gh issue view ISSUE_NUMBER --comments --json number,title,body,comments,labels,state,url
   ```

   Summarize the requested outcome, acceptance criteria, files likely to change, and verification commands.

3. Ask clarifying questions.

   Ask concise questions only when implementation would otherwise require guessing. If the issue is clear, state the implementation assumption and continue.

4. Create a branch.

   Before branching, inspect the worktree:

   ```sh
   git status --short
   git log --oneline -10
   ```

   Do not revert or modify unrelated user changes. Create and switch to a branch named for the issue:

   ```sh
   git switch -c issues/ISSUE_NUMBER
   ```

5. Plan the work.

   Create a short task list with implementation, tests, documentation, and verification. Keep the PR scope limited to the issue.

6. Use sub-agents.

   Use a coding sub-agent for implementation when the change touches multiple files or requires design choices. The coding sub-agent must return changed files, key decisions, and verification notes.

   Use a testing sub-agent after implementation to review tests and run or recommend targeted verification. The testing sub-agent must return gaps, failing cases, and commands run.

   Do not duplicate a sub-agent's work while it is running. Continue only with non-overlapping tasks or wait for results.

7. Implement.

   Make the smallest correct change that satisfies the issue and the relevant spec file. Keep handlers, repositories, DTOs, migrations, scripts, and tests aligned with the spec. Use `apply_patch` for manual edits.

8. Test.

   Run targeted tests first, then full verification required by the issue. At minimum, run:

   ```sh
   dotnet restore
   dotnet build
   dotnet test
   ```

   Correct any test or build failures before continuing.

9. Review changes.

   Inspect:

   ```sh
   git status --short
   git diff
   ```

   Confirm only intended files changed. Check for secrets, generated files, account-specific IDs, and accidental edits to unrelated work.

10. Commit.

    Use a conventional commit message scoped to the issue, for example:

    ```sh
    git add INTENDED_FILES
    git commit -m "feat: implement issue summary"
    ```

    Do not amend existing commits unless explicitly asked.

11. Push.

    Push the branch:

    ```sh
    git push -u origin issues/ISSUE_NUMBER
    ```

## Completion Response

When done, report:

- Issue number and branch name.
- Summary of implementation.
- Tests and checks run.
- Any skipped verification with reasons.
- Any follow-up issues discovered.
