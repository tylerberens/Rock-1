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

namespace Rock.SampleData.Crm
{
    /// <summary>
    /// A set of unit tests for Visual Studio that allow management of sample data for the Steps feature of Rock.
    /// </summary>
    [TestClass]
    public class PersonSampleDataActions
    {
        /// <summary>
        /// Remove all Steps test data from the current database.
        /// </summary>
        //[TestMethod]
        //[TestCategory( TestCategories.RemoveData )]
        //[TestProperty( "Feature", TestFeatures.DataMaintenance )]
        //public void RemoveAllTestData()
        //{
        //    var creator = new StepsFeatureDataFactory();

        //    creator.RemoveStepsFeatureTestData();
        //}

        /// <summary>
        /// Add a complete set of Steps test data to the current database.
        /// Existing Steps test data will be removed.
        /// </summary>
        [TestMethod]
        [TestCategory( TestCategories.AddData )]
        [TestProperty( "Feature", TestFeatures.PeopleAndFamilies )]
        public void AddAllTestData()
        {
            var creator = new PersonDataFactory();

            creator.LoadStandardDataSet();
        }

        /// <summary>
        /// Adds a set of near-identical Person records to the database that are suitable for testing the merge functionality.
        /// </summary>
        [TestMethod]
        [TestCategory( TestCategories.AddData )]
        [TestProperty( "Feature", TestFeatures.PeopleAndFamilies )]
        public void AddDuplicatePersonHelenEvans()
        {
            var creator = new PersonDataFactory();

            string xmlFamily = @"
<?xml version=''1.0'' encoding=''utf-8''?>
<data>
  <families>
    <!--Helen Evans came to a VLE (very large event) on campus and she's someone who should be followed up with.-->
    <family name=''Evans'' guid=''89b175ad-6bf1-4590-b936-1cf42014a337''>
        <addresses>
        <address type=''home'' street1=''10828 N Biltmore Dr'' street2='''' city=''Phoenix'' state=''AZ'' postalCode=''85029'' lat=''33.5855064'' long=''-112.1186981'' />
        </addresses>
        <members>
        <person guid=''1dfff821-e97c-4324-9883-cf59b5c5bdd6'' lastName=''Evans'' firstName=''Helen'' nickName=''Helen'' middleName='''' gender=''female'' familyRole=''adult'' recordStatus=''active'' connectionStatus=''Web Prospect'' birthDate=''4/2/1979'' maritalStatus=''Single'' email=''hevans@fakeinbox.com'' emailIsActive=''true'' emailDoNotEmail=''false'' homePhone=''6235552540'' cellPhone=''4805558410'' photoUrl=''http://storage.rockrms.com/sampledata/person-images/evans_helen_is_stylecampaign.jpg''>
            <attributes>
            <attribute FirstVisit=''10/11/2015'' />
            </attributes>
            <notes />
        </person>
        </members>
    </family>
    <family name=''Evans'' guid=''39B80794-BAE1-4057-B843-CD7B56D005D0''>
        <addresses>
        <address type=''home'' street1=''10828 N Biltmore Dr'' street2='''' city=''Phoenix'' state=''AZ'' postalCode=''85029'' lat=''33.5855064'' long=''-112.1186981'' />
        </addresses>
        <members>
        <person guid=''0F5FA8C3-F9FE-42B7-B6E0-DBFF6C3FF973'' lastName=''Evans'' firstName=''Helena'' nickName=''Helena'' middleName='''' gender=''female'' familyRole=''adult'' recordStatus=''active'' connectionStatus=''Web Prospect'' birthDate=''4/2/1979'' maritalStatus=''Single'' email=''hevans2@fakeinbox.com'' emailIsActive=''true'' emailDoNotEmail=''false'' homePhone=''6235552540'' cellPhone=''4805558410''>
            <attributes>
            <attribute FirstVisit=''10/11/2015'' />
            </attributes>
            <notes />
        </person>
        </members>
    </family>
  </families>
</data>
";

            xmlFamily = xmlFamily.Replace( "''", "\"" );

            creator.Password = "password";
            creator.CreateFromXmlDocumentText( xmlFamily, removeExistingItems:true );
        }        
    }
}