using System.Collections.Generic;

namespace System.Diagnostics
{
    public enum TaskActivityExecutionStateSpecifier
    {
        Pending,
        InProgress,
        Completed
    }

    public enum TaskActivityResultSpecifier
    {
        None = 0,
        Succeeded = 1,
        Failed,
        CompletedWithWarnings
    }

    public interface ITaskActivity : ITaskMonitorComponent
    {
        /// <summary>
        /// If set to True, task state changes will be written to the log.
        /// </summary>
        bool LoggingEnabled { get; set; }
        List<TaskLogEventTypeSpecifier> LogEventTypeFilter { get; }
        TaskLogEventLevelSpecifier LogEventLevelFilter { get; set; }

        TaskActivityExecutionStateSpecifier ExecutionState { get; set; }
        TaskActivityResultSpecifier Result { get; set; }
        void BeginActivity();

        TaskAllocationHandle NewActivityAllocation( decimal percentOfTotal );
        void Update();
        void Update( decimal relativeProgress );
        void Update( string activityDescription );
        void Update( string activityDescription, params object[] args );
        void Update( decimal currentCount, decimal totalCount );
        void Update( decimal currentCount, decimal totalCount, string activityDescription );

        /// <summary>
        ///     Update the status of this Activity.
        /// </summary>
        /// <param name="currentCount"></param>
        /// <param name="totalCount"></param>
        /// <param name="activityDescription"></param>
        void Update( decimal currentCount, decimal totalCount, string activityDescription, params object[] args );

        /// <summary>
        ///     The percentage of this task completion that is represented by a new Child task
        /// </summary>
        decimal ChildProcessAllocationPercent { get; set; }

        string Description { get; set; }
        TaskActivityDurationSpecifier DurationType { get; set; }
        int ID { get; }
        int MinimiumDuration { get; set; }
        string Name { get; set; }
        decimal RelativeProgress { get; set; }

        int ErrorCount { get; set; }
        int WarningCount { get; set; }
        int ProcessingLevel { get; }

        ITaskActivity ParentActivity { get; set; }

        /// <summary>
        /// An optional identifier that can be used to classify the type or nature of this activity.
        /// </summary>
        string TypeCode { get; set; }

        //bool IsCompleted { get; }
        DateTime StartTime { get; }
        TaskActivityCollection ChildActivities { get; }
        TaskMonitor Monitor { get; }
        TaskActivityHandle AddChildActivity( decimal percentOfTotal, string activityName, string message );
        TaskActivityHandle AddChildActivity( string message );

        TaskActivityHandle CreateHandle();

        /// <summary>
        /// The timestamp of the most recent update to the status of this task.
        /// </summary>
        DateTime? LastUpdated { get; set; }

        /// <summary>
        ///     Opens and immediately starts a new child activity.
        /// </summary>
        /// <param name="percentOfTotal"></param>
        /// <param name="activityName"></param>
        /// <param name="message"></param>
        /// <param name="canCancel"></param>
        /// <param name="duration"></param>
        /// <param name="minimumDuration"></param>
        /// <returns></returns>
        TaskActivityHandle AddChildActivity( decimal percentOfTotal, string activityName, string message, bool canCancel, TaskActivityDurationSpecifier duration, int minimumDuration );

        TaskActivityHandle AddChildActivity( decimal percentOfTotal, ITaskActivity subActivity );

        //void OpenSubActivity( decimal percentOfTotal );
        void EndActivity();
        void Fail(string message);
        void Fail(Exception ex);

        /// <summary>
        /// Indicates the relative weighting of this task to others at the same processing level.
        /// </summary>
        decimal ActivityGroupRequestedAllocationPercent { get; set; }
        decimal ActivityGroupActualAllocationPercent { get; set; }

        void LogMessage( TaskMessage message );
        void LogMessages( IEnumerable<TaskMessage> messages );

        TaskMessage LogDebug( string message, params object[] args );
        TaskMessage LogError( string message, params object[] args );
        TaskMessage LogException( Exception ex );
        TaskMessage LogException( Exception ex, string message );
        TaskMessage LogInformation( string message, params object[] args );
        
        TaskMessage LogMessage( TaskLogEventTypeSpecifier eventType, string message, params object[] args );
        TaskMessage LogInformation( TaskLogEventLevelSpecifier eventLevel, string message, params object[] args );
        
        TaskMessage LogActionFailure( string message, params object[] args );
        TaskMessage LogActionSuccess( string message, params object[] args );
        TaskMessage LogWarning( string message, params object[] args );

        /// <summary>
        ///     A flag indicating if the message log contains any Error messages.
        /// </summary>
        bool HasErrors { get; }

        /// <summary>
        ///     A flag indicating if the current log contains any Error or Warning messages.
        /// </summary>
        bool HasExceptions { get; }

        List<TaskMessage> GetWarningMessages();

        List<TaskMessage> GetErrorMessages();

        /// <summary>
        /// Returns a collection of Success or Failure messages found in the log.
        /// </summary>
        /// <returns></returns>
        List<TaskMessage> GetResultMessages();

        /// <summary>
        /// Returns a collection of Errors and Warnings found in the log.
        /// </summary>
        /// <returns></returns>
        List<TaskMessage> GetExceptionMessages();

        Exception GetException();

        Exception GetException( IEnumerable<TaskLogEventTypeSpecifier> includeMessageTypes );

        /// <summary>
        /// Remove all messages related to this Activity from the Log.
        /// </summary>
        void LogPurge();
    }
}