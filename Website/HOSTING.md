# Website hosting

CodeGlyphX uses GitHub Pages behind Cloudflare at `https://codeglyphx.com`.
The PowerForge pipeline builds the documentation, API reference, search data,
release listings, and browser playground into `Website/_site`.
The playground runs entirely in the browser through Blazor WebAssembly.
Hosting does not require a server process, database, or OVH build timer.

The `Deploy Website to GitHub Pages` workflow publishes changes from `master`
and refreshes release listings when a GitHub release is published. The shared
PowerForge workflow builds the site and uploads the Pages artifact. GitHub's
Pages action deploys it through the protected `github-pages` environment.
A separate job in the protected `production` environment applies the Cloudflare
cache and response-header policy from `site.json` and purges the hostname.
The Cloudflare write credential is available only to that post-deployment job.

GitHub Pages must use the Actions publishing source with `codeglyphx.com` as
its custom domain. Cloudflare routes the apex and `www` to `evotecit.github.io`
with proxying enabled. Keep SSL mode at Full (strict) once GitHub has issued
the custom-domain certificate. Keep the verification and mail-policy TXT records.
The `production` environment variable `CLOUDFLARE_ZONE_ID` selects the zone; the existing
organization secret `CLOUDFLARE_API_TOKEN` supplies the policy credential.

To recover a site regression, dispatch the deployment workflow at a known good
source revision with the same static-site layout, or revert the affected change
on `master`. A failed build leaves the previous Pages deployment available.
Check the home page, `/docs/`, `/api/`, `/playground/`, `/sitemap.xml`, and a real
playground generation after deployment. Check `www` redirects to the apex.

During the hosting migration, retain the previous OVH release and configuration
as a rollback snapshot until Pages HTTPS and the browser playground are verified.
Only then disable the CodeGlyphX pull and backup timers and Apache virtual hosts.
Shared OVH services and other products are independent of this migration.
