# steamfitter.api

This project provides a restful api for steamfitter functionality in the Crucible ecosystem.

By default, steamfitter.api is available at localhost:4400, with the swagger page at localhost:4400/swagger/index.html.

# Entity Description
<b>ScenarioTemplate:</b> A definition of a series of dispatch tasks that can be used to run a view

<b>Scenario:</b> An instantiation of a series of dispatch tasks that run a particular view.

<b>Task:</b> An individual task that is defined to run on a group of VM's (defined by a VM mask) or that runs against an external API.

<b>Result:</b> The result from the API or a single VM of running a Task.  There will be a Result for each VM on which the Task was run. If no VM is associated with the Task, there wil be one Result.

# Task Execution
1. Ad-hoc Tasks must have a VmList associated with it.
2. A Scenario can have a Task that uses a VmMask, <b>ONLY</b> if the Scenario is associated with a Player View.

## Reporting bugs and requesting features

Think you found a bug? Please report all Crucible bugs - including bugs for the individual Crucible apps - in the [cmu-sei/crucible issue tracker](https://github.com/cmu-sei/crucible/issues). 

Include as much detail as possible including steps to reproduce, specific app involved, and any error messages you may have received.

Have a good idea for a new feature? Submit all new feature requests through the [cmu-sei/crucible issue tracker](https://github.com/cmu-sei/crucible/issues). 

Include the reasons why you're requesting the new feature and how it might benefit other Crucible users.
