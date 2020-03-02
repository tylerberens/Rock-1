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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace System.Diagnostics
{
    /// <summary>
    ///     A snapshot of the current status of a Task Activity
    /// </summary>
    [Serializable]
    public class TaskActivityBase : ITaskActivity
    {
        public bool LoggingEnabled { get; set; } = true;

        private decimal _ChildProcessAllocationPercent;
        private string _Description;
        private TaskActivityDurationSpecifier _Duration;
        private TaskActivityExecutionStateSpecifier _ExecutionState = TaskActivityExecutionStateSpecifier.Pending;
        private DateTime? _LastUpdated;
        private int _MinimumDuration;
        private string _Name;

        private ITaskActivity _ParentActivity;
        private decimal _RelativeProgress;
        private TaskActivityResultSpecifier _Result = TaskActivityResultSpecifier.None;

        public TaskActivityBase( string name, string description, DateTime? startTime, TaskActivityDurationSpecifier durationType )
        {
            ChildActivities = new TaskActivityCollection( this );

            _Name = name;
            _Description = description;

            StartTime = startTime.GetValueOrDefault( DateTime.Now );
            _Duration = durationType;
        }

        private readonly List<TaskLogEventTypeSpecifier> _LogEventTypeFilter = new List<TaskLogEventTypeSpecifier>();

        //TODO: This property currently has no effect. Should be used to filter the type of events that are added to the Task Log.
        public List<TaskLogEventTypeSpecifier> LogEventTypeFilter
        {
            get { return _LogEventTypeFilter; }
        }
                 
        private TaskLogEventLevelSpecifier _LogEventLevelFilter = TaskLogEventLevelSpecifier.Normal;

        //TODO: This property currently has no effect. Should be used to filter the type of events that are added to the Task Log. Replaces the property "LoggingEnabled".
        public TaskLogEventLevelSpecifier LogEventLevelFilter
        {
            get { return _LogEventLevelFilter; }
            set { _LogEventLevelFilter = value; }
        }

        public string StatusMessage { get; set; }

        /// <summary>
        ///     The total number of actions that comprise this Activity.
        /// </summary>
        public int ActionCount { get; set; }

        /// <summary>
        ///     The number of actions processed for this Activity.
        /// </summary>
        public int ProcessedCount { get; set; }

        /// <summary>
        ///     The number of errors that have occurred during the processing of this Activity.
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        ///     The number of warnings that have occurred during the processing of this Activity.
        /// </summary>
        public int WarningCount { get; set; }

        public TaskActivityResultSpecifier Result
        {
            get { return _Result; }
            set { _Result = value; }
        }

        public TaskActivityExecutionStateSpecifier ExecutionState
        {
            get { return _ExecutionState; }
            set { _ExecutionState = value; }
        }

        public TaskAllocationHandle NewActivityAllocation( decimal percentOfTotal )
        {
            _ChildProcessAllocationPercent = percentOfTotal;

            return new TaskAllocationHandle( Monitor, ID );
        }

        public void Update()
        {
            Update( -1, -1, string.Empty, null );
        }

        public void Update( decimal relativeProgress )
        {
            Update( relativeProgress, 100, string.Empty, null );
        }

        public void Update( string activityDescription )
        {
            Update( _RelativeProgress, 100, activityDescription, null );
        }

        public void Update( string activityDescription, params object[] args )
        {
            Update( _RelativeProgress, 100, activityDescription, args );
        }

        public void Update( decimal currentCount, decimal totalCount )
        {
            Update( currentCount, totalCount, string.Empty, null );
        }

        public void Update( decimal currentCount, decimal totalCount, string activityDescription )
        {
            Update( currentCount, totalCount, activityDescription, null );
        }

        /// <summary>
        ///     Signals that the activity has started.
        ///     If this method is not called explicitly, it will be called automatically when the Activity is first updated.
        /// </summary>
        public void BeginActivity()
        {
            if ( _ExecutionState == TaskActivityExecutionStateSpecifier.Pending )
            {
                // Ensure that the parent activity has been started.
                if ( _ParentActivity != null )
                {
                    _ParentActivity.BeginActivity();
                }

                _ExecutionState = TaskActivityExecutionStateSpecifier.InProgress;

                if ( LoggingEnabled )
                {
                    if ( !string.IsNullOrWhiteSpace( _Name ) )
                    {
                        var msg = new TaskMessage { EventType = TaskLogEventTypeSpecifier.ActivityStart,
                                                       EventLevel = TaskLogEventLevelSpecifier.Normal,
                                                       TransactionId = this.GetMessageTransactionId(),
                                                       Message = string.Format( "<-- Started: {0} -->", _Name ) };

                        this.Monitor.LogMessage( msg );
                    }
                }

                Monitor.NotifyActivityStarted( this );
            }
        }

        private string GetMessageTransactionId()
        {
            return "activity_" + this.ID.ToString();
        }

        /// <summary>
        ///     Indicates the desired relative weighting of this activity to others in the same processing group.
        ///     If set to zero, the allocation given to this activity will be the same as all other unallocated activities in the
        ///     same processing group.
        /// </summary>
        public decimal ActivityGroupRequestedAllocationPercent { get; set; }

        /// <summary>
        ///     Indicates the actual relative weighting of this task to others at the same processing level.
        /// </summary>
        public decimal ActivityGroupActualAllocationPercent { get; set; }

        public ReadOnlyCollection<TaskMessage> Messages
        {
            //TODO: Return messages for this activity only.
            get { return Monitor.Messages; }
        }

        public TaskMessage LogDebug( string message, params object[] args )
        {
            return this.LogMessage( TaskLogEventTypeSpecifier.Information, TaskLogEventLevelSpecifier.Low, message, args );
        }

        public TaskMessage LogError(string message, params object[] args)
        {
            return LogMessage( TaskLogEventTypeSpecifier.Error, TaskLogEventLevelSpecifier.Critical, message, args );
        }

        public TaskMessage LogException(Exception ex)
        {
            var msg = TaskLog.CreateException(ex, null);

            this.LogMessage(msg);

            return msg;
        }

        public TaskMessage LogException(Exception ex, string message)
        {
            // Add Exceptions to the message log.
            var tpm = TaskLog.CreateException( ex, message );

            this.LogMessage( tpm );

            return tpm;
        }

        public TaskMessage LogInformation( string message, params object[] args )
        {
            return this.LogMessage( TaskLogEventTypeSpecifier.Information, TaskLogEventLevelSpecifier.Normal, message, args );
        }

        public void LogMessage(TaskMessage message)
        {
            this.LogMessages( new List<TaskMessage> { message });
        }

        public TaskMessage LogInformation( TaskLogEventLevelSpecifier eventLevel, string message, params object[] args )
        {
            return this.LogMessage( TaskLogEventTypeSpecifier.Information, eventLevel, message, args );
        }

        public TaskMessage LogActivityStart( string contentCode, string message, params object[] args )
        {
            return this.LogMessage( contentCode, TaskLogEventTypeSpecifier.ActivityStart, TaskLogEventLevelSpecifier.Normal, message, args );
        }

        public TaskMessage LogActivityEnd( string contentCode, string message, params object[] args )
        {
            return this.LogMessage( contentCode, TaskLogEventTypeSpecifier.ActivityEnd, TaskLogEventLevelSpecifier.Normal, message, args );
        }

        public TaskMessage LogActivityProgress( string contentCode, string message, params object[] args )
        {
            return this.LogMessage( contentCode, TaskLogEventTypeSpecifier.ActivityProgress, TaskLogEventLevelSpecifier.Normal, message, args );
        }

        public TaskMessage LogActionSuccess( string message, params object[] args )
        {
            return this.LogMessage( string.Empty, TaskLogEventTypeSpecifier.ResultSuccess, TaskLogEventLevelSpecifier.Normal, message, args );
        }

        public TaskMessage LogActionFailure( string message, params object[] args )
        {
            return this.LogMessage( string.Empty, TaskLogEventTypeSpecifier.ResultFailure, TaskLogEventLevelSpecifier.High, message, args );
        }

        public TaskMessage LogMessage( TaskLogEventTypeSpecifier eventType, string message, params object[] args )
        {
            return this.LogMessage( string.Empty, eventType, TaskLogEventLevelSpecifier.Normal, message, args );
        }

        public TaskMessage LogMessage( TaskLogEventTypeSpecifier eventType, TaskLogEventLevelSpecifier eventLevel, string message, params object[] args )
        {
            return this.LogMessage( string.Empty, eventType, eventLevel, message, args );
        }

        public void LogMessages( IEnumerable<TaskMessage> messages )
        {
            if (messages == null)
            {
                return;
            }

            // Assign a TransactionID based on the unique identifier for this Activity.
            var allMessages = messages.ToList();

            foreach (var msg in allMessages )
            {
                msg.TransactionId = this.GetMessageTransactionId();
            }

            this.Monitor.LogMessages(allMessages);
        }

        public TaskMessage LogWarning( string message, params object[] args )
        {
            return this.LogMessage( TaskLogEventTypeSpecifier.Warning, TaskLogEventLevelSpecifier.Normal, message, args );
        }

        // TODO: All of the methods below should return a result that is specific to this Activity rather than the entire Task.
        public bool HasErrors
        {
            get { return this.Monitor.HasErrors; }
        }

        public bool HasExceptions
        {
            get { return this.Monitor.HasExceptions; }
        }

        public List<TaskMessage> GetWarningMessages()
        {
             return this.Monitor.GetWarningMessages();
        }

        public List<TaskMessage> GetErrorMessages()
        {
            return this.Monitor.GetErrorMessages();
        }

        public List<TaskMessage> GetResultMessages()
        {
            return this.Monitor.GetResultMessages();
        }

        public List<TaskMessage> GetExceptionMessages()
        {
            return this.Monitor.GetExceptionMessages();
        }

        public Exception GetException()
        {
            return this.Monitor.GetException();
        }

        public Exception GetException(IEnumerable<TaskLogEventTypeSpecifier> includeMessageTypes)
        {
            return this.Monitor.GetException( includeMessageTypes );
        }

        public void LogPurge()
        {
            var transactionId = this.GetMessageTransactionId();
            
            var activityMessages = this.Monitor.Messages.Where( m => m.TransactionId.Equals( transactionId, StringComparison.OrdinalIgnoreCase ) );

            this.Monitor.ProcessLogInternal.RemoveMessages( activityMessages );
        }

        public TaskMessage LogMessage( string contentCode, TaskLogEventTypeSpecifier eventType, TaskLogEventLevelSpecifier eventLevel, string message, params object[] args )
        {
            var msg = new TaskMessage();
            
            msg.ContentCode = contentCode;
            msg.EventType = eventType;
            msg.EventLevel = eventLevel;

            if ( args == null
                 || args.Length == 0 )
                msg.Message = message;
            else
                msg.Message = string.Format( message, args );

            this.LogMessage( msg );

            return msg;
        }

        /// <summary>
        ///     Update the status of the specified Activity
        /// </summary>
        /// <param name="currentCount"></param>
        /// <param name="totalCount"></param>
        /// <param name="activityDescription"></param>
        public void Update( decimal currentCount, decimal totalCount, string activityDescription, params object[] args )
        {
            if ( _ExecutionState == TaskActivityExecutionStateSpecifier.Pending )
                BeginActivity();
            else if ( _ExecutionState == TaskActivityExecutionStateSpecifier.Completed )
                return;

            // Substitute tokens in new activity description
            if ( args != null
                && args.Length > 0 )
                activityDescription = string.Format( activityDescription, args );

            // Update Activity Name and Description
            var updated = false;

            // Set Description
            if ( !string.IsNullOrWhiteSpace( activityDescription )
                && activityDescription != Description )
            {
                updated = true;

                Description = activityDescription;
            }

            // Update Progress
            if ( ChildActivities.Count == 0 )
            {
                // If this activity has no child activites, set the relative progress.
                decimal relativeProgress = 0;

                if ( totalCount <= 0 )
                    totalCount = 100;

                // Is progress greater than 100%?
                if ( currentCount > totalCount )
                    currentCount = totalCount;

                // Calculate Total Progress of Parent Activity
                if ( totalCount > 0 )
                {
                    relativeProgress = currentCount / totalCount * 100;

                    if ( relativeProgress > 100 )
                        relativeProgress = 100;
                }

                // Store Completion % of Current Task
                if ( RelativeProgress < relativeProgress )
                {
                    RelativeProgress = relativeProgress;

                    updated = true;
                }
            }

            if ( updated )
            {
                _LastUpdated = DateTime.Now;

                // Progress event not raised if task has been cancelled                
                if ( !Monitor.IsCancelled )
                {
                    Monitor.NotifyActivityUpdated( this );

                    // Force current thread to yield and allow notification events to be processed.
                    Thread.Yield();
                }
            }
        }

        /// <summary>
        ///     The percentage of this task completion that is represented by a new Child task
        /// </summary>
        [Obsolete]
        public decimal ChildProcessAllocationPercent
        {
            get { return _ChildProcessAllocationPercent; }

            set { _ChildProcessAllocationPercent = value; }
        }

        public string Description
        {
            get { return _Description; }

            set { _Description = value; }
        }

        public TaskActivityDurationSpecifier DurationType
        {
            get { return _Duration; }

            set { _Duration = value; }
        }

        public int ID { get; private set; }

        public int MinimiumDuration
        {
            get { return _MinimumDuration; }

            set { _MinimumDuration = value; }
        }

        public string Name
        {
            get { return _Name; }

            set { _Name = value; }
        }

        public decimal RelativeProgress
        {
            get
            {
                if ( ChildActivities.Count > 0 )
                {
                    decimal groupProgress = 0;

                    foreach ( var activity in ChildActivities )
                    {
                        var activityProgress = activity.ActivityGroupActualAllocationPercent * ( activity.RelativeProgress / 100 );

                        groupProgress += activityProgress;
                    }

                    if ( groupProgress > 100 )
                        groupProgress = 100;

                    return groupProgress;
                }
                return _RelativeProgress;
            }

            set
            {
                if ( ChildActivities.Count > 0 )
                    throw new Exception( "TaskActivity.SetRelativeProgress Action failed. Relative progress cannot be explicitly set for an Activity that has child activities." );

                _RelativeProgress = value;
            }
        }

        /// <summary>
        ///     An optional identifier that can be used to classify the type or nature of this activity.
        /// </summary>
        public virtual string TypeCode { get; set; }

        public DateTime StartTime { get; }

        public TaskActivityCollection ChildActivities { get; }

        public TaskMonitor Monitor { get; private set; }

        public ITaskActivity ParentActivity
        {
            get { return _ParentActivity; }
            set { _ParentActivity = value; }
        }

        public TaskActivityHandle AddChildActivity( decimal percentOfTotal, string activityName, string message )
        {
            return AddChildActivity( percentOfTotal, activityName, message, false, TaskActivityDurationSpecifier.Finite, 0 );
        }

        public TaskActivityHandle AddChildActivity( string message )
        {
            return AddChildActivity( 0, Name, message, false, TaskActivityDurationSpecifier.Unknown, 0 );
        }

        /// <summary>
        ///     Creates a new child activity and returns a handle that can be used to start, stop or update the activity.
        /// </summary>
        /// <param name="percentOfTotal"></param>
        /// <param name="activityName"></param>
        /// <param name="message"></param>
        /// <param name="canCancel"></param>
        /// <param name="duration"></param>
        /// <param name="minimumDuration"></param>
        /// <returns></returns>
        public TaskActivityHandle AddChildActivity( decimal percentOfTotal, string activityName, string message, bool canCancel, TaskActivityDurationSpecifier duration, int minimumDuration )
        {
            // Add New Child Activity
            var activityNew = ChildActivities.AddNew( activityName, message, DateTime.Now, minimumDuration );

            activityNew.ActivityGroupRequestedAllocationPercent = percentOfTotal;

            _ChildProcessAllocationPercent = percentOfTotal;

            return activityNew.CreateHandle();
        }

        public TaskActivityHandle AddChildActivity( decimal percentOfTotal, ITaskActivity subActivity )
        {
            _ChildProcessAllocationPercent = percentOfTotal;

            // Add New Child Activity
            ChildActivities.Add( subActivity, percentOfTotal );

            subActivity.AttachToMonitor( Monitor );

            return subActivity.CreateHandle();
        }

        public void EndActivity()
        {
            if ( _ExecutionState == TaskActivityExecutionStateSpecifier.Completed )
                return;

            if ( _ExecutionState == TaskActivityExecutionStateSpecifier.Pending )
                BeginActivity();

            // Set process to full completion if it is the current process and has a finite duration.
            var isCompleted = true;

            if ( _RelativeProgress < 100 )
            {
                isCompleted = false;

                Update( 100, 100 );
            }

            // Close all child activities of this activity.
            foreach ( var activity in ChildActivities )
                activity.EndActivity();

            // If this activity has a minimum duration, wait until it has elapsed
            if ( _MinimumDuration > 0 )
            {
                var elapsed = DateTime.Now.Subtract( StartTime );

                var waitTime = _MinimumDuration - (int)elapsed.TotalMilliseconds;

                if ( waitTime > 0 )
                {
                    // Send Status Report to transition task from Finite to Indefinite
                    var report = Monitor.GetStatusReport();

                    Monitor.SendTaskUpdateNotification( report );

                    Thread.Sleep( waitTime );
                }
            }

            // Mark this activity as complete and set the result flag.
            _ExecutionState = TaskActivityExecutionStateSpecifier.Completed;

            if ( ErrorCount > 0 )
                _Result = TaskActivityResultSpecifier.Failed;
            else if ( WarningCount > 0 )
                _Result = TaskActivityResultSpecifier.CompletedWithWarnings;
            else
                _Result = TaskActivityResultSpecifier.Succeeded;


            LastExecutionTime = DateTime.Now.Subtract( StartTime );

            if ( LoggingEnabled )
            {
                if ( !string.IsNullOrWhiteSpace( _Name ) )
                {
                    var resultDescription = isCompleted ? "Completed" : "Terminated";

                    var msg = new TaskMessage
                    {
                        EventType = TaskLogEventTypeSpecifier.ActivityEnd,
                        EventLevel = TaskLogEventLevelSpecifier.Normal,
                        TransactionId = this.GetMessageTransactionId(),
                        Message = string.Format( "<-- {0} [{2:0.000}s] -->", resultDescription, _Name, LastExecutionTime.TotalSeconds )
                    };

                    this.Monitor.LogMessage( msg );
                }
            }
        }

        public TimeSpan LastExecutionTime { get; set; }

        /// <summary>
        /// The filter level for messages emitted by this task.
        /// </summary>
        public TaskLogEventLevelSpecifier LogMessageFilterLevel { get; set; } = TaskLogEventLevelSpecifier.Normal;

        void ITaskMonitorComponent.AttachToMonitor( TaskMonitor monitor )
        {
            if ( monitor == null )
                throw new ArgumentNullException( "monitor" );

            Monitor = monitor;

            ID = monitor.GetNextActivityId();

            ChildActivities.AttachToMonitor( monitor );
        }

        public TaskActivityHandle CreateHandle()
        {
            return new TaskActivityHandle( Monitor, ID );
        }

        public void Fail( string message )
        {
            EndActivity();

            if (ErrorCount == 0)
            {
                ErrorCount = 1;
            }

            StatusMessage = message;

            Monitor.LogError( message );
        }

        public void Fail( Exception ex )
        {
            var e = ex;

            string message = string.Empty;

            while (e != null)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    message += "\n";
                }

                message += e.Message;

                e = e.InnerException;
            }

            Fail( message );
        }

        public DateTime? LastUpdated
        {
            get { return _LastUpdated; }
            set { _LastUpdated = value; }
        }

        public int ProcessingLevel
        {
            get
            {
                var parentActivity = ParentActivity;
                var level = 0;

                while ( parentActivity != null )
                {
                    level++;

                    parentActivity = parentActivity.ParentActivity;
                }

                return level;
            }
        }

        public override string ToString()
        {
            return Name ?? "{Unnamed Task}";
        }
    }
}