// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Steamfitter.Api.Data.Models
{
    public class ScenarioTemplateEntity : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public virtual ICollection<TaskEntity> Tasks { get; set; } = new HashSet<TaskEntity>();
        public int? DurationHours { get; set; }
        public virtual ICollection<ScenarioEntity> Scenarios { get; set; } = new HashSet<ScenarioEntity>();
        public Guid? DefaultVmCredentialId { get; set; }
        public virtual ICollection<VmCredentialEntity> VmCredentials { get; set; } = new HashSet<VmCredentialEntity>();
    }
}

