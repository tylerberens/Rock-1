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
namespace Rock.SampleData
{
    /// <summary>
    /// Configuration data for the execution of a SampleDataChangeAction.
    /// </summary>
    public interface ISampleDataChangeActionSettings
    {
        //
    }

    /// <summary>
    /// Response data containing the result of executing a SampleDataChangeAction.
    /// </summary>
    public class SampleDataChangeActionExecutionResponse
    {
        //
    }

    /// <summary>
    /// Request data specifying the parameters for executing a SampleDataChangeAction.
    /// </summary>
    public class SampleDataChangeActionExecutionRequest
    {
        public string ActionId { get; set; }

        ISampleDataChangeActionSettings Settings { get; set; }
    }

    /// <summary>
    /// An action that can be performed that adds, changes or removes sample data.
    /// </summary>
    public class SampleDataChangeAction
    {
        /// <summary>
        /// Gets or sets a category name for this action, describing the feature or module affected by this action.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the descriptive name of the action.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the user-friendly description for this action.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Specifies a key that can be used to uniquely identify this action.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets a verb that best describes the net effect of this action: add, modify, delete, etc.
        /// </summary>
        /// <value>
        /// The name of the source.
        /// </value>
        public string Action { get; set; } = "Add";
    }
}
