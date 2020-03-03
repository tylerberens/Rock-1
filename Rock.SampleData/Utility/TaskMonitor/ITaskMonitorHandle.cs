using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Diagnostics
{
    /// <summary>
    /// Allows access to and control of a Task Monitor.
    /// </summary>
    public interface ITaskMonitorHandle
    {
        event EventHandler<TaskLogUpdatedEventArgs> LogUpdated;

        TaskMonitor Monitor { get; }
        void Dispose();

        TaskActivityCollection Activities { get; }

        [Obsolete("Add an Activity to the Activities collection instead.")]
        //TaskAllocationHandle NewActivityAllocation( decimal percentAllocated );

        /// <summary>
        ///     Creates a new child activity of the Task.
        /// </summary>
        /// <param name="percentOfTotal"></param>
        /// <param name="activityName"></param>
        /// <param name="duration"></param>
        /// <returns>A handle to the new activity that can be used to control the lifetime of the activity.</returns>
        TaskActivityHandle AddChildActivity( decimal percentOfTotal, string activityName = null, TaskActivityDurationSpecifier duration = TaskActivityDurationSpecifier.Finite );

        /// <summary>
        ///     Adds a new child activity to the Task.
        /// </summary>
        /// <param name="percentOfTotal"></param>
        /// <param name="childActivity"></param>
        /// <returns>A handle to the new activity that can be used to control the lifetime of the activity.</returns>
        TaskActivityHandle AddChildActivity(decimal percentOfTotal, ITaskActivity childActivity);

        /// <summary>
        ///     Create and immediately start a new child activity for the Task.
        /// </summary>
        /// <param name="percentOfTotal"></param>
        /// <param name="activityName"></param>
        /// <param name="duration"></param>
        /// <returns>A handle to the new activity that can be used to control the lifetime of the activity.</returns>
        TaskActivityHandle StartChildActivity( decimal percentOfTotal, string activityName = null, TaskActivityDurationSpecifier duration = TaskActivityDurationSpecifier.Finite );

        /// <summary>
        ///     Create and immediately start a new child activity for the Task.
        /// </summary>
        /// <param name="percentOfTotal"></param>
        /// <param name="childActivity"></param>
        /// <returns>A handle to the new activity that can be used to control the lifetime of the activity.</returns>
        TaskActivityHandle StartChildActivity( decimal percentOfTotal, ITaskActivity childActivity );

        /// <summary>
        /// Returns a handle to the current activity for the Task.
        /// </summary>
        /// <returns></returns>
        TaskActivityHandle GetCurrentActivityHandle();

        #region Message Management

        ReadOnlyCollection<TaskMessage> Messages { get; }

        TaskMessage LogDebug( string message, params object[] args );
        TaskMessage LogError( string message, params object[] args );
        void LogException( Exception ex );
        void LogException( Exception ex, string message );
        TaskMessage LogInformation( string message, params object[] args );
        void LogMessage( TaskMessage message );
        TaskMessage LogMessage( TaskLogEventTypeSpecifier eventType, string message, params object[] args );
        TaskMessage LogInformation(TaskLogEventLevelSpecifier eventLevel, string message, params object[] args);

        //ProcessMessage AddMessage( string contentCode, ProcessLogEventTypeSpecifier eventType, string message, params object[] args );
        //ProcessMessage AddMessage( string actionId, string contentCode, ProcessLogEventTypeSpecifier eventType, ProcessLogEventLevelSpecifier eventLevel, string message, params object[] args );
        void LogMessages( IEnumerable<TaskMessage> messages );
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

        #endregion

        void ClearLog();

        Exception GetException();

        Exception GetException(IEnumerable<TaskLogEventTypeSpecifier> includeMessageTypes);

        TaskLogEventLevelSpecifier LogEventLevelFilter { get; set; }

        List<TaskLogEventTypeSpecifier> LogEventTypeFilter { get; }
    }
}