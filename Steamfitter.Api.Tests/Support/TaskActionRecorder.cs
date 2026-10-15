// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Steamfitter.Api.Services;

namespace Steamfitter.Api.Tests.Support;

/// <summary>
/// Stands in for the three task executors that leave the process (the Player VM API, SSH and SMTP), keeps
/// every call it was given, and answers each with <see cref="Output"/>.
/// </summary>
/// <remarks>
/// Written out rather than substituted, for the reason <c>HubRecorder</c> is: the run-wide factory
/// registers one instance for the whole run. A test reads it by naming something it owns, such as a value
/// in the task's action parameters (<see cref="CallsContaining"/>).
/// </remarks>
public sealed class TaskActionRecorder : IVmOperationsService, ISshService, IEmailService
{
    /// <summary>What every executor call returns, as a guest command or an SMTP send would.</summary>
    public const string Output = "recorded";

    private readonly ConcurrentQueue<TaskActionCall> _calls = new();

    /// <summary>A snapshot of the calls, in order.</summary>
    public IReadOnlyList<TaskActionCall> Calls => [.. _calls];

    /// <summary>The calls whose parameters contain <paramref name="marker"/>, a value the test owns.</summary>
    public IReadOnlyList<TaskActionCall> CallsContaining(string marker) =>
        [.. _calls.Where(x => x.Parameters != null && x.Parameters.Contains(marker))];

    private Task<string> Record(string operation, string parameters)
    {
        _calls.Enqueue(new TaskActionCall(operation, parameters));

        return Task.FromResult(Output);
    }

    public Task<string> GuestCommand(string parameters) => Record(nameof(GuestCommand), parameters);

    public Task<string> GuestCommandFast(string parameters) => Record(nameof(GuestCommandFast), parameters);

    public Task<string> GuestReadFile(string parameters) => Record(nameof(GuestReadFile), parameters);

    public Task<string> GuestFileUploadContent(string parameters) => Record(nameof(GuestFileUploadContent), parameters);

    public Task<string> GuestFileUploadFile(string parameters) => Record(nameof(GuestFileUploadFile), parameters);

    public Task<string> VmPowerOn(string parameters) => Record(nameof(VmPowerOn), parameters);

    public Task<string> VmPowerOff(string parameters) => Record(nameof(VmPowerOff), parameters);

    public Task<string> VmSnapshotCreate(string parameters) => Record(nameof(VmSnapshotCreate), parameters);

    public Task<string> VmSnapshotRevert(string parameters) => Record(nameof(VmSnapshotRevert), parameters);

    public Task<string> VmSnapshotDelete(string parameters) => Record(nameof(VmSnapshotDelete), parameters);

    public Task<string> SendLinuxRemoteCommand(string parameters) => Record(nameof(SendLinuxRemoteCommand), parameters);

    public Task<string> LinuxFileTouch(string parameters) => Record(nameof(LinuxFileTouch), parameters);

    public Task<string> LinuxRm(string parameters) => Record(nameof(LinuxRm), parameters);

    public Task<string> SendEmail(string parameters) => Record(nameof(SendEmail), parameters);
}

/// <summary>One executor call: the operation and the action parameters (the task's input string).</summary>
public sealed record TaskActionCall(string Operation, string Parameters);
