// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Text.Json;
using Steamfitter.Api.Data;

namespace Steamfitter.Api.Infrastructure.Authorization;

public class ScenarioPermissionClaim
{
    public Guid ScenarioId { get; set; }
    public ScenarioPermission[] Permissions { get; set; } = [];

    public ScenarioPermissionClaim() { }

    public static ScenarioPermissionClaim FromString(string json)
    {
        return JsonSerializer.Deserialize<ScenarioPermissionClaim>(json);
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}