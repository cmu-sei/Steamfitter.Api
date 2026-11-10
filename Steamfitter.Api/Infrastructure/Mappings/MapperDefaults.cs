// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Riok.Mapperly.Abstractions;

// Defaults for every Mapperly [Mapper] in the application. .editorconfig raises unmapped members and
// projections that cannot be inlined to errors, so a member that stops being mapped fails the build.
[assembly: MapperDefaults(
    // Outbound maps (entity to view model) must fill every member of the view model, so a response
    // never silently carries a default. Inbound maps (request onto entity) ask the opposite question
    // instead, with [MapperRequiredMapping(RequiredMappingStrategy.Source)]: every member of the
    // request must reach the entity, while navigations and members the request does not carry are
    // left alone.
    RequiredMappingStrategy = RequiredMappingStrategy.Target,

    // A null source value is skipped rather than written when the target cannot hold null, so the
    // target keeps the value it already had. False is Mapperly's default, stated here so it cannot
    // change unnoticed. It matches the AutoMapper convention it replaced (a nullable value type onto
    // its own underlying type kept the destination's value); nullable reference types are disabled,
    // so a null string is still written.
    ThrowOnPropertyMappingNullMismatch = false)]
