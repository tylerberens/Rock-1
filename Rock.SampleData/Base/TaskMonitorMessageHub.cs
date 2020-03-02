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
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace Rock.SampleData
{
    /// <summary>
    /// A hub for sending/receiving SignalR messages relating to the progress and status of long-running tasks.
    /// </summary>
    [HubName( "TaskMonitorMessageHub" )]
    public class TaskMonitorMessageHub : Hub<ITaskMonitorMessageHub>
    {
        public void UpdateTaskLog( string message )
        {
            Clients.All.UpdateTaskLog( message );
        }

        public void UpdateTaskProgress( TaskActivityMessage message )
        {
            Clients.All.UpdateTaskProgress( message );
        }

        public void NotifyTaskStarted( TaskInfoMessage message )
        {
            Clients.All.NotifyTaskStarted( message );
        }

        public void NotifyTaskComplete( TaskInfoMessage message )
        {
            Clients.All.NotifyTaskComplete( message );
        }
    }

    /// <summary>
    /// A hub for sending/receiving SignalR messages relating to the progress and status of long-running tasks.
    /// </summary>
    public interface ITaskMonitorMessageHub
    {
        void UpdateTaskLog( string message );

        void UpdateTaskProgress( TaskActivityMessage message );

        void NotifyTaskStarted( TaskInfoMessage message );

        void NotifyTaskComplete( TaskInfoMessage message );
    }

    /// <summary>
    /// A message to indicate the activity of a running Task.
    /// </summary>
    public class TaskActivityMessage
    {
        public string Message { get; set; }

        public decimal CompletionPercentage { get; set; }

        public string ProgressSummary { get; set; }

        public bool IsIdenticalTo( TaskActivityMessage obj )
        {
            var report = obj as TaskActivityMessage;

            if ( report == null )
            {
                return false;
            }

            if ( CompletionPercentage == report.CompletionPercentage
                && Message == report.Message
                && ProgressSummary == report.ProgressSummary )
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// A message that provides a summary of information about a Task.
    /// </summary>
    public class TaskInfoMessage
    {
        public string TaskName { get; set; }

        public string TaskDescription { get; set; }

        public string LogFilePath { get; set; }

        public bool IsStarted { get; set; }

        public bool IsFinished { get; set; }

        public string StatusMessage { get; set; }

        public bool HasErrors { get; set; }

    }

    /// <summary>
    /// The current status of a task.
    /// </summary>
    public enum TaskStatusSpecifier
    {
        Ready = 0,
        Running = 1,
        CompletedWithSuccess = 2,
        CompletedWithWarnings = 3,
        CompletedWithFailure = 4
    }
}