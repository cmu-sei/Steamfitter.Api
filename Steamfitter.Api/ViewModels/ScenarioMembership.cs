// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Steamfitter.Api.ViewModels
{
    public class ScenarioMembership : IAuthorizationType
    {

        public Guid Id { get; set; }

        public Guid ScenarioId { get; set; }

        public Guid? UserId { get; set; }

        public Guid? GroupId { get; set; }

        public Guid RoleId { get; set; }
    }
}
