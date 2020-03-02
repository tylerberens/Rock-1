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

namespace Rock.SampleData.Communications
{
    /// <summary>
    /// Provides access to Rock sample data creation scripts through the Visual Studio Test Explorer interface.
    /// </summary>
    [TestClass]
    public class CommunicationModuleSampleDataActions
    {
        /// <summary>
        /// Adds the required test data to the current database.
        /// </summary>
        [TestMethod]
        [TestCategory( TestCategories.AddData )]
        [TestProperty( "Feature", TestFeatures.DataSetup )]
        public void AddCommunicationModuleTestData()
        {
            var creator = new CommunicationModuleDataFactory();

            creator.RemoveCommunicationModuleTestData();
            creator.AddTestDataForEmailCommunications();
            creator.AddTestDataForSmsCommunications();
        }

        /// <summary>
        /// Removes the test data from the current database.
        /// </summary>
        [TestMethod]
        [TestCategory( TestCategories.RemoveData )]
        [TestProperty( "Feature", TestFeatures.DataMaintenance )]
        public void RemoveCommunicationModuleTestData()
        {
            var creator = new CommunicationModuleDataFactory();

            creator.RemoveCommunicationModuleTestData();
        }

        /// <summary>
        /// Add a set of Nameless Person records.
        /// </summary>
        [TestMethod]
        [TestCategory( TestCategories.AddData )]
        [TestProperty( "Feature", TestFeatures.DataSetup )]
        public void AddNamelessPersonRecords()
        {
            var creator = new CommunicationModuleDataFactory();

            creator.AddNamelessPersonRecords();
        }

        [TestMethod]
        [TestCategory( TestCategories.AddData )]
        [TestProperty( "Feature", TestFeatures.DataSetup )]
        public void AddNamelessSmsConversation()
        {
            var creator = new CommunicationModuleDataFactory();

            creator.AddNamelessSmsConversation();

        }
    }
}
