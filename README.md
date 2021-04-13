# Steamfitter.api Readme

This project provides a restful api for steamfitter functionality in the Crucible ecosystem. By default, steamfitter.api is available at `localhost:4400`, with the Swagger page at `localhost:4400/swagger/index.html`.

## Entity Description

**ScenarioTemplate:** A definition of a series of dispatch tasks that can be used to run a view.

**Scenario:** An instantiation of a series of dispatch tasks that run a particular view.

**Task:** An individual task that is defined to run on a group of VM's (defined by a VM mask) or that runs against an external API.

**Result:** The result from the API or a single VM of running a Task.  There will be a Result for each VM on which the Task was run. If no VM is associated with the Task, there will be one Result.

## Task Execution

1. An Ad-hoc Task must have a VmList associated with it.
2. A Scenario can have a Task that uses a VmMask, **only** if the Scenario is associated with a Player View.

## Reporting bugs and requesting features

Think you found a bug? Please report all Crucible bugs - including bugs for the individual Crucible apps - in the [cmu-sei/crucible issue tracker](https://github.com/cmu-sei/crucible/issues). 

Include as much detail as possible including steps to reproduce, specific app involved, and any error messages you may have received.

Have a good idea for a new feature? Submit all new feature requests through the [cmu-sei/crucible issue tracker](https://github.com/cmu-sei/crucible/issues). 

Include the reasons why you're requesting the new feature and how it might benefit other Crucible users.

## License

Copyright 2021 Carnegie Mellon University. See the [LICENSE.md](https://github.com/cmu-sei/Alloy.Api/blob/development/LICENSE.md) files for details.
