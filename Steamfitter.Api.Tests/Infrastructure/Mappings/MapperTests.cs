// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Mappings;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Tests.Infrastructure.Mappings;

/// <summary>The mappers' conversions that no endpoint test reaches directly.</summary>
public class MapperTests
{
    /// <summary>A form's null clears a nullable member rather than keeping the stored value.</summary>
    [Fact]
    public void A_form_without_a_duration_clears_the_templates_duration()
    {
        var destination = new ScenarioTemplateEntity { DurationHours = 8 };

        new SAVM.ScenarioTemplateForm { Name = "Renamed", DurationHours = null }.ApplyTo(destination);

        Assert.Equal(("Renamed", null), (destination.Name, destination.DurationHours));
    }

    /// <summary>An empty input string reads back as no action parameters, as a missing one does.</summary>
    [Fact]
    public void An_empty_input_string_is_read_back_as_no_action_parameters()
    {
        var task = new TaskEntity { InputString = "" }.ToTask();

        Assert.Empty(task.ActionParameters);
    }

    /// <summary>A task sent as a summary keeps the summary's members and loses the rest.</summary>
    [Fact]
    public void A_task_round_tripped_through_its_summary_keeps_only_the_summarys_members()
    {
        var task = new SAVM.Task
        {
            Id = Guid.NewGuid(),
            Name = "Task",
            Score = 3,
            ApiUrl = "stackstorm",
            ActionParameters = new() { ["Secret"] = "value" }
        };

        var summarized = task.ToTaskSummary().ToTask();

        Assert.Equal((task.Id, "Task", 3), (summarized.Id, summarized.Name, summarized.Score));
        Assert.Equal((null, null), (summarized.ApiUrl, summarized.ActionParameters));
    }

    /// <summary>A copy is a new object, not the original: a mapping between two values of one type returns the source unless it deep-clones.</summary>
    [Fact]
    public void A_copy_is_a_new_entity_without_the_originals_id()
    {
        var original = new TaskEntity { Id = Guid.NewGuid(), Name = "Original" };

        var copy = original.ToCopy();

        Assert.NotSame(original, copy);
        Assert.Equal((Guid.Empty, "Original"), (copy.Id, copy.Name));
    }

    /// <summary>
    /// A task copy keeps the original's totals, as AutoMapper's copy did; they are recalculated only when
    /// a scored task is added.
    /// </summary>
    [Fact]
    public void A_task_copy_keeps_the_originals_totals()
    {
        var original = new TaskEntity { TotalScore = 4, TotalStatus = Data.TaskStatus.succeeded };

        var copy = original.ToCopy();

        Assert.Equal((4, Data.TaskStatus.succeeded), (copy.TotalScore, copy.TotalStatus));
    }

    /// <summary>
    /// A scenario copy keeps its view and template ids but none of the original's collections or
    /// navigations: copying the memberships would also loop through their scenario back to this one.
    /// </summary>
    [Fact]
    public void A_scenario_copy_keeps_its_ids_and_leaves_out_the_originals_collections()
    {
        var original = new ScenarioEntity
        {
            Id = Guid.NewGuid(),
            ViewId = Guid.NewGuid(),
            ScenarioTemplateId = Guid.NewGuid(),
            ScenarioTemplate = new ScenarioTemplateEntity()
        };
        original.Tasks.Add(new TaskEntity { Id = Guid.NewGuid() });
        original.VmCredentials.Add(new VmCredentialEntity { Id = Guid.NewGuid() });
        original.Memberships.Add(new ScenarioMembershipEntity { Id = Guid.NewGuid(), Scenario = original });

        var copy = original.ToCopy();

        Assert.Equal((original.ViewId, original.ScenarioTemplateId), (copy.ViewId, copy.ScenarioTemplateId));
        Assert.Null(copy.ScenarioTemplate);
        Assert.Empty(copy.Tasks);
        Assert.Empty(copy.VmCredentials);
        Assert.Empty(copy.Memberships);
    }

    /// <summary>A template copy leaves out the original's tasks, scenarios, credentials and memberships.</summary>
    [Fact]
    public void A_template_copy_leaves_out_the_originals_collections()
    {
        var original = new ScenarioTemplateEntity { Id = Guid.NewGuid(), Name = "Original" };
        original.Tasks.Add(new TaskEntity { Id = Guid.NewGuid() });
        original.Scenarios.Add(new ScenarioEntity { Id = Guid.NewGuid() });
        original.VmCredentials.Add(new VmCredentialEntity { Id = Guid.NewGuid() });
        original.Memberships.Add(new ScenarioTemplateMembershipEntity { Id = Guid.NewGuid(), ScenarioTemplate = original });

        var copy = original.ToCopy();

        Assert.Equal("Original", copy.Name);
        Assert.Empty(copy.Tasks);
        Assert.Empty(copy.Scenarios);
        Assert.Empty(copy.VmCredentials);
        Assert.Empty(copy.Memberships);
    }

    /// <summary>A copy carries none of the original's navigations, even loaded ones; the copy requests copy subtasks themselves.</summary>
    [Fact]
    public void A_copy_leaves_out_the_originals_navigations()
    {
        var parent = new TaskEntity { Id = Guid.NewGuid() };
        var original = new TaskEntity { Id = Guid.NewGuid(), TriggerTask = parent, Scenario = new ScenarioEntity() };
        original.Children.Add(new TaskEntity { Id = Guid.NewGuid() });
        original.Results.Add(new ResultEntity { Id = Guid.NewGuid() });

        var copy = original.ToCopy();

        Assert.Equal((null, null), (copy.TriggerTask, copy.Scenario));
        Assert.Empty(copy.Children);
        Assert.Empty(copy.Results);
    }
}
