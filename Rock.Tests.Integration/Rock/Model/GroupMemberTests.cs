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
using Rock;
using Rock.Data;
using Rock.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Entity;

namespace Rock.Tests.Integration.Model
{
    /// <summary>
    /// Used for testing anything regarding GroupMember.
    /// </summary>
    [TestClass]
    public class GroupMemberTests
    {
        #region Setup

        /// <summary>
        /// Runs before any tests in this class are executed.
        /// </summary>
        [ClassInitialize]
        public static void ClassInitialize( TestContext testContext )
        {
            DatabaseTests.ResetDatabase();

            // Rock Context
            RockContext rc = new RockContext();

            // Gets Small Group Type to change EnableGroupHistory to TRUE
            var smallgroupTypeGUID = "50FCFB30-F51A-49DF-86F4-2B176EA1820B".AsGuid();
            GroupType smallGroup = new GroupTypeService( rc ).Queryable()
                .Where( x => x.Guid == smallgroupTypeGUID )
                .First();

            smallGroup.EnableGroupHistory = true;

            // Sets Group to insert its Id by GUID
            var deckerGroupGuid = "62DC3753-01D5-48B5-B22D-D2825D92900B".AsGuid();
            Group deckerGroup = new GroupService( rc ).Queryable()
                .Where( x => x.Guid == deckerGroupGuid )
                .FirstOrDefault();

            // Gets Noah's Id
            var noahsGUID = "32AAB9E4-970D-4551-A17E-385E66113BD5".AsGuid();
            Person noah = new PersonService( rc ).Queryable().AsNoTracking()
                .Where( x => x.Guid == noahsGUID )
                .First();

            // Gets Benjamin's Id
            var benjaminsGUID = "3C402382-3BD2-4337-A996-9E62F1BAB09D".AsGuid();
            Person benjaminJones = new PersonService( rc ).Queryable().AsNoTracking()
                .Where( x => x.Guid == benjaminsGUID )
                .First();

            // Sets Group Type role to insert it's Id by GUID
            var groupTypeRoleGUID = "8F63AB81-A2F7-4D69-82E9-158FDD92DB3D".AsGuid();
            GroupTypeRole groupRoleForID = new GroupTypeRoleService( rc ).Queryable()
                .AsNoTracking()
                .Where( x => x.Guid == groupTypeRoleGUID )
                .First();

            GroupMember noahAsNewGroupMember = new GroupMember
            {
                GroupId = deckerGroup.Id,
                PersonId = noah.Id,
                GroupRoleId = groupRoleForID.Id,
            };

            GroupMember benjaminJonesAsNewGroupMemberArchived = new GroupMember
            {
                GroupId = deckerGroup.Id,
                PersonId = benjaminJones.Id,
                GroupRoleId = groupRoleForID.Id,
                IsArchived = true
            };

            rc.GroupMembers.Add( noahAsNewGroupMember );
            rc.GroupMembers.Add( benjaminJonesAsNewGroupMemberArchived );


            rc.SaveChanges();

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

        #region Group Member Tests

        /// <summary>
        /// Ensure the Exclude Archived Group Member
        /// </summary>
        [TestMethod]
        public void GetGroupMembersExcludeArchived()
        {
            List<GroupMember> deceasedList = new List<GroupMember>();

            using ( var rockContext = new RockContext() )
            {
                deceasedList = new GroupMemberService( rockContext )
                    .Queryable( true )
                    .ToList();
            }

            var areAnyArchived = deceasedList.Any( x => x.IsArchived );
            Assert.IsFalse( areAnyArchived );
        }

        /// <summary>
        /// Checks 'the Group Member Can Delete' method
        /// </summary>
        [TestMethod]
        public void VerifyTedIsUnabletoDeleteAfterAddingNoah()
        {
            // Rock Context
            RockContext rc = new RockContext();
            string message = string.Empty;

            // Gets Ted
            var tedDecker = "F0AD1122-6F82-48FA-AFF3-A9C372AA54F4".AsGuid();
            GroupMember tedGroupMember = new GroupMemberService( rc ).Queryable()
               .AsNoTracking()
               .Where( x => x.Guid == tedDecker )
               .First();

            bool isTedUnabletoDelete = new GroupMemberService( rc )
                    .CanDelete( tedGroupMember, out message  );

            
            Assert.IsFalse( isTedUnabletoDelete );
        }

        /// <summary>
        /// Checks 'Get Group Member By GUID' method
        /// </summary>
        [TestMethod]
        public void GetTedByGUID()
        {
            // Rock Context
            RockContext rc = new RockContext();

            // Gets Ted
            var tedsGUID = "F0AD1122-6F82-48FA-AFF3-A9C372AA54F4".AsGuid();
            GroupMember ted = new GroupMemberService( rc ).Get( tedsGUID );
            
            Assert.IsFalse( ted == null );
        }

        /// <summary>
        /// Checks 'Get Person By Group Member Id' method
        /// </summary>
        [TestMethod]
        public void GetPersonTedByGroupMemberId()
        {
            // Rock Context
            RockContext rc = new RockContext();

            // Gets Ted
            var tedsGUID = "F0AD1122-6F82-48FA-AFF3-A9C372AA54F4".AsGuid();
            GroupMember tedGM = rc.GroupMembers.AsNoTracking().Where( x => x.Guid == tedsGUID ).First();
            Person ted = new GroupMemberService( rc ).GetPerson(tedGM.Id );

            Assert.IsFalse( ted == null );
        }

        /// <summary>
        /// Checks 'Get Archived Group Members' method
        /// </summary>
        [TestMethod]
        public void ChecksBenjaminJonesasBeingArchivedThroughGetArchivedGroupMembers()
        {
            // Rock Context
            RockContext rc = new RockContext();
            
            IQueryable<GroupMember> archivedList = new GroupMemberService( rc ).GetArchived();
            
            Assert.IsTrue( archivedList.Any( x => x.Guid == "6d6122ca-c5a1-46fd-839c-de9534bd0519".AsGuid() ) );
        }

        /// <summary>
        /// Queryable
        /// </summary>
        [TestMethod]
        public void ChecksTheQueryableMethodReturnType()
        {
            // Rock Context
            RockContext rc = new RockContext();

            var queryable = new GroupMemberService( rc ).Queryable();
            
            Assert.IsTrue( queryable is IQueryable );
        }

        /// <summary>
        /// Queryable with deceased included
        /// </summary>
        [TestMethod]
        public void ChecksTheQueryableMethodReturnTypeIncludingDeceased()
        {
            // Rock Context
            RockContext rc = new RockContext();

            var queryable = new GroupMemberService( rc ).Queryable( true );
            
            Assert.IsTrue( queryable is IQueryable && queryable.Any( x => x.Person.IsDeceased == true ) );
        }

        /// <summary>
        /// Queryable with deceased included and including archived
        /// </summary>
        [TestMethod]
        public void ChecksTheQueryableMethodReturnTypeIncludingDeceasedIncludeArchived()
        {
            // Rock Context
            RockContext rc = new RockContext();

            var queryable = new GroupMemberService( rc ).Queryable( true , true );

            Assert.IsTrue( queryable is IQueryable && queryable.Any( x => x.Person.IsDeceased == true ) && queryable.Any( x => x.IsArchived == true ) );
        }

        /// <summary>
        /// Ensure the Deceased Group Member is included
        /// Depends on at least one Group Member being in a group that is deceased
        /// </summary>
        [TestMethod]
        public void GetGroupMembersIncludeDeceased()
        {
            List<GroupMember> deceasedList = new List<GroupMember>();
            List<Person> deceasedpersonList = new List<Person>();
            using ( var rockContext = new RockContext() )
            {

                deceasedList = new GroupMemberService( rockContext ).Queryable( true ).ToList();

                foreach ( GroupMember deceasedgMember in deceasedList )
                {
                    if ( deceasedgMember.Person.IsDeceased )
                    {
                        Assert.IsTrue( deceasedgMember.Person.IsDeceased );
                    }
                }
                Assert.Fail();
            }
        }

        #endregion
    }
}
