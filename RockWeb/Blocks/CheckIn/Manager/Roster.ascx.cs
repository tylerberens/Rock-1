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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.CheckIn.Manager
{
    /// <summary>
    /// Block used to view people currently checked into a classroom, mark a person as 'present' in the classroom, check them out, Etc.
    /// </summary>
    [DisplayName( "Roster" )]
    [Category( "Check-in > Manager" )]
    [Description( "Block used to view people currently checked into a classroom, mark a person as 'present' in the classroom, check them out, Etc." )]

    #region Block Attributes

    [GroupTypeField( "Check-in Type",
        Key = AttributeKey.CheckInAreaGuid,
        Description = "The Check-in Area for the rooms to be managed by this Block. This value can also be overridden through the URL query string 'Area' key (e.g. when navigated to from the Check-in Type selection block).",
        IsRequired = false,
        GroupTypePurposeValueGuid = Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE,
        Order = 1 )]

    [LinkedPage( "Person Page",
        Key = AttributeKey.PersonPage,
        Description = "The page used to display a selected person's details.",
        IsRequired = true,
        Order = 2 )]

    [LinkedPage( "Area Select Page",
        Key = AttributeKey.AreaSelectPage,
        Description = "The page to redirect user to if a Check-in Area has not been configured or selected.",
        IsRequired = true,
        Order = 3 )]

    #endregion Block Attributes

    public partial class Roster : Rock.Web.UI.RockBlock
    {
        #region Attribute Keys

        /// <summary>
        /// Keys to use for block attributes.
        /// </summary>
        private class AttributeKey
        {
            public const string CheckInAreaGuid = "CheckInAreaGuid";
            public const string PersonPage = "PersonPage";
            public const string AreaSelectPage = "AreaSelectPage";
        }

        #endregion Attribute Keys

        #region Page Parameter Keys

        private class PageParameterKey
        {
            public const string Area = "Area";
            public const string LocationId = "LocationId";
            public const string Person = "Person";
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
            public const string CheckInAreaGuid = "CheckInAreaGuid";
            public const string AllowCheckout = "AllowCheckout";
            public const string EnablePresence = "EnablePresence";
            public const string CurrentStatusFilter = "CurrentStatusFilter";
        }

        #endregion ViewState Keys

        #region Entity Attribute Value Keys

        /// <summary>
        /// Keys to use for entity attribute values.
        /// </summary>
        private class EntityAttributeValueKey
        {
            public const string GroupType_AllowCheckout = "core_checkin_AllowCheckout";
            public const string GroupType_EnablePresence = "core_checkin_EnablePresence";

            public const string Person_Allergy = "Allergy";
            public const string Person_LegalNotes = "LegalNotes";
        }

        #endregion Entity Attribute Value Keys

        #region Properties

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

        /// <summary>
        /// The current area unique identifier.
        /// </summary>
        public Guid? CurrentAreaGuid
        {
            get
            {
                return ViewState[ViewStateKey.CheckInAreaGuid] as Guid?;
            }

            set
            {
                ViewState[ViewStateKey.CheckInAreaGuid] = value;
            }
        }

        /// <summary>
        /// Whether to allow checkout.
        /// </summary>
        public bool AllowCheckout
        {
            get
            {
                return ViewState[ViewStateKey.AllowCheckout] as bool? ?? false;
            }

            set
            {
                ViewState[ViewStateKey.AllowCheckout] = value;
            }
        }

        /// <summary>
        /// Whether to enable presence.
        /// </summary>
        public bool EnablePresence
        {
            get
            {
                return ViewState[ViewStateKey.EnablePresence] as bool? ?? false;
            }

            set
            {
                ViewState[ViewStateKey.EnablePresence] = value;
            }
        }

        /// <summary>
        /// The current status filter.
        /// </summary>
        public StatusFilter CurrentStatusFilter
        {
            get
            {
                StatusFilter statusFilter = ViewState[ViewStateKey.CurrentStatusFilter] as StatusFilter? ?? StatusFilter.Unknown;
                return statusFilter;
            }

            set
            {
                ViewState[ViewStateKey.CurrentStatusFilter] = value;
            }
        }

        #endregion Properties

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

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

            BuildRoster();
        }

        #endregion Base Control Methods

        #region Control Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            // Wipe out these values to trigger reloading of data.
            CurrentAreaGuid = null;
            CurrentLocationId = 0;
            CurrentStatusFilter = StatusFilter.Unknown;

            BuildRoster();
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
                SaveRosterConfigurationToCookie( CurrentCampusId, location.Id );
            }
            else
            {
                SaveRosterConfigurationToCookie( CurrentCampusId, null );
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the bgStatus control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void bgStatus_SelectedIndexChanged( object sender, EventArgs e )
        {
            StatusFilter statusFilter = GetStatusFilterValueFromControl();
            SaveRosterConfigurationToCookie( statusFilter );
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

            Attendee attendee = e.Row.DataItem as Attendee;

            string statusClass = string.Empty;
            string mobileIcon = @"<i class=""fa fa-{0}""></i>";
            switch ( attendee.Status )
            {
                case AttendeeStatus.CheckedIn:
                    statusClass = "warning";
                    mobileIcon = "&nbsp;";
                    break;
                case AttendeeStatus.Present:
                    statusClass = "success";
                    mobileIcon = string.Format( mobileIcon, "check" );
                    break;
                case AttendeeStatus.CheckedOut:
                    statusClass = "danger";
                    mobileIcon = string.Format( mobileIcon, "minus" );
                    break;
            }

            // Desktop only.
            var lPhoto = e.Row.FindControl( "lPhoto" ) as Literal;
            if ( lPhoto != null )
            {
                var imgTag = Rock.Model.Person.GetPersonPhotoImageTag( attendee.PersonId, attendee.PhotoId, attendee.Age, attendee.Gender, null, 50, 50, attendee.Name, "avatar avatar-lg" );
                lPhoto.Text = string.Format( @"{0}", imgTag );
            }

            // Mobile only.
            var lMobileIcon = e.Row.FindControl( "lMobileIcon" ) as Literal;
            if ( lMobileIcon != null )
            {
                lMobileIcon.Text = string.Format( @"<span class=""badge badge-circle badge-{0}"">{1}</span>", statusClass, mobileIcon );
            }

            // Shared between desktop and mobile.
            var lName = e.Row.FindControl( "lName" ) as Literal;
            if ( lName != null )
            {
                lName.Text = string.Format( @"<div class=""name""><span class=""js-checkin-person-name"">{0}</span><span class=""badges d-sm-none"">{1}</span></div><div class=""parent-name small text-muted"">{2}</div>",
                    attendee.Name,
                    GetBadges( attendee, true ),
                    attendee.ParentNames );
            }

            // Desktop only.
            var lBadges = e.Row.FindControl( "lBadges" ) as Literal;
            if ( lBadges != null )
            {
                lBadges.Text = string.Format( "<div>{0}</div>", GetBadges( attendee, false ) );
            }

            // Mobile only.
            var lMobileTagAndSchedules = e.Row.FindControl( "lMobileTagAndSchedules" ) as Literal;
            if ( lMobileTagAndSchedules != null )
            {
                lMobileTagAndSchedules.Text = string.Format( @"<div class=""person-tag"">{0}</div><div class=""small text-muted"">{1}</div>", attendee.Tag, attendee.ServiceTimes );
            }

            // Desktop only.
            var lCheckInTime = e.Row.FindControl( "lCheckInTime" ) as Literal;
            if ( lCheckInTime != null )
            {
                lCheckInTime.Text = RockFilters.HumanizeTimeSpan( attendee.CheckInTime, DateTime.Now, unit: "Second" );
            }

            // Desktop only.
            var lStatusTag = e.Row.FindControl( "lStatusTag" ) as Literal;
            if ( lStatusTag != null )
            {
                lStatusTag.Text = string.Format( @"<span class=""badge badge-{0}"">{1}</span>", statusClass, attendee.StatusString );
            }
        }

        /// <summary>
        /// Gets the badges markup.
        /// </summary>
        /// <param name="attendee">The attendee.</param>
        /// <returns></returns>
        private string GetBadges( Attendee attendee, bool isMobile )
        {
            var badgesSb = new StringBuilder();

            if ( attendee.IsBirthdayWeek )
            {
                if ( isMobile )
                {
                    badgesSb.Append( @"&nbsp;<i class=""fa fa-birthday-cake text-success""></i>" );
                }
                else
                {
                    badgesSb.AppendFormat( @"<div class=""text-center text-success pull-left""><div><i class=""fa fa-birthday-cake fa-2x""></i></div><div style=""font-size: small;"">{0}</div></div>", attendee.Birthday );
                }
            }

            var openDiv = isMobile ? string.Empty : @"<div class=""pull-left"">";
            var closeDiv = isMobile ? string.Empty : "</div>";
            var fa2x = isMobile ? string.Empty : " fa-2x";

            if ( attendee.HasHealthNote )
            {
                badgesSb.AppendFormat( @"{0}&nbsp;<i class=""fa fa-notes-medical{1} text-danger""></i>{2}", openDiv, fa2x, closeDiv );
            }

            if ( attendee.HasLegalNote )
            {
                badgesSb.AppendFormat( @"{0}&nbsp;<i class=""fa fa-clipboard{1}""></i>{2}", openDiv, fa2x, closeDiv );
            }

            return badgesSb.ToString();
        }

        /// <summary>
        /// Handles the RowSelected event of the gAttendees control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs"/> instance containing the event data.</param>
        protected void gAttendees_RowSelected( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            // the attendance grid's DataKeyNames="PersonGuid,AttendanceIds". So each row is a PersonGuid, with a list of attendanceIds (usually one attendance, but could be more)
            string personGuid = e.RowKeyValues[0].ToString();
            var queryParams = new Dictionary<string, string>
            {
                { PageParameterKey.Person, personGuid }
            };

            // If an Area Guid was passed to the Page, pass it along so it can be passed back.
            string areaGuid = PageParameter( PageParameterKey.Area );
            if ( areaGuid.IsNotNullOrWhiteSpace() )
            {
                queryParams.Add( PageParameterKey.Area, areaGuid );
            }

            if ( !NavigateToLinkedPage( AttributeKey.PersonPage, queryParams ) )
            {
                ShowWarningMessage( "The 'Person Page' Block Attribute must be defined.", true );
            }
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs"/> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, RowEventArgs e )
        {
            // the attendance grid's DataKeyNames="PersonGuid,AttendanceIds". So each row is a PersonGuid, with a list of attendanceIds (usually one attendance, but could be more)
            var personGuid = ( Guid ) e.RowKeyValues[0];
            var person = new PersonService( new RockContext() ).Get( personGuid );
            var attendanceIds = e.RowKeyValues[1] as List<int>;
            if ( !attendanceIds.Any() )
            {
                return;
            }

            using ( var rockContext = new RockContext() )
            {
                var attendanceService = new AttendanceService( rockContext );
                foreach ( var attendance in attendanceService
                    .Queryable()
                    .Where( a => attendanceIds.Contains( a.Id ) ) )
                {
                    attendanceService.Delete( attendance );
                }

                rockContext.SaveChanges();

                // Reset the cache for this Location so the kiosk will show the correct counts.
                Rock.CheckIn.KioskLocationAttendance.Remove( CurrentLocationId );
            }

            ShowAttendees();
        }

        /// <summary>
        /// Handles the Click event of the btnPresent control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void btnPresent_Click( object sender, RowEventArgs e )
        {
            // the attendance grid's DataKeyNames="PersonGuid,AttendanceIds". So each row is a PersonGuid, with a list of attendanceIds (usually one attendance, but could be more)
            var attendanceIds = e.RowKeyValues[1] as List<int>;
            if ( !attendanceIds.Any() )
            {
                return;
            }

            using ( var rockContext = new RockContext() )
            {
                var now = RockDateTime.Now;
                var attendanceService = new AttendanceService( rockContext );
                foreach ( var attendee in attendanceService
                    .Queryable()
                    .Where( a => attendanceIds.Contains( a.Id ) ) )
                {
                    attendee.PresentDateTime = now;
                    attendee.PresentByPersonAliasId = CurrentPersonAliasId;
                }

                rockContext.SaveChanges();
            }

            ShowAttendees();
        }

        /// <summary>
        /// Handles the Click event of the btnCheckOut control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void btnCheckOut_Click( object sender, RowEventArgs e )
        {
            // the attendance grid's DataKeyNames="PersonGuid,AttendanceIds". So each row is a PersonGuid, with a list of attendanceIds (usually one attendance, but could be more)
            var attendanceIds = e.RowKeyValues[1] as List<int>;
            if ( !attendanceIds.Any() )
            {
                return;
            }

            using ( var rockContext = new RockContext() )
            {
                var now = RockDateTime.Now;
                var attendanceService = new AttendanceService( rockContext );
                foreach ( var attendee in attendanceService
                    .Queryable()
                    .Where( a => attendanceIds.Contains( a.Id ) ) )
                {
                    attendee.EndDateTime = now;
                    attendee.CheckedOutByPersonAliasId = CurrentPersonAliasId;
                }

                rockContext.SaveChanges();
            }

            ShowAttendees();
        }

        #endregion Control Events

        #region Internal Methods

        /// <summary>
        /// Builds the roster for the selected campus and location.
        /// </summary>
        private void BuildRoster()
        {
            ResetControlVisibility();

            if ( !SetArea() )
            {
                return;
            }

            CampusCache campus = GetCampusFromContext();
            if ( campus == null )
            {
                ShowWarningMessage( "Please select a Campus.", true );
                return;
            }

            // If the Campus selection has changed, we need to reload the LocationItemPicker with the Locations specific to that Campus.
            if ( campus.Id != CurrentCampusId )
            {
                CurrentCampusId = campus.Id;
                lpLocation.NamedPickerRootLocationId = campus.LocationId.GetValueOrDefault();
            }

            // Check the LocationPicker for the Location ID.
            int locationId = lpLocation.Location != null
                ? lpLocation.Location.Id
                : 0;

            if ( locationId <= 0 )
            {
                // If not defined on the LocationPicker, check first for a LocationId Page parameter.
                locationId = PageParameter( PageParameterKey.LocationId ).AsInteger();

                if ( locationId > 0 )
                {
                    // If the Page parameter was set, make sure it's valid for the selected Campus.
                    if ( !IsLocationWithinCampus( locationId ) )
                    {
                        locationId = 0;
                    }
                }

                if ( locationId > 0 )
                {
                    SaveRosterConfigurationToCookie( CurrentCampusId, locationId );
                }
                else
                {
                    // If still not defined, check for a Block user preference.
                    locationId = GetRosterConfigurationFromCookie().LocationIdFromSelectedCampusId.GetValueOrNull( CurrentCampusId ) ?? 0;

                    if ( locationId <= 0 )
                    {
                        ShowWarningMessage( "Please select a Location.", false );
                        return;
                    }
                }

                SetLocationControl( locationId );
            }

            InitializeSubPageNav( locationId );

            // Check the ButtonGroup for the StatusFilter value.
            StatusFilter statusFilter = GetStatusFilterValueFromControl();
            if ( statusFilter == StatusFilter.Unknown )
            {
                // If not defined on the ButtonGroup, check for a Block user preference.
                statusFilter = GetRosterConfigurationFromCookie().StatusFilter;

                if ( statusFilter == StatusFilter.Unknown )
                {
                    // If we still don't know the value, set it to 'All'.
                    statusFilter = StatusFilter.All;
                }

                SetStatusFilterControl( statusFilter );
            }

            // If the Location or StatusFilter selections have changed, we need to reload the attendees.
            if ( locationId != CurrentLocationId || statusFilter != CurrentStatusFilter )
            {
                CurrentLocationId = locationId;
                CurrentStatusFilter = statusFilter;

                ShowAttendees();
            }
        }

        /// <summary>
        /// Resets control visibility to default values.
        /// </summary>
        private void ResetControlVisibility()
        {
            nbWarning.Visible = false;
            lpLocation.Visible = true;
            pnlSubPageNav.Visible = true;
            pnlRoster.Visible = true;
        }

        /// <summary>
        /// Sets the area.
        /// </summary>
        private bool SetArea()
        {
            if ( CurrentAreaGuid.HasValue )
            {
                // We have already set the Area-related properties on initial page load and placed them in ViewState.
                return true;
            }

            // If a query string parameter is defined, it takes precedence.
            Guid? areaGuid = PageParameter( PageParameterKey.Area ).AsGuidOrNull();

            if ( !areaGuid.HasValue )
            {
                // Next, check the Block AttributeValue.
                areaGuid = this.GetAttributeValue( AttributeKey.CheckInAreaGuid ).AsGuidOrNull();
            }

            if ( !areaGuid.HasValue )
            {
                // Finally, fall back to the Area select page.
                if ( !NavigateToLinkedPage( AttributeKey.AreaSelectPage ) )
                {
                    ShowWarningMessage( "The 'Area Select Page' Block Attribute must be defined.", true );
                }

                return false;
            }

            // Save the Area Guid in ViewState.
            CurrentAreaGuid = areaGuid;

            // Get the GroupType represented by the Check-in Area Guid Block Attribute so we can set the related runtime properties.
            using ( var rockContext = new RockContext() )
            {
                GroupType area = new GroupTypeService( rockContext ).Get( areaGuid.Value );
                if ( area == null )
                {
                    ShowWarningMessage( "The specified Check-in Area is not valid.", true );
                    return false;
                }

                area.LoadAttributes( rockContext );

                // Save the following Area-related values in ViewState.
                EnablePresence = area.GetAttributeValue( EntityAttributeValueKey.GroupType_EnablePresence ).AsBoolean();
                AllowCheckout = area.GetAttributeValue( EntityAttributeValueKey.GroupType_AllowCheckout ).AsBoolean();
            }

            return true;
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
            pnlRoster.Visible = false;
        }

        /// <summary>
        /// Determines whether the specified location is within the current campus.
        /// </summary>
        /// <param name="locationId">The location identifier.</param>
        private bool IsLocationWithinCampus( int locationId )
        {
            using ( var rockContext = new RockContext() )
            {
                var locationCampusId = new LocationService( rockContext ).GetCampusIdForLocation( locationId );
                return locationCampusId == CurrentCampusId;
            }
        }

        /// <summary>
        /// Sets the value of the lpLocation control.
        /// </summary>
        /// <param name="locationId">The identifier of the location.</param>
        private void SetLocationControl( int locationId )
        {
            using ( var rockContext = new RockContext() )
            {
                Location location = new LocationService( rockContext ).Get( locationId );
                if ( location != null )
                {
                    lpLocation.Location = location;
                }
            }
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
        /// Gets the status filter value from the bgStatus control.
        /// </summary>
        /// <returns></returns>
        private StatusFilter GetStatusFilterValueFromControl()
        {
            StatusFilter statusFilter = bgStatus.SelectedValue.ConvertToEnumOrNull<StatusFilter>() ?? StatusFilter.Unknown;
            return statusFilter;
        }

        /// <summary>
        /// Sets the value of the bgStatus control.
        /// </summary>
        /// <param name="statusFilter">The status filter.</param>
        private void SetStatusFilterControl( StatusFilter statusFilter )
        {
            bgStatus.SelectedValue = statusFilter.ToString( "d" );
        }

        /// <summary>
        /// Shows the attendees.
        /// </summary>
        private void ShowAttendees()
        {
            IList<Attendee> attendees = null;

            using ( var rockContext = new RockContext() )
            {
                RemoveDisabledStatusFilters();

                attendees = GetAttendees( rockContext );
            }

            ToggleColumnVisibility();

            var attendeesSorted = attendees.OrderByDescending( a => a.Status == AttendeeStatus.Present ).ThenByDescending( a => a.CheckInTime ).ThenBy( a => a.PersonGuid ).ToList();

            gAttendees.DataSource = attendeesSorted;
            gAttendees.DataBind();
        }

        /// <summary>
        /// Removes the disabled status filters.
        /// </summary>
        private void RemoveDisabledStatusFilters()
        {
            // Reset the visibility, just in case the control was previously hidden.
            bgStatus.Visible = true;

            if ( !EnablePresence )
            {
                // When EnablePresence is false for a given Check-in Area, the [Attendance].[PresentDateTime] value will have already been set upon check-in.

                if ( !AllowCheckout )
                {
                    // When both EnablePresence and AllowCheckout are false, it doesn't make sense to show the status filters at all.
                    bgStatus.Visible = false;
                    CurrentStatusFilter = StatusFilter.Present;

                    return;
                }

                // If EnablePresence is false, it doesn't make sense to show the 'Checked-in' filter.
                var checkedInItem = bgStatus.Items.FindByValue( StatusFilter.CheckedIn.ToString( "d" ) );
                if ( checkedInItem != null )
                {
                    bgStatus.Items.Remove( checkedInItem );
                }

                if ( CurrentStatusFilter == StatusFilter.CheckedIn )
                {
                    CurrentStatusFilter = StatusFilter.Present;
                    SetStatusFilterControl( CurrentStatusFilter );
                }
            }
        }

        /// <summary>
        /// Gets the attendees.
        /// </summary>
        private IList<Attendee> GetAttendees( RockContext rockContext )
        {
            var attendees = new List<Attendee>();

            var startDateTime = RockDateTime.Today;
            var now = GetCampusTime();

            // Get all Attendance records for the current day and location.
            var attendanceQuery = new AttendanceService( rockContext )
                .Queryable( "AttendanceCode,PersonAlias.Person,Occurrence.Schedule" )
                .AsNoTracking()
                .Where( a => a.StartDateTime >= startDateTime &&
                             a.StartDateTime <= now &&
                             a.PersonAliasId.HasValue &&
                             a.Occurrence.LocationId == CurrentLocationId &&
                             a.Occurrence.ScheduleId.HasValue );

            /*
                If StatusFilter == All, no further filtering is needed.
                If StatusFilter == Checked-in, only retrieve records that have neither a EndDateTime nor a PresentDateTime value.
                If StatusFilter == Present, only retrieve records that have a PresentDateTime value but don't have a EndDateTime value.
            */

            if ( CurrentStatusFilter == StatusFilter.CheckedIn )
            {
                attendanceQuery = attendanceQuery
                    .Where( a => !a.PresentDateTime.HasValue &&
                                 !a.EndDateTime.HasValue );
            }
            else if ( CurrentStatusFilter == StatusFilter.Present )
            {
                attendanceQuery = attendanceQuery
                    .Where( a => a.PresentDateTime.HasValue &&
                                 !a.EndDateTime.HasValue );
            }

            List<Attendance> attendances = attendanceQuery.ToList();

            /*
                If AllowCheckout is false, remove all Attendees whose schedules are not currently active. Per the 'WasSchedule...ActiveForCheckOut()'
                method below: "Check-out can happen while check-in is active or until the event ends (start time + duration)." This will help to keep
                the list of 'Present' attendees cleaned up and accurate, based on the room schedules, since the volunteers have no way to manually mark
                an Attendee as 'Checked-out'.

                If, on the other hand, AllowCheckout is true, it will be the volunteers' responsibility to click the [Check-out] button when an
                Attendee leaves the room, in order to keep the list of 'Present' Attendees in order. This will also allow the volunteers to continue
                'Checking-out' Attendees in the case that the parents are running late in picking them up.
            */
            if ( !AllowCheckout )
            {
                attendances = attendances.Where( a => a.Occurrence.Schedule.WasScheduleOrCheckInActiveForCheckOut( now ) ).ToList();
            }

            foreach ( var attendance in attendances.Where( a => a.PersonAlias != null && a.PersonAlias.Person != null ) )
            {
                // Create an Attendee for each unique Person within the Attendance records.
                var person = attendance.PersonAlias.Person;

                Attendee attendee = attendees.FirstOrDefault( a => a.PersonGuid == person.Guid );
                if ( attendee == null )
                {
                    attendee = CreateAttendee( rockContext, person );
                    attendees.Add( attendee );
                }

                // Add the attendance-specific property values.
                SetAttendanceInfo( attendance, attendee );
            }

            return attendees;
        }

        /// <summary>
        /// Gets the current campus time.
        /// </summary>
        private DateTime GetCampusTime()
        {
            CampusCache campusCache = CampusCache.Get( CurrentCampusId );
            return campusCache != null
                ? campusCache.CurrentDateTime
                : RockDateTime.Now;
        }

        /// <summary>
        /// Creates an attendee.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="person">The person.</param>
        private Attendee CreateAttendee( RockContext rockContext, Rock.Model.Person person )
        {
            person.LoadAttributes( rockContext );

            var attendee = new Attendee
            {
                PersonId = person.Id,
                PersonGuid = person.Guid,
                Name = person.FullName,
                PhotoId = person.PhotoId,
                Age = person.Age,
                Gender = person.Gender,
                Birthday = GetBirthday( person ),
                HasHealthNote = GetHasHealthNote( person ),
                HasLegalNote = GetHasLegalNote( person )
            };

            if ( person.AgeClassification != AgeClassification.Adult )
            {
                attendee.ParentNames = Rock.Model.Person.GetFamilySalutation( person, finalSeparator: "and" );
            }

            return attendee;
        }

        /// <summary>
        /// Gets the birthday (abbreviated day of week).
        /// </summary>
        /// <param name="person">The person.</param>
        private string GetBirthday( Rock.Model.Person person )
        {
            // If this Person's birthday is today, simply return "Today".
            int daysToBirthday = person.DaysToBirthday;
            if ( daysToBirthday == 0 )
            {
                return "Today";
            }

            // Otherwise, if their birthday falls within the next 6 days, return the abbreviated day of the week (Mon-Sun) on which their birthday falls.
            if ( daysToBirthday < 7 )
            {
                return person.BirthdayDayOfWeekShort;
            }

            return null;
        }

        /// <summary>
        /// Gets whether the person has a health note.
        /// </summary>
        /// <param name="person">The person.</param>
        private bool GetHasHealthNote( Rock.Model.Person person )
        {
            string attributeValue = person.GetAttributeValue( EntityAttributeValueKey.Person_Allergy );
            return attributeValue.IsNotNullOrWhiteSpace();
        }

        /// <summary>
        /// Gets whether the person has a legal note.
        /// </summary>
        /// <param name="person">The person.</param>
        private bool GetHasLegalNote( Rock.Model.Person person )
        {
            string attributeValue = person.GetAttributeValue( EntityAttributeValueKey.Person_LegalNotes );
            return attributeValue.IsNotNullOrWhiteSpace();
        }

        /// <summary>
        /// Sets the attendance-specific properties.
        /// </summary>
        /// <param name="attendance">The attendance.</param>
        /// <param name="attendee">The attendee.</param>
        private void SetAttendanceInfo( Attendance attendance, Attendee attendee )
        {
            // Keep track of each Attendance ID tied to this Attendee so we can manage them all as a group.
            attendee.AttendanceIds.Add( attendance.Id );

            // Tag(s).
            string tag = attendance.AttendanceCode != null
                ? attendance.AttendanceCode.Code
                : null;

            if ( tag.IsNotNullOrWhiteSpace() && !attendee.UniqueTags.Contains( tag, StringComparer.OrdinalIgnoreCase ) )
            {
                attendee.UniqueTags.Add( tag );
            }

            // Service Time(s).
            string serviceTime = attendance.Occurrence != null && attendance.Occurrence.Schedule != null
                ? attendance.Occurrence.Schedule.Name
                : null;

            if ( serviceTime.IsNotNullOrWhiteSpace() && !attendee.UniqueServiceTimes.Contains( serviceTime, StringComparer.OrdinalIgnoreCase ) )
            {
                attendee.UniqueServiceTimes.Add( serviceTime );
            }

            // Status: if this Attendee has multiple AttendanceOccurrences, the highest AttendeeStatus value among them wins.
            AttendeeStatus attendeeStatus = AttendeeStatus.CheckedIn;
            if ( attendance.EndDateTime.HasValue )
            {
                attendeeStatus = AttendeeStatus.CheckedOut;
            }
            else if ( attendance.PresentDateTime.HasValue )
            {
                attendeeStatus = AttendeeStatus.Present;
            }

            if ( attendeeStatus > attendee.Status )
            {
                attendee.Status = attendeeStatus;
            }

            // Check-in Time: if this Attendee has multiple AttendanceOccurrences, the latest StartDateTime value among them wins.
            if ( attendance.StartDateTime > attendee.CheckInTime )
            {
                attendee.CheckInTime = attendance.StartDateTime;
            }
        }

        /// <summary>
        /// Toggles the column visibility within the gAttendees grid.
        /// </summary>
        private void ToggleColumnVisibility()
        {
            // StatusFilter.All:
            var mobileIcon = gAttendees.ColumnsOfType<RockLiteralField>().First( c => c.ID == "lMobileIcon" );
            var serviceTimes = gAttendees.ColumnsOfType<RockBoundField>().First( c => c.DataField == "ServiceTimes" );
            var statusTag = gAttendees.ColumnsOfType<RockLiteralField>().First( c => c.ID == "lStatusTag" );

            // The Cancel button is Visible in two different cases
            //  1) When Presence is not enabled (and they are checked in)
            //  2) When Presence is enabled, and they are not marked present yet
            var btnCancel = gAttendees.ColumnsOfType<LinkButtonField>().First( c => c.ID == "btnCancel" );

            // StatusFilter.Checked-in:
            var lCheckInTime = gAttendees.ColumnsOfType<RockLiteralField>().First( c => c.ID == "lCheckInTime" );
            var btnPresent = gAttendees.ColumnsOfType<LinkButtonField>().First( c => c.ID == "btnPresent" );

            // StatusFilter.Present:
            var btnCheckOut = gAttendees.ColumnsOfType<LinkButtonField>().First( c => c.ID == "btnCheckOut" );

            switch ( CurrentStatusFilter )
            {
                case StatusFilter.All:
                    mobileIcon.Visible = true;
                    serviceTimes.Visible = true;
                    statusTag.Visible = true;

                    lCheckInTime.Visible = false;

                    // only show these action buttons if they are on the CheckedIn or Present Tabs
                    btnCancel.Visible = false;
                    btnPresent.Visible = false;
                    btnCheckOut.Visible = false;

                    break;
                case StatusFilter.CheckedIn:
                    mobileIcon.Visible = false;
                    serviceTimes.Visible = false;
                    statusTag.Visible = false;

                    lCheckInTime.Visible = true;

                    // We are on the CheckedIn Tab (which is people that haven't been marked present yet)
                    // so show Cancel and Present buttons
                    btnCancel.Visible = true;
                    btnPresent.Visible = true;

                    // since that haven't been marked Present yet, they shouldn't have the option to check out (to be marked as not present)
                    btnCheckOut.Visible = false;

                    break;
                case StatusFilter.Present:
                    mobileIcon.Visible = false;
                    serviceTimes.Visible = true;
                    statusTag.Visible = false;

                    lCheckInTime.Visible = false;
                    if ( EnablePresence )
                    {
                        // if Presence is enabled, they were marked as present by a human, so it don't show the Cancel option (since the attendee is physically there) 
                        btnCancel.Visible = false;
                    }
                    else
                    {
                        // if Presence is not enabled, the attendance records can be canceled (deleted) regardless 
                        btnCancel.Visible = true;
                    }

                    // don't show the Present button while on the Present tab, because they have already been marked as present
                    btnPresent.Visible = false;

                    btnCheckOut.Visible = AllowCheckout;

                    break;
            }
        }

        protected void SaveRosterConfigurationToCookie( int campusId, int? locationId )
        {
            SaveRosterConfigurationToCookie( campusId, locationId, null );
        }

        protected void SaveRosterConfigurationToCookie( StatusFilter statusFilter )
        {
            SaveRosterConfigurationToCookie( null, null, statusFilter );
        }

        /// <summary>
        /// Saves the roster configuration to a cookie.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="locationId">The location identifier.</param>
        /// <param name="statusFilter">The status filter.</param>
        protected void SaveRosterConfigurationToCookie( int? campusId, int? locationId, StatusFilter? statusFilter )
        {
            CheckinManagerRosterConfiguration checkinManagerRosterConfiguration = GetRosterConfigurationFromCookie();
            if ( campusId.HasValue )
            {
                if ( locationId.HasValue )
                {
                    checkinManagerRosterConfiguration.LocationIdFromSelectedCampusId.AddOrReplace( campusId.Value, locationId.Value );
                }
                else
                {
                    checkinManagerRosterConfiguration.LocationIdFromSelectedCampusId.Remove( campusId.Value );
                }

            }

            if ( statusFilter.HasValue )
            {
                checkinManagerRosterConfiguration.StatusFilter = statusFilter.Value;
            }

            var checkinManagerRosterConfigurationJson = checkinManagerRosterConfiguration.ToJson( Newtonsoft.Json.Formatting.None );
            Rock.Web.UI.RockPage.AddOrUpdateCookie( CheckInCookieKey.CheckinManagerRosterConfiguration, checkinManagerRosterConfigurationJson, RockDateTime.Now.AddYears( 1 ) );
        }

        /// <summary>
        /// Gets the roster configuration from cookie.
        /// </summary>
        /// <returns></returns>
        protected CheckinManagerRosterConfiguration GetRosterConfigurationFromCookie()
        {
            CheckinManagerRosterConfiguration checkinManagerRosterConfiguration = null;
            var checkinManagerRosterConfigurationCookie = this.Page.Request.Cookies[CheckInCookieKey.CheckinManagerRosterConfiguration];
            if ( checkinManagerRosterConfigurationCookie != null )
            {
                checkinManagerRosterConfiguration = checkinManagerRosterConfigurationCookie.Value.FromJsonOrNull<CheckinManagerRosterConfiguration>();
            }

            if ( checkinManagerRosterConfiguration == null )
            {
                checkinManagerRosterConfiguration = new CheckinManagerRosterConfiguration();
            }

            if ( checkinManagerRosterConfiguration.LocationIdFromSelectedCampusId == null )
            {
                checkinManagerRosterConfiguration.LocationIdFromSelectedCampusId = new Dictionary<int, int>();
            }


            return checkinManagerRosterConfiguration;
        }

        #endregion Internal Methods

        #region Helper Classes

        /// <summary>
        /// A class to represent an attendee.
        /// </summary>
        protected class Attendee
        {
            public int PersonId { get; set; }

            public Guid PersonGuid { get; set; }

            private readonly List<int> _attendanceIds = new List<int>();

            public List<int> AttendanceIds
            {
                get
                {
                    return _attendanceIds;
                }
            }

            public string Name { get; set; }

            public string ParentNames { get; set; }

            public int? PhotoId { get; set; }

            public int? Age { get; set; }

            public Gender Gender { get; set; }

            public string Birthday { get; set; }

            public bool IsBirthdayWeek
            {
                get
                {
                    return Birthday.IsNotNullOrWhiteSpace();
                }
            }

            public bool HasHealthNote { get; set; }

            public bool HasLegalNote { get; set; }

            private readonly List<string> _uniqueTags = new List<string>();

            public List<string> UniqueTags
            {
                get
                {
                    return _uniqueTags;
                }
            }

            public string Tag
            {
                get
                {
                    return string.Join( ", ", UniqueTags );
                }
            }

            private readonly List<string> _uniqueServiceTimes = new List<string>();

            public List<string> UniqueServiceTimes
            {
                get
                {
                    return _uniqueServiceTimes;
                }
            }

            public string ServiceTimes
            {
                get
                {
                    return string.Join( ", ", UniqueServiceTimes );
                }
            }

            public AttendeeStatus Status { get; set; }

            public string StatusString
            {
                get
                {
                    return Status.GetDescription();
                }
            }

            public DateTime CheckInTime { get; set; }
        }

        /// <summary>
        /// The status of an attendee.
        /// </summary>
        protected enum AttendeeStatus
        {
            [Description( "Checked-in" )]
            CheckedIn = 1,

            [Description( "Present" )]
            Present = 2,

            [Description( "Checked-out" )]
            CheckedOut = 3
        }

        /// <summary>
        /// The status filter to be applied to attendees displayed.
        /// </summary>
        public enum StatusFilter
        {
            /// <summary>
            /// Status filter not set to anything yet
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// Don't filter
            /// </summary>
            All = 1,

            /// <summary>
            /// Only show attendees that are checked-in, but haven't been marked present
            /// </summary>
            CheckedIn = 2,


            /// <summary>
            /// Only show attendees are the marked present.
            /// Note that if Presence is NOT enabled, the attendance records will automatically marked as Present.
            /// So this would be the default filter mode when Presence is not enabled
            /// </summary>
            Present = 3
        }

        protected class CheckinManagerRosterConfiguration
        {
            public Dictionary<int, int> LocationIdFromSelectedCampusId { get; set; }
            public StatusFilter StatusFilter { get; set; }
        }

        #endregion Helper Classes
    }
}