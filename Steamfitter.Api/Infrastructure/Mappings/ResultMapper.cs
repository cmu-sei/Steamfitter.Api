// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Steamfitter.Api.Infrastructure.Mappings;

/// <summary>Results' mappings. A result's action parameters are stored as a JSON input string.</summary>
[Mapper]
public static partial class ResultMapper
{
    [MapPropertyFromSource(nameof(Result.ActionParameters), Use = nameof(ReadActionParameters))]
    public static partial Result ToResult(this ResultEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial ResultEntity ToEntity(this ResultCreate source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial void ApplyTo(this ResultEdit source, ResultEntity target);

    // An input string that is not JSON, a missing one included, is kept under BadInputString.
    [UserMapping(Default = false)]
    private static Dictionary<string, string> ReadActionParameters(ResultEntity source)
    {
        var parameters = new Dictionary<string, string>();
        try
        {
            parameters = JsonSerializer.Deserialize<Dictionary<string, string>>(source.InputString);
        }
        catch (Exception)
        {
            parameters["BadInputString"] = source.InputString;
            Console.WriteLine($"Error mapping InputString for Result {source.Id}");
        }

        // The JSON null reads as no dictionary at all; it is returned as an empty one.
        return parameters ?? [];
    }
}
