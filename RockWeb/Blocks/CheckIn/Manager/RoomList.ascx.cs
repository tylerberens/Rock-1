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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.CheckIn.Manager
{
    /// <summary>
    /// </summary>
    [DisplayName( "Room List" )]
    [Category( "Check-in > Manager" )]
    [Description( "Shows all locations of the type room for the campus (context) and selected schedules." )]

    #region Block Attributes

    [BooleanField(
        "Show All Areas",
        Key = AttributeKey.ShowAllAreas,
        Description = "If enabled, all Check-in Areas will be shown. This setting will be ignored if a specific area is specified in the URL.",
        DefaultBooleanValue = true,
        Order = 1 )]

    [LinkedPage(
        "Area Select Page",
        Key = AttributeKey.AreaSelectPage,
        Description = "If Show All Areas is not enabled, the page to redirect user to if a Check-in Area has not been configured or selected.",
        IsRequired = false,
        Order = 2 )]

    [GroupTypeField(
        "Check-in Area",
        Key = AttributeKey.CheckInAreaGuid,
        Description = "If Show All Areas is not enabled, the Check-in Area for the rooms to be managed by this Block.",
        IsRequired = false,
        GroupTypePurposeValueGuid = Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE,
        Order = 3 )]

    #endregion Block Attributes
    public partial class RoomList : RockBlock
    {
        #region Attribute Keys

        /// <summary>
        /// Keys to use for block attributes.
        /// </summary>
        private class AttributeKey
        {
            public const string PersonPage = "PersonPage";
            public const string ShowAllAreas = "ShowAllAreas";
            public const string AreaSelectPage = "AreaSelectPage";

            /// <summary>
            /// Gets or sets the current 'Check-in Configuration' Guid (which is a <see cref="Rock.Model.GroupType" /> Guid).
            /// For example "Weekly Service Check-in".
            /// </summary>
            public const string CheckInAreaGuid = "CheckInAreaGuid";
        }

        #endregion Attribute Keys

        #region Page Parameter Keys

        private class PageParameterKey
        {
            /// <summary>
            /// Gets or sets the current 'Check-in Configuration' Guid (which is a <see cref="Rock.Model.GroupType" /> Guid).
            /// For example "Weekly Service Check-in".
            /// </summary>
            public const string Area = "Area";
        }

        #endregion Page Parameter Keys

        #region UserPreferenceKeys

        private static class UserPreferenceKey
        {
            public const string SelectedScheduleIds = "SelectedScheduleIds";
        }

        #endregion UserPreferenceKeys

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gRoomList.GridRebind += gList_GridRebind;

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                BindGrid();
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            // reload if block settings change
            NavigateToCurrentPageReference();
        }

        /// <summary>
        /// Handles the GridRebind event of the gPledges control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gList_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        #region Methods

        /// <summary>
        /// If <seealso cref="AttributeKey.ShowAllAreas"/> is set to false, the 'Check-in Configuration' (which is a <see cref="Rock.Model.GroupType" /> Guid) to limit to
        /// For example "Weekly Service Check-in".
        /// </summary>
        public GroupTypeCache GetCheckinAreaFilter()
        {
            // If a Check-in Area query string parameter is defined, it takes precedence.
            Guid? checkinManagerPageParameterCheckinAreaGuid = PageParameter( PageParameterKey.Area ).AsGuidOrNull();
            if ( checkinManagerPageParameterCheckinAreaGuid.HasValue )
            {
                var checkinManagerPageParameterCheckinArea = GroupTypeCache.Get( checkinManagerPageParameterCheckinAreaGuid.Value );

                if ( checkinManagerPageParameterCheckinArea != null )
                {
                    return checkinManagerPageParameterCheckinArea;
                }
            }

            // if ShowAllAreas is enabled, we won't filter by Check-in Area (unless there was a page parameter)
            if ( this.GetAttributeValue( AttributeKey.ShowAllAreas ).AsBoolean() )
            {
                return null;
            }

            // we ShowAllAreas is false, get the area filter from the cookie
            var checkinManagerCookieCheckinAreaGuid = CheckinManagerHelper.GetCheckinManagerConfigurationFromCookie().CheckinAreaGuid;
            if ( checkinManagerCookieCheckinAreaGuid != null )
            {
                var checkinManagerCookieCheckinArea = GroupTypeCache.Get( checkinManagerCookieCheckinAreaGuid.Value );
                if ( checkinManagerCookieCheckinArea != null )
                {
                    return checkinManagerCookieCheckinArea;
                }
            }

            // Next, check the Block AttributeValue.
            var checkinManagerBlockAttributeCheckinAreaGuid = this.GetAttributeValue( AttributeKey.CheckInAreaGuid ).AsGuidOrNull();
            if ( checkinManagerBlockAttributeCheckinAreaGuid.HasValue )
            {
                var checkinManagerBlockAttributeCheckinArea = GroupTypeCache.Get( checkinManagerBlockAttributeCheckinAreaGuid.Value );
                if ( checkinManagerBlockAttributeCheckinArea != null )
                {
                    return checkinManagerBlockAttributeCheckinArea;
                }
            }

            return null;
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            GroupTypeCache checkinAreaFilter = null;

            // if ShowAllAreas is false, the CheckinAreaFilter is required
            if ( this.GetAttributeValue( AttributeKey.ShowAllAreas ).AsBoolean() == false )
            {
                checkinAreaFilter = GetCheckinAreaFilter();
                if ( checkinAreaFilter == null )
                {
                    if ( NavigateToLinkedPage( AttributeKey.AreaSelectPage ) )
                    {
                        // we are navigating to get the Area Filter, which will get the Area cookie
                        return;
                    }
                    else
                    {
                        nbWarning.Text = "The 'Area Select Page' Block Attribute must be defined.";
                        nbWarning.NotificationBoxType = NotificationBoxType.Warning;
                        nbWarning.Visible = true;
                        return;
                    }
                }
            }

            var selectedScheduleIds = GetBlockUserPreference( UserPreferenceKey.SelectedScheduleIds ).SplitDelimitedValues().AsIntegerList();
            if ( selectedScheduleIds.Any() )
            {
                btnShowFilter.AddCssClass( "criteria-exists" );
            }
            else
            {
                btnShowFilter.RemoveCssClass( "criteria-exists" );
            }

            var rockContext = new RockContext();
            var groupService = new GroupService( rockContext );
            var groupTypeService = new GroupTypeService( rockContext );
            IEnumerable<CheckinAreaPath> checkinAreaPaths;
            if ( checkinAreaFilter != null )
            {
                checkinAreaPaths = groupTypeService.GetCheckinAreaDescendantsPath( checkinAreaFilter.Id );
            }
            else
            {
                checkinAreaPaths = groupTypeService.GetAllCheckinAreaPaths();
            }

            var selectedGroupTypeIds = checkinAreaPaths.Select( a => a.GroupTypeId ).Distinct().ToArray();

            var groupLocationService = new GroupLocationService( rockContext );
            var groupLocationsQuery = groupLocationService.Queryable().Where( gl => selectedGroupTypeIds.Contains( gl.Group.GroupTypeId ) );
            if ( selectedScheduleIds.Any() )
            {
                groupLocationsQuery = groupLocationsQuery.Where( a => a.Schedules.Any( s => s.IsActive && s.CheckInStartOffsetMinutes.HasValue && selectedScheduleIds.Contains( s.Id ) ) );
            }
            else
            {
                groupLocationsQuery = groupLocationsQuery.Where( a => a.Schedules.Any( s => s.IsActive && s.CheckInStartOffsetMinutes.HasValue ) );
            }

            var groupLocationList = groupLocationsQuery.Select( a => new
            {
                LocationId = a.LocationId,
                LocationName = a.Location.Name,
                GroupId = a.Group.Id,
                GroupName = a.Group.Name,
                GroupTypeId = a.Group.GroupTypeId
            } ).ToList();

            var startDateTime = RockDateTime.Today;
            var currentDateTime = RockDateTime.Now;

            // Get all Attendance records for the current day and location
            var attendanceQuery = new AttendanceService( rockContext ).Queryable().Where( a =>
                a.StartDateTime >= startDateTime
                && a.DidAttend == true
                && a.StartDateTime <= currentDateTime
                && a.Occurrence.GroupId.HasValue
                && a.PersonAliasId.HasValue
                && a.Occurrence.LocationId.HasValue
                && a.Occurrence.ScheduleId.HasValue );

            attendanceQuery = attendanceQuery.Where( a => selectedGroupTypeIds.Contains( a.Occurrence.Group.GroupTypeId ) );

            if ( selectedScheduleIds.Any() )
            {
                attendanceQuery = attendanceQuery.Where( a => selectedScheduleIds.Contains( a.Occurrence.ScheduleId.Value ) );
            }

            var attendanceCheckinTimeInfoList = attendanceQuery.Select( a => new
            {
                LocationId = a.Occurrence.LocationId.Value,
                GroupId = a.Occurrence.GroupId.Value,
                EndDateTime = a.EndDateTime,
                PresentDateTime = a.PresentDateTime
            } );

            var locationCountLookup = attendanceCheckinTimeInfoList.GroupBy( a => a.LocationId ).ToDictionary( k => k.Key, v => new RoomCounts
            {
                CheckedInCount = v.Where( x => RosterAttendee.GetRosterAttendeeStatus( x.EndDateTime, x.PresentDateTime ) == RosterAttendeeStatus.CheckedIn ).Count(),
                PresentCount = v.Where( x => RosterAttendee.GetRosterAttendeeStatus( x.EndDateTime, x.PresentDateTime ) == RosterAttendeeStatus.Present ).Count(),
                CheckedOutCount = v.Where( x => RosterAttendee.GetRosterAttendeeStatus( x.EndDateTime, x.PresentDateTime ) == RosterAttendeeStatus.CheckedOut ).Count()
            } );

            var checkinAreaPathsLookup = checkinAreaPaths.ToDictionary( k => k.GroupTypeId, v => v );

            var roomList = groupLocationList.GroupBy( a => a.LocationId ).Select( a => new RoomInfo
            {
                Id = a.Key,
                LocationName = a.FirstOrDefault().LocationName,
                RoomCounts = locationCountLookup.GetValueOrNull( a.Key ),
                GroupList = a.Select( g => new RoomGroupPathInfo
                {
                    GroupId = g.GroupId,
                    GroupName = g.GroupName,
                    GroupTypeId = g.GroupTypeId,
                    GroupTypePath = checkinAreaPathsLookup.GetValueOrNull( g.GroupTypeId )
                } ).ToList()
            } ).ToList();

            var sortedRoomList = roomList.OrderBy( a => a.LocationName ).ToList();

            gRoomList.DataKeyNames = new string[1] { "Id" };
            gRoomList.DataSource = sortedRoomList;
            gRoomList.DataBind();
        }

        /// <summary>
        /// Gets the campus from the current context.
        /// </summary>
        private CampusCache GetCampusFromContext()
        {
            CampusCache campus = null;

            var campusEntityType = EntityTypeCache.Get( "Rock.Model.Campus" );
            if ( campusEntityType != null )
            {
                var campusContext = RockPage.GetCurrentContext( campusEntityType ) as Campus;

                campus = CampusCache.Get( campusContext );
            }

            return campus;
        }

        #endregion

        /// <summary>
        /// Handles the RowDataBound event of the gRoomList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        protected void gRoomList_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            RoomInfo roomInfo = e.Row.DataItem as RoomInfo;
            if ( roomInfo == null )
            {
                return;
            }

            var lRoomName = e.Row.FindControl( "lRoomName" ) as Literal;
            var lGroupName = e.Row.FindControl( "lGroupName" ) as Literal;
            var lCheckedInCount = e.Row.FindControl( "lCheckedInCount" ) as Literal;
            var lPresentCount = e.Row.FindControl( "lPresentCount" ) as Literal;
            var lCheckedOutCount = e.Row.FindControl( "lCheckedOutCount" ) as Literal;

            lRoomName.Text = roomInfo.LocationName;

            lGroupName.Text = roomInfo.GroupsPathHTML;
            lCheckedInCount.Text = "TODO";
            lPresentCount.Text = "TODO";
            lCheckedOutCount.Text = "TODO";
        }

        /// <summary>
        /// Handles the Click event of the btnShowFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnShowFilter_Click( object sender, EventArgs e )
        {
            pnlFilterCriteria.Visible = !pnlFilterCriteria.Visible;
            if ( pnlFilterCriteria.Visible )
            {
                ShowFilters();
            }
        }

        /// <summary>
        /// Shows the filters.
        /// </summary>
        private void ShowFilters()
        {
            ScheduleService scheduleService = new ScheduleService( new RockContext() );

            List<int> selectedScheduleIds = GetBlockUserPreference( UserPreferenceKey.SelectedScheduleIds ).SplitDelimitedValues().AsIntegerList();

            // limit Schedules to ones that are Active, have a CheckInStartOffsetMinutes, and are Named schedules
            var scheduleQry = scheduleService.Queryable().Where( a => a.IsActive && a.CheckInStartOffsetMinutes != null && a.Name != null && a.Name != string.Empty );

            var scheduleList = scheduleQry.ToList().OrderBy( a => a.Name ).ToList();

            var sortedScheduleList = scheduleList.OrderByOrderAndNextScheduledDateTime();
            lbSchedules.Items.Clear();

            foreach ( var schedule in sortedScheduleList )
            {
                var listItem = new ListItem();
                if ( schedule.Name.IsNotNullOrWhiteSpace() )
                {
                    listItem.Text = schedule.Name;
                }
                else
                {
                    listItem.Text = schedule.FriendlyScheduleText;
                }

                listItem.Value = schedule.Id.ToString();
                listItem.Selected = selectedScheduleIds.Contains( schedule.Id );
                lbSchedules.Items.Add( listItem );
            }
        }

        /// <summary>
        /// Handles the Click event of the btnApplyFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnApplyFilter_Click( object sender, EventArgs e )
        {
            SetBlockUserPreference( UserPreferenceKey.SelectedScheduleIds, lbSchedules.SelectedValuesAsInt.AsDelimited( "," ) );
            pnlFilterCriteria.Visible = false;
            BindGrid();
        }

        /// <summary>
        /// Handles the Click event of the btnClearFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnClearFilter_Click( object sender, EventArgs e )
        {
            SetBlockUserPreference( UserPreferenceKey.SelectedScheduleIds, null );
            ShowFilters();
        }

        /// <summary>
        /// 
        /// </summary>
        private class RoomInfo
        {
            public int Id { get; internal set; }

            public string LocationName { get; internal set; }

            public List<RoomGroupPathInfo> GroupList { get; internal set; }

            public RoomCounts RoomCounts { get; set; } 

            public string GroupsPathHTML
            {
                get
                {
                    var sortGroupList = GroupList.OrderBy( x => x.GroupTypePath.Path );
                    var groupsPathList = sortGroupList.Select( s => string.Format( "<small>{0} &gt; {1} </small>", s.GroupTypePath.Path, s.GroupName ) ).ToList();
                    return groupsPathList.AsDelimited( "<br>" );
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private class RoomGroupPathInfo
        {
            public int GroupId { get; internal set; }

            public string GroupName { get; internal set; }

            public int GroupTypeId { get; internal set; }

            public CheckinAreaPath GroupTypePath { get; internal set; }
        }

        private class RoomCounts
        {
            public int CheckedInCount { get; internal set; }
            public int PresentCount { get; internal set; }
            public int CheckedOutCount { get; internal set; }
        }
    }
}