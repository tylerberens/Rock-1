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
    ///Before the Group Sync job is ran, the Group History Job needs to be ran. 
    /// 
    /// It should adhere to the following truth table for a person in a particular role
    /// (A Person can be in the same group with a different role):
    /// 
    ///     In       In Group &   In Group &
    ///     DataView   Archived   !Archived   Result
    ///     --------   --------   ---------   ----------------------------
    ///            0          0           0   do nothing
    ///            0          0           1   remove from group (ExpectedAlishaTobeRemoved)
    ///            0          1           0   do nothing (ExpectedCindyToRemainInGroup)
    ///            1          0           0   add to group (ExpectedAddedNoahDeckerToGroup)
    ///            1          0           1   do nothing (ExpectedBillMarbleToRemainInGroup)
    ///            1          1           0   change IsArchived to false (TedDeckerSwitchedFromArchiveTrue)
    ///                                    Checks DataView Person list against Group Members (AreGroupMembersInDataView)
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

            #region Set up of Group Sync (Decker Group with Male Dataview)

            // Rock Context
            RockContext rc = new RockContext();

            // Sets Group to insert its Id by GUID
            var deckerGroupGuid = "62DC3753-01D5-48B5-B22D-D2825D92900B".AsGuid();
            Group deckerGroup = new GroupService( rc ).Queryable()
                .Where( x => x.Guid == deckerGroupGuid )
                .FirstOrDefault();

            // Gets Small Group Type to change AllowGroupSync to TRUE
            var smallgroupTypeGUID = "50FCFB30-F51A-49DF-86F4-2B176EA1820B".AsGuid();
            GroupType smallGroup = new GroupTypeService( rc ).Queryable()
                .Where( x => x.Guid == smallgroupTypeGUID )
                .First();

            smallGroup.AllowGroupSync = true;
            rc.SaveChanges();

            // Sets Group Type role to insert it's Id by GUID
            var groupTypeRoleGUID = "F0806058-7E5D-4CA9-9C04-3BDF92739462".AsGuid();
            GroupTypeRole smallGroupTypeRoleForID = new GroupTypeRoleService( rc ).Queryable()
                .AsNoTracking()
                .Where( x => x.Guid == groupTypeRoleGUID )
                .First();

            // Sets Group Type role to insert it's Id by GUID
            var dataViewGUID = "C43983D7-1F22-4E94-9F5C-342DA3A0E168".AsGuid();
            DataView dataViewForID = new DataViewService( rc ).Queryable().AsNoTracking()
                .Where( x => x.Guid == dataViewGUID ).First();

            GroupSync syncWithMalesDV = new GroupSync
            {
                GroupId = deckerGroup.Id,
                GroupTypeRoleId = smallGroupTypeRoleForID.Id,
                SyncDataViewId = dataViewForID.Id,
            };

            rc.GroupSyncs.Add( syncWithMalesDV );
            rc.SaveChanges();

            // Set a Decker Group Member(Ted) as Archived
            var deckerMemberToBeArchived = "F0AD1122-6F82-48FA-AFF3-A9C372AA54F4".AsGuid();
            GroupMember deckerMemberToSwitch = new GroupMemberService( rc ).Queryable()
               .AsNoTracking()
               .Where( x => x.Guid == deckerMemberToBeArchived )
               .First();
            
            // Gets Cindy Decker 
            var cindydeckerMemberToBeArchived = "024FE57F-5DCF-483E-A663-DE45A2083AA2".AsGuid();
            GroupMember cindydeckerMemberToSwitch = new GroupMemberService( rc ).Queryable()
               .AsNoTracking()
               .Where( x => x.Guid == cindydeckerMemberToBeArchived )
               .First();

            cindydeckerMemberToSwitch.IsArchived = true;
            deckerMemberToSwitch.IsArchived = true;
            rc.SaveChanges();

            #endregion
          
            #region Run Group Sync

            // run Group History Job
            var gHistoryJobGUID = "D81E577D-2D87-4CEB-9585-7BA8DBA0F556".AsGuid();
           
            var gHistoryjob = new ServiceJobService( rc ).Queryable().AsNoTracking()
                .Where( x => x.Guid == gHistoryJobGUID )
                .First();

            if ( gHistoryjob != null )
            {
                var transaction = new Transactions.RunJobNowTransaction( gHistoryjob.Id );
                transaction.Execute();
            }

            // run process group sync
            var groupSyncGUID = "57B539BC-7C4D-25BB-4EEB-39DF0EF62EBC".AsGuid();
            var job = new ServiceJobService( rc ).Queryable().AsNoTracking()
                .Where( x => x.Guid == groupSyncGUID )
                .First();

            if ( job != null )
            {
                var transaction = new Transactions.RunJobNowTransaction( job.Id );
                transaction.Execute();
            }

            #endregion

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
        /// Ensures Ted is switched back to not being Archived
        /// </summary>
        [TestMethod]
        public void TedDeckerSwitchedFromArchiveTrue()
        {
            // Rock Context
            RockContext rc = new RockContext();

            // Gets Ted 
            var tedPostGroupSync = "F0AD1122-6F82-48FA-AFF3-A9C372AA54F4".AsGuid();
            GroupMember tedAsUnArchived = new GroupMemberService( rc ).Queryable()
               .AsNoTracking()
               .Where( x => x.Guid == tedPostGroupSync )
               .First();

            Assert.IsFalse( tedAsUnArchived.IsArchived );
        }
        
        /// <summary>
        /// Checks to see if Noah was added to the group.
        /// </summary>
        [TestMethod]
        public void ExpectedAddedNoahDeckerToGroup()
        {
            // Rock Context
            RockContext rc = new RockContext();

            var noahsGUID = "32AAB9E4-970D-4551-A17E-385E66113BD5".AsGuid();
            var deckerGroupGuid = "62DC3753-01D5-48B5-B22D-D2825D92900B".AsGuid();

            // Gets Noah's Id
            Person noah = new PersonService( rc ).Queryable().AsNoTracking()
                .Where( x => x.Guid == noahsGUID )
                .First();

            // Sets Group to insert its Id by GUID
            Group deckerGroup = new GroupService( rc ).Queryable()
                .Where( x => x.Guid == deckerGroupGuid )
                .FirstOrDefault();

            // Gets Noah
            GroupMember noahStatusAfterGroupSync = new GroupMemberService( rc ).Queryable()
               .AsNoTracking()
               .Where( x => x.PersonId == noah.Id && x.GroupId == deckerGroup.Id )
               .First();

            Assert.IsFalse( noahStatusAfterGroupSync ==null  );
        }
       
        /// <summary>
        /// Checks to see if Bill remains in group
        /// </summary>
        [TestMethod]
        public void ExpectedBillMarbleToRemainInGroup()
        {
            // Rock Context
            RockContext rc = new RockContext();

            var billsGroupMemberGUID = "8D9C1AA0-4253-4E37-B187-C552B6FF93E8".AsGuid();
            var deckerGroupGuid = "62DC3753-01D5-48B5-B22D-D2825D92900B".AsGuid();

            // Sets Group to insert its Id by GUID
            Group deckerGroup = new GroupService( rc ).Queryable()
                .Where( x => x.Guid == deckerGroupGuid )
                .FirstOrDefault();

            // Gets Bill
            GroupMember billStatusAfterGroupSync = new GroupMemberService( rc ).Queryable()
               .AsNoTracking()
               .Where( x => x.Guid==billsGroupMemberGUID && x.GroupId == deckerGroup.Id )
               .First();

            Assert.IsFalse( billStatusAfterGroupSync == null );
        }

        /// <summary>
        /// Checks to see if Cindy remains in group
        /// </summary>
        [TestMethod]
        public void ExpectedCindyToRemainInGroup()
        {
            // Rock Context
            RockContext rc = new RockContext();

            var deckerGroupGuid = "62DC3753-01D5-48B5-B22D-D2825D92900B".AsGuid();
            var cindydeckerMemberToBeArchived = "024FE57F-5DCF-483E-A663-DE45A2083AA2".AsGuid();

            // Sets Group to insert its Id by GUID
            Group deckerGroup = new GroupService( rc ).Queryable()
                .AsNoTracking()
                .Where( x => x.Guid == deckerGroupGuid )
                .FirstOrDefault();

            // Gets Cindy Decker 
            GroupMember cindydeckerMemberToRemain = new GroupMemberService( rc ).Queryable(false, true)
               .AsNoTracking()
               .Where( x => x.GroupId==deckerGroup.Id && x.Guid== cindydeckerMemberToBeArchived )
               .First();

            Assert.IsFalse( cindydeckerMemberToRemain == null );
        }
        
        /// <summary>
        /// Checks to see if Alisha is still in the group(Group Sync Should have removed her from Decker Group)
        /// </summary>
        [TestMethod]
        public void ExpectedAlishaTobeRemoved()
        {
            // Rock Context
            RockContext rc = new RockContext();
            var deckerGroupGuid = "62DC3753-01D5-48B5-B22D-D2825D92900B".AsGuid();

            // Sets Group to insert its Id by GUID
            Group deckerGroup = new GroupService( rc ).Queryable()
                .AsNoTracking()
                .Where( x => x.Guid == deckerGroupGuid )
                .FirstOrDefault();

            // Ensure Alisha is removed from the Decker Group
            var alishaToBeArchivedFalse = "D389DC8B-6EB4-40FE-9ABB-BCE72F789D62".AsGuid();
            GroupMember deckergroupMember = new GroupMemberService( rc ).Queryable()
               .AsNoTracking()
               .Where( x => x.Guid == alishaToBeArchivedFalse && x.GroupId ==deckerGroup.Id )
               .FirstOrDefault();

            Assert.IsTrue( deckergroupMember == null );
        }

        /// <summary>
        /// Checks to see if same people are in the group that correspond to the dataview.
        /// </summary>
        [TestMethod]
        public void AreGroupMembersInDataView()
        {
            // Rock Context
            RockContext rc = new RockContext();

            var personService = new PersonService( rc );

            var deckerGroupGuid = "62DC3753-01D5-48B5-B22D-D2825D92900B".AsGuid();

            // Sets Group to insert its Id by GUID
            Group deckerGroup = new GroupService( rc ).Queryable()
                .AsNoTracking()
                .Where( x => x.Guid == deckerGroupGuid )
                .FirstOrDefault();

            // Gets DataView (Men)
            var dataViewGUID = "C43983D7-1F22-4E94-9F5C-342DA3A0E168".AsGuid();
            DataView dataViewForID = new DataViewService( rc ).Queryable().AsNoTracking()
                .Where( x => x.Guid == dataViewGUID ).First();

            // Filter people by dataview
            var errorMessages = new List<string>();
            var paramExpression = personService.ParameterExpression;
            var whereExpression = dataViewForID.GetExpression( personService, paramExpression, out errorMessages );
            List<Person> personQry = personService
                .Queryable().AsNoTracking()
                .Where( paramExpression, whereExpression, null ).ToList();

            // Gets Decker Group Member list
            List<GroupMember> deckergroupMember = new GroupMemberService( rc ).Queryable()
               .AsNoTracking()
               .Where( x => x.GroupId == deckerGroup.Id )
               .ToList();

            foreach ( GroupMember person in deckergroupMember )
            {
                var fullName = person.Person.FullName;
                if ( personQry.Any( x => x.FullName == fullName ))
                {
                    continue;
                }
                else
                {
                    Assert.Fail();
                }
            }

            Assert.IsTrue( 1 == 1 );
        }

        #endregion

    }
}
