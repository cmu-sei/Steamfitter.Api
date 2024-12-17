// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Text.Json;
using Steamfitter.Api.Data.Models;

namespace Steamfitter.Api.Infrastructure.Authorization;

public class ScenarioTemplatePermissionClaim
{
    public Guid ScenarioTemplateId { get; set; }
    public ScenarioTemplatePermission[] Permissions { get; set; } = [];

    public ScenarioTemplatePermissionClaim() { }

    public static ScenarioTemplatePermissionClaim FromString(string json)
    {
        return JsonSerializer.Deserialize<ScenarioTemplatePermissionClaim>(json);
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}