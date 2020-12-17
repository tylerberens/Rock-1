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
using System.Web;
using Rock.Model;
using Rock.Web.UI;

namespace Rock.CheckIn
{
    /// <summary>
    /// 
    /// </summary>
    public static class CheckinManagerHelper
    {
        /// <summary>
        /// Saves the campus location configuration to the response cookie
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="locationId">The location identifier.</param>
        public static void SaveCampusLocationConfigurationToCookie( int campusId, int? locationId )
        {
            CheckinManagerConfiguration checkinManagerConfiguration = GetCheckinManagerConfigurationFromCookie();

            if ( locationId.HasValue )
            {
                checkinManagerConfiguration.LocationIdFromSelectedCampusId.AddOrReplace( campusId, locationId.Value );
            }
            else
            {
                checkinManagerConfiguration.LocationIdFromSelectedCampusId.Remove( campusId );
            }

            SaveCheckinManagerConfigurationToCookie( checkinManagerConfiguration );
        }

        /// <summary>
        /// Saves the roster configuration to the response cookie.
        /// </summary>
        /// <param name="rosterStatusFilter">The roster status filter.</param>
        public static void SaveRosterConfigurationToCookie( RosterStatusFilter rosterStatusFilter )
        {
            CheckinManagerConfiguration checkinManagerConfiguration = GetCheckinManagerConfigurationFromCookie();
            checkinManagerConfiguration.RosterStatusFilter = rosterStatusFilter;
            SaveCheckinManagerConfigurationToCookie( checkinManagerConfiguration );
        }

        /// <summary>
        /// Saves the selected checkin area unique identifier to the response cookie
        /// </summary>
        /// <returns></returns>
        public static void SaveSelectedCheckinAreaGuidToCookie( Guid? checkinAreaGuid )
        {
            CheckinManagerConfiguration checkinManagerConfiguration = GetCheckinManagerConfigurationFromCookie();
            checkinManagerConfiguration.CheckinAreaGuid = checkinAreaGuid;
            SaveCheckinManagerConfigurationToCookie( checkinManagerConfiguration );
        }

        /// <summary>
        /// Saves the room list filter to the response cookie
        /// </summary>
        /// <param name="roomListScheduleIdsFilter">The room list schedule ids filter.</param>
        public static void SaveRoomListFilterToCookie( int[] roomListScheduleIdsFilter )
        {
            CheckinManagerConfiguration checkinManagerConfiguration = GetCheckinManagerConfigurationFromCookie();
            checkinManagerConfiguration.RoomListScheduleIdsFilter = roomListScheduleIdsFilter;
            SaveCheckinManagerConfigurationToCookie( checkinManagerConfiguration );
        }

        /// <summary>
        /// Saves the checkin manager configuration to response cookie.
        /// </summary>
        /// <param name="checkinManagerConfiguration">The checkin manager configuration.</param>
        private static void SaveCheckinManagerConfigurationToCookie( CheckinManagerConfiguration checkinManagerConfiguration )
        {
            var checkinManagerConfigurationJson = checkinManagerConfiguration.ToJson( Newtonsoft.Json.Formatting.None );
            Rock.Web.UI.RockPage.AddOrUpdateCookie( CheckInManagerCookieKey.CheckinManagerConfiguration, checkinManagerConfigurationJson, RockDateTime.Now.AddYears( 1 ) );
        }

