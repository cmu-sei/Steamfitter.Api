// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Steamfitter.Api.Data;
using System;
using System.Collections.Generic;

namespace Steamfitter.Api.ViewModels
{
    public class Scenario : Base, IAuthorizationType
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ScenarioStatus Status { get; set; }
        public bool OnDemand { get; set; }
        public Guid? ScenarioTemplateId { get; set; }
        public Guid? ViewId { get; set; }
        public string View { get; set; }
        public Guid? DefaultVmCredentialId { get; set; }
        public List<VmCredential> VmCredentials { get; set; }
        public List<Guid> Users { get; set; }
        public int Score { get; set; }
        public int ScoreEarned { get; set; }
        public IEnumerable<string> ScenarioPermissions { get; set; }
    }

    /// <summary>
    /// Returned to unprivileged users
    /// </summary>
    public class ScenarioSummary : Base
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Score { get; set; }
        public int ScoreEarned { get; set; }
        public Guid? ViewId { get; set; }
    }


    public class ScenarioForm
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ScenarioStatus Status { get; set; }
        public bool OnDemand { get; set; }
        public Guid? ScenarioTemplateId { get; set; }
        public Guid? ViewId { get; set; }
        public string View { get; set; }
        public Guid? DefaultVmCredentialId { get; set; }
        public List<VmCredential> VmCredentials { get; set; }
    }

    public class ScenarioCloneOptions
    {
        public string NameSuffix { get; set; }
        public Guid? ViewId { get; set; }

        /// <summary>
        /// Ids of the Users to add to the new Scenario as Members. Deprecated as of client version 3.10.0 in favor of Users, which also carries the name to create a missing Steamfitter User with. Ids sent here are still honored, but a User created from one of them is named after its Id.
        /// </summary>
        public List<Guid> UserIds { get; set; }

        /// <summary>
        /// The Users to add to the new Scenario as Members. A Steamfitter User record is created for any of these that has not been seen before, so that a user who has never signed in to Steamfitter can still be given a Scenario Membership.
        /// </summary>
        public List<ScenarioCloneUser> Users { get; set; }
    }

    /// <summary>
    /// A User to add to a cloned Scenario, including the name to use if the User has to be created.
    /// </summary>
    public class ScenarioCloneUser
    {
        /// <summary>
        /// Id of the User.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the User, used only if the User has to be created.
        /// </summary>
        public string Name { get; set; }
    }
}
