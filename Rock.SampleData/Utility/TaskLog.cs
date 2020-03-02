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
using System.Text;

namespace System.Diagnostics
{
    /// <summary>
    /// An in-memory message log that accepts and stores simple messages that relate to a specific activity or process.
    /// Useful for collating information, warning and error messages for deferred processing during a complex task.
    /// </summary>
    public class TaskLog : IDisposable, ITaskLog
    {
        public event EventHandler<TaskLogUpdatedEventArgs> LogUpdated;

        private TaskLogEventLevelSpecifier _FilterLevel = TaskLogEventLevelSpecifier.Low;

        private readonly List<TaskLogEventTypeSpecifier> _EventTypeFilter = new List<TaskLogEventTypeSpecifier>();
        private readonly List<TaskMessage> _Messages = new List<TaskMessage>();

        private int _NextSequenceNo = 1;

        /// <summary>
        /// If set to True, an Exception will be thrown immediately an Error message is added to the log.
        /// </summary>
        public bool ThrowOnError = false;

        /// <summary>
        /// If set to True, an Exception will be thrown immediately a Warning message is added to the log.
        /// </summary>
        public bool ThrowOnWarning = false;

        public string ExceptionHeaderMessage = null;

        public TaskLog()
        {
            //
        }

        public TaskLog( IEnumerable<TaskMessage> messages )
        {
            AddMessages( messages );
        }

        /// <summary>
        ///     Attach the Task Log to an existing log device and start a new task.
        /// </summary>
        /// <param name="taskName"></param>
        /// <param name="args"></param>
        public TaskLog( string taskName, params object[] args )
            : this()
        {
            //_Log = logDevice;

            //if (logDevice == null)
            //    throw new ArgumentNullException("logDevice");

            if ( args != null
                && args.Length > 0 )
                taskName = string.Format( taskName, args );

            if ( !string.IsNullOrEmpty( taskName ) )
                StartTask( taskName );
        }

        public int BufferSize = 1000;

        /// <summary>
        ///     A flag indicating if the current Validation Result contains any Error messages.
        /// </summary>
        public bool HasErrors
        {
            get { return _Messages.Any( x => x.EventType == TaskLogEventTypeSpecifier.Error ); }
        }

        /// <summary>
        ///     A flag indicating if the current log contains any Error or Warning messages.
        /// </summary>
        public bool HasExceptions
        {
            get { return _Messages.Any( x => x.EventType == TaskLogEventTypeSpecifier.Error || x.EventType == TaskLogEventTypeSpecifier.Warning ); }
        }

        public List<TaskMessage> GetWarningMessages()
        {
            return _Messages.Where( x => x.EventType == TaskLogEventTypeSpecifier.Warning ).ToList();
        }

        public List<TaskMessage> GetErrorMessages()
        {
            return _Messages.Where( x => x.EventType == TaskLogEventTypeSpecifier.Error ).ToList();
        }

        /// <summary>
        /// Returns a collection of Success or Failure messages found in the log.
        /// </summary>
        /// <returns></returns>
        public List<TaskMessage> GetResultMessages()
        {
            return _Messages.Where( x => x.EventType == TaskLogEventTypeSpecifier.ResultFailure || x.EventType == TaskLogEventTypeSpecifier.ResultSuccess ).ToList();
        }

        /// <summary>
        /// Returns a collection of Errors and Warnings found in the log.
        /// </summary>
        /// <returns></returns>
        public List<TaskMessage> GetExceptionMessages()
        {
            return _Messages.Where( x => x.EventType == TaskLogEventTypeSpecifier.Error || x.EventType == TaskLogEventTypeSpecifier.Warning ).ToList();
        }

        public ReadOnlyCollection<TaskMessage> Messages
        {
            get { return _Messages.AsReadOnly(); }
        }

        public void RemoveMessages( IEnumerable<TaskMessage> messages )
        {
            _Messages.RemoveAll( messages.Contains );
        }

        public TaskMessage AddError( string message, params object[] args )
        {
            return AddMessage( TaskLogEventTypeSpecifier.Error, TaskLogEventLevelSpecifier.Critical, message, args );
        }

        public TaskMessage AddException( Exception ex )
        {
            return this.AddException( ex, null );
        }

        public TaskMessage AddException( Exception ex, string message )
        {
            // Add Exceptions to the message log.
            var tpm = CreateException( ex, message );

            AddMessage( tpm );

            return tpm;
        }

        public static TaskMessage CreateException( Exception ex, string message )
        {
            // Add Exceptions to the message log.
            // Each Exception is logged as a seperate entry, and the innermost exception is logged first to reflect the order in which the exceptions occurred.
            var exceptions = new List<Exception>();

            var exCurrent = ex;

            while ( exCurrent != null )
            {
                exceptions.Add( exCurrent );

                exCurrent = exCurrent.InnerException;
            }

            var tpm = new TaskMessage { EventType = TaskLogEventTypeSpecifier.Error, EventLevel = TaskLogEventLevelSpecifier.Critical };

            string details = string.Empty;

            // Set the Error Message using the top-level Exception.            
            if ( string.IsNullOrWhiteSpace( message ) )
                message = ex.Message;
            else
                details = ex.Message;

            tpm.Message = message;

            // Log inner Exceptions as Detail.
            if ( exceptions.Count > 1 )
            {
                foreach ( var exception in exceptions )
                {
                    if ( !string.IsNullOrWhiteSpace( details ) )
                        details = details + "\n";

                    details = details + exception.Message;
                }
            }

            tpm.Details = details;

            //AddMessage( tpm );

            return tpm;
        }

        public TaskMessage AddDebug( string message, params object[] args )
        {
            return this.AddMessage( string.Empty, string.Empty, TaskLogEventTypeSpecifier.Information, TaskLogEventLevelSpecifier.Low, message, args );
        }

        public TaskMessage AddInformation( string message, params object[] args )
        {
            return this.AddMessage( string.Empty, string.Empty, TaskLogEventTypeSpecifier.Information, TaskLogEventLevelSpecifier.Normal, message, args );
        }

        public TaskMessage AddInformation( TaskLogEventLevelSpecifier eventLevel, string message, params object[] args )
        {
            return this.AddMessage( string.Empty, string.Empty, TaskLogEventTypeSpecifier.Information, eventLevel, message, args );
        }

        public TaskMessage AddActivityStart( string topicId, string message, params object[] args )
        {
            return this.AddMessage( topicId, string.Empty, TaskLogEventTypeSpecifier.ActivityStart, TaskLogEventLevelSpecifier.Normal, message, args );
        }

        public TaskMessage AddActivityEnd( string topicId, string message, params object[] args )
        {
            return this.AddMessage( topicId, string.Empty, TaskLogEventTypeSpecifier.ActivityEnd, TaskLogEventLevelSpecifier.Normal, message, args );
        }

        public TaskMessage AddActivityProgress( string topicId, string message, params object[] args )
        {
            return this.AddMessage( topicId, string.Empty, TaskLogEventTypeSpecifier.ActivityProgress, TaskLogEventLevelSpecifier.Normal, message, args );
        }

        public TaskMessage AddActionSuccess( string message, params object[] args )
        {
            return this.AddMessage( string.Empty, string.Empty, TaskLogEventTypeSpecifier.ResultSuccess, TaskLogEventLevelSpecifier.Normal, message, args );
        }

        public TaskMessage AddActionFailure( string message, params object[] args )
        {
            return this.AddMessage( string.Empty, string.Empty, TaskLogEventTypeSpecifier.ResultFailure, TaskLogEventLevelSpecifier.High, message, args );
        }

        public TaskMessage AddMessage( TaskLogEventTypeSpecifier eventType, string message, params object[] args )
        {
            return this.AddMessage( string.Empty, string.Empty, eventType, TaskLogEventLevelSpecifier.Normal, message, args );
        }

        public TaskMessage AddMessage( TaskLogEventTypeSpecifier eventType, TaskLogEventLevelSpecifier eventLevel, string message, params object[] args )
        {
            return this.AddMessage( string.Empty, string.Empty, eventType, eventLevel, message, args );
        }

        public TaskMessage AddMessage( string contentCode, TaskLogEventTypeSpecifier eventType, string message, params object[] args )
        {
            return this.AddMessage( string.Empty, contentCode, eventType, TaskLogEventLevelSpecifier.Normal, message, args );
        }

        public TaskMessage AddMessage( string transactionId, string contentCode, TaskLogEventTypeSpecifier eventType, TaskLogEventLevelSpecifier eventLevel, string message, params object[] args )
        {
            var msg = new TaskMessage();

            msg.TransactionId = transactionId;
            msg.ContentCode = contentCode;
            msg.EventType = eventType;
            msg.EventLevel = eventLevel;

            if ( args == null
                || args.Length == 0 )
                msg.Message = message;
            else
                msg.Message = string.Format( message, args );

            this.AddMessages( new[] { msg } );

            return msg;
        }

        /// <summary>
        ///     Adds a collection of messages to the log.
        /// </summary>
        /// <param name="messages">The messages.</param>
        public void AddMessages( IEnumerable<TaskMessage> messages )
        {
            if ( messages == null )
            {
                return;
            }

            List<TaskMessage> filteredMessages = messages.ToList();

            // Assign sequence numbers in the order that the messages arrive.
            foreach ( var message in filteredMessages )
            {
                message.SequenceNo = _NextSequenceNo;

                _NextSequenceNo++;
            }

            if ( _EventTypeFilter.Any() )
            {
                filteredMessages = filteredMessages.Where( m => (int)m.EventLevel >= (int)_FilterLevel && _EventTypeFilter.Contains( m.EventType ) ).ToList();
            }
            else
            {
                filteredMessages = filteredMessages.Where( m => (int)m.EventLevel >= (int)_FilterLevel ).ToList();
            }

            if ( !filteredMessages.Any() )
            {
                return;
            }

            // Add all of the messages to the buffer.
            bool hasErrors = false;
            bool hasWarnings = false;

            foreach ( var message in filteredMessages )
            {
                _Messages.Add( message );

                switch ( message.EventType )
                {
                    case TaskLogEventTypeSpecifier.Error:
                        {
                            _ErrorCount++;
                            hasErrors = true;
                        }
                        break;
                    case TaskLogEventTypeSpecifier.Warning:
                        {
                            _WarningCount++;
                            hasWarnings = true;
                        }
                        break;
                }
            }

            // Trim the message collection to the specified buffer size;
            if ( this.BufferSize > 0 )
            {
                while ( _Messages.Count > this.BufferSize )
                {
                    _Messages.RemoveAt( 0 );
                }
            }

            this.LogUpdated?.Invoke( this, new TaskLogUpdatedEventArgs( filteredMessages ) );

            // Check if an exception should be thrown.
            if ( hasErrors && this.ThrowOnError )
                this.ThrowExceptions();

            if ( hasWarnings && this.ThrowOnWarning )
                this.ThrowExceptions();
        }

        public void AddMessage( TaskMessage message )
        {
            this.AddMessages( new TaskMessage[] { message } );
        }

        public TaskMessage AddWarning( string message, params object[] args )
        {
            return AddMessage( TaskLogEventTypeSpecifier.Warning, message, args );
        }

        public void Clear()
        {
            _Messages.Clear();
        }

        public Exception GetException()
        {
            var messageTypes = new List<TaskLogEventTypeSpecifier>();

            messageTypes.Add( TaskLogEventTypeSpecifier.Error );

            return this.GetException( messageTypes );
        }

        /// <summary>
        ///     Create an Exception for the Validation Result if it contains any errors
        /// </summary>
        /// <returns>An Exception object containing Errors emitted during the validation process, or Null if there are no Errors</returns>
        public Exception GetException( IEnumerable<TaskLogEventTypeSpecifier> includeMessageTypes )
        {
            var errors = _Messages.Where( x => includeMessageTypes.Contains( x.EventType ) ).ToList();

            Exception ex = null;

            if ( errors.Any() )
            {
                foreach ( var taskMessage in errors )
                {
                    // Create a nested Exception, with the first log message as the innermost Exception
                    if ( ex == null )
                        ex = new Exception( taskMessage.Message );
                    else
                        ex = new Exception( taskMessage.Message, ex );
                }
            }

            return ex;
        }

        /// <summary>
        ///     Create and throw an Exception if the log contains any message types that are to be regarded as errors.
        /// </summary>
        public void ThrowExceptions()
        {
            var ex = this.GetException();

            if ( ex != null )
                throw ex;
        }

        public TaskMessage[] ToArray()
        {
            return _Messages.ToArray();
        }

        /// <summary>
        ///     Returns a text summary of the messages in the collection
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();

            var itemNo = 0;

            foreach ( TaskMessage msg in _Messages )
            {
                itemNo++;

                string typeName;

                switch ( msg.EventType )
                {
                    case TaskLogEventTypeSpecifier.Error:
                    default:
                        typeName = "ERROR";
                        break;

                    case TaskLogEventTypeSpecifier.Warning:
                        typeName = "WARNING";
                        break;

                    case TaskLogEventTypeSpecifier.Information:
                        typeName = "INFORMATION";
                        break;
                }

                if ( itemNo > 1 )
                    sb.Append( "\n" );

                sb.AppendFormat( "{0:hh:mm:ss.ff} [{1}] {2}", msg.TimeStamp, typeName, msg.Message );

                if ( msg.Details.Length > 0 )
                    sb.AppendFormat( "\n" + msg.Details );
            }

            return sb.ToString();
        }


        #region Tasklog Additions

        private TaskLogWorkItem _ActiveItem;
        private string _ActiveTaskName = string.Empty;
        private int _ErrorCount;
        private DateTime? _FinishTime;
        private bool _ItemWarningHeaderWritten;
        private DateTime? _StartTime;
        private bool _TaskWarningHeaderWritten;
        private int _TotalCount;
        private int _WarningCount;
        private readonly Stopwatch _WorkItemStopwatch = new Stopwatch();
        private decimal _WorkItemAcceptableExecutionTime;
        private readonly int _BufferCapacity = 1000;

        private readonly object _LockQueue = new Object();
        //private readonly Queue<TraceMessage> _Messages;

        private readonly List<TaskLogWorkItem> _WorkItems = new List<TaskLogWorkItem>();

        public string TrackingId { get; set; }

        public string ActiveWorkItem
        {
            get { return _ActiveItem == null ? string.Empty : _ActiveItem.Name; }
        }

        public int CurrentCount
        {
            get { return _WorkItems.Count; }
        }

        /// <summary>
        ///     The date and time when this Task started.
        /// </summary>
        public TimeSpan ElapsedTime
        {
            get
            {
                if ( _StartTime != null )
                    if ( _FinishTime != null )
                        return _FinishTime.Value.Subtract( _StartTime.Value );
                    else
                        return DateTime.Now.Subtract( _StartTime.Value );
                return new TimeSpan( 0 );
            }
        }

        public int ErrorCount
        {
            get { return _ErrorCount; }
            set { _ErrorCount = value; }
        }

        public DateTime? FinishTime
        {
            get { return _FinishTime; }
            set { _FinishTime = value; }
        }

        public DateTime? StartTime
        {
            get { return _StartTime; }
            set { _StartTime = value; }
        }

        public int TotalCount
        {
            get { return _TotalCount; }
            set { _TotalCount = value; }
        }

        public int WarningCount
        {
            get { return _WarningCount; }
            set { _WarningCount = value; }
        }

        public void Dispose()
        {
            FinishTask();
        }

        public TaskLogWorkItem LastWorkItem
        {
            get { return _WorkItems.LastOrDefault(); }
        }

        /// <summary>
        /// Get or set the maximum number of seconds in which a work item is expected to complete.
        /// If the execution time exceeds this threshold, a warning will be recorded in the log.
        /// </summary>
        public decimal WorkItemAcceptableExecutionTime
        {
            get { return _WorkItemAcceptableExecutionTime; }
            set { _WorkItemAcceptableExecutionTime = value; }
        }

        public List<TaskLogEventTypeSpecifier> EventTypeFilter
        {
            get { return _EventTypeFilter; }
        }

        public TaskLogEventLevelSpecifier FilterLevel
        {
            get { return _FilterLevel; }
            set { _FilterLevel = value; }
        }

        public ReadOnlyCollection<TaskLogWorkItem> WorkItems()
        {
            return _WorkItems.AsReadOnly();
        }

        public void FinishTask()
        {
            // If there is an outstanding work item, close it off
            FinishWorkItem();

            _FinishTime = DateTime.Now;

            // Close the task
            var msg = "--\n-- Task Finished: " + _ActiveTaskName + "\n--\n";

            int itemsProcessed = _WorkItems.Count;

            if ( itemsProcessed > 0
                || _TotalCount > 0 )
            {
                // Show task statistics.
                msg += string.Format( "-- Processed {0} items in {1:hh\\:mm\\:ss\\.ff}s.\n", itemsProcessed, ElapsedTime );

                decimal totalExecutionTime = 0;
                decimal minTime = 0;
                decimal maxTime = 0;

                foreach ( var command in _WorkItems )
                {
                    totalExecutionTime += command.ExecutionTimeInSeconds;

                    if ( minTime == 0
                        || minTime > command.ExecutionTimeInSeconds )
                        minTime = command.ExecutionTimeInSeconds;

                    if ( maxTime == 0
                        || maxTime < command.ExecutionTimeInSeconds )
                        maxTime = command.ExecutionTimeInSeconds;
                }

                decimal averageExecutionTime = totalExecutionTime / (decimal)itemsProcessed;

                msg += string.Format( "-- (Minimum={0:00.00}s, Maximum={1:00.00}s, Average={2:00.00}s)\n", minTime, maxTime, averageExecutionTime );
            }
            else
                msg += string.Format( "-- Processing completed in {0:hh\\:mm\\:ss\\.ff}s.\n", ElapsedTime );

            if ( _ErrorCount > 0
                || _WarningCount > 0 )
                msg += string.Format( "-- ({0} errors, {1} warnings)\n", _ErrorCount, _WarningCount );

            this.AddInformation( msg );
            //TraceInformation( msg );

            _ActiveTaskName = string.Empty;
        }

        /// <summary>
        ///     Signals the end of processing for the current work item.
        /// </summary>
        public void FinishWorkItem()
        {
            if ( _ActiveItem == null )
                return;

            _WorkItemStopwatch.Stop();

            _ActiveItem.IsCompleted = true;

            // Reset the current work item
            _ActiveItem.ExecutionTimeInSeconds = (decimal)_WorkItemStopwatch.Elapsed.TotalSeconds;

            // Log an activity completion message.
            var message = "Activity Completed: " + _ActiveItem.Name;

            message += string.Format( " ({0:00.00}s)", _ActiveItem.ExecutionTimeInSeconds );

            this.AddInformation( message );

            if ( _WorkItemAcceptableExecutionTime > 0
                && _ActiveItem.ExecutionTimeInSeconds > _WorkItemAcceptableExecutionTime )
            {
                this.AddWarning( "Work Item processing exceeded the maximum expected execution time of {0:00.00}s", _WorkItemAcceptableExecutionTime );
            }

            _ActiveItem = null;
        }

        /// <summary>
        ///     Initializes a new task and resets all existing work item counters.
        /// </summary>
        public void StartTask( string taskName, params object[] args )
        {
            if ( args != null
                && args.Length > 0 )
                taskName = string.Format( taskName, args );

            if ( _ActiveTaskName != taskName )
            {
                _ActiveTaskName = taskName;

                _StartTime = DateTime.Now;
                _FinishTime = null;

                _ActiveItem = null;

                this.AddInformation( "--\n-- Task Started: {0}\n--", _ActiveTaskName );

                _TaskWarningHeaderWritten = false;
            }
        }

        public void StartWorkItem( string workItemName )
        {
            StartWorkItem( workItemName, null );
        }

        public void StartWorkItem( string workItemName, params object[] args )
        {
            if ( args != null
                && args.Length > 0 )
                workItemName = string.Format( workItemName, args );

            // Close existing work item.
            if ( _ActiveItem != null )
                FinishWorkItem();

            _ActiveItem = new TaskLogWorkItem();

            _ActiveItem.Name = workItemName;

            _WorkItems.Add( _ActiveItem );

            // Log an activity start message
            var message = "Activity Started: " + _ActiveItem.Name;

            if ( _TotalCount > 0 )
                message += string.Format( " ({0} of {1})", _WorkItems.Count, _TotalCount );

            this.AddInformation( message );

            _ItemWarningHeaderWritten = false;

            // Start the timer for this work item.
            _WorkItemStopwatch.Restart();
        }

        private string AddWarningMessageHeader( string message )
        {
            // Prepend an Activity Exception header if not yet written for the current work item
            if ( !_ItemWarningHeaderWritten )
            {
                if ( _ActiveItem != null )
                    message = "-- Activity Exception: " + _ActiveItem.Name + "\n" + message;

                _ItemWarningHeaderWritten = true;
            }

            // Prepend a Task Exception header if not yet written for the current task
            if ( !_TaskWarningHeaderWritten )
            {
                if ( !string.IsNullOrWhiteSpace( _ActiveTaskName ) )
                    message = "--\n-- Task Exception: " + _ActiveTaskName + "\n--\n" + message;

                _TaskWarningHeaderWritten = true;
            }

            return message;
        }

        #endregion
    }

    public class TaskLogUpdatedEventArgs : EventArgs
    {
        public TaskLogUpdatedEventArgs( IEnumerable<TaskMessage> messages )
        {
            this.NewMessages = messages;
        }

        public IEnumerable<TaskMessage> NewMessages;
    }

    public enum TaskExceptionBehaviourSpecifier
    {
        TerminateProcessing = 0,
        LogError = 1,
        LogWarning = 2
    }

    /// <summary>
    /// The nature of the event recorded by the log message.
    /// </summary>
    public enum TaskLogEventTypeSpecifier
    {
        Information = 0,
        Error = 1,
        Warning = 2,

        /// <summary>
        /// An informational message that represents the result of an activity.
        /// </summary>
        ResultSuccess = 4,
        ResultFailure = 5,

        /// <summary>
        /// An informational message that represents the progress of an activity.
        /// </summary>
        ActivityProgress = 30,
        ActivityStart = 31,
        ActivityEnd = 32,
        ActivityResult = 33
    }

    /// <summary>
    /// The level of importance associated with a log event.
    /// </summary>
    public enum TaskLogEventLevelSpecifier
    {
        Low,
        Normal,
        High,
        Critical
    }

    public class TaskMessage
    {
        public int SequenceNo = 0;

        public DateTime TimeStamp = DateTime.Now;

        public string Details = string.Empty;
        public string Message = string.Empty;

        /// <summary>
        /// A custom code used to identify the processor that generated this message.
        /// </summary>
        public string ProcessorId = string.Empty;

        /// <summary>
        /// A custom code used to identify the task-specific nature of the message content.
        /// </summary>
        public string ContentCode = string.Empty;

        /// <summary>
        /// A custom identifier that can be used to group related messages.
        /// </summary>
        public string TransactionId = string.Empty;

        public TaskLogEventTypeSpecifier EventType = TaskLogEventTypeSpecifier.Error;
        public TaskLogEventLevelSpecifier EventLevel = TaskLogEventLevelSpecifier.Normal;

        public override string ToString()
        {
            string message = string.Format( "{0:hh:mm:sstt}\t[{1}]\t{2}\t{3}", TimeStamp, ProcessorId, EventType, Message );

            if ( !string.IsNullOrWhiteSpace( Details ) )
            {
                string details = Details.Replace( "\n", "\n--> " );

                message += "\n-->" + details;
            }

            return message.Trim();
        }
    }

    public class TaskException
        : Exception
    {
        private readonly IEnumerable<TaskMessage> _ValidationMessages;
        private readonly string _Message = string.Empty;

        public TaskException( IEnumerable<TaskMessage> messages, string messageSummaryText, Exception ex )
            : base( messageSummaryText, ex )
        {
            _ValidationMessages = messages;

            _Message = messageSummaryText;
        }

        public TaskException( IEnumerable<TaskMessage> messages, string messageSummaryText )
            : base( messageSummaryText )
        {
            _ValidationMessages = messages;

            _Message = messageSummaryText;
        }

        public override string Message
        {
            get { return _Message; }
        }

        public IEnumerable<TaskMessage> ValidationMessages
        {
            get { return _ValidationMessages.ToArray(); }
        }
    }

    public class TaskLogWorkItem
    {
        public string Name;
        public decimal ExecutionTimeInSeconds;
        public bool IsCompleted;
    }

    /// <summary>
    /// Represents a message log that accepts and stores simple messages that relate to a specific activity or process.
    /// </summary>
    public interface ITaskLog
    {
        #region Events

        event EventHandler<TaskLogUpdatedEventArgs> LogUpdated;

        #endregion

        #region Status Information

        string ActiveWorkItem { get; }
        int CurrentCount { get; }
        TimeSpan ElapsedTime { get; }
        int ErrorCount { get; set; }
        DateTime? FinishTime { get; set; }
        bool HasErrors { get; }
        bool HasExceptions { get; }
        TaskLogWorkItem LastWorkItem { get; }
        DateTime? StartTime { get; set; }
        int TotalCount { get; set; }
        string TrackingId { get; set; }
        int WarningCount { get; set; }
        decimal WorkItemAcceptableExecutionTime { get; set; }

        #endregion

        #region Message Management

        ReadOnlyCollection<TaskMessage> Messages { get; }

        void AddMessage( TaskMessage message );
        void AddMessages( IEnumerable<TaskMessage> messages );

        TaskMessage AddActivityProgress( string actionId, string message, params object[] args );
        TaskMessage AddDebug( string message, params object[] args );
        TaskMessage AddError( string message, params object[] args );
        TaskMessage AddException( Exception ex );
        TaskMessage AddException( Exception ex, string message );
        TaskMessage AddInformation( string message, params object[] args );
        TaskMessage AddMessage( TaskLogEventTypeSpecifier eventType, string message, params object[] args );
        TaskMessage AddMessage( string contentCode, TaskLogEventTypeSpecifier eventType, string message, params object[] args );
        TaskMessage AddMessage( string actionId, string contentCode, TaskLogEventTypeSpecifier eventType, TaskLogEventLevelSpecifier eventLevel, string message, params object[] args );
        TaskMessage AddActionFailure( string message, params object[] args );
        TaskMessage AddActionSuccess( string message, params object[] args );
        TaskMessage AddWarning( string message, params object[] args );

        #endregion

        #region Lifetime Management

        void Clear();
        void Dispose();

        #endregion

        #region Error Management

        Exception GetException();
        Exception GetException( IEnumerable<TaskLogEventTypeSpecifier> includeMessageTypes );
        List<TaskMessage> GetExceptionMessages();

        void ThrowExceptions();

        #endregion

        #region Message Filters

        List<TaskMessage> GetResultMessages();
        List<TaskMessage> GetWarningMessages();
        List<TaskMessage> GetErrorMessages();

        #endregion

        #region Task and Work Item Management

        ReadOnlyCollection<TaskLogWorkItem> WorkItems();

        void StartTask( string taskName, params object[] args );
        void StartWorkItem( string workItemName );
        void StartWorkItem( string workItemName, params object[] args );
        void FinishTask();
        void FinishWorkItem();

        #endregion

        #region Utility Functions

        TaskMessage[] ToArray();
        string ToString();

        #endregion
    }
}