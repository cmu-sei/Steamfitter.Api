// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Steamfitter.Api.Data.Models;
using Steamfitter.Api.ViewModels;
using System;
using System.Text.Json;
using System.Collections.Generic;

namespace Steamfitter.Api.Infrastructure.Mappings
{
    public class TaskProfile : AutoMapper.Profile
    {
        public TaskProfile()
        {
            CreateMap<TaskSummary, Task>();
            CreateMap<TaskEntity, TaskSummary>();
            CreateMap<TaskEntity, Task>()
                .ForMember(dest => dest.ActionParameters, m => m.MapFrom(src => ConvertToActionParameters(src)));

            CreateMap<Task, TaskEntity>()
                .ForMember(dest => dest.InputString, m => m.MapFrom(src => ConvertToInputString(src.ActionParameters)));

            CreateMap<TaskEntity, TaskEntity>()
                .ForMember(dt => dt.Id, opt => opt.Ignore());

            CreateMap<TaskForm, TaskEntity>();
        }

        private Dictionary<string, string> ConvertToActionParameters(TaskEntity src)
        {
            var parameters = new Dictionary<string, string>();
            try
            {
                if (!string.IsNullOrEmpty(src.InputString))
                {
                    parameters = JsonSerializer.Deserialize<Dictionary<string, string>>(src.InputString);
                }
            }
            catch (Exception ex)
            {
                parameters["BadInputString"] = src.InputString;
                Console.WriteLine($"Error mapping InputString for Task {src.Id}");
            }

            return parameters;
        }

        private string ConvertToInputString(Dictionary<string, string> actionParameters)
        {
            var inputString = JsonSerializer.Serialize(actionParameters);

            return inputString;
        }

    }
}
