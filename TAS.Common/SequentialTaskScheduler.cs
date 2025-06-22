namespace TAS.Common
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Schedules tasks to run sequentially, ensuring only one task runs at a time.
    /// </summary>
    /// <remarks>
    /// This scheduler follows specific rules for queuing:
    /// 1. If no task is running, a new task starts immediately.
    /// 2. If a task is already running, the new task is queued to run after the current one completes.
    /// 3. If a task is already queued, a new task will *replace* the queued one. This ensures that
    ///    only the latest requested task is executed next.
    /// </remarks>
    public class SequentialTaskScheduler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly object _lock = new object();

        private Task _runningTask = Task.CompletedTask;

        private Func<Task> _waitingTaskFactory;

        /// <summary>
        /// Schedules an task factory to create and run it, according to the scheduler's rules.
        /// </summary>
        /// <param name="taskFactory">The asynchronous action to execute. It must return a Task.</param>
        public void Schedule(Func<Task> taskFactory)
        {
            lock (_lock)
            {
                // Rule 3: If there is a task waiting, this new one replaces it.
                _waitingTaskFactory = taskFactory;

                // If the scheduler is currently idle (_runningTask is completed),
                // start processing the newly scheduled task.
                if (_runningTask.IsCompleted)
                {
                    // Kick off the processing loop. We don't await it here.
                    _runningTask = ProcessNextTaskAsync();
                }
            }
        }

        /// <summary>
        /// The core processing loop that executes tasks sequentially.
        /// </summary>
        private async Task ProcessNextTaskAsync()
        {
            while (true)
            {
                Func<Task> taskToRun;

                lock (_lock)
                {
                    // Dequeue the waiting task.
                    taskToRun = _waitingTaskFactory;
                    _waitingTaskFactory = null;

                    // If no task is waiting, the work is done.
                    if (taskToRun == null)
                    {
                        _runningTask = Task.CompletedTask;
                        break;
                    }

                    // A new task starts its run. We assign its execution to _runningTask.
                    _runningTask = ExecuteSafelyAsync(taskToRun);
                }

                // Await the task outside the lock to avoid holding the lock during user code execution.
                await _runningTask.ConfigureAwait(false);
            }
        }

        private async Task ExecuteSafelyAsync(Func<Task> taskFactory)
        {
            try
            {
                await taskFactory().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "A scheduled task failed in SequentialTaskScheduler.");
            }
        }
    }
}
