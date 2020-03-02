using System;

namespace System.Diagnostics
{
    /// <summary>
    /// A component that is connected to a Task Monitor.
    /// </summary>
    public interface ITaskMonitorComponent
    {
        /// <summary>
        /// This method is called when the Activity is associated with a Task Monitor.
        /// </summary>
        /// <param name="monitor"></param>
        void AttachToMonitor( TaskMonitor monitor );
    }
}