// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Steamfitter.Api.Data.Models;
using Steamfitter.Api.ViewModels;
using System;
using System.Text.Json;
using System.Collections.Generic;

namespace Steamfitter.Api.Infrastructure.Mappings
{
    public class ResultProfile : AutoMapper.Profile
    {
        public ResultProfile()
        {
            CreateMap<ResultSummary, Result>();
            CreateMap<ResultEntity, ResultSummary>();
            CreateMap<ResultEntity, Result>()
                .ForMember(dest => dest.ActionParameters, m => m.MapFrom(src => ConvertToActionParameters(src)));

            CreateMap<Result, ResultEntity>()
                .ForMember(dest => dest.InputString, m => m.MapFrom(src => ConvertToInputString(src.ActionParameters)));
        }

        private Dictionary<string, string> ConvertToActionParameters(ResultEntity src)
        {
            var parameters = new Dictionary<string, string>();
            try
            {
                parameters = JsonSerializer.Deserialize<Dictionary<string, string>>(src.InputString);
            }
            catch (Exception)
            {
                parameters["BadInputString"] = src.InputString;
                Console.WriteLine($"Error mapping InputString for Result {src.Id}");
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
