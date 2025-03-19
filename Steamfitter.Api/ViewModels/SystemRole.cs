// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Steamfitter.Api.Data;

namespace Steamfitter.Api.ViewModels
{
    public class SystemRole
    {

        public Guid Id { get; set; }

        public string Name { get; set; }

        public bool AllPermissions { get; set; }
        public bool Immutable { get; set; }
        public SystemPermission[] Permissions { get; set; }
    }
}
