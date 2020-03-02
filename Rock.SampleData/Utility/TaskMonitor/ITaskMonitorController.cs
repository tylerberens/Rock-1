namespace System.Diagnostics
{
    public interface ITaskMonitorController
    {
        event TaskMonitor.ActivityEndingEventHandler ActivityEnding;
        event TaskMonitor.ActivityStartedEventHandler ActivityStarted;
        event TaskMonitor.FinishedEventHandler Finished;
        event TaskMonitor.FinishingEventHandler Finishing;
        event TaskMonitor.StartedEventHandler Started;
        event TaskMonitor.StartingEventHandler Starting;

        /// <summary>
        ///     Occurs when the Task is modified in any way.
        /// </summary>
        event TaskMonitor.TaskUpdatedEventHandler TaskUpdated;

        TaskLog Log { get; }
        TaskActivityCollection Activities { get; }
        bool CanCancel { get; }
        TaskActivity CurrentActivity { get; }
        bool IsCancelled { get; set; }
        decimal RelativeProgress { get; }
        TaskStatusReport Status { get; }
        string TaskDescription { get; set; }
        string TaskName { get; set; }
        DateTime? StartTime { get; }
        TimeSpan ElapsedTime { get; }
        void EndTask();
        TaskStatusReport GetStatusReport();
        void InitializeTask();

        /// <summary>
        ///     Creates a new child activity of the Task
        /// </summary>
        /// <param name="percentOfTotal"></param>
        /// <param name="activityName"></param>
        /// <param name="duration"></param>
        /// <returns>A handle to the new activity that can be used to control the lifetime of the activity.</returns>
        TaskActivityHandle NewActivity( decimal percentOfTotal, string activityName, TaskActivityDurationSpecifier duration );

        TaskActivityHandle NewActivity( string activityName );
        TaskActivityHandle NewActivity( string activityName, string message );

        /// <summary>
        ///     Creates a new Activity.
        /// </summary>
        /// <param name="activityName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        TaskActivityHandle NewActivity( string activityName, string message, TaskActivityDurationSpecifier duration );

        TaskAllocationHandle NewActivityAllocation( decimal percentOfTotal );
        void Reset();
    }
}