using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace System.Diagnostics
{
    /// <summary>
    ///     A collection of Task Activities
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class TaskActivityCollection : IEnumerable<ITaskActivity>, ITaskMonitorComponent
    {
        private readonly ObservableCollection<ITaskActivity> _Activities = new ObservableCollection<ITaskActivity>();

        private TaskMonitor _Monitor;
        private ITaskActivity _ParentActivity = null;

        public ITaskActivity ParentActivity
        {
            get
            {
                return _ParentActivity;
            }
            set
            {
                _ParentActivity = value;

                this.SetParentActivity( _Activities );
            }
        }

        private void SetParentActivity(IEnumerable<ITaskActivity> childActivities)
        {
            foreach ( var activity in childActivities )
            {
                activity.ParentActivity = _ParentActivity;
            }
        }

        public TaskActivityCollection( ITaskActivity parentActivity )
            : this()
        {
            _ParentActivity = parentActivity;
        }

        public TaskActivityCollection()
        {
            _Activities.CollectionChanged += _Activities_CollectionChanged;
        }

        private void _Activities_CollectionChanged( object sender, Collections.Specialized.NotifyCollectionChangedEventArgs e )
        {
            if ( e.Action == NotifyCollectionChangedAction.Add )
            {
                var activities = e.NewItems.OfType<ITaskActivity>().ToList();

                if ( _Monitor != null )
                {
                    AttachActivities( activities );
                }

                this.SetParentActivity( activities );
            }

            this.RecalculateAllocations();
        }

        private void AttachActivities( IEnumerable<ITaskActivity> activities )
        {
            if ( _Monitor != null )
            {
                foreach ( var activity in activities )
                {
                    activity.AttachToMonitor( _Monitor );
                }
            }

            foreach ( var activity in activities )
            {
                activity.ParentActivity = _ParentActivity;
            }

        }

        public List<ITaskActivity> Flatten()
        {
            var activities = new List<ITaskActivity>();

            foreach ( var activity in _Activities )
            {
                this.AddActivityAndDescendants( activities, activity );
            }

            return activities;
        }

        private void AddActivityAndDescendants( List<ITaskActivity> activities, ITaskActivity newActivity  )
        {
            activities.Add( newActivity );

            if ( newActivity.ChildActivities.Any() )
            {
                foreach ( var childActivity in newActivity.ChildActivities )
                {
                    this.AddActivityAndDescendants( activities, childActivity );
                }
            }
        }

        public int Count
        {
            get { return _Activities.Count; }
        }

        public ITaskActivity this[int index]
        {
            get
            {
                try
                {
                    return _Activities[index];
                }
                catch
                {
                    return null;
                }
            }
        }

        public ITaskActivity GetByID( string id )
        {
            try
            {
                var activityID = int.Parse( id );

                return GetByID( activityID );
            }
            catch
            {
                return null;
            }
        }

        public ITaskActivity GetByID( int id )
        {
            try
            {
                foreach ( ITaskActivity activity in _Activities )
                {
                    if ( activity.ID == id )
                        return activity;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public ITaskActivity GetFirst()
        {
            return this[0];
        }

        public ITaskActivity GetLast()
        {
            return this[_Activities.Count - 1];
        }

        internal ITaskActivity AddNew( string name, string description )
        {
            return AddNew( name, description, DateTime.Now );
        }

        internal ITaskActivity AddNew( string name, string description, DateTime startTime )
        {
            return AddNew( name, description, startTime, 0 );
        }

        internal ITaskActivity AddNew( string name, string description, DateTime startTime, int minimumDuration )
        {
            var activity = new TaskActivity( name, description, startTime, TaskActivityDurationSpecifier.Finite);

            activity.MinimiumDuration = minimumDuration;

            //var component = (ITaskMonitorComponent)activity;

            //component.AttachToMonitor( _Monitor );

            _Activities.Add( activity );

            this.RecalculateAllocations();

            return activity;
        }

        public ITaskActivity Add( string name, decimal relativeAllocationPercent = 0 )
        {
            var activity = new TaskActivity( name );

            activity.ActivityGroupRequestedAllocationPercent = relativeAllocationPercent;

            _Activities.Add( activity );

            this.RecalculateAllocations();

            return activity;
        }

        public ITaskActivity Add( ITaskActivity activity, decimal relativeAllocationPercent = 0 )
        {
            activity.ActivityGroupRequestedAllocationPercent = relativeAllocationPercent;

            _Activities.Add( activity );

            return activity;
        }

        internal void Remove( int activityID )
        {
            var iCount = 0;

            iCount = 0;

            foreach ( ITaskActivity activity in _Activities )
            {
                iCount = iCount + 1;

                if ( activity.ID == activityID )
                {
                    _Activities.Remove( activity );

                    this.RecalculateAllocations();

                    break;
                }
            }
        }

        public decimal RelativeProgress
        {
            get
            {
                decimal relativeProgress = 0;

                foreach ( var activity in _Activities )
                {
                    relativeProgress += activity.RelativeProgress;
                }

                if ( relativeProgress > 100 )
                {
                    relativeProgress = 100;
                }

                return relativeProgress;
            }
        }

        private void RecalculateAllocations()
        {
            decimal hardAllocationTotal = 0;
            int softAllocationCount = 0;

            foreach ( var activity in _Activities )
            {
                if ( activity.ActivityGroupRequestedAllocationPercent == 0 )
                {
                    softAllocationCount++;
                }
                else
                {
                    hardAllocationTotal += activity.ActivityGroupRequestedAllocationPercent;
                }
            }

            decimal softAllocationPercentage = 0;

            if ( softAllocationCount > 0 )
            {
                softAllocationPercentage = ( 100 - hardAllocationTotal ) / softAllocationCount;
            }

            foreach ( var activity in _Activities )
            {
                if ( activity.ActivityGroupRequestedAllocationPercent == 0 )
                {
                    activity.ActivityGroupActualAllocationPercent = softAllocationPercentage;
                }
                else
                {
                    activity.ActivityGroupActualAllocationPercent = activity.ActivityGroupRequestedAllocationPercent;
                }
            }
        }

        public IEnumerator<ITaskActivity> GetEnumerator()
        {
            return _Activities.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _Activities.GetEnumerator();
        }

        public void AttachToMonitor( TaskMonitor monitor )
        {
            _Monitor = monitor;

            AttachActivities( this );
        }
    }
}