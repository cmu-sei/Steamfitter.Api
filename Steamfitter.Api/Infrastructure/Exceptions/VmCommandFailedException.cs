// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Steamfitter.Api.Infrastructure.Exceptions
{
    /// <summary>
    /// Thrown when a guest process executed on a VM ran to completion but reported failure
    /// (non-zero exit code). Carries the guest output and exit code so task execution can mark
    /// the result failed and surface the command's output/error to the user.
    /// </summary>
    public class VmCommandFailedException : Exception
    {
        public int ExitCode { get; }

        public VmCommandFailedException(string message, int exitCode)
            : base(message)
        {
            ExitCode = exitCode;
        }
    }
}
