As of version 3.8.0, Steamfitter transitioned to a new permissions model, allowing for more granular access control to different features of the application. This document will detail how the new system works.

# Permissions

Access to features of Steamfitter are governed by sets of Permissions. Permissions can apply globally or on a per Scenario Template/Scenario basis. Examples of global Permissions are:

- CreateScenarioTemplates - Allows creation of new Scenario Templates
- ViewScenarios - Allows viewing all Scenarios and their Users and Groups
- ManageUsers - Allows for making changes to Users.

The Administration area now can be accessed by any User with View or Manage Permission to an Administration function (e.g. ViewScenarioTemplates, ManageGroups, etc), but only the areas they have Permissions for will be accessible in the sidebar menu.

There are many more Permissions available. They can be viewed by going to the new Roles section of the Administration area.

# Roles

Permissions can be applied to Users by grouping them into Roles. There are two types of Roles in Steamfitter:

- System Roles - Each User can have a System Role applied to them that gives global Permissions across all of Steamfitter. The three default System Roles are:

  - Administrator - Has all Permissions within the system.
  - Content Developer - Has the `CreateScenarioTemplates`, `CreateScenarios`, `ExecuteScenarios`, `ManageTasks`, `ViewUsers`, and `ViewGroups` Permissions. Users in this Role can create and manage their own Scenario Templates and Scenarios, but not affect any global settings or other User's Scenario Templates and Scenarios.
  - Observer - Has the `ViewScenarioTemplates` and `ViewScenarios` Permissions.  Users in this role can view all ScenarioTemplates and Scenarios, but cannot make any changes.

  Custom System Roles can be created by Users with the `ManageRoles` Permission that include whatever Permissions are desired for that Role. This can be done in the Roles section of the Administration area.

A User can be assigned a Role in the Users section of the Administration area.

Roles can also optionally be integrated with your Identity Provider. See Identity Provider Integration below.

# Seed Data

The SeedData section of appsettings.json has been changed to support the new model. You can now use this section to add Roles and Users on application startup. See appsettings.json for examples.

SeedData will only add objects if they do not exist. It will not modify existing Roles and Users so as not to undo changes made in the application on every restart. It will re-create objects if they are deleted in the application, so be sure to remove them from SeedData if they are no longer wanted.

# Identity Provider Integration

Roles can optionally be integrated with the Identity Provider that is being used to authenticate to Steamfitter. There are new settings under `ClaimsTransformation` to configure this integration. See appsettings.json. This integration is compatible with any Identity Provider that is capable of putting Roles into the auth token.

## Roles

If enabled, Roles from the User's auth token will be applied as if the Role was set on the User directly in Steamfitter. The Role must exist in Steamfitter and the name of the Role in the token must match exactly with the name of the Role in the token.

- UseRolesFromIdp: If true, Roles from the User's auth token will be used. Defaults to true.
- RolesClaimPath: The path within the User's auth token to look for Roles. Defaults to Keycloak's default value of `realm_access.roles`.

  Example: If the defaults are set, Steamfitter will apply the `Content Developer` Role to a User whose token contains the following:

```json
  realm_access {
    roles: [
        "Content Developer"
    ]
  }
```

If multiple Roles are present in the token, or if one Role is in the token and one Role is set directly on the User in Steamfitter, the Permissions of all of the Roles will be combined.

## Keycloak

If you are using Keycloak as your Identity Provider, Roles should work by default if you have not changed the default `RolesClaimPath`. You may need to adjust this value if your Keycloak is configured to put Roles in a different location within the token.

# Migration

When moving from a version prior to 3.8.0, the database will be migrated from the old Permissions sytem to the new one. The end result should be no change in access to any existing Users.

- Any existing Users with the old `SystemAdmin` Permission will be migrated to the new `Administrator` Role
- Any existing Users with the old `ContentDeveloper` Permissions will be migrated to the new `ContentDeveloper` Role

Be sure to double check all of your Roles and Team Memberships once the migration is complete.
