// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using AutoMapper.Internal;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Tests.Support;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Tests.Infrastructure.Mappings;

/// <summary>The AutoMapper profiles and Startup's global convention, over <see cref="TestMapper"/>.</summary>
public class MappingConfigurationTests
{
    /// <summary>
    /// Startup's IgnoreNullSourceValues convention (copied into TestMapper) applies only from a nullable
    /// source to a non-nullable destination; between two nullable members a null source clears the value.
    /// </summary>
    [Fact]
    public void A_form_without_a_duration_clears_the_templates_duration()
    {
        var destination = new ScenarioTemplateEntity { DurationHours = 8 };

        TestMapper.Mapper.Map(new SAVM.ScenarioTemplateForm { Name = "Renamed", DurationHours = null }, destination);

        Assert.Equal(("Renamed", null), (destination.Name, destination.DurationHours));
    }

    [Fact]
    public void Every_type_map_the_profiles_declare_is_built()
    {
        TestMapper.Configuration.CompileMappings();
    }

    [Fact]
    public void A_tasks_input_string_is_read_back_as_its_action_parameters()
    {
        var task = TestMapper.Mapper.Map<SAVM.Task>(new TaskEntity { InputString = """{"Moid":"{moid}"}""" });

        Assert.Equal("{moid}", task.ActionParameters["Moid"]);
    }

    [Fact]
    public void An_unreadable_input_string_is_kept_under_BadInputString()
    {
        var task = TestMapper.Mapper.Map<SAVM.Task>(new TaskEntity { Id = Guid.NewGuid(), InputString = "not json" });

        Assert.Equal("not json", task.ActionParameters["BadInputString"]);
    }

    // Same case as TaskControllerTests.GetByViewId_answers_a_member_holding_only_ViewTasks_with_a_server_error.
    [Fact]
    public void The_configuration_has_no_map_from_a_task_to_its_summary()
    {
        Assert.Null(TestMapper.Configuration.Internal().FindTypeMapFor<SAVM.Task, SAVM.TaskSummary>());
    }
}
