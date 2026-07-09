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
        // vm/ssh/email actions
        guest_process_run = 100,
        guest_file_read = 101,
        guest_file_write = 102,
        vm_hw_power_off = 103,
        vm_hw_power_on = 104,
        guest_process_run_fast = 107,
        guest_file_upload_content = 108,
        send_email = 109,
        guest_file_upload_file = 114,
        linux_file_touch = 115,
        linux_rm = 116,
        core_remote = 117,
        vm_snapshot_create = 120,
        vm_snapshot_revert = 121,
        vm_snapshot_delete = 122,
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


    public enum SystemPermission
    {
        CreateScenarioTemplates,
        ViewScenarioTemplates,
        EditScenarioTemplates,
        ManageScenarioTemplates,
        CreateScenarios,
        ViewScenarios,
        EditScenarios,
        ExecuteScenarios,
        ManageScenarios,
        ManageTasks,
        ViewUsers,
        ManageUsers,
        ViewRoles,
        ManageRoles,
        ViewGroups,
        ManageGroups
    }

    public enum ScenarioPermission
    {
        ViewScenario,
        EditScenario,
        ExecuteScenario,
        ManageScenario,
        ViewTasks
    }

    public enum ScenarioTemplatePermission
    {
        ViewScenarioTemplate,
        EditScenarioTemplate,
        ManageScenarioTemplate
    }

}
