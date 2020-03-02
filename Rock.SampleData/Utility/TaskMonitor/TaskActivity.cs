using System;

namespace System.Diagnostics
{
    /// <summary>
    /// An activity that does not have easily measurable progress, but has an estimated completion time.
    /// </summary>
    [Serializable]
    public class TaskActivityTimedDuration : TaskActivityBase
    {
        public TaskActivityTimedDuration( string name, string description = null, DateTime? startTime = null)
            : base( name, description, startTime, TaskActivityDurationSpecifier.Unknown )
        {
            //
        }

    }

    /// <summary>
    /// An activity that represents processing a batch of items.
    /// </summary>
    [Serializable]
    public class TaskActivityProcessItems : TaskActivityBase
    {
        public TaskActivityProcessItems( string name, int totalItems, string description = null )
            : base( name, description, DateTime.Now, TaskActivityDurationSpecifier.Finite )
        {
            TotalItems = totalItems;
        }

        public int TotalItems { get; set; }
    }

    /// <summary>
    /// An activity that consists of a set of child activities.
    /// </summary>
    [Serializable]
    public class TaskActivityGroup : TaskActivityBase
    {
        public TaskActivityGroup( string name, string description = null )
            : base( name, description, DateTime.Now, TaskActivityDurationSpecifier.Finite )
        {
        }
    }

    [Serializable]
    public class TaskActivity : TaskActivityBase
    {
        protected internal TaskActivity()
            : this( null, null, DateTime.Now, TaskActivityDurationSpecifier.Unknown )
        {
        }

        public TaskActivity( string name, string description, DateTime startTime, TaskActivityDurationSpecifier durationType )
            : base( name, description, startTime, durationType )
        {
            //
        }

        public TaskActivity( string name )
            : base( name, null, null, TaskActivityDurationSpecifier.Finite)
        {
        }

    }
}