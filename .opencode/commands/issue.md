# Issue Workflow

Use this workflow to implement a GitHub issue from `CommunityToolkit/Datasync` with a single-responsibility pull request.  You are operating in a fork: `adrianhall/CommunityToolkit-Datasync` that is used to create PRs.

## Inputs

This command takes the GitHub issue number as its first argument:

```text
/issue 42
```

The issue number for this run is:

```text
$1
```

If that value is blank, stop and ask the user for an issue number before doing any other work. Every `$1` used later in this workflow refers to that same issue number.

## Workflow

1. Load project context.

   Review the entire project, including [README.md](./README.md), and documentation in the `docs` before continuing.

2. Inspect the issue.

   Run:

   ```sh
   gh issue view $1 --repo CommunityToolkit/Datasync --comments --json number,title,body,comments,labels,state,url
   ```

   Summarize the requested outcome, acceptance criteria, files likely to change, and verification commands.

3. Ask clarifying questions.

   Ask concise questions only when implementation would otherwise require guessing. If the issue is clear, state the implementation assumption and continue.

4. Create a worktree.

   Because multiple issues are often worked on concurrently, do this work in a dedicated git worktree instead of switching branches in the current checkout. This keeps the primary checkout free for other in-progress work and avoids any risk of clobbering unrelated uncommitted changes there.

   Run this from the primary checkout (not from inside another issue's worktree) to compute the deterministic worktree path and check whether it already exists, following the `~/.worktrees/<repo-name>-<issue-number>` convention already used in this repo:

   ```sh
   REPO_NAME=$(basename "$(git rev-parse --show-toplevel)")
   WORKTREE_DIR="$HOME/.worktrees/${REPO_NAME}-$1"
   git worktree list
   ```

   If `$WORKTREE_DIR` already appears in that list, reuse it (resuming earlier work) and skip directly to step 5. Otherwise, create it, reusing the branch if it already exists locally (e.g. left over from a manually removed worktree) instead of failing on `-b`:

   ```sh
   if git show-ref --verify --quiet "refs/heads/issues/$1"; then
     git worktree add "$WORKTREE_DIR" issues/$1
   else
     git fetch origin main
     git worktree add "$WORKTREE_DIR" -b issues/$1 origin/main
   fi
   ```

   From this point forward, run every remaining command — build, test, `git status`/`diff`, commit, push — with `$WORKTREE_DIR` as the working directory, not the original checkout.

5. Plan the work.

   Create a short task list with implementation, tests, documentation, and verification. Keep the PR scope limited to the issue.

6. Implement.

   Make the smallest correct change that satisfies the issue and the relevant spec file, in the worktree. Keep handlers, repositories, DTOs, migrations, scripts, and tests aligned with the spec. Use `apply_patch` for manual edits.

   For simple changes (less than 10 lines of fixed code), perform fixes on the main agent.

   For larger changes, use a coding sub-agent for implementation when the change touches multiple files or requires design choices. Sub-agents start with a fresh context and do not inherit the orchestrator's shell session — always include the absolute worktree path (the resolved value of `$WORKTREE_DIR`, not the variable name) explicitly in the sub-agent's prompt so it edits and runs commands in the right place. The coding sub-agent must return changed files, key decisions, and verification notes. Use a testing sub-agent after implementation, with the same absolute worktree path, to review tests and run or recommend targeted verification. The testing sub-agent must return gaps, failing cases, and commands run.

   Do not duplicate a sub-agent's work while it is running. Continue only with non-overlapping tasks or wait for results.

7. Test.

   Run targeted tests first, then full verification required by the issue, from within the worktree. At minimum, run:

   ```sh
   dotnet restore
   dotnet build
   dotnet test
   ```

   Correct any test or build failures before continuing.

8. Review changes.

   Inspect, from within the worktree:

   ```sh
   git status --short
   git diff
   ```

   Confirm only intended files changed. Check for secrets, generated files, account-specific IDs, and accidental edits to unrelated work.

9. Commit.

    Use a conventional commit message scoped to the issue from within the worktree, for example:

    ```sh
    git add INTENDED_FILES
    git commit -m "feat: implement issue summary"
    ```

    Do not amend existing commits unless explicitly asked.

10. Push.

    Push the branch:

    ```sh
    git push -u origin issues/$1
    ```

11. Validate with CI on the fork, via `workflow_dispatch`.

    `gh workflow run` runs at the remote's default branch unless a ref is given, so `--ref issues/$1` is required — without it you would be validating `main`, not this change.

    Trigger every workflow relevant to what changed (more than one may apply):

    - For library/test changes (`src/**`, `tests/**`), trigger `build-library.yml`.
    - For sample changes (`samples/**`), trigger `build-samples.yml`.
    - For template changes (`templates/**`), trigger `build-template.yml`.

    Do **not** dispatch `build-docs.yml`. Its `deploy` job has no branch guard, so any `workflow_dispatch` — including from a feature branch — publishes straight to the live GitHub Pages site. 

    If none of the changed paths match any of the above, state that no CI workflow applies and continue with local test results only.

    For each workflow triggered, dispatch it, then find and watch the resulting run:

    ```sh
    WORKFLOW_FILE=build-library.yml   # substitute the workflow being run
    gh workflow run "$WORKFLOW_FILE" --repo adrianhall/CommunityToolkit-Datasync --ref issues/$1
    sleep 5
    RUN_ID=$(gh run list --repo adrianhall/CommunityToolkit-Datasync --workflow="$WORKFLOW_FILE" --branch="issues/$1" --limit 1 --json databaseId --jq '.[0].databaseId')
    gh run watch "$RUN_ID" --repo adrianhall/CommunityToolkit-Datasync --exit-status
    ```

    Sample builds can take several minutes per job (they run in parallel); do not assume a run is stuck before ~6-8 minutes. If any run fails, fix the problem (return to step 6) and repeat steps 8-11 before proceeding.

12. If all triggered CI runs succeed, create a PR using the `gh` command for the change against the upstream `CommunityToolkit/Datasync` repository:

    ```sh
    gh pr create --repo CommunityToolkit/Datasync --head adrianhall:issues/$1 --base main --title "feat: implement issue summary" --body "Closes #$1"
    ```

    Follow best practices for conventional commits when creating the body of the PR.  Consider using a body file when quoting is going to be an issue for the shell.

## Worktree Cleanup

Do not remove the worktree as part of this workflow — the branch may still need follow-up commits during PR review.

## Completion Response

When done, report:

- Issue number, branch name, and worktree path.
- Summary of implementation.
- Local tests run and their results.
- CI workflows dispatched, their run URLs, and outcomes (or why none applied).
- Any skipped verification with reasons.
- Any follow-up issues discovered.
