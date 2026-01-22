# Jellyfin 2FA Plugin

This plugin adds a standalone 2FA login flow for Jellyfin and backend endpoints for enrolling users in TOTP.

Important: this does not change the default Jellyfin login page. Users can still log in via `/web`. The 2FA flow lives at `/sso/2fa`.

## Features

- Standalone login page at `/sso/2fa`
- TOTP enrollment and verification
- Optional per-user enrollment (no UI changes to jellyfin-web required)

## Requirements

- Jellyfin Server 10.11.6 (packages target 10.11.6)
- .NET 9 SDK for building the plugin

## Usage

### 1) Install the plugin

Build and copy the plugin output to your Jellyfin plugins folder, then restart Jellyfin.

If you want to add this plugin via a repository URL, add the manifest below in Dashboard -> Plugins -> Repositories:

```url
https://raw.githubusercontent.com/jonathand-cf/jellyfin-plugin-twoFA/main/manifest.json
```

### 2) Open the 2FA login page

Visit:

```url
http://YOUR_JELLYFIN:8096/sso/2fa
```

If you run Jellyfin under a reverse-proxy path (e.g. `/jellyfin`), use:

```url
https://yourdomain/jellyfin/sso/2fa
```

### 3) Enroll a user for TOTP

On the `/sso/2fa` page:

- Enter username and password
- Click "Generate secret"
- Copy the secret or the OTP URI into your authenticator app
- Enter the code from your authenticator and click "Confirm 2FA"

### 4) Sign in with 2FA

Use the same `/sso/2fa` page and enter username, password, and OTP code.

## Reverse proxy redirect (optional)

If you want to force browser logins through the 2FA page, add a redirect in your reverse proxy so `/web/#!/login.html` goes to `/sso/2fa`.

Nginx example:

```bash
location = /web/index.html {
  if ($args ~* "login") {
    return 302 /sso/2fa;
  }
}
```

Caddy example:

```bash
@login path /web/index.html
@login query *login*
redir @login /sso/2fa 302
```

This only affects the web UI, not other Jellyfin clients.

## Plugin configuration

Configure the plugin in Dashboard -> Plugins -> 2FA:

- Enable TOTP: turn TOTP support on/off
- Issuer: label shown in authenticator apps
- Allow users to opt in: allows self-service enrollment via `/sso/2fa`
- Authentik settings: optional, for future provider support

## Notes

- The default Jellyfin login remains available and is not blocked by this plugin.
- The plugin stores per-user 2FA settings in the plugin configuration directory.

## Development

Build:

```bash
dotnet build Jellyfin.Plugin.2FA.sln
```

Publish:

```bash
dotnet publish Jellyfin.Plugin.2FA/Jellyfin.Plugin.2FA.csproj -c Release -o ./publish
```
