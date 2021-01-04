// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Steamfitter.Api.Data.Models
{
    public class ResultEntity : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public Guid? TaskId { get; set; }
        public virtual TaskEntity Task { get; set; }
        public Guid? VmId { get; set; }
        public string VmName { get; set; }
        public string ApiUrl { get; set; }
        public TaskAction Action { get; set; }
        public string InputString { get; set; }
        public int ExpirationSeconds { get; set; }
        public int Iterations { get; set; }
        public int IntervalSeconds { get; set; }
        public int CurrentIteration { get; set; }
        public TaskStatus Status { get; set; }
        public string ExpectedOutput { get; set; }
        public string ActualOutput { get; set; }
        public DateTime SentDate { get; set; }
        public DateTime StatusDate { get; set; }
    }

    public class ResultEntityConfiguration : IEntityTypeConfiguration<ResultEntity>
    {
        public void Configure(EntityTypeBuilder<ResultEntity> builder)
        {
            builder
                .HasOne(w => w.Task)
                .WithMany(d => d.Results)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

}

