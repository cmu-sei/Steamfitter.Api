// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Steamfitter.Api.Data
{
    public enum TaskAction
    {
        // http actions
        http_get = 11,
        http_post = 12,
        http_put = 13,
        http_delete = 14,
        // stackstorm actions
        guest_process_run = 100,
        guest_file_read = 101,
        guest_file_write = 102,
        vm_hw_power_off = 103,
        vm_hw_power_on = 104,
        vm_create_from_template = 105,
        vm_hw_remove = 106,
        guest_process_run_fast = 107,
        guest_file_upload_content = 108,
        send_email = 109,
        az_vm_shell_script = 110,
        az_get_vms = 111,
        az_vm_power_off = 112,
        az_vm_power_on = 113,
        guest_file_upload_file = 114,
        file_touch = 115,
    }

    public enum TaskStatus
    {
        none = 0,
        pending = 10,
        queued = 20,
        sent = 30,
        cancelled = 40,
        expired = 50,
        failed = 60,
        succeeded = 70,
        error = 80
    }

    public enum TaskTrigger
    {
        Time = 1,
        Success = 2,
        Failure = 3,
        Completion = 4,
        Expiration = 5,
        Manual = 6
    }

    public enum TaskIterationTermination
    {
        IterationCount = 0,
        UntilSuccess = 1,
        UntilFailure = 2
    }

    public enum ScenarioStatus
    {
        ready = 1,
        active = 2,
        paused = 3,
        ended = 4,
        archived = 5,
        error = 6
    }

}
