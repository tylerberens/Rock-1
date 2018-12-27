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

            var personAttributes = new AttributeService( rockContext ).GetByEntityTypeId( new Person().TypeId );
            foreach ( var attribute in personAttributes )
            {
                foreach ( var category in attribute.Categories )
                {
                    var importAttribute = new Slingshot.Core.Model.PersonAttribute()
                    {
                        Key = attribute.Key,
                        FieldType = attribute.FieldType.Name,
                        Name = attribute.Name,
                        Category = category.Name
                    };
                    ImportPackage.WriteToPackage<Slingshot.Core.Model.PersonAttribute>( importAttribute );
                }
            }

            var familyAttributes = new AttributeService( rockContext ).Get( new Group().TypeId, "GroupTypeId", GroupTypeCache.GetFamilyGroupType().Id.ToString() );
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

            var persons = new PersonService( rockContext ).GetByIds( dataViewPersonIds );
            foreach ( var person in persons )
            {
                var importPerson = new Slingshot.Core.Model.Person()
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
                            importPerson.MaritalStatus = Slingshot.Core.Model.MaritalStatus.Single;
                            break;
                        case Rock.SystemGuid.DefinedValue.PERSON_MARITAL_STATUS_MARRIED:
                            importPerson.MaritalStatus = Slingshot.Core.Model.MaritalStatus.Married;
                            break;
                        case Rock.SystemGuid.DefinedValue.PERSON_MARITAL_STATUS_DIVORCED:
                            importPerson.MaritalStatus = Slingshot.Core.Model.MaritalStatus.Divorced;
                            break;
                        default:
                            importPerson.MaritalStatus = Slingshot.Core.Model.MaritalStatus.Unknown;
                            break;
                    }
                }

                switch ( person.EmailPreference )
                {
                    case EmailPreference.EmailAllowed:
                        importPerson.EmailPreference = Slingshot.Core.Model.EmailPreference.EmailAllowed;
                        break;
                    case EmailPreference.NoMassEmails:
                        importPerson.EmailPreference = Slingshot.Core.Model.EmailPreference.NoMassEmails;
                        break;
                    case EmailPreference.DoNotEmail:
                    default:
                        importPerson.EmailPreference = Slingshot.Core.Model.EmailPreference.DoNotEmail;
                        break;
                }


                var campus = person.GetCampus();
                if ( campus != null )
                {
                    importPerson.Campus = new Slingshot.Core.Model.Campus()
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
                            importPerson.RecordStatus = Slingshot.Core.Model.RecordStatus.Active;
                            break;
                        case Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING:
                            importPerson.RecordStatus = Slingshot.Core.Model.RecordStatus.Pending;
                            break;
                        case Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE:
                        default:
                            importPerson.RecordStatus = Slingshot.Core.Model.RecordStatus.Inactive;
                            break;
                    }
                }

                person.LoadAttributes( rockContext );
                importPerson.Attributes = person.AttributeValues.Where( a => !string.IsNullOrEmpty( a.Value.Value ) ).Select( a => new Slingshot.Core.Model.PersonAttributeValue()
                {
                    AttributeKey = a.Key,
                    AttributeValue = a.Value.Value,
                    PersonId = person.Id
                } ).ToList();

                importPerson.PhoneNumbers = person.PhoneNumbers.Select( a => new Slingshot.Core.Model.PersonPhone()
                {
                    IsMessagingEnabled = a.IsMessagingEnabled,
                    IsUnlisted = a.IsUnlisted,
                    PersonId = person.Id,
                    PhoneNumber = a.Number,
                    PhoneType = a.NumberTypeValue.Value
                } ).ToList();


                foreach ( var family in person.GetFamilies( rockContext ) )
                {
                    importPerson.FamilyId = family.Id;
                    importPerson.FamilyName = family.Name;
                    var familyRole = person.GetFamilyRole( rockContext );
                    if ( familyRole != null )
                    {
                        if ( familyRole.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() )
                        {
                            importPerson.FamilyRole = Slingshot.Core.Model.FamilyRole.Child;
                        }
                        else
                        {
                            importPerson.FamilyRole = Slingshot.Core.Model.FamilyRole.Adult;
                        }
                    }
                    else
                    {
                        importPerson.FamilyRole = Slingshot.Core.Model.FamilyRole.Adult;
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

                    ImportPackage.WriteToPackage<Slingshot.Core.Model.Person>( importPerson );
                }
            }

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

        #endregion
    }
}