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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rock.SampleData.Crm.Steps
{
    /// <summary>
    /// A set of unit tests for Visual Studio that allow management of sample data for the Steps feature of Rock.
    /// </summary>
    [TestClass]
    public class StepsFeatureSampleDataActions
    {
        /// <summary>
        /// Remove all Steps test data from the current database.
        /// </summary>
        [TestMethod]
        [TestCategory( TestCategories.RemoveData )]
        [TestProperty( "Feature", TestFeatures.DataMaintenance )]
        public void RemoveAllTestData()
        {
            var creator = new StepsFeatureDataFactory();

            creator.RemoveStepsFeatureTestData();
        }

        /// <summary>
        /// Add a complete set of Steps test data to the current database.
        /// Existing Steps test data will be removed.
        /// </summary>
        [TestMethod]
        [TestCategory( TestCategories.AddData )]
        [TestProperty( "Feature", TestFeatures.DataSetup )]
        public void AddAllTestData()
        {
            var creator = new StepsFeatureDataFactory();

            creator.AddStepsFeatureTestData();
        }

        /// <summary>
        /// Adds a set of Step Programs to the current database that are required for integration testing.
        /// This function does not need to be executed separately - it is executed as part of the AddTestDataToCurrentDatabase() function.
        /// </summary>
        [TestMethod]
        [TestCategory( TestCategories.DeveloperSetup )]
        [TestProperty( "Feature", TestFeatures.DataMaintenance )]
        public void AddStepPrograms()
        {
            var creator = new StepsFeatureDataFactory();

            creator.AddTestDataStepPrograms();
        }

        [TestMethod]
        [TestCategory( TestCategories.DeveloperSetup )]
        [TestProperty( "Feature", TestFeatures.DataMaintenance )]
        public void AddKnownStepParticipations()
        {
            var creator = new StepsFeatureDataFactory();

            creator.AddKnownStepParticipations();
        }

        [TestMethod]
        [TestCategory( TestCategories.DeveloperSetup )]
        [TestProperty( "Feature", TestFeatures.DataMaintenance )]
        public void AddStepDataViews()
        {
            var creator = new StepsFeatureDataFactory();

            creator.AddStepDataViews();
        }

        [TestMethod]
        [TestCategory( TestCategories.DeveloperSetup )]
        [TestProperty( "Feature", TestFeatures.DataSetup )]
        public void AddStepsRandomParticipationEntries()
        {
            var creator = new StepsFeatureDataFactory();

            creator.AddStepsRandomParticipationEntries();
        }
    }
}