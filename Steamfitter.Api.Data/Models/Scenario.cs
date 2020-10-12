/*
Crucible
Copyright 2020 Carnegie Mellon University.
NO WARRANTY. THIS CARNEGIE MELLON UNIVERSITY AND SOFTWARE ENGINEERING INSTITUTE MATERIAL IS FURNISHED ON AN "AS-IS" BASIS. CARNEGIE MELLON UNIVERSITY MAKES NO WARRANTIES OF ANY KIND, EITHER EXPRESSED OR IMPLIED, AS TO ANY MATTER INCLUDING, BUT NOT LIMITED TO, WARRANTY OF FITNESS FOR PURPOSE OR MERCHANTABILITY, EXCLUSIVITY, OR RESULTS OBTAINED FROM USE OF THE MATERIAL. CARNEGIE MELLON UNIVERSITY DOES NOT MAKE ANY WARRANTY OF ANY KIND WITH RESPECT TO FREEDOM FROM PATENT, TRADEMARK, OR COPYRIGHT INFRINGEMENT.
Released under a MIT (SEI)-style license, please see license.txt or contact permission@sei.cmu.edu for full terms.
[DISTRIBUTION STATEMENT A] This material has been approved for public release and unlimited distribution.  Please see Copyright notice for non-US Government use and distribution.
Carnegie Mellon(R) and CERT(R) are registered in the U.S. Patent and Trademark Office by Carnegie Mellon University.
DM20-0181
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steamfitter.Api.Data.Models
{
    public class ScenarioEntity : BaseEntity
    {
        public ScenarioEntity()
        {
            this.StartDate = DateTime.UtcNow.AddDays(1);
            this.EndDate = this.StartDate.AddMonths(1);
            this.Status = ScenarioStatus.ready;
        }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ScenarioStatus Status { get; set; }
        public bool OnDemand { get; set; }
        public virtual ICollection<TaskEntity> Tasks { get; set; } = new HashSet<TaskEntity>();
        public Guid? ScenarioTemplateId { get; set; }
        public virtual ScenarioTemplateEntity ScenarioTemplate { get; set; }
        public Guid? ViewId { get; set; }
        public string View { get; set; }
        public Guid? DefaultVmCredentialId { get; set; }
        public virtual ICollection<VmCredentialEntity> VmCredentials { get; set; } = new HashSet<VmCredentialEntity>();
    }

    public class ScenarioEntityConfiguration : IEntityTypeConfiguration<ScenarioEntity>
    {
        public void Configure(EntityTypeBuilder<ScenarioEntity> builder)
        {
            builder
                .HasOne(d => d.ScenarioTemplate)
                .WithMany(d => d.Scenarios)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

}
