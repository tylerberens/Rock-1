using System;
using System.Linq;

namespace System.Diagnostics
{
    /// <summary>
    ///     An object representing a handle to a child Task allocation that is used to control the lifespan of the allocation.
    /// </summary>
    public class TaskAllocationHandle
        : IDisposable
    {
        private readonly int _ActivityId;

        private TaskMonitor _TaskMonitor;

        public TaskAllocationHandle( TaskMonitor monitor, int parentActivityID )
        {
            _TaskMonitor = monitor;

            _ActivityId = parentActivityID;
        }

        public int TaskID
        {
            get { return _ActivityId; }
        }

        public ITaskActivity Activity
        {
            get
            {
                return _TaskMonitor.Activities.FirstOrDefault( x => x.ID == _ActivityId );
            }
        }

        public void Dispose()
        {
            if ( _TaskMonitor != null )
            {
                this.Activity.EndActivity();

                _TaskMonitor = null;
            }
        }
    }
}