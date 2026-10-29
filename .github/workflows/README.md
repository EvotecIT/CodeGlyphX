# CI coverage

Pull requests run the .NET 8 and .NET 10 suites on Linux and Windows, plus the
.NET Framework suite on Windows. Pushes to the default branch and manual CI
runs also run both modern frameworks on macOS. Existing scheduled workflows
keep their original triggers and coverage.

Each PR-triggered workflow cancels only superseded runs for the same PR. Other
events use a unique run group. Deployment and maintenance workflows keep their
existing serialization.

`changes.yml` selects library and website coverage from the complete PR diff.
Documentation and website presentation changes skip library tests, package
contracts and CodeQL. Website changes still build the website. Generated
catalogs, schemas, packaged Markdown, source files and shipped artwork used by
scan tests keep library validation enabled. An empty or unreadable diff never
produces a successful documentation-only result.

The `Build & Test` platform gates, `Package & NativeAOT contracts` and `Analyze`
keep their existing check names. They pass an intentional documentation-only
skip and fail if selection or selected validation fails or is cancelled. The
macOS-named gate on a PR confirms the selected Linux/Windows matrix; actual
macOS suites run on default-branch pushes and manual CI runs.

Dependabot checks NuGet and GitHub Actions weekly, with one group and one open
version-update PR per ecosystem. There are no npm manifests to maintain here.

xUnit already runs independent test collections across the available CPU
threads. The scanner and global-state collections retain their explicit serial
execution; increasing their concurrency needs separate timing and stability
evidence.
