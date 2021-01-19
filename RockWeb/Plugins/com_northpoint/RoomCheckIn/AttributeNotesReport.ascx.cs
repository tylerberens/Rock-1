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
using System.IO;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using Rock.Web.UI;
using Rock.CheckIn;
using Rock.Lava;
using System.Collections.Specialized;

namespace RockWeb.Plugins.org_northpoint.RoomCheckin
{
    /// <summary>
    /// </summary>
    [DisplayName( "Attribute Notes Report" )]
    [Category( "NorthPoint > CheckinManager " )]
    [Description( "Lists the people who are checked-in and have values for the configured attributes." )]

    [AttributeField( "Attributes",
        Key = AttributeKey.PersonAttributes,
        Description = "Attributes to show in the grid.",
        EntityTypeGuid = Rock.SystemGuid.EntityType.PERSON,
        AllowMultiple = true,
        Order = 0 )]

    [TextField( "Report Title",
        Key = AttributeKey.ReportTitle,
        DefaultValue = "Attribute Notes Report",
        Order = 1 )]

    [BooleanField( "Show Only If Has Attribute Value",
        Key = AttributeKey.ShowOnlyIfHasAttributeValue,
        Description = "When checked, a person will only appear on the report if the configured attributes have a value for the person",
        DefaultBooleanValue = true,
        Order = 2 )]

    public partial class AttributeNotesReport : RockBlock
    {
        #region Keys

        /// <summary>
        /// Keys for attributes
        /// </summary>
        private static class AttributeKey
        {
            public const string PersonAttributes = "PersonAttributes";
            public const string ReportTitle = "PanelTitle";
            public const string ShowOnlyIfHasAttributeValue = "ShowOnlyIfHasAttributeValue";
        }

        #endregion

        #region Page Parameter Keys

        private class PageParameterKey
        {
            /// <summary>
            /// Gets or sets the current 'Check-in Configuration' Guid (which is a <see cref="Rock.Model.GroupType" /> Guid).
            /// For example "Weekly Service Check-in".
            /// </summary>
            public const string Area = "Area";

            public const string LocationId = "LocationId";
        }

        #endregion Page Parameter Keys

        #region ViewState Keys

        /// <summary>
        /// Keys to use for ViewState.
        /// </summary>
        private class ViewStateKey
        {
            public const string CurrentCampusId = "CurrentCampusId";
            public const string CurrentLocationId = "CurrentLocationId";
        }

        #endregion ViewState Keys

        /// <summary>
        /// The current campus identifier.
        /// </summary>
        public int CurrentCampusId
        {
            get
            {
                return ViewState[ViewStateKey.CurrentCampusId] as int? ?? 0;
            }

            set
            {
                ViewState[ViewStateKey.CurrentCampusId] = value;
            }
        }

        /// <summary>
        /// The current location identifier.
        /// </summary>
        public int CurrentLocationId
        {
            get
            {
                return ViewState[ViewStateKey.CurrentLocationId] as int? ?? 0;
            }

            set
            {
                ViewState[ViewStateKey.CurrentLocationId] = value;
            }
        }

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

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
                AddDynamicAttributeColumns();
                BindGrid();
            }
        }

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            AddDynamicAttributeColumns();
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
            NavigateToCurrentPageReference();
        }

        /// <summary>
        /// Handles the SelectLocation event of the lpLocation control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lpLocation_SelectLocation( object sender, EventArgs e )
        {
            Location location = lpLocation.Location;
            if ( location != null )
            {
                CheckinManagerHelper.SetSelectedLocation( this, lpLocation, location.Id, CurrentCampusId );
            }
            else
            {
                CheckinManagerHelper.SetSelectedLocation( this, lpLocation, 0, CurrentCampusId );
            }
        }

        /// <summary>
        /// Handles the Click event of the btnRefresh control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnRefresh_Click( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the campus from context.
        /// </summary>
        /// <returns></returns>
        private CampusCache GetCampusFromContext()
        {
            CampusCache campus = null;

            var campusEntityType = EntityTypeCache.Get( "Rock.Model.Campus" );
            if ( campusEntityType != null )
            {
                var campusContext = RockPage.GetCurrentContext( campusEntityType ) as Campus;
                if ( campusContext != null )
                {
                    campus = CampusCache.Get( campusContext.Id );
                }
            }

            return campus;
        }

        /// <summary>
        /// Handles the RowDataBound event of the gAttendees control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        protected void gAttendees_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType != DataControlRowType.DataRow )
            {
                return;
            }

            RosterAttendee attendee = e.Row.DataItem as RosterAttendee;

            var lPhoto = e.Row.FindControl( "lPhoto" ) as Literal;
            lPhoto.Text = attendee.GetPersonPhotoImageHtmlTag();
            var lName = e.Row.FindControl( "lName" ) as Literal;
            lName.Text = attendee.GetAttendeeNameHtml();

            var lGroupNameAndPath = e.Row.FindControl( "lGroupNameAndPath" ) as Literal;
            if ( lGroupNameAndPath != null && lGroupNameAndPath.Visible )
            {
                lGroupNameAndPath.Text = string.Format( @"<div class='group-name'>{0}</div> <div class='small text-muted text-wrap'>{1}</div>", attendee.GroupName, attendee.GroupTypePath );
            }
        }

        /// <summary>
        /// Shows a warning message, and optionally hides the content panels.
        /// </summary>
        /// <param name="warningMessage">The warning message to show.</param>
        /// <param name="hideLocationPicker">Whether to hide the lpLocation control.</param>
        private void ShowWarningMessage( string warningMessage, bool hideLocationPicker )
        {
            nbWarning.Text = warningMessage;
            nbWarning.Visible = true;
            lpLocation.Visible = !hideLocationPicker;
            pnlSubPageNav.Visible = false;
        }

        /// <summary>
        /// Initializes the sub page navigation.
        /// </summary>
        /// <param name="locationId">The location identifier.</param>
        private void InitializeSubPageNav( int locationId )
        {
            RockPage rockPage = this.Page as RockPage;
            if ( rockPage != null )
            {
                PageCache page = PageCache.Get( rockPage.PageId );
                if ( page != null )
                {
                    pbSubPages.RootPageId = page.ParentPageId ?? 0;
                }
            }

            pbSubPages.QueryStringParametersToAdd = new NameValueCollection
            {
                { PageParameterKey.LocationId, locationId.ToString() }
            };
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            lPanelTitle.Text = GetAttributeValue( AttributeKey.ReportTitle );

            var checkinAreaFilter = CheckinManagerHelper.GetCheckinAreaFilter( this );
            CampusCache campus = GetCampusFromContext();
            if ( campus == null )
            {
                ShowWarningMessage( "Please select a Campus.", true );
                return;
            }

            int? locationId = CheckinManagerHelper.GetSelectedLocation( this, campus, lpLocation );
            if ( !locationId.HasValue )
            {
                ShowWarningMessage( "Please select a location", false );
                return;
            }

            CheckinManagerHelper.SetSelectedLocation( this, lpLocation, locationId, CurrentCampusId );

            InitializeSubPageNav( locationId.Value );

            IList<RosterAttendee> attendees = null;

            using ( var rockContext = new RockContext() )
            {
                attendees = GetAttendees( rockContext );
            }

            // filter attendees to ones that have an attribute value
            var attributesToShow = GetAttributeValues( AttributeKey.PersonAttributes ).AsGuidList().Select( a => AttributeCache.Get( a ) );
            var showOnlyIfHasAttributeValue = GetAttributeValue( AttributeKey.ShowOnlyIfHasAttributeValue ).AsBoolean();
            attendees = attendees.Where( a =>
            {
                var person = a.Person;

                if ( !showOnlyIfHasAttributeValue )
                {
                    // if we aren't filtering to people that have a value for the attributes, include everybody
                    return true;
                }

                // if showOnlyIfHasAttributeValue is true, limit to people that have a value for at least one of the attributes
                if ( person.Attributes == null )
                {
                    person.LoadAttributes();
                }

                foreach ( var attribute in attributesToShow )
                {
                    if ( person.GetAttributeValue( attribute.Key ).IsNotNullOrWhiteSpace() )
                    {
                        return true;
                    }
                }

                return false;
            } ).ToList();

            // sort by CheckinTime, and also by PersonGuid (so that stay in a consistent order in cases where CheckinTimes are identical
            var attendeesSorted = attendees
                .OrderByDescending( a => a.CheckInTime )
                .ThenBy( a => a.PersonGuid )
                .ToList();

            // tell the Grid where to get Attributes from since we aren't passing a list of Person to the grid
            gAttendees.ObjectList = attendeesSorted.Select( a => a.Person ).ToDictionary( k => k.Guid.ToString(), v => ( object ) v );

            gAttendees.DataSource = attendeesSorted;
            gAttendees.DataBind();
        }

        /// <summary>
        /// Gets the attendees.
        /// </summary>
        private IList<RosterAttendee> GetAttendees( RockContext rockContext )
        {
            var startDateTime = RockDateTime.Today;

            CampusCache campusCache = GetCampusFromContext();
            DateTime currentDateTime;
            if ( campusCache != null )
            {
                currentDateTime = campusCache.CurrentDateTime;
            }
            else
            {
                currentDateTime = RockDateTime.Now;
            }

            var locationId = CheckinManagerHelper.GetSelectedLocation( this, campusCache, lpLocation );
            if ( !locationId.HasValue )
            {
                // shouldn't happen, locationId is checked BindGrid
                return null;
            }

            // Get all Attendance records for the current day and location
            var attendanceQuery = new AttendanceService( rockContext ).Queryable().Where( a =>
                a.StartDateTime >= startDateTime
                && a.DidAttend == true
                && a.StartDateTime <= currentDateTime
                && a.PersonAliasId.HasValue
                && a.Occurrence.GroupId.HasValue
                && a.Occurrence.ScheduleId.HasValue
                && a.Occurrence.LocationId.HasValue
                && a.Occurrence.ScheduleId.HasValue );

            attendanceQuery = attendanceQuery.Where( a => a.Occurrence.LocationId.Value == locationId.Value );

            attendanceQuery = CheckinManagerHelper.FilterByRosterStatusFilter( attendanceQuery, RosterStatusFilter.CheckedIn );

            List<Attendance> attendanceList = attendanceQuery
                .Include( a => a.AttendanceCode )
                .Include( a => a.PersonAlias.Person )
                .Include( a => a.Occurrence.Schedule )
                .Include( a => a.Occurrence.Group )
                .AsNoTracking()
                .ToList();

            attendanceList = CheckinManagerHelper.FilterByActiveCheckins( currentDateTime, attendanceList );

            attendanceList = attendanceList.Where( a => a.PersonAlias != null && a.PersonAlias.Person != null ).ToList();
            var attendees = RosterAttendee.GetFromAttendanceList( attendanceList );

            return attendees;
        }

        /// <summary>
        /// Adds the dynamic attribute columns.
        /// </summary>
        private void AddDynamicAttributeColumns()
        {
            var attributesToShow = GetAttributeValues( AttributeKey.PersonAttributes ).AsGuidList().Select( a => AttributeCache.Get( a ) );
            foreach ( var attribute in attributesToShow )
            {
                bool columnExists = gAttendees.Columns.OfType<AttributeField>().FirstOrDefault( a => a.AttributeId == attribute.Id ) != null;
                if ( !columnExists )
                {
                    AttributeField boundField = new AttributeField();
                    boundField.DataField = attribute.Key;
                    boundField.AttributeId = attribute.Id;
                    boundField.HeaderText = attribute.Name;
                    boundField.ItemStyle.HorizontalAlign = HorizontalAlign.Left;

                    gAttendees.Columns.Add( boundField );
                }
            }
        }

        #endregion
    }
}