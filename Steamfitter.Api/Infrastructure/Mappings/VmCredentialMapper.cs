// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Steamfitter.Api.Data.Models;
using Steamfitter.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Steamfitter.Api.Infrastructure.Mappings;

/// <summary>VM credentials' mappings.</summary>
[Mapper]
public static partial class VmCredentialMapper
{
    public static partial VmCredential ToVmCredential(this VmCredentialEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial VmCredentialEntity ToEntity(this VmCredentialCreate source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial void ApplyTo(this VmCredentialEdit source, VmCredentialEntity target);
}
