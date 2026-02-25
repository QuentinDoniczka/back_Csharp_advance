# EF Core Migrations

## Prerequisites

Install the EF Core CLI tool:

```bash
dotnet tool install --global dotnet-ef
```

## Commands

### Create a migration

```bash
dotnet ef migrations add <Name> --project Infrastructure --startup-project API
```

### Apply migrations

```bash
dotnet ef database update --project Infrastructure --startup-project API
```

### Rollback to a specific migration

```bash
dotnet ef database update <MigrationName> --project Infrastructure --startup-project API
```

### Rollback all migrations

```bash
dotnet ef database update 0 --project Infrastructure --startup-project API
```

### List migrations

```bash
dotnet ef migrations list --project Infrastructure --startup-project API
```

### Remove last unapplied migration

```bash
dotnet ef migrations remove --project Infrastructure --startup-project API
```

### Drop database

```bash
dotnet ef database drop --project Infrastructure --startup-project API --force
```

### Generate SQL script

```bash
dotnet ef migrations script --project Infrastructure --startup-project API --idempotent -o migration.sql
```
