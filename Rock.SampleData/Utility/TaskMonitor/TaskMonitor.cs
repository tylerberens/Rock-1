// <copyright>
// Copyright by the Spark Development Network
// 
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.rockrms.com/license
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//  

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace System.Diagnostics
{
    /// <summary>
    ///     A counter to monitor and report the progress of a Task consisting of any number of child Activities.
    ///  The Task Monitor maintains a set of hierarchical Activities for which progress can be reported at any level.
    /// It also incorporates a log that can be used to record processing activity.
    /// </summary>
    public class TaskMonitor : ITaskMonitorHandle
    {
        /// <summary>
        /// If enabled, TaskMonitor state changes will be automatically written to the Process Log.
        /// </summary>
        //public bool ActivityLoggingEnabled = false;

        // The root activity to which all other child activities are linked
        private const int _RootActivityId = 1;

        private int _LastActivityId = 1;

        private ITaskActivity _RootActivity;
        private readonly TaskLog _Log;

        public TaskMonitor()
        {
            // Create a new root activity and attach it to this task monitor.
            _RootActivity = new TaskActivity();

            _RootActivity.AttachToMonitor( this );

            _Log = new TaskLog();

            _Log.BufferSize = 0;
        }

        public bool CanCancel { get; } = true;

        public bool IsCancelled { get { return _IsCancelled; } }

        private bool _IsCancelled = false;

        public void Cancel()
        {
            if ( _IsCancelled )
            {
                return;
            }

            // Set the cancel flag
            _IsCancelled = true;
        }

        public decimal RelativeProgress
        {
            get { return _RootActivity.RelativeProgress; }
        }

        public string TaskDescription { get; set; }

        public string TaskName { get; set; }

        public DateTime? StartTime { get; private set; }

        public TimeSpan ElapsedTime
        {
            get
            {
                if ( StartTime.HasValue )
                {
                    return DateTime.Now.Subtract( StartTime.Value );
                }
                return new TimeSpan( 0 );
            }
        }

        /// <summary>
        /// Returns the collection of top-level activities associated with this Task Monitor.
        /// Each of these activities may itself contain child activities.
        /// </summary>
        public TaskActivityCollection Activities
        {
            get { return _RootActivity.ChildActivities; }
        }

        /// <summary>
        /// Returns the top-level activity for the task monitor.
        /// </summary>
        public ITaskActivity Activity
        {
            get
            {
                return _RootActivity;
            }
        }

        public TaskActivityHandle GetCurrentActivityHandle()
        {
            var activities = this.Activities.Flatten();

            int maxActivityId;

            if ( activities.Any() )
            {
                maxActivityId = activities.Select( x => x.ID ).Max();
            }
            else
            {
                maxActivityId = 1;
            }

            return new TaskActivityHandle( this, maxActivityId );
        }

        public TaskAllocationHandle NewActivityAllocation( decimal percentOfTotal )
        {
            return _RootActivity.NewActivityAllocation( percentOfTotal );
        }

        internal int GetNextActivityId()
        {
            _LastActivityId++;

            return _LastActivityId;
        }

        internal void NotifyActivityStarted( ITaskActivity activity )
        {
            var report = GetStatusReport();

            // TODO: Send TaskActivityStatusReport instead?
            NotifyActivityStarted( report );
        }

        public TaskStatusReport GetStatusReport()
        {
            var report = new TaskStatusReport( _RootActivityId, _RootActivity.Name, _RootActivity.Description );

            var currentActivities = _RootActivity.ChildActivities.Flatten().Where( a => a.ExecutionState == TaskActivityExecutionStateSpecifier.InProgress );

            foreach ( var activity in currentActivities )
            {
                var activityReport = new TaskActivityStatusReport( activity.ID, activity.ProcessingLevel, activity.Name, activity.Description, activity.DurationType, activity.LastUpdated );

                activityReport.RelativeProgress = activity.RelativeProgress;

                report.CurrentActivities.Add( activityReport );
            }

            report.TaskRelativeProgress = _RootActivity.RelativeProgress;
            report.TotalErrors = _RootActivity.ErrorCount;
            report.TotalWarnings = _RootActivity.WarningCount;

            return report;
        }

        internal void NotifyActivityStarted( TaskStatusReport report )
        {
            if ( ActivityStarted != null )
            {
                ActivityStarted( this, new TaskMonitorUpdateEventArgs( report ) );
            }
        }

        internal void NotifyTaskUpdated( TaskStatusReport report )
        {
            if ( TaskUpdated != null )
            {
                TaskUpdated( this, new TaskMonitorUpdateEventArgs( report ) );
            }
        }

        internal void NotifyActivityUpdated( ITaskActivity activity )
        {
            if ( TaskUpdated != null )
            {
                // TODO: Send TaskActivityStatusReport instead?
                var report = GetStatusReport();

                TaskUpdated( this, new TaskMonitorUpdateEventArgs( report ) );
            }

            if ( activity.LoggingEnabled )
            {
                while ( this.RelativeProgress >= _NextRelativeProgressNotification )
                {
                    //this.Log.AddMessage(ProcessLogEventTypeSpecifier.ActivityProgress, ProcessLogEventLevelSpecifier.High, "Task Completion: {0:0.00}%", _NextRelativeProgressNotification);

                    _NextRelativeProgressNotification = _NextRelativeProgressNotification + 10;
                }
            }
        }

        private decimal _NextRelativeProgressNotification = 0;
        //private ReadOnlyCollection<ProcessMessage> _Messages;
        //private ProcessLogEventLevelSpecifier _LogEventLevelFilter;
        //private List<ProcessLogEventTypeSpecifier> _LogEventTypeFilter;

        public void EndTask()
        {
            _RootActivity.EndActivity();

            Reset();
        }

        public void InitializeTask()
        {
            Reset();
        }

        /// <summary>
        ///     Create and immediately start a new child activity for this Task.
        /// </summary>
        /// <param name="percentOfTotal"></param>
        /// <param name="activityName"></param>
        /// <param name="duration"></param>
        /// <returns>A handle to the new activity that can be used to control the lifetime of the activity.</returns>
        public TaskActivityHandle StartActivity( decimal percentOfTotal, string activityName, TaskActivityDurationSpecifier duration )
        {
            var activity = StartNewActivity( percentOfTotal, activityName, null, duration );

            activity.StartActivity();

            return activity;
        }

        /// <summary>
        ///     Creates a new child activity of the Task
        /// </summary>
        /// <param name="percentOfTotal"></param>
        /// <param name="activityName"></param>
        /// <param name="duration"></param>
        /// <returns>A handle to the new activity that can be used to control the lifetime of the activity.</returns>
        public TaskActivityHandle AddActivity( decimal percentOfTotal, string activityName, TaskActivityDurationSpecifier duration )
        {
            return StartNewActivity( percentOfTotal, activityName, null, duration );
        }

        public TaskActivityHandle AddActivity( string activityName )
        {
            return AddActivity( activityName, null, TaskActivityDurationSpecifier.Finite );
        }

        public TaskActivityHandle AddActivity( string activityName, string message )
        {
            return AddActivity( activityName, null, TaskActivityDurationSpecifier.Finite );
        }

        /// <summary>
        ///     Creates a new Activity.
        /// </summary>
        /// <param name="activityName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public TaskActivityHandle AddActivity( string activityName, string message, TaskActivityDurationSpecifier duration )
        {
            return StartNewActivity( 100, activityName, message, duration, 0 );
        }

        private TaskActivityHandle StartNewActivity( decimal percentOfTotal,
            string activityName,
            string activityDescription = null,
            TaskActivityDurationSpecifier duration = TaskActivityDurationSpecifier.Unknown,
            int minimumDuration = 0 )
        {
            var activity = new TaskActivity();

            activity.Name = activityName;
            activity.Description = activityDescription;
            activity.DurationType = duration;
            activity.MinimiumDuration = minimumDuration;

            var handle = StartActivity( percentOfTotal, activity );

            return handle;
        }

        /// <summary>
        /// Add a new Activity to the Task Monitor and launch it.
        /// </summary>
        /// <param name="percentOfTotal"></param>
        /// <param name="activity"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public TaskActivityHandle AddActivity( decimal percentOfTotal, ITaskActivity activity, string message = null )
        {
            var newActivity = _RootActivity.AddChildActivity( percentOfTotal, activity );

            return newActivity;
        }

        /// <summary>
        /// Add a new Activity to the Task Monitor and launch it.
        /// </summary>
        /// <param name="percentOfTotal"></param>
        /// <param name="activity"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public TaskActivityHandle StartActivity( decimal percentOfTotal, ITaskActivity activity, string message = null )
        {
            // Determine if this is the first activity to be started for this Task.
            bool isFirstActivity = ( _RootActivity.ChildActivities.Count == 0 );

            if ( isFirstActivity )
            {
                StartTime = DateTime.Now;
                _NextRelativeProgressNotification = 10;

                if ( Starting != null )
                {
                    Starting();
                }
            }

            var newActivity = this.AddActivity( percentOfTotal, activity, message );

            if ( isFirstActivity )
            {
                if ( activity.LoggingEnabled )
                {
                    // ??? Add a Task Start message to the log.            
                    TaskLogEventLevelSpecifier eventLevel = GetLogLevelForActivity( newActivity.Activity );

                    var msg = new TaskMessage { EventType = TaskLogEventTypeSpecifier.ActivityStart, EventLevel = eventLevel, Message = TaskName, Details = TaskDescription };

                    _Log.AddMessage( msg );
                }

                if ( Started != null )
                {
                    Started();
                }
            }

            // Start the new Activity and return a handle.
            newActivity.StartActivity();

            return newActivity;
        }

        private TaskLogEventLevelSpecifier GetLogLevelForActivity( ITaskActivity activity )
        {
            // Set an Event Level corresponding to the processing level for this activity.
            TaskLogEventLevelSpecifier eventLevel;

            if ( activity.ProcessingLevel == 1 )
            {
                eventLevel = TaskLogEventLevelSpecifier.Critical;
            }
            else if ( activity.ProcessingLevel == 2 )
            {
                eventLevel = TaskLogEventLevelSpecifier.Normal;
            }
            else
            {
                eventLevel = TaskLogEventLevelSpecifier.Low;
            }

            return eventLevel;
        }

        public void Reset()
        {
            _RootActivity = new TaskActivity( this.TaskName, this.TaskDescription, DateTime.Now, TaskActivityDurationSpecifier.Unknown);

            _IsCancelled = false;
        }

        internal void SendTaskUpdateNotification( TaskStatusReport report )
        {
            if ( TaskUpdated != null )
            {
                TaskUpdated( this, new TaskMonitorUpdateEventArgs( report ) );
            }
        }

        #region Events

        public delegate void ActivityEndingEventHandler( object sender, TaskMonitorUpdateEventArgs args );

        public delegate void ActivityStartedEventHandler( object sender, TaskMonitorUpdateEventArgs args );

        public delegate void FinishedEventHandler();

        public delegate void FinishingEventHandler();

        public delegate void StartedEventHandler();

        public delegate void StartingEventHandler();

        public delegate void TaskUpdatedEventHandler( object sender, TaskMonitorUpdateEventArgs args );

        public event ActivityEndingEventHandler ActivityEnding;

        public event ActivityStartedEventHandler ActivityStarted;

        public event FinishedEventHandler Finished;

        public event FinishingEventHandler Finishing;

        public event StartedEventHandler Started;

        public event StartingEventHandler Starting;

        /// <summary>
        ///     Occurs when the Task is modified in any way.
        /// </summary>
        public event TaskUpdatedEventHandler TaskUpdated;

        #endregion Events

        #region ITaskMonitor

        public event EventHandler<TaskLogUpdatedEventArgs> LogUpdated
        {
            add { _Log.LogUpdated += value; }
            remove { _Log.LogUpdated -= value; }
        }

        TaskMonitor ITaskMonitorHandle.Monitor
        {
            get { return this; }
        }

        void ITaskMonitorHandle.Dispose()
        {
            // no effect.
        }

        TaskActivityHandle ITaskMonitorHandle.AddChildActivity( decimal percentOfTotal, string activityName, TaskActivityDurationSpecifier duration )
        {
            return AddActivity( percentOfTotal, activityName, duration );
        }

        /// <summary>
        ///     Creates a new child activity of the Task
        /// </summary>
        /// <param name="percentOfTotal"></param>
        /// <param name="childActivity"></param>
        /// <returns>A handle to the new activity that can be used to control the lifetime of the activity.</returns>
        TaskActivityHandle ITaskMonitorHandle.AddChildActivity( decimal percentOfTotal, ITaskActivity childActivity )
        {
            var handle = StartActivity( percentOfTotal, childActivity );

            return handle;
        }

        /// <summary>
        ///     Create and immediately start a new child activity for this Task.
        /// </summary>
        /// <param name="percentOfTotal"></param>
        /// <param name="activityName"></param>
        /// <param name="duration"></param>
        /// <returns>A handle to the new activity that can be used to control the lifetime of the activity.</returns>
        TaskActivityHandle ITaskMonitorHandle.StartChildActivity( decimal percentOfTotal, string activityName, TaskActivityDurationSpecifier duration )
        {
            var activity = this.AddActivity( percentOfTotal, activityName, duration );

            activity.StartActivity();

            return activity;
        }

        /// <summary>
        ///     Create and immediately start a new child activity for this Task.
        /// </summary>
        /// <param name="percentOfTotal"></param>
        /// <param name="childActivity"></param>
        /// <returns>A handle to the new activity that can be used to control the lifetime of the activity.</returns>
        TaskActivityHandle ITaskMonitorHandle.StartChildActivity( decimal percentOfTotal, ITaskActivity childActivity )
        {
            var activity = this.AddActivity( percentOfTotal, childActivity );
            //var handle = StartActivity(percentOfTotal, childActivity);
            activity.StartActivity();

            return activity;
        }

        public ReadOnlyCollection<TaskMessage> Messages
        {
            get { return _Log.Messages; }
        }

        internal TaskLog ProcessLogInternal
        {
            get { return _Log; }
        }

        public TaskMessage LogDebug( string message, params object[] args )
        {
            return _RootActivity.LogDebug( message, args );
        }

        public TaskMessage LogError( string message, params object[] args )
        {
            return _RootActivity.LogError( message, args );
        }

        public void LogException( Exception ex )
        {
            _RootActivity.LogException( ex );
        }

        public void LogException( Exception ex, string message )
        {
            _RootActivity.LogException( ex, message );
        }

        public TaskMessage LogInformation( string message, params object[] args )
        {
            return _RootActivity.LogInformation( message, args );
        }

        public TaskMessage LogInformation( TaskLogEventLevelSpecifier eventLevel, string message, params object[] args )
        {
            return _RootActivity.LogInformation( eventLevel, message, args );
        }

        public void LogMessage( TaskMessage message )
        {
            _RootActivity.LogMessage( message );
        }

        public TaskMessage LogMessage( TaskLogEventTypeSpecifier eventType, string message, params object[] args )
        {
            return _RootActivity.LogMessage( eventType, message, args );
        }

        public void LogMessages( IEnumerable<TaskMessage> messages )
        {
            _Log.AddMessages( messages );
        }

        public TaskMessage LogActionFailure( string message, params object[] args )
        {
            return _RootActivity.LogActionFailure( message, args );
        }

        public TaskMessage LogActionSuccess( string message, params object[] args )
        {
            return _RootActivity.LogActionSuccess( message, args );
        }

        public TaskMessage LogWarning( string message, params object[] args )
        {
            return _RootActivity.LogWarning( message, args );
        }

        public void ClearLog()
        {
            _Log.Clear();
        }

        public Exception GetException()
        {
            return _Log.GetException();
        }

        public Exception GetException( IEnumerable<TaskLogEventTypeSpecifier> includeMessageTypes )
        {
            return _Log.GetException( includeMessageTypes );
        }

        public TaskLogEventLevelSpecifier LogEventLevelFilter
        {
            get { return _RootActivity.LogEventLevelFilter; }
            set { _RootActivity.LogEventLevelFilter = value; }
        }

        public List<TaskLogEventTypeSpecifier> LogEventTypeFilter
        {
            get { return _RootActivity.LogEventTypeFilter; }
        }

        /// <summary>
        ///     A flag indicating if the current Validation Result contains any Error messages.
        /// </summary>
        public bool HasErrors
        {
            get { return _Log.HasErrors; }
        }

        /// <summary>
        ///     A flag indicating if the current log contains any Error or Warning messages.
        /// </summary>
        public bool HasExceptions
        {
            get { return _Log.HasExceptions; }
        }

        public List<TaskMessage> GetWarningMessages()
        {
            return _Log.GetWarningMessages();
        }

        public List<TaskMessage> GetErrorMessages()
        {
            return _Log.GetErrorMessages();
        }

        /// <summary>
        /// Returns a collection of Success or Failure messages found in the log.
        /// </summary>
        /// <returns></returns>
        public List<TaskMessage> GetResultMessages()
        {
            return _Log.GetResultMessages();
        }

        /// <summary>
        /// Returns a collection of Errors and Warnings found in the log.
        /// </summary>
        /// <returns></returns>
        public List<TaskMessage> GetExceptionMessages()
        {
            return _Log.GetExceptionMessages();
        }

        #endregion
    }

    public enum TaskActivityDurationSpecifier
    {
        Unknown,
        Finite
    }

    public class TaskMonitorUpdateEventArgs
    {
        public TaskStatusReport TaskStatus;

        public TaskMonitorUpdateEventArgs( TaskStatusReport report )
        {
            TaskStatus = report;
        }
    }
}