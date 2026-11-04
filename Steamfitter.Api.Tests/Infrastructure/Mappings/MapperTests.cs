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
    /// <summary>A null nullable member clears the value; only a null onto a non-nullable member is skipped.</summary>
    [Fact]
    public void A_form_without_a_duration_clears_the_templates_duration()
    {
        var destination = new ScenarioTemplateEntity { DurationHours = 8 };

        new SAVM.ScenarioTemplateForm { Name = "Renamed", DurationHours = null }.ApplyTo(destination);

        Assert.Equal(("Renamed", null), (destination.Name, destination.DurationHours));
    }

    [Fact]
    public void A_tasks_input_string_is_read_back_as_its_action_parameters()
    {
        var task = new TaskEntity { InputString = """{"Moid":"{moid}"}""" }.ToTask();

        Assert.Equal("{moid}", task.ActionParameters["Moid"]);
    }

    [Fact]
    public void An_unreadable_input_string_is_kept_under_BadInputString()
    {
        var task = new TaskEntity { Id = Guid.NewGuid(), InputString = "not json" }.ToTask();

        Assert.Equal("not json", task.ActionParameters["BadInputString"]);
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
