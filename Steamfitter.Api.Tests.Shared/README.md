# Steamfitter.Api.Tests.Shared

Copyright 2026 Carnegie Mellon University. All Rights Reserved.
Released under a MIT (SEI)-style license.

## Purpose

Shared test fixtures and utilities for Steamfitter API test projects. Provides AutoFixture customizations for all Steamfitter entity types to avoid circular reference issues from Entity Framework navigation properties.

## Files

- **`Fixtures/SteamfitterCustomization.cs`** - AutoFixture customization that registers factories for Steamfitter entities:
  - ScenarioEntity, ScenarioTemplateEntity
  - TaskEntity (note: uses `SteamfitterTaskStatus` alias to avoid namespace conflict with `System.Threading.Tasks.Task`)
  - ResultEntity
  - UserEntity, PermissionEntity, UserPermissionEntity
  - VmCredentialEntity, FileEntity
  - GroupEntity, GroupMembershipEntity
  - ScenarioMembershipEntity, ScenarioTemplateMembershipEntity
  - ScenarioRoleEntity, ScenarioTemplateRoleEntity, SystemRoleEntity
  - UserScenarioEntity
  - XApiQueuedStatementEntity

## Key Patterns

- Omits navigation properties (`.Without(x => x.NavigationProp)`) to prevent infinite recursion
- Uses `OmitOnRecursionBehavior` for safe fixture generation
- Generates valid GUIDs and default enum values for all entities
- Handles the TaskEntity naming conflict by using `SteamfitterTaskStatus` type alias

## Dependencies

- AutoFixture 4.18.1
- Steamfitter.Api (project reference)
- Steamfitter.Api.Data (project reference)
- Crucible.Common.Testing (shared library)

## Usage

```csharp
var fixture = new Fixture().Customize(new SteamfitterCustomization());
var taskEntity = fixture.Create<TaskEntity>();
```

This project is referenced by both `Steamfitter.Api.Tests.Unit` and `Steamfitter.Api.Tests.Integration`.
