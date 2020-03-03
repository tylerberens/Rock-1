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
using System.Diagnostics;

namespace Rock.SampleData
{
    /// <summary>
    /// Provides base functionality for a factory that produces sample data for a Rock database.
    /// </summary>
    public abstract class SampleDataFactoryBase : ISampleDataFactory
    {
        ITaskMonitorHandle _ActiveTaskMonitor = null;

        public SampleDataChangeActionExecutionResponse ExecuteAction( string actionId, ISampleDataChangeActionSettings settings, ITaskMonitorHandle monitor )
        {
            _ActiveTaskMonitor = monitor;

            var result = this.OnExecuteAction( actionId, settings, monitor );

            _ActiveTaskMonitor = null;

            return result;
        }

        protected abstract SampleDataChangeActionExecutionResponse OnExecuteAction( string actionId, ISampleDataChangeActionSettings settings, ITaskMonitorHandle monitor );

        // TODO: Add a default implementation to create Actions using Reflection and Test Attributes?
        public abstract List<SampleDataChangeAction> GetActionList();

        /// <summary>
        /// Gets a task monitor for the action that is currently executing.
        /// </summary>
        /// <returns></returns>
        protected TaskActivityHandle GetCurrentTaskActivity()
        {
            if ( _ActiveTaskMonitor == null )
            {
                _ActiveTaskMonitor = new TaskMonitor();
            }
            
            return _ActiveTaskMonitor.GetCurrentActivityHandle();
        }
    }
}