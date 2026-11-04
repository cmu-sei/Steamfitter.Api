// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Steamfitter.Api.Infrastructure.Mappings;

/// <summary>Tasks' mappings.</summary>
/// <remarks>
/// A task's action parameters are stored as a JSON input string, written with
/// <c>JsonSerializer.Serialize</c>. A request without parameters is therefore stored as the JSON
/// <c>null</c>, which is read back as an empty dictionary.
/// </remarks>
[Mapper]
public static partial class TaskMapper
{
    // ---- Entity to view model -------------------------------------------------------------------

    /// <remarks>Tasks have no VM list of their own: a request's VM list is folded into the mask.</remarks>
    [MapPropertyFromSource(nameof(Task.ActionParameters), Use = nameof(ReadActionParameters))]
    [MapperIgnoreTarget(nameof(Task.VmList))]
    public static partial Task ToTask(this TaskEntity source);

    public static partial TaskSummary ToTaskSummary(this TaskEntity source);

    public static partial TaskSummary ToTaskSummary(this Task source);

    /// <summary>A task carrying only its summary's members, for a caller who may not see the rest.</summary>
    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial Task ToTask(this TaskSummary source);

    // An empty input string has no parameters; one that is not JSON is kept under BadInputString.
    [UserMapping(Default = false)]
    private static Dictionary<string, string> ReadActionParameters(TaskEntity source)
    {
        var parameters = new Dictionary<string, string>();
        try
        {
            if (!string.IsNullOrEmpty(source.InputString))
            {
                parameters = JsonSerializer.Deserialize<Dictionary<string, string>>(source.InputString);
            }
        }
        catch (Exception)
        {
            parameters["BadInputString"] = source.InputString;
            Console.WriteLine($"Error mapping InputString for Task {source.Id}");
        }

        // The JSON null reads as no dictionary at all; it is returned as an empty one.
        return parameters ?? [];
    }

    // ---- Request to entity ----------------------------------------------------------------------

    /// <remarks>The handler folds the VM list into the mask and sets the owner and audit fields.</remarks>
    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapperIgnoreSource(nameof(TaskCreate.VmList))]
    [MapperIgnoreSource(nameof(TaskCreate.Executable))]
    [MapProperty(nameof(TaskCreate.ActionParameters), nameof(TaskEntity.InputString), Use = nameof(WriteInputString))]
    public static partial TaskEntity ToEntity(this TaskCreate source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapperIgnoreSource(nameof(TaskEdit.VmList))]
    [MapperIgnoreSource(nameof(TaskEdit.Executable))]
    [MapProperty(nameof(TaskEdit.ActionParameters), nameof(TaskEntity.InputString), Use = nameof(WriteInputString))]
    public static partial void ApplyTo(this TaskEdit source, TaskEntity target);

    [UserMapping(Default = false)]
    private static string WriteInputString(Dictionary<string, string> actionParameters) =>
        JsonSerializer.Serialize(actionParameters);
}
