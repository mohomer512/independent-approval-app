# Antiforgery API testing

Browser-initiated `POST`, `PUT`, `PATCH`, and `DELETE` controller requests use
the `X-CSRF-TOKEN` request header and the matching HttpOnly antiforgery cookie.
Safe requests such as `GET`, `HEAD`, and `OPTIONS` do not need either token.

## curl upload flow

Use a cookie jar across both requests. First obtain the authenticated request
token and store the response cookie, then send both the cookie jar and the JSON
`requestToken` value with the upload. The old one-command upload flow is no
longer valid.

The following PowerShell example uses the current Windows identity. Replace the
sample document path before running it. `-k` is only for the local development
certificate; do not disable certificate verification in production.

```powershell
$cookieJar = Join-Path $env:TEMP 'independent-approval-antiforgery.cookies'
$tokenResponse = Join-Path $env:TEMP 'independent-approval-antiforgery-token.json'

curl.exe -k --negotiate -u : -c $cookieJar -o $tokenResponse `
  https://localhost:7074/api/security/antiforgery-token

$requestToken = (Get-Content -Raw -LiteralPath $tokenResponse |
  ConvertFrom-Json).requestToken

curl.exe -k --negotiate -u : -b $cookieJar `
  -H "X-CSRF-TOKEN: $requestToken" `
  -F "File=@C:\path\to\sample.txt;type=text/plain" `
  -F "Description=Antiforgery curl test" `
  https://localhost:7074/api/documents
```

The token endpoint returns only the request token in JSON. The cookie value is
kept in the curl cookie jar and must not be copied into the request header or
logged. Delete the temporary cookie and token files after testing.

List and download requests remain safe `GET` requests. They still need Windows
authentication, but they do not need the `X-CSRF-TOKEN` header:

```powershell
curl.exe -k --negotiate -u : -b $cookieJar `
  https://localhost:7074/api/documents
```

## Environment behavior

Development runs the Vite client at `http://localhost:5173` (or the existing
configured `5174` origin) against `https://localhost:7074`. The browser must use
`credentials: include`; the API issues a `SameSite=None; Secure; HttpOnly`
antiforgery cookie so that scheme-cross-site development requests can send it.

The current IIS test site at `http://spse26h:8090` is same-origin. Its cookie is
`SameSite=Strict; HttpOnly` and follows the request's security, so it is not
marked `Secure` over that HTTP test binding and remains usable. HTTP provides no
transport confidentiality or integrity: an on-path attacker can observe or
modify the token, cookie, credentials exchange, and document traffic. Real
production deployment must use HTTPS; on HTTPS the same policy marks the cookie
`Secure`.
