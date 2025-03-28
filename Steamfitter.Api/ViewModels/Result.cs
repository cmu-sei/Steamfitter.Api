// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Steamfitter.Api.Data;
using System;
using System.Collections.Generic;

namespace Steamfitter.Api.ViewModels
{
    public interface IResult : IAuthorizationType
    {
        Guid Id { get; set; }
        Guid? TaskId { get; set; }
        Guid? VmId { get; set; }
        string VmName { get; set; }
        TaskStatus Status { get; set; }
        string ActualOutput { get; set; }
        DateTime SentDate { get; set; }
        DateTime StatusDate { get; set; }
    }

    public class ResultSummary : Base, IResult
    {
        public Guid Id { get; set; }
        public Guid? TaskId { get; set; }
        public Guid? VmId { get; set; }
        public string VmName { get; set; }
        public TaskStatus Status { get; set; }
        public string ActualOutput { get; set; }
        public DateTime SentDate { get; set; }
        public DateTime StatusDate { get; set; }
    }

    public class Result : Base, IResult
    {
        public Guid Id { get; set; }
        public Guid? TaskId { get; set; }
        public Guid? VmId { get; set; }
        public string VmName { get; set; }
        public string ApiUrl { get; set; }
        public TaskAction Action { get; set; }
        public Dictionary<string, string> ActionParameters { get; set; }
        public int ExpirationSeconds { get; set; }
        public int Iterations { get; set; }
        public int CurrentIteration { get; set; }
        public int IntervalSeconds { get; set; }
        public TaskStatus Status { get; set; }
        public string ExpectedOutput { get; set; }
        public string ActualOutput { get; set; }
        public DateTime SentDate { get; set; }
        public DateTime StatusDate { get; set; }
    }
}
