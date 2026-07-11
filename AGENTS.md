# Agent Notes

Guidance for coding agents working in this repository.

## Testing samples

The `samples/` directory contains many platform-specific projects (Blazor WASM,
MAUI, Uno, Avalonia, WPF, WinUI3, MVC, etc.). Most of these require platform
SDKs, mobile workloads, or OS-specific tooling (e.g. Windows, iOS/Android
workloads, WinUI3) that are **not** guaranteed to be installed in an agent's
local environment. Do not assume `dotnet build`/`dotnet workload` for these
projects will succeed locally, and do not spend time trying to install missing
workloads just to validate a samples change.

The proper way to validate a change to anything under `samples/` is:

1. Push your branch to your local fork (`origin`), not upstream.
2. Trigger the `Build Samples` GitHub Actions workflow via `workflow_dispatch`
   against that branch, e.g.:

   ```sh
   git push -u origin <branch>
   gh workflow run "Build Samples" --repo <your-fork> --ref <branch>
   ```

3. Poll the run (`gh run list` / `gh run view <run-id>`) until it completes,
   and review the per-job results — in particular the job(s) relevant to the
   sample(s) you changed, plus the `all-samples-built` aggregation job.

This builds every sample on its correct runner/OS/toolchain in CI, which is
far more reliable than trying to reproduce that locally.

**Timing:** a full `Build Samples` run across all sample projects takes
approximately **6 minutes** end-to-end. Individual jobs (e.g.
`todoapp-blazor-wasm`) typically finish in well under a minute, but slower
jobs (Windows, Uno iOS/maccatalyst) can take several minutes each, and they
run in parallel. Plan polling/wait times accordingly — don't assume the run
is stuck if it's still going after a minute or two; do check back in if it
hasn't completed after ~6-8 minutes.
