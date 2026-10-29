// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;

namespace Steamfitter.Api.ViewModels
{
    /// <summary>
    /// The fields a Result's create and edit requests share. The task runner owns everything that decides
    /// what runs and where: the status, the VM, the action parameters and the iterations are not among them,
    /// so a Result written through the API is never picked up and run. Only <see cref="ResultCreate"/> names
    /// the Task, so an edit cannot move a Result to another Task.
    /// </summary>
    public abstract class ResultFields
    {
        public string ExpectedOutput { get; set; }
        public string ActualOutput { get; set; }
    }

    public class ResultCreate : ResultFields
    {
        /// <summary>The Task the Result belongs to.</summary>
        [Required]
        public Guid? TaskId { get; set; }
    }

    public class ResultEdit : ResultFields
    {
    }
}
