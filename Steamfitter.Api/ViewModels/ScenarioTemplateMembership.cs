// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Steamfitter.Api.Data;

namespace Steamfitter.Api.ViewModels
{
    public class ScenarioTemplateMembership : IAuthorizationType
    {
        public Guid Id { get; set; }

        public Guid ScenarioTemplateId { get; set; }

        public Guid? UserId { get; set; }

        public Guid? GroupId { get; set; }

        public Guid RoleId { get; set; }
    }

    /// <summary>
    /// The one field a Scenario Template Membership's edit request carries. The Scenario Template, User
    /// and Group are not among them, so an edit cannot move a membership or hand it to someone else.
    /// </summary>
    public class ScenarioTemplateMembershipEdit
    {
        public Guid RoleId { get; set; }
    }
}
