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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Slingshot;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using Slingshot.Core.Utilities;

namespace RockWeb.Blocks.BulkExport
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Bulk Export" )]
    [Category( "Bulk Export" )]
    [Description( "Block to export the data to Slingshot files" )]
    public partial class BulkExportTool : RockBlock
    {
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            dpDataView.EntityTypeId = EntityTypeCache.Get<Person>().Id;
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
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the Click event of the btnExport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnExport_Click( object sender, EventArgs e )
        {
            nbPersonsAlert.Visible = false;
            var dataViewId = dpDataView.SelectedValue.AsInteger();
            var dataViewPersonIds = new List<int>();
            var rockContext = new RockContext();
            var dataView = new DataViewService( rockContext ).Get( dataViewId );
            if ( dataView != null )
            {
                var errorMessages = new List<string>();
                var dvPersonService = new PersonService( rockContext );
                ParameterExpression paramExpression = dvPersonService.ParameterExpression;
                Expression whereExpression = dataView.GetExpression( dvPersonService, paramExpression, out errorMessages );

                SortProperty sort = null;
                var dataViewPersonIdQry = dvPersonService
                    .Queryable().AsNoTracking()
                    .Where( paramExpression, whereExpression, sort )
                    .Select( p => p.Id );
                dataViewPersonIds = dataViewPersonIdQry.ToList();
            }

            if ( !dataViewPersonIds.Any() )
            {
                nbPersonsAlert.Text = "The selected list doesn't have any people. <span>At least one person is required.</span>";
                nbPersonsAlert.Visible = true;
                return;
            }

            ExportFamilyWithPerson( dataViewPersonIds );
            ExportGroup( dataViewPersonIds );

            ImportPackage.FinalizePackage( tbFileName.Text );

            string filename = Server.MapPath( "~/" + tbFileName.Text + ".slingshot" );
            FileInfo fileInfo = new FileInfo( filename );

            if ( fileInfo.Exists )
            {
                System.Web.HttpResponse response = System.Web.HttpContext.Current.Response;
                response.ClearContent();
                response.Clear();
                Response.ContentType = "application/octet-stream";
                response.AddHeader( "Content-Disposition",
                                   "attachment; filename=" + tbFileName.Text + ".slingshot;" );
                response.TransmitFile( fileInfo.FullName );
                response.Flush();
                fileInfo.Delete();
                response.End();
            }
        }

        private RockContext ExportGroup( List<int> dataViewPersonIds )
        {
            RockContext rockContext = new RockContext();
            var familyGroupTypeId = GroupTypeCache.GetFamilyGroupType().Id;
            var groups = new GroupService( rockContext ).Queryable().Where( t => t.Members.Any( a => dataViewPersonIds.Contains( a.PersonId ) ) && t.GroupTypeId != familyGroupTypeId );

            foreach ( var group in groups )
            {
                var importGroup = new Slingshot.Core.Model.Group()
                {
                    CampusId = group.CampusId,
                    Description = group.Description,
                    Name = group.Name,
                    Order = group.Order,
                    Capacity = group.GroupCapacity,
                    IsPublic = group.IsPublic,
                    IsActive = group.IsActive,
                    ParentGroupId = group.ParentGroupId ?? 0,
                    GroupTypeId = group.GroupTypeId,
                    Id = group.Id
                };

                if ( group.Schedule != null )
                {
                    importGroup.MeetingDay = group.Schedule.WeeklyDayOfWeek.ToStringSafe();
                    importGroup.MeetingTime = group.Schedule.WeeklyTimeOfDay.ToStringSafe();
                }

                group.LoadAttributes( rockContext );
                importGroup.Attributes = group.AttributeValues.Where( a => !string.IsNullOrEmpty( a.Value.Value ) ).Select( a => new Slingshot.Core.Model.GroupAttributeValue()
                {
                    AttributeKey = a.Key,
                    AttributeValue = a.Value.Value,
                    GroupId = group.Id
                } ).ToList();

                importGroup.GroupMembers = group.Members
                    .Where( a => dataViewPersonIds.Contains( a.PersonId ) )
                    .Select( a => new Slingshot.Core.Model.GroupMember()
                    {
                        GroupId = group.Id,
                        PersonId = a.PersonId,
                        Role = a.GroupRole.Name
                    } ).ToList();


                foreach ( var groupLocation in group.GroupLocations )
                {
                    var address = new Slingshot.Core.Model.GroupAddress()
                    {
                        City = groupLocation.Location.City,
                        Country = groupLocation.Location.Country,
                        IsMailing = groupLocation.IsMailingLocation,
                        PostalCode = groupLocation.Location.PostalCode,
                        State = groupLocation.Location.State,
                        Street1 = groupLocation.Location.Street1,
                        Street2 = groupLocation.Location.Street2,
                        GroupId = group.Id
                    };

                    if ( groupLocation.Location.GeoPoint != null )
                    {
                        address.Latitude = groupLocation.Location.GeoPoint.Latitude.ToStringSafe();
                        address.Longitude = groupLocation.Location.GeoPoint.Longitude.ToStringSafe();
                    }

                    if ( groupLocation.GroupLocationTypeValueId.HasValue )
                    {
                        switch ( groupLocation.GroupLocationTypeValue.Guid.ToString() )
                        {
                            case Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME:
                                address.AddressType = Slingshot.Core.Model.AddressType.Home;
                                break;
                            case Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_WORK:
                                address.AddressType = Slingshot.Core.Model.AddressType.Work;
                                break;
                            case Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_PREVIOUS:
                                address.AddressType = Slingshot.Core.Model.AddressType.Previous;
                                break;
                            case Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_MEETING_LOCATION:
                            default:
                                address.AddressType = Slingshot.Core.Model.AddressType.Other;
                                break;
                        }
                    }
                    else
                    {
                        address.AddressType = Slingshot.Core.Model.AddressType.Other;
                    }
                }
                ImportPackage.WriteToPackage<Slingshot.Core.Model.Group>( importGroup );
            }

            return rockContext;
        }

        private void ExportFamilyWithPerson( List<int> dataViewPersonIds )
        {
            RockContext rockContext = new RockContext();
            var personAttributes = new AttributeService( rockContext ).GetByEntityTypeId( new Person().TypeId );
            foreach ( var attribute in personAttributes )
            {
                foreach ( var category in attribute.Categories )
                {
                    var exportAttribute = new Slingshot.Core.Model.PersonAttribute()
                    {
                        Key = attribute.Key,
                        FieldType = attribute.FieldType.Name,
                        Name = attribute.Name,
                        Category = category.Name
                    };
                    ImportPackage.WriteToPackage<Slingshot.Core.Model.PersonAttribute>( exportAttribute );
                }
            }

            var familyType = GroupTypeCache.GetFamilyGroupType();
            var familyAttributes = new AttributeService( rockContext ).Get( new Group().TypeId, "GroupTypeId", familyType.Id.ToString() );
            foreach ( var attribute in familyAttributes )
            {
                foreach ( var category in attribute.Categories )
                {
                    var importAttribute = new Slingshot.Core.Model.FamilyAttribute()
                    {
                        Key = attribute.Key,
                        FieldType = attribute.FieldType.Name,
                        Name = attribute.Name,
                        Category = category.Name
                    };
                    ImportPackage.WriteToPackage<Slingshot.Core.Model.FamilyAttribute>( importAttribute );
                }
            }

            var familyEntityTypeId = new Group().TypeId;
            var personEntityTypeId = new Person().TypeId;
            var familyNoteTypeIds = new NoteTypeService( rockContext ).Queryable().Where( a => a.EntityTypeId == familyEntityTypeId ).Select(a=>a.Id);
            var personNoteTypeIds = new NoteTypeService( rockContext ).Queryable().Where( a => a.EntityTypeId == personEntityTypeId ).Select( a => a.Id );

            var persons = new PersonService( rockContext ).GetByIds( dataViewPersonIds );
            foreach ( var person in persons )
            {
                var exportPerson = new Slingshot.Core.Model.Person()
                {
                    Suffix = person.SuffixValueId.HasValue ? person.SuffixValue.Value : string.Empty,
                    Salutation = person.TitleValueId.HasValue ? person.TitleValue.Value : string.Empty,
                    AnniversaryDate = person.AnniversaryDate,
                    Birthdate = person.BirthDate,
                    ConnectionStatus = person.ConnectionStatusValueId.HasValue ? person.ConnectionStatusValue.Value : string.Empty,
                    CreatedDateTime = person.CreatedDateTime,
                    Id = person.Id,
                    InactiveReason = person.InactiveReasonNote,
                    IsDeceased = person.IsDeceased,
                    Email = person.Email,
                    NickName = person.NickName,
                    ModifiedDateTime = person.ModifiedDateTime,
                    FamilyId = person.PrimaryFamilyId,
                    Note = person.SystemNote,
                    MiddleName = person.MiddleName,
                    LastName = person.LastName,
                    FirstName = person.FirstName,
                    Grade = person.GradeFormatted,
                    Gender = ( Slingshot.Core.Model.Gender ) person.Gender,
                    GiveIndividually = !person.GivingGroupId.HasValue
                };

                if ( person.MaritalStatusValueId.HasValue )
                {
                    switch ( person.MaritalStatusValue.Value )
                    {
                        case Rock.SystemGuid.DefinedValue.PERSON_MARITAL_STATUS_SINGLE:
                            exportPerson.MaritalStatus = Slingshot.Core.Model.MaritalStatus.Single;
                            break;
                        case Rock.SystemGuid.DefinedValue.PERSON_MARITAL_STATUS_MARRIED:
                            exportPerson.MaritalStatus = Slingshot.Core.Model.MaritalStatus.Married;
                            break;
                        case Rock.SystemGuid.DefinedValue.PERSON_MARITAL_STATUS_DIVORCED:
                            exportPerson.MaritalStatus = Slingshot.Core.Model.MaritalStatus.Divorced;
                            break;
                        default:
                            exportPerson.MaritalStatus = Slingshot.Core.Model.MaritalStatus.Unknown;
                            break;
                    }
                }

                switch ( person.EmailPreference )
                {
                    case EmailPreference.EmailAllowed:
                        exportPerson.EmailPreference = Slingshot.Core.Model.EmailPreference.EmailAllowed;
                        break;
                    case EmailPreference.NoMassEmails:
                        exportPerson.EmailPreference = Slingshot.Core.Model.EmailPreference.NoMassEmails;
                        break;
                    case EmailPreference.DoNotEmail:
                    default:
                        exportPerson.EmailPreference = Slingshot.Core.Model.EmailPreference.DoNotEmail;
                        break;
                }


                var campus = person.GetCampus();
                if ( campus != null )
                {
                    exportPerson.Campus = new Slingshot.Core.Model.Campus()
                    {
                        CampusId = campus.Id,
                        CampusName = campus.Name
                    };
                }

                if ( person.RecordStatusValueId.HasValue )
                {
                    switch ( person.RecordStatusValue.Guid.ToString() )
                    {
                        case Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE:
                            exportPerson.RecordStatus = Slingshot.Core.Model.RecordStatus.Active;
                            break;
                        case Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING:
                            exportPerson.RecordStatus = Slingshot.Core.Model.RecordStatus.Pending;
                            break;
                        case Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE:
                        default:
                            exportPerson.RecordStatus = Slingshot.Core.Model.RecordStatus.Inactive;
                            break;
                    }
                }

                person.LoadAttributes( rockContext );
                exportPerson.Attributes = person.AttributeValues.Where( a => !string.IsNullOrEmpty( a.Value.Value ) ).Select( a => new Slingshot.Core.Model.PersonAttributeValue()
                {
                    AttributeKey = a.Key,
                    AttributeValue = a.Value.Value,
                    PersonId = person.Id
                } ).ToList();

                exportPerson.PhoneNumbers = person.PhoneNumbers.Select( a => new Slingshot.Core.Model.PersonPhone()
                {
                    IsMessagingEnabled = a.IsMessagingEnabled,
                    IsUnlisted = a.IsUnlisted,
                    PersonId = person.Id,
                    PhoneNumber = a.Number,
                    PhoneType = a.NumberTypeValue.Value
                } ).ToList();


                var notes = new NoteService( rockContext ).Queryable().Where( a => a.EntityId == person.Id && personNoteTypeIds.Contains( a.NoteTypeId ) );
                foreach ( var note in notes )
                {
                    var personNote = new Slingshot.Core.Model.PersonNote()
                    {
                        Id = note.Id,
                        Caption = note.Caption,
                        CreatedByPersonId = note.CreatedByPersonId,
                        DateTime = note.ApprovedDateTime,
                        IsAlert = note.IsAlert ?? false,
                        IsPrivateNote = note.IsPrivateNote,
                        PersonId = person.Id,
                        Text = note.Text,
                        NoteType = note.NoteType.Name
                    };
                    ImportPackage.WriteToPackage<Slingshot.Core.Model.PersonNote>( personNote );
                }

                foreach ( var family in person.GetFamilies( rockContext ) )
                {
                    exportPerson.FamilyId = family.Id;
                    exportPerson.FamilyName = family.Name;
                    var familyRole = person.GetFamilyRole( rockContext );
                    if ( familyRole != null )
                    {
                        if ( familyRole.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() )
                        {
                            exportPerson.FamilyRole = Slingshot.Core.Model.FamilyRole.Child;
                        }
                        else
                        {
                            exportPerson.FamilyRole = Slingshot.Core.Model.FamilyRole.Adult;
                        }
                    }
                    else
                    {
                        exportPerson.FamilyRole = Slingshot.Core.Model.FamilyRole.Adult;
                    }

                    foreach ( var familyLocation in family.GroupLocations )
                    {
                        var address = new Slingshot.Core.Model.PersonAddress()
                        {
                            City = familyLocation.Location.City,
                            Country = familyLocation.Location.Country,
                            IsMailing = familyLocation.IsMailingLocation,
                            PersonId = person.Id,
                            PostalCode = familyLocation.Location.PostalCode,
                            State = familyLocation.Location.State,
                            Street1 = familyLocation.Location.Street1,
                            Street2 = familyLocation.Location.Street2
                        };

                        if ( familyLocation.Location.GeoPoint != null )
                        {
                            address.Latitude = familyLocation.Location.GeoPoint.Latitude.ToStringSafe();
                            address.Longitude = familyLocation.Location.GeoPoint.Longitude.ToStringSafe();
                        }

                        if ( familyLocation.GroupLocationTypeValueId.HasValue )
                        {
                            switch ( familyLocation.GroupLocationTypeValue.Guid.ToString() )
                            {
                                case Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME:
                                    address.AddressType = Slingshot.Core.Model.AddressType.Home;
                                    break;
                                case Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_WORK:
                                    address.AddressType = Slingshot.Core.Model.AddressType.Work;
                                    break;
                                case Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_PREVIOUS:
                                    address.AddressType = Slingshot.Core.Model.AddressType.Previous;
                                    break;
                                case Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_MEETING_LOCATION:
                                default:
                                    address.AddressType = Slingshot.Core.Model.AddressType.Other;
                                    break;
                            }
                        }
                        else
                        {
                            address.AddressType = Slingshot.Core.Model.AddressType.Other;
                        }

                        family.LoadAttributes( rockContext );
                        var familyAttributeValues = person.AttributeValues.Where( a => !string.IsNullOrEmpty( a.Value.Value ) ).Select( a => new Slingshot.Core.Model.GroupAttributeValue()
                        {
                            AttributeKey = a.Key,
                            AttributeValue = a.Value.Value,
                            GroupId = family.Id
                        } ).ToList();
                        foreach ( var familyAttributeValue in familyAttributeValues )
                        {
                            ImportPackage.WriteToPackage<Slingshot.Core.Model.GroupAttributeValue>( familyAttributeValue );
                        }
                    }

                    var familyNotes = new NoteService( rockContext ).Queryable().Where( a => a.EntityId == family.Id && familyNoteTypeIds.Contains( a.NoteTypeId ) );
                    foreach ( var note in familyNotes )
                    {
                        var familyNote = new Slingshot.Core.Model.FamilyNote()
                        {
                            Id = note.Id,
                            Caption = note.Caption,
                            CreatedByPersonId = note.CreatedByPersonId,
                            DateTime = note.ApprovedDateTime,
                            IsAlert = note.IsAlert ?? false,
                            IsPrivateNote = note.IsPrivateNote,
                            Text = note.Text,
                            NoteType = note.NoteType.Name,
                            FamilyId = family.Id
                        };
                        ImportPackage.WriteToPackage<Slingshot.Core.Model.FamilyNote>( familyNote );
                    }

                    ImportPackage.WriteToPackage<Slingshot.Core.Model.Person>( exportPerson );
                }
            }
        }

        #endregion
    }
}