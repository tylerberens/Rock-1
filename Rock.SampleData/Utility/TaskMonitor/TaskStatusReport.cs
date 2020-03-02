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
using System.Linq;

namespace System.Diagnostics
{
    /// <summary>
    ///     A snapshot of the current status of a Task
    /// </summary>
    [Serializable]
    public class TaskStatusReport
    {
        private readonly int _TaskId;
        private string _TaskDescription = "";
        private string _TaskName = "";
        private decimal _TaskRelativeProgress;

        public int TotalErrors = 0;
        public int TotalWarnings = 0;

        public List<TaskActivityStatusReport> CurrentActivities = new List<TaskActivityStatusReport>();

        internal TaskStatusReport()
        {
            // Default constructor
        }

        public TaskActivityStatusReport GetLastUpdatedActivity()
        {
            return this.CurrentActivities.OrderByDescending(a => a.LastUpdated).FirstOrDefault();
        }

        internal TaskStatusReport( int taskId, string taskName, string taskDescription ) //, int activityId, string activityName, string activityDescription, TaskActivityDurationSpecifier activityDuration )
        {
            _TaskId = taskId;
            _TaskName = taskName;
            _TaskDescription = taskDescription;
        }

        public string TaskDescription
        {
            get { return _TaskDescription; }

            set { _TaskDescription = value; }
        }

        public int TaskId
        {
            get { return _TaskId; }
        }

        public string TaskName
        {
            get { return _TaskName; }

            set { _TaskName = value; }
        }

        public decimal TaskRelativeProgress
        {
            get { return _TaskRelativeProgress; }

            set { _TaskRelativeProgress = value; }
        }
    }
}