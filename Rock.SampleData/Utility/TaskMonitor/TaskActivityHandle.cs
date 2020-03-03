using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace System.Diagnostics
{
    /// <summary>
    /// A handle to an active Task Activity that can be used to control the lifespan of the activity.
    /// </summary>
    public class TaskActivityHandle
        : IDisposable, ITaskMonitorHandle
    {
        private readonly int _ActivityId;

        private TaskMonitor _TaskMonitor;

        public TaskActivityHandle( TaskMonitor monitor, int activityId )
        {
            if ( monitor == null )
            {
                throw new ArgumentNullException( "monitor" );
            }

            _TaskMonitor = monitor;

            _TaskMonitor.LogUpdated += _TaskMonitor_LogUpdated;
            
            _ActivityId = activityId;

            var activity = this.Activity;
        }

        private void _TaskMonitor_LogUpdated( object sender, TaskLogUpdatedEventArgs e )
        {
            // Relay task monitor messages to any subscribers to this handle.
            LogUpdated?.Invoke( sender, e );
        }

        public int ID
        {
            get { return _ActivityId; }
        }

        public event EventHandler<TaskLogUpdatedEventArgs> LogUpdated;

        public TaskMonitor Monitor
        {
            get { return _TaskMonitor; }
        }

        public void Dispose()
        {
            if ( _TaskMonitor != null )
            {
                var activity = this.Activity;

                if ( activity != null )
                {
                    activity.EndActivity();
                }

                _TaskMonitor = null;
            }
        }

        private ITaskActivity GetActivity()
        {
            if ( _ActivityId == 1 )
            {
                return _TaskMonitor.Activity;
            }
            else
            {
                return _TaskMonitor.Activities.Flatten().FirstOrDefault( x => x.ID == _ActivityId );
            }
        }

        public ITaskActivity Activity
        {
            get
            {
                return GetActivity();
            }
        }

        /// <summary>
        /// A flag indicating if this handle points to a valid activity.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return GetActivity() != null;
            }
        }
        public override string ToString()
        {
            if ( this.Activity == null || string.IsNullOrWhiteSpace( this.Activity.Name ) )
            {
                return "ActivityID=" + _ActivityId;
            }

            return this.Activity.Name;
        }

        public TaskAllocationHandle NewActivityAllocation( decimal percentAllocated )
        {
            this.Activity.ChildProcessAllocationPercent = percentAllocated;

            return new TaskAllocationHandle( _TaskMonitor, _ActivityId );
        }

        /// <summary>
        ///     Creates a new child activity of the Task.
        /// </summary>
        /// <param name="percentOfTotal"></param>
        /// <param name="activityName"></param>
        /// <param name="duration"></param>
        /// <returns>A handle to the new activity that can be used to control the lifetime of the activity.</returns>
        public TaskActivityHandle AddChildActivity( decimal percentOfTotal, string activityName, TaskActivityDurationSpecifier duration = TaskActivityDurationSpecifier.Unknown )
        {
            return this.Activity.AddChildActivity( percentOfTotal, activityName, string.Empty, false, duration, 0 );
        }

        /// <summary>
        ///     Creates a new child activity of the Task
        /// </summary>
        /// <param name="percentOfTotal"></param>
        /// <param name="childActivity"></param>
        /// <returns>A handle to the new activity that can be used to control the lifetime of the activity.</returns>
        public TaskActivityHandle AddChildActivity( decimal percentOfTotal, ITaskActivity childActivity )
        {
            return this.Activity.AddChildActivity( percentOfTotal, childActivity );
        }

        /// <summary>
        ///     Create and immediately start a new child activity for this Task.
        /// </summary>
        /// <param name="percentOfTotal"></param>
        /// <param name="activityName"></param>
        /// <param name="duration"></param>
        /// <returns>A handle to the new activity that can be used to control the lifetime of the activity.</returns>
        public TaskActivityHandle StartChildActivity( decimal percentOfTotal, string activityName, TaskActivityDurationSpecifier duration = TaskActivityDurationSpecifier.Unknown )
        {
            var activity = this.AddChildActivity( percentOfTotal, activityName, duration );

            activity.StartActivity();

            return activity;
        }

        /// <summary>
        ///     Create and immediately start a new child activity for this Task.
        /// </summary>
        /// <param name="percentOfTotal"></param>
        /// <param name="activityName"></param>
        /// <param name="duration"></param>
        /// <returns>A handle to the new activity that can be used to control the lifetime of the activity.</returns>
        public TaskActivityHandle StartChildActivity( string activityName, TaskActivityDurationSpecifier duration = TaskActivityDurationSpecifier.Unknown )
        {
            decimal percentOfTotal;

            if (this.MaximumProgressCount > 0)
            {
                percentOfTotal = 1 / this.MaximumProgressCount * 100;
            }
            else
            {
                percentOfTotal = 100;
            }

            var activity = this.AddChildActivity( percentOfTotal, activityName, duration );

            return activity;
        }

        /// <summary>
        ///     Create and immediately start a new child activity for this Task.
        /// </summary>
        /// <param name="percentOfTotal"></param>
        /// <param name="activityName"></param>
        /// <param name="duration"></param>
        /// <returns>A handle to the new activity that can be used to control the lifetime of the activity.</returns>
        public TaskActivityHandle StartChildActivity( decimal percentOfTotal, ITaskActivity childActivity )
        {
            var activity = this.AddChildActivity( percentOfTotal, childActivity );

            activity.StartActivity();

            return activity;
        }


        public ReadOnlyCollection<TaskMessage> Messages
        {
            get { return _TaskMonitor.Messages; }
        }

        public TaskMessage LogDebug( string message, params object[] args )
        {
            return this.Activity.LogDebug( message, args );
        }

        public TaskMessage LogError( string message, params object[] args )
        {
            return this.Activity.LogError( message, args );
        }

        public void LogException( Exception ex )
        {
            this.Activity.LogException( ex );
        }

        public void LogException( Exception ex, string message )
        {
            this.Activity.LogException( ex, message );
        }

        public TaskMessage LogInformation( string message, params object[] args )
        {
            return this.Activity.LogInformation( message, args );
        }

        public void LogMessage( TaskMessage message )
        {
            this.Activity.LogMessage( message );
        }

        public TaskMessage LogMessage( TaskLogEventTypeSpecifier eventType, string message, params object[] args )
        {
            return this.Activity.LogMessage( eventType, message, args );
        }

        public TaskMessage LogInformation( TaskLogEventLevelSpecifier eventLevel, string message, params object[] args )
        {
            return this.Activity.LogInformation( eventLevel, message, args );
        }

        public void LogMessages( IEnumerable<TaskMessage> messages )
        {
            this.Activity.LogMessages( messages );
        }

        public TaskMessage LogActionFailure( string message, params object[] args )
        {
            return this.Activity.LogActionFailure( message, args );
        }

        public TaskMessage LogActionSuccess( string message, params object[] args )
        {
            return this.Activity.LogActionSuccess( message, args );
        }

        public TaskMessage LogWarning( string message, params object[] args )
        {
            return this.Activity.LogWarning( message, args );
        }

        public bool HasErrors
        {
            get
            {
                return this.Activity.HasErrors;
            }
        }

        public bool HasExceptions
        {
            get
            {
                return this.Activity.HasExceptions;
            }
        }

        public List<TaskMessage> GetWarningMessages()
        {
            return this.Activity.GetWarningMessages();
        }

        public List<TaskMessage> GetErrorMessages()
        {
            return this.Activity.GetErrorMessages();
        }

        public List<TaskMessage> GetResultMessages()
        {
            return this.Activity.GetResultMessages();
        }

        public List<TaskMessage> GetExceptionMessages()
        {
            return this.Activity.GetExceptionMessages();
        }

        public void ClearLog()
        {
            this.Activity.LogPurge();
        }

        public Exception GetException()
        {
            return this.Activity.GetException();
        }

        public Exception GetException( IEnumerable<TaskLogEventTypeSpecifier> includeMessageTypes )
        {
            return this.Activity.GetException( includeMessageTypes );
        }

        public TaskLogEventLevelSpecifier LogEventLevelFilter
        {
            get { return this.Activity.LogEventLevelFilter; }
            set { this.Activity.LogEventLevelFilter = value; }
        }

        public List<TaskLogEventTypeSpecifier> LogEventTypeFilter
        {
            get { return this.Activity.LogEventTypeFilter; }
        }

        public int MaximumProgressCount = 100;
        private int _CurrentProgressCount = 0;

        public int CurrentProgressCount
        {
            get
            {
                return _CurrentProgressCount;
            }
            set
            {
                if ( _CurrentProgressCount != value )
                {
                    _CurrentProgressCount = value;

                    this.Update( _CurrentProgressCount );
                }
            }
        }

        /// <summary>
        ///     Update the status of this Activity
        /// </summary>
        /// <param name="activityDescription"></param>
        /// <param name="args"></param>
        public void Update( string activityDescription, params object[] args )
        {
            var activity = this.Activity;

            if ( activity == null )
                return;

            activity.Update( this.CurrentProgressCount, this.MaximumProgressCount, activityDescription, args );
        }

        /// <summary>
        ///     Update the status of this Activity
        /// </summary>
        /// <param name="currentCount"></param>
        /// <param name="activityDescription"></param>
        /// <param name="args"></param>
        public void Update( decimal currentCount, string activityDescription, params object[] args )
        {
            var activity = this.Activity;

            if ( activity == null )
                return;

            activity.Update( currentCount, this.MaximumProgressCount, activityDescription, args );
        }

        public void Update( decimal currentCount )
        {
            var activity = this.Activity;

            activity?.Update( currentCount, this.MaximumProgressCount );
        }

        /// <summary>
        /// Signals that the Task is complete.
        /// </summary>
        public void EndActivity()
        {
            var activity = this.Activity;

            activity?.EndActivity();
        }

        public void Fail( string message )
        {
            var activity = this.Activity;

            activity?.Fail( message );
        }

        public void Fail( Exception ex )
        {
            var activity = this.Activity;

            activity?.Fail( ex );
        }

        public void StartActivity()
        {
            var activity = this.Activity;

            if ( activity == null )
                return;

            activity.BeginActivity();
        }

        TaskActivityHandle ITaskMonitorHandle.GetCurrentActivityHandle()
        {
            if ( this.Activities.Any() )
            {
                var lastActivityId = this.Activities.Flatten().Select(x => x.ID).Last();

                return new TaskActivityHandle( _TaskMonitor, lastActivityId );
            }
            else
            {
                return this.Activity.CreateHandle();
            }
        }

        public TaskActivityCollection Activities
        {
            get
            {
                return Activity.ChildActivities;
            }
        }
    }
}