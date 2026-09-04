# Website deployment security

The Block website is static. It must not collect passwords, API keys, identity
documents, or other sensitive form data.

The source and deployment checks for the public site live on the repository's
`gh-pages` branch. The default branch contains the engine and its release
automation only.

## Two hosting contracts

- GitHub Pages serves the repository site, but does not apply a repository
  `_headers` file as HTTP response headers. Every HTML page therefore carries a
  restrictive Content Security Policy meta element as an in-document fallback.
- Cloudflare Pages/Workers and compatible static hosts may apply `_headers`.
  Deployments there must retain the CSP, frame, MIME, referrer, permissions, and
  transport headers defined in that file.

The legacy `block-io` Worker is intentionally reduced to the version-controlled
rule in `cloudflare-redirect/_redirects`. It sends a permanent redirect to the
canonical GitHub Pages origin and does not duplicate or proxy the website.

A meta CSP cannot replace every response header. In particular, frame protection,
MIME sniffing protection, and HSTS must be verified on the actual public response.
Do not claim those controls are active on GitHub Pages merely because `_headers`
exists on the website branch.

## Release checks

1. Check out `gh-pages`; its `site-ci.yml` workflow runs
   `tests/Test-WebsiteLinks.ps1` and `tests/Test-WebsiteSecurity.ps1`.
2. Confirm downloads point to the matching `v2.7.0` GitHub Release or a byte-for-
   byte identical same-origin artifact.
3. Inspect public response headers with browser developer tools or `curl -I`.
4. Verify that no secret, private source map, credential, or local path was
   copied into the static asset deployment.
5. Keep contact, privacy, terms, and security pages reachable without JavaScript.
