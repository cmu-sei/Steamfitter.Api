// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Steamfitter.Api.Data.Models;

namespace Steamfitter.Api.Services
{

    public interface ITaskExecutionQueue
    {
        void Add(TaskEntity taskEntity);

        TaskEntity Take(CancellationToken cancellationToken);
    }

    public class TaskExecutionQueue : ITaskExecutionQueue
    {
        private BlockingCollection<TaskEntity> _taskExecutionQueue = new BlockingCollection<TaskEntity>();

        public void Add(TaskEntity taskEntity)
        {
            if (taskEntity == null)
            {
                throw new ArgumentNullException(nameof(taskEntity));
            }
            _taskExecutionQueue.Add(taskEntity);
        }

        public TaskEntity Take(CancellationToken cancellationToken)
        {
            return _taskExecutionQueue.Take(cancellationToken);
        }
    }

}
