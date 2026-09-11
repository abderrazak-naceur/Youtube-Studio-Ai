# Database Migrations

## Strategy

- every schema change is versioned
- forward migrations are deterministic
- destructive changes require staged rollout
- production backups verified before risky migrations
- migration runtime and lock impact are monitored

## Deployment

1. deploy backward-compatible schema
2. deploy application supporting old/new schema
3. migrate data
4. switch reads/writes
5. remove obsolete schema in a later release

Never couple irreversible data deletion to an ordinary application deployment.
