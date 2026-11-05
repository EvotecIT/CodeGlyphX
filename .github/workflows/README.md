# CI coverage

| PR changes | Library tests | Package & NativeAOT | Website build |
| --- | --- | --- | --- |
| Documentation or website presentation only | Skipped | Skipped | Website changes only |
| Library, test or tool sources | Linux .NET 8 and .NET 10, Windows .NET 8 and .NET Framework | Run | Skipped |
| Website, playground, `README.md` or `docs/fair-comparisons.md` with code | As above | Run | Run |
| Shared build inputs (`.github/**`, `Directory.Build.*`, `global.json`, `NuGet.config`, lock files, solutions) | As above plus Windows .NET 10 | Run | Run |

Pushes to the default branch and manual CI runs run every framework on Linux,
Windows and macOS. The Windows solution build on PRs still compiles the website
and playground projects, and library changes rebuild the site on deployment.
Existing scheduled workflows keep their original triggers and coverage.

The .NET 8 and .NET 10 suites on Linux and Windows run in three shards. Most of
their time is the serial image-scanner collection, so the first two shards take
its slowest classes and the third runs everything else, including new tests.
The .NET Framework and macOS suites run in one job each.

Each PR-triggered workflow cancels only superseded runs for the same PR. Other
events use a unique run group. Deployment and maintenance workflows keep their
existing serialization.

`changes.yml` selects library and website coverage from the complete PR diff.
Documentation and website presentation changes skip library tests, package
contracts and CodeQL. Website changes still build the website. Generated
catalogs, schemas, packaged Markdown, source files and shipped artwork used by
scan tests keep library validation enabled. An empty or unreadable diff never
produces a successful documentation-only result.

Changes to `docs/fair-comparisons.md` also build the website because the site
copies that file as a static asset; they do not select library validation.

The `Build & Test` platform gates, `Package & NativeAOT contracts` and `Analyze`
keep their existing check names. They pass an intentional documentation-only
skip and fail if selection or selected validation fails or is cancelled. The
macOS-named gate on a PR confirms the selected Linux/Windows matrix; actual
macOS suites run on default-branch pushes and manual CI runs.

Dependabot checks NuGet and GitHub Actions weekly, with one group and one open
version-update PR per ecosystem. There are no npm manifests to maintain here.

xUnit already runs independent test collections across the available CPU
threads. The scanner and global-state collections retain their explicit serial
execution inside each shard's process; increasing their concurrency needs
separate timing and stability evidence.
