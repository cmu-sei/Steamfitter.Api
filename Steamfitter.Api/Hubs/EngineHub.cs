// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading;
using STT = System.Threading.Tasks;

namespace Steamfitter.Api.Hubs
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class EngineHub : Hub
    {
        public EngineHub()
        {
        }

    }

    public static class EngineMethods
    {
        public const string ScenarioTemplateCreated = "ScenarioTemplateCreated";
        public const string ScenarioTemplateUpdated = "ScenarioTemplateUpdated";
        public const string ScenarioTemplateDeleted = "ScenarioTemplateDeleted";
        public const string ScenarioCreated = "ScenarioCreated";
        public const string ScenarioUpdated = "ScenarioUpdated";
        public const string ScenarioDeleted = "ScenarioDeleted";
        public const string TaskCreated = "TaskCreated";
        public const string TaskUpdated = "TaskUpdated";
        public const string TaskDeleted = "TaskDeleted";
        public const string ResultCreated = "ResultCreated";
        public const string ResultUpdated = "ResultUpdated";
        public const string ResultsUpdated = "ResultsUpdated";
        public const string ResultDeleted = "ResultDeleted";
    } 
}

