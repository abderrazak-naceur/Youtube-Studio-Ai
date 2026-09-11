# Authentication

## Users

Use OIDC/OAuth-compatible identity with short-lived access tokens and rotating refresh tokens.

## YouTube

Use OAuth consent and encrypted refresh tokens. Scopes must be least-privilege and separated by feature.

## Providers

Provider keys live only in server-side secret management. Never store them in source code, browser local storage or logs.

## Enterprise

Support SSO/SAML/OIDC and SCIM only when enterprise demand justifies implementation.

## Audit

Record authentication events, credential changes, permission changes and sensitive actions.
