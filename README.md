# Steamfitter.api Readme

This project provides a restful api for steamfitter functionality in the Crucible ecosystem. By default, steamfitter.api is available at `localhost:4400`, with the Swagger page at `localhost:4400/api`.

## Entity Description

**ScenarioTemplate:** A definition of a series of dispatch tasks that can be instantiated as a Scenario.

**Scenario:** An instantiation of a series of dispatch tasks that run in a particular view.

**Task:** An individual task that is defined to run on a group of VM's (defined by a VM mask) or that runs against an external API.

**Result:** The result from the API or a single VM of running a Task.  There will be a Result for each VM on which the Task was run. If no VM is associated with the Task, there will be one Result.

## Task Execution

Tasks can be executed in multiple ways:

1. **VM Tasks:** Tasks that run against virtual machines using either:
   - **VmMask:** Pattern-based VM selection (requires Scenario to be associated with a Player View)
   - **VmList:** Explicit list of VM IDs (for ad-hoc tasks or specific VM targeting)

2. **API Tasks:** Tasks that execute against external APIs without requiring VMs.

3. **Manual Tasks:** Tasks that are marked as manual execution and do not automatically execute.

## Reporting bugs and requesting features

Think you found a bug? Please report all Crucible bugs - including bugs for the individual Crucible apps - in the [cmu-sei/crucible issue tracker](https://github.com/cmu-sei/crucible/issues).

Include as much detail as possible including steps to reproduce, specific app involved, and any error messages you may have received.

Have a good idea for a new feature? Submit all new feature requests through the [cmu-sei/crucible issue tracker](https://github.com/cmu-sei/crucible/issues).

Include the reasons why you're requesting the new feature and how it might benefit other Crucible users.

# Database Migrations

When the data model is changed, a new database migration must be created. All database migration commands are run from the Steamfitter.Api directory.
- Create a new migration:
    - dotnet ef migrations add <new_migration_name> --project ../Steamfitter.Api.Migrations.PostgreSQL/Steamfitter.Api.Migrations.PostgreSQL.csproj
- Update the database
    - Running the app updates the database.
- Roll back a migration
    - update the database to the previous migration
        - dotnet ef database update <previous_migration_name> --project ../Steamfitter.Api.Migrations.PostgreSQL/Steamfitter.Api.Migrations.PostgreSQL.csproj
    - Remove the migration
        - dotnet ef migrations remove --project ../Steamfitter.Api.Migrations.PostgreSQL/Steamfitter.Api.Migrations.PostgreSQL.csproj

## License

Copyright 2021 Carnegie Mellon University. See the [LICENSE.md](https://github.com/cmu-sei/Alloy.Api/blob/development/LICENSE.md) files for details.
