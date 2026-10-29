// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Steamfitter.Api.Data.Models;

namespace Steamfitter.Api.Services
{
    /// <summary>
    /// The action parameters that name the VM a Task runs on. A Task stores markers in them, and the task runner
    /// fills them from the VM it bound each result to, which is always one of the Scenario's View. The VM is never
    /// the Task's to choose: its VM Mask chooses among the View's VMs.
    /// </summary>
    public static class TaskVmParameters
    {
        public const string Moid = "Moid";
        public const string VmName = "VmName";
        public const string MoidMarker = "{moid}";
        public const string VmNameMarker = "{VmName}";

        /// <summary>Replaces any VM a Task's parameters name with the markers, as a Task is stored.</summary>
        public static void Reserve(IDictionary<string, string> actionParameters)
        {
            if (actionParameters == null)
                return;

            if (actionParameters.ContainsKey(Moid))
                actionParameters[Moid] = MoidMarker;

            if (actionParameters.ContainsKey(VmName))
                actionParameters[VmName] = VmNameMarker;
        }

        /// <summary>
        /// The input string a result runs with: the markers filled from the result's VM, and the Moid and VmName
        /// parameters set to that VM whatever the Task stored, so an imported Task or one made from an old result
        /// cannot run on a VM of another View. A result with no VM leaves them empty.
        /// </summary>
        public static string BindToVm(string inputString, ResultEntity result)
        {
            if (inputString == null)
                return null;

            var vmId = result.VmId?.ToString() ?? string.Empty;
            var vmName = result.VmName ?? string.Empty;
            inputString = inputString.Replace(MoidMarker, vmId).Replace(VmNameMarker, vmName);

            Dictionary<string, JsonElement> parameters;
            try
            {
                // a repeated key reads as its last value, as the executors read it
                parameters = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(inputString);
            }
            catch (JsonException)
            {
                // not a json object, so no executor can read a VM from it either
                return inputString;
            }

            if (parameters == null)
                return inputString;

            var bound = false;
            foreach (var key in parameters.Keys.ToList())
            {
                if (string.Equals(key, Moid, StringComparison.OrdinalIgnoreCase))
                {
                    parameters[key] = JsonSerializer.SerializeToElement(vmId);
                    bound = true;
                }
                else if (string.Equals(key, VmName, StringComparison.OrdinalIgnoreCase))
                {
                    parameters[key] = JsonSerializer.SerializeToElement(vmName);
                    bound = true;
                }
            }

            return bound ? JsonSerializer.Serialize(parameters) : inputString;
        }
    }
}
