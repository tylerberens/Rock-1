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
using System.Linq;
using Rock;
using Rock.Model;
using System.Data.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rock.Data;


namespace Rock.Tests.Integration.Model
{
    /// <summary>
    /// This job synchronizes the members of a group with the people in a Rock data view based on
    /// the configuration of data view and role found in the group. It is also responsible for
    /// sending any ExitSystemEmail or WelcomeSystemEmail as well as possibly creating any 
    /// user login for the person.
    /// 
    /// It should adhere to the following truth table for a person in a particular role
    /// (A Person can be in the same group with a different role):
    /// 
    ///     In         In Group   In Group
    ///     DataView   Archived   !Archived   Result
    ///     --------   --------   ---------   ----------------------------
    ///            0          0           0   do nothing
    ///            0          0           1   remove from group
    ///            0          1           0   do nothing
    ///            1          0           0   add to group
    ///            1          0           1   do nothing
    ///            1          1           0   change IsArchived to false
    ///
    /// NOTE: It should do this regardless of the person's IsDeceased flag.
    /// NOTE: The job can sync new people at about 45/sec or 2650/minute.
    /// </summary>
    [TestClass]
    public class GroupSyncTests
    {
        #region Setup

        /// <summary>
        /// Runs before any tests in this class are executed.
        /// </summary>
        [ClassInitialize]
        public static void ClassInitialize( TestContext testContext )
        {
            DatabaseTests.ResetDatabase();
        }

        /// <summary>
        /// Runs after all tests in this class is executed.
        /// </summary>
        [ClassCleanup]
        public static void ClassCleanup()
        {
            DatabaseTests.DeleteDatabase();
        }

        /// <summary>
        /// Runs after each test in this class is executed.
        /// Deletes the test data added to the database for each tests.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {


        }

        #endregion

        #region Group Sync Tests

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void GroupSyncDeckerGroupWithMaleDataview()
        {
            ///Sets Small Group Type to allow group Sync
            RockContext rc = new RockContext();

            ///Gets Small Group Type to change AllowGroupSync to TRUE
            var smallgroupTypeGUID = "50FCFB30-F51A-49DF-86F4-2B176EA1820B".AsGuid();
            GroupType smallGroup =new GroupTypeService( rc ).Queryable().Where( x => x.Guid == smallgroupTypeGUID ).First();
            smallGroup.AllowGroupSync = true;
            rc.SaveChanges();

            #region Set up of Group Sync

            ///Sets Group to insert its Id by GUID
            var deckerGuid = "62DC3753-01D5-48B5-B22D-D2825D92900B".AsGuid();
            Group deckerGroup =new GroupService(rc).Queryable()
                .Where( x => x.Guid == deckerGuid ).FirstOrDefault();

            ///Sets Group Type role to insert it's Id by GUID
            var groupTypeRoleGUID = "F0806058-7E5D-4CA9-9C04-3BDF92739462".AsGuid();
            GroupTypeRole smallGroupTypeRoleForID = new GroupTypeRoleService( rc ).Queryable().AsNoTracking()
                .Where( x => x.Guid == groupTypeRoleGUID ).First();

            ///Sets Group Type role to insert it's Id by GUID
            var dataViewGUID = "C43983D7-1F22-4E94-9F5C-342DA3A0E168".AsGuid();
            DataView dataViewForID = new DataViewService( rc ).Queryable().AsNoTracking()
                .Where( x => x.Guid == dataViewGUID ).First();


            GroupSync syncWithMalesDV = new GroupSync
            {
                GroupId = deckerGroup.Id,
                GroupTypeRoleId = smallGroupTypeRoleForID.Id,
                SyncDataViewId = dataViewForID.Id,
            };

            rc.GroupSyncs.Add(syncWithMalesDV);
            rc.SaveChanges();
            
            #endregion

            var job = new ServiceJobService( new RockContext() ).Get( 13 );
            if ( job != null )
            {
                var transaction = new Transactions.RunJobNowTransaction( job.Id );
                transaction.Execute();
            }

            ////Gets all members of the Decker Group
            GroupMember updatedGroupMembers = new GroupMemberService( rc ).Queryable().AsNoTracking().Where( x => x.Guid == deckerGuid ).First();
        }
        
        #endregion

    }
}
