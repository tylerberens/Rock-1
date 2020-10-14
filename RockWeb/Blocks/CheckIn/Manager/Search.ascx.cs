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
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Web.Cache;

namespace RockWeb.Blocks.CheckIn.Manager
{
    /// <summary>
    /// Block used to search current check-in
    /// </summary>
    [DisplayName( "Search" )]
    [Category( "Check-in > Manager" )]
    [Description( "Block used to search current check-in." )]

    #region Block Attributes

    [LinkedPage(
        "Person Page",
        Description = "The page used to display a selected person's details.",
        Order = 0,
        Key = AttributeKey.PersonPage )]

    [BooleanField(
        "Search By Code",
        Description = "A flag indicating if security codes should also be evaluated in the search box results.",
        Order = 1,
        Key = AttributeKey.SearchByCode )]
    #endregion Block Attributes
    public partial class Search : Rock.Web.UI.RockBlock
    {
        #region Attribute Keys

        private static class AttributeKey
        {
            public const string PersonPage = "PersonPage";
            public const string SearchByCode = "SearchByCode";
        }

        #endregion Attribute Keys

        #region Page Parameter Keys

        private class PageParameterKey
        {
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
        }

        #endregion ViewState Keys
        #region Entity Attribute Value Keys

        /// <summary>
        /// Keys to use for entity attribute values.
        /// </summary>
        private class EntityAttributeValueKey
        {
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
                return ( ViewState[ViewStateKey.CurrentCampusId] as string ).AsInteger();
            }

            set
            {
                ViewState[ViewStateKey.CurrentCampusId] = value.ToString();
            }
        }

        #endregion
        #region Base Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );
        }

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

            nbWarning.Visible = false;
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            return base.SaveViewState();
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
        }

        /// <summary>
        /// Handles the TextChanged event of the tbSearch control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void tbSearch_TextChanged( object sender, EventArgs e )
        {
            if ( tbSearch.Text.Length > 2 )
            {
                ShowAttendees();
            }
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
        /// Handles the RowSelected event of the gAttendees control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs"/> instance containing the event data.</param>
        protected void gAttendees_RowSelected( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            string personGuid = e.RowKeyValues[0].ToString();
            var queryParams = new Dictionary<string, string>
            {
                { PageParameterKey.Person, personGuid }
            };

            if ( !NavigateToLinkedPage( AttributeKey.PersonPage, queryParams ) )
            {
                ShowWarningMessage( "The 'Person Page' Block Attribute must be defined.", true );
            }
        }

        #endregion

        #region Internal Methods

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
        /// Builds the roster for the selected campus and location.
        /// </summary>
        private void BuildRoster()
        {
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
            }

            ShowAttendees();
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
        /// Shows the attendees.
        /// </summary>
        private void ShowAttendees()
        {
            IList<Attendee> attendees = null;

            using ( var rockContext = new RockContext() )
            {
                attendees = GetAttendees( rockContext );
            }

            gAttendees.DataSource = attendees;
            gAttendees.DataBind();
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
                             a.Occurrence.ScheduleId.HasValue &&
                             a.PersonAlias != null &&
                             a.PersonAlias.Person != null );

            // Do the person search
            var personService = new PersonService( rockContext );
            List<Rock.Model.Person> people = null;
            bool reversed = false;

            string searchValue = tbSearch.Text.Trim();
            if ( searchValue.IsNullOrWhiteSpace() )
            {
                people = new List<Rock.Model.Person>();
            }
            else
            {
                // If searching by code is enabled, first search by the code
                if ( GetAttributeValue( AttributeKey.SearchByCode ).AsBoolean() )
                {
                    var dayStart = RockDateTime.Today;
                    var personIds = new AttendanceService( rockContext )
                        .Queryable().Where( a =>
                            a.StartDateTime >= dayStart &&
                            a.StartDateTime <= now &&
                            a.AttendanceCode.Code == searchValue )
                        .Select( a => a.PersonAlias.PersonId )
                        .Distinct();
                    people = personService.Queryable()
                        .Where( p => personIds.Contains( p.Id ) )
                        .ToList();
                }

                if ( people == null || !people.Any() )
                {
                    // If searching by code was disabled or nobody was found with code, search by name
                    people = personService
                        .GetByFullName( searchValue, false, false, false, out reversed )
                        .ToList();
                }
            }

            var attendances = people
                    .GroupJoin(
                        attendanceQuery,
                        p => p.Id,
                        a => a.PersonAlias.PersonId,
                        ( p, a ) => a )
                    .SelectMany( a => a )
                    .Distinct()
                    .ToList();

            foreach ( var attendance in attendances )
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
                ParentNames = Rock.Model.Person.GetFamilySalutation( person, finalSeparator: "and" ),
                PhotoId = person.PhotoId,
                Age = person.Age,
                Gender = person.Gender,
                Birthday = GetBirthday( person ),
                HasHealthNote = GetHasHealthNote( person ),
                HasLegalNote = GetHasLegalNote( person )
            };

            return attendee;
        }

        /// <summary>
        /// Gets the birthday (abbreviated day of week).
        /// </summary>
        /// <param name="person">The person.</param>
        private string GetBirthday( Rock.Model.Person person )
        {
            // If this Person's bday is today, simply return "Today".
            int daysToBirthday = person.DaysToBirthday;
            if ( daysToBirthday == 0 )
            {
                return "Today";
            }

            // Otherwise, if their bday falls within the next 6 days, return the abbreviated day of the week (Mon-Sun) on which their bday falls.
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
        /// Shows a warning message, and optionally hides the content panels.
        /// </summary>
        /// <param name="warningMessage">The warning message to show.</param>
        /// <param name="hideLocationPicker">Whether to hide the lpLocation control.</param>
        private void ShowWarningMessage( string warningMessage, bool hideLocationPicker )
        {
            nbWarning.Text = warningMessage;
            nbWarning.Visible = true;
            pnlContent.Visible = false;
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

        #endregion Helper Classes
    }
}