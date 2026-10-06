# Entity Framework Migrations

## Initial Setup

```bash
# Install EF CLI tool (if not already installed)
dotnet tool install --global dotnet-ef

# Create initial migration
dotnet ef migrations add InitialCreate --project EntraIdPoc.Api

# Apply to database
dotnet ef database update --project EntraIdPoc.Api
```

## Subsequent Changes

```bash
dotnet ef migrations add <MigrationName> --project EntraIdPoc.Api
dotnet ef database update --project EntraIdPoc.Api
```