        /// <summary>
        /// Gets the roster configuration from the request cookie.
        /// NOTE: This is the value from the Browser. It doesn't have the changes that were saved to the response cookie during this request.
        /// Always returns a non-null CheckinManagerConfiguration.
        /// </summary>
        /// <returns></returns>
        public static CheckinManagerConfiguration GetCheckinManagerConfigurationFromCookie()
        {
            CheckinManagerConfiguration checkinManagerConfiguration = null;
            var checkinManagerRosterConfigurationCookie = HttpContext.Current.Request.Cookies[CheckInManagerCookieKey.CheckinManagerConfiguration];
            if ( checkinManagerRosterConfigurationCookie != null )
            {
                checkinManagerConfiguration = checkinManagerRosterConfigurationCookie.Value.FromJsonOrNull<CheckinManagerConfiguration>();
            }

            if ( checkinManagerConfiguration == null )
            {
                checkinManagerConfiguration = new CheckinManagerConfiguration();
            }

            if ( checkinManagerConfiguration.LocationIdFromSelectedCampusId == null )
            {
                checkinManagerConfiguration.LocationIdFromSelectedCampusId = new Dictionary<int, int>();
            }

            if ( checkinManagerConfiguration.RoomListScheduleIdsFilter == null )
            {
                checkinManagerConfiguration.RoomListScheduleIdsFilter = new int[0];
            }

            return checkinManagerConfiguration;
        }

        /// <summary>
        /// Filters an IQueryable of Attendance by the specified roster status filter.
        /// </summary>
        /// <param name="attendanceQuery">The attendance query.</param>
        /// <param name="rosterStatusFilter">The roster status filter.</param>
        /// <returns></returns>
        public static IQueryable<Attendance> FilterByRosterStatusFilter( IQueryable<Attendance> attendanceQuery, RosterStatusFilter rosterStatusFilter )
        {
            /*
                If StatusFilter == All, no further filtering is needed.
                If StatusFilter == Checked-in, only retrieve records that have neither a EndDateTime nor a PresentDateTime value.
                If StatusFilter == Present, only retrieve records that have a PresentDateTime value but don't have a EndDateTime value.
                If StatusFilter == Checked-Out, only retrieve records that have an EndDateTime
            */
            switch ( rosterStatusFilter )
            {
                case RosterStatusFilter.CheckedIn:
                    attendanceQuery = attendanceQuery.Where( a => !a.PresentDateTime.HasValue && !a.EndDateTime.HasValue );
                    break;
                case RosterStatusFilter.Present:
                    attendanceQuery = attendanceQuery.Where( a => a.PresentDateTime.HasValue && !a.EndDateTime.HasValue );
                    break;
                case RosterStatusFilter.CheckedOut:
                    attendanceQuery = attendanceQuery.Where( a => a.EndDateTime.HasValue );
                    break;
                default:
                    break;
            }

            return attendanceQuery;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class CheckInManagerCookieKey
    {
        /// <summary>
        /// The checkin manager roster configuration
        /// </summary>
        public static readonly string CheckinManagerConfiguration = "CheckinManager.CheckinManagerConfiguration";
    }

    /// <summary>
    /// The status filter to be applied to attendees displayed.
    /// </summary>
    public enum RosterStatusFilter
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
        Present = 3,

        /// <summary>
        /// Only show attendees that are checked-out.
        /// </summary>
        CheckedOut = 4
    }

    /// <summary>
    /// 
    /// </summary>
    public class CheckinManagerConfiguration
    {
        /// <summary>
        /// Gets or sets the location identifier from selected campus identifier.
        /// </summary>
        /// <value>
        /// The location identifier from selected campus identifier.
        /// </value>
        public Dictionary<int, int> LocationIdFromSelectedCampusId { get; set; }

        /// <summary>
        /// Gets or sets the roster status filter.
        /// </summary>
        /// <value>
        /// The roster status filter.
        /// </value>
        public RosterStatusFilter RosterStatusFilter { get; set; }

        /// <summary>
        /// Gets or sets the checkin area unique identifier.
        /// </summary>
        /// <value>
        /// The checkin area unique identifier.
        /// </value>
        public Guid? CheckinAreaGuid { get; set; }

        /// <summary>
        /// Gets or sets the room list schedule ids filter.
        /// </summary>
        /// <value>
        /// The room list schedule ids filter.
        /// </value>
        public int[] RoomListScheduleIdsFilter { get; set; }
    }
}