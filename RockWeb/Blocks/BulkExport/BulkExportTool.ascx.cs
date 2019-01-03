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
        #region Fields
        /// <summary>
        /// This holds the reference to the RockMessageHub SignalR Hub context.
        /// </summary>
        private IHubContext _hubContext = GlobalHost.ConnectionManager.GetHubContext<RockMessageHub>();

        /// <summary>
        /// Gets the signal r notification key.
        /// </summary>
        /// <value>
        /// The signal r notification key.
        /// </value>
        public string SignalRNotificationKey
        {
            get
            {
                return string.Format( "BulkExport_BlockId:{0}_SessionId:{1}", this.BlockId, Session.SessionID );
            }
        }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            RockPage.AddScriptLink( "~/Scripts/jquery.signalR-2.2.0.min.js", false );

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
                    .Queryable( true ).AsNoTracking()
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
            ExportFinance( dataViewPersonIds );

            string relativeFilename = "App_Data/" + tbFileName.Text + ".slingshot";

            ImportPackage.FinalizePackage( relativeFilename );

            string filename = Server.MapPath( "~/" + relativeFilename );
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

        private void ExportGroup( List<int> dataViewPersonIds )
        {
            RockContext rockContext = new RockContext();
            var groups = new GroupService( rockContext ).Queryable().AsNoTracking().Where( t => t.Members.Any( a => dataViewPersonIds.Contains( a.PersonId ) ) );

            foreach ( var groupType in groups.Select( a => a.GroupType ).Distinct() )
            {
                var exportGroupType = new Slingshot.Core.Model.GroupType()
                {
                    Id = groupType.Id,
                    Name = groupType.Name
                };
                ImportPackage.WriteToPackage<Slingshot.Core.Model.GroupType>( exportGroupType );
            }

            foreach ( var group in groups )
            {
                var exportGroup = new Slingshot.Core.Model.Group()
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
                    exportGroup.MeetingDay = group.Schedule.WeeklyDayOfWeek.ToStringSafe();
                    exportGroup.MeetingTime = group.Schedule.WeeklyTimeOfDay.ToStringSafe();
                }

                group.LoadAttributes( rockContext );
                exportGroup.Attributes = group.AttributeValues.Where( a => !string.IsNullOrEmpty( a.Value.Value ) ).Select( a => new Slingshot.Core.Model.GroupAttributeValue()
                {
                    AttributeKey = a.Key,
                    AttributeValue = a.Value.Value,
                    GroupId = group.Id
                } ).ToList();

                exportGroup.GroupMembers = group.Members
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
                ImportPackage.WriteToPackage<Slingshot.Core.Model.Group>( exportGroup );
            }
        }

        private void ExportFamilyWithPerson( List<int> dataViewPersonIds )
        {
            RockContext rockContext = new RockContext();
            var personAttributes = new AttributeService( rockContext ).GetByEntityTypeId( new Person().TypeId ).AsNoTracking();
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
            var familyAttributes = new AttributeService( rockContext ).Get( new Group().TypeId, "GroupTypeId", familyType.Id.ToString() ).AsNoTracking();
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
            var familyNoteTypeIds = NoteTypeCache.GetByEntity( familyEntityTypeId, string.Empty, string.Empty, true ).Select( a => a.Id );
            var personNoteTypeIds = NoteTypeCache.GetByEntity( personEntityTypeId, string.Empty, string.Empty, true ).Select( a => a.Id );

            var persons = new PersonService( rockContext ).Queryable( includeDeceased: true ).AsNoTracking().Where( t => dataViewPersonIds.Contains( t.Id ) );
            int total = persons.Count();
            int completed = 0;
            WriteProgressMessage( "Exporting", "", completed, total );

            foreach ( var person in persons )
            {
                WriteProgressMessage( "Exporting...", person.FullNameFormalReversed, completed, total );

                var exportPerson = new Slingshot.Core.Model.Person()
                {
                    Suffix = person.SuffixValueId.HasValue ? person.SuffixValue.Value : string.Empty,
                    Salutation = person.TitleValueId.HasValue ? person.TitleValue.Value : string.Empty,
                    AnniversaryDate = person.AnniversaryDate,
                    Birthdate = person.BirthDate,
                    ConnectionStatus = person.ConnectionStatusValueId.HasValue ? person.ConnectionStatusValue.Value : string.Empty,
                    CreatedDateTime = person.CreatedDateTime,
                    Id = person.Id,
                    InactiveReason = string.IsNullOrWhiteSpace( person.InactiveReasonNote ) ? ( person.RecordStatusReasonValue != null ? person.RecordStatusReasonValue.Value : string.Empty ) : person.InactiveReasonNote,
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
                    var maritalStatusValue = person.MaritalStatusValue.Guid;
                    if ( maritalStatusValue == Rock.SystemGuid.DefinedValue.PERSON_MARITAL_STATUS_SINGLE.AsGuid() )
                    {
                        exportPerson.MaritalStatus = Slingshot.Core.Model.MaritalStatus.Single;
                    }
                    else if ( maritalStatusValue == Rock.SystemGuid.DefinedValue.PERSON_MARITAL_STATUS_MARRIED.AsGuid() )
                    {
                        exportPerson.MaritalStatus = Slingshot.Core.Model.MaritalStatus.Married;
                    }
                    else if ( maritalStatusValue == Rock.SystemGuid.DefinedValue.PERSON_MARITAL_STATUS_DIVORCED.AsGuid() )
                    {
                        exportPerson.MaritalStatus = Slingshot.Core.Model.MaritalStatus.Divorced;
                    }
                    else
                    {
                        exportPerson.MaritalStatus = Slingshot.Core.Model.MaritalStatus.Unknown;
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
                    var recordStatusValue = person.RecordStatusValue.Guid;
                    if ( recordStatusValue == Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() )
                    {
                        exportPerson.RecordStatus = Slingshot.Core.Model.RecordStatus.Active;
                    }
                    else if ( recordStatusValue == Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() )
                    {
                        exportPerson.RecordStatus = Slingshot.Core.Model.RecordStatus.Pending;

                    }
                    else
                    {
                        exportPerson.RecordStatus = Slingshot.Core.Model.RecordStatus.Inactive;
                    }
                }

                exportPerson.PersonPhotoUrl = Person.GetPersonPhotoUrl( person );

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


                var notes = new NoteService( rockContext ).Queryable().AsNoTracking().Where( a => a.EntityId == person.Id && personNoteTypeIds.Contains( a.NoteTypeId ) );
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

                var family = person.GetFamily( rockContext );
                if ( family != null )
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

                    exportPerson.Addresses = new List<Slingshot.Core.Model.PersonAddress>();
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

                        exportPerson.Addresses.Add( address );

                        family.LoadAttributes( rockContext );
                        var familyAttributeValues = family.AttributeValues.Where( a => !string.IsNullOrEmpty( a.Value.Value ) ).Select( a => new Slingshot.Core.Model.GroupAttributeValue()
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

                    var familyNotes = new NoteService( rockContext ).Queryable().AsNoTracking().Where( a => a.EntityId == family.Id && familyNoteTypeIds.Contains( a.NoteTypeId ) );
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

                completed++;
            }

            WriteProgressMessage( "Export Complete", string.Empty, completed, total );
            ProcessingCompleted();
        }

        private void ExportFinance( List<int> dataViewPersonIds )
        {
            RockContext rockContext = new RockContext();

            var financialTransactions = new FinancialTransactionService( rockContext ).Queryable().AsNoTracking().Where( a => a.AuthorizedPersonAliasId.HasValue && dataViewPersonIds.Contains( a.AuthorizedPersonAlias.PersonId ) );
            var batchIds = financialTransactions.Where( a => a.BatchId.HasValue ).Select( a => a.BatchId.Value ).Distinct().ToList();

            var batches = new FinancialBatchService( rockContext ).GetByIds( batchIds ).AsNoTracking();
            var distinctAccountIds = new List<int>();
            foreach ( var batch in batches )
            {
                var exportBatch = new Slingshot.Core.Model.FinancialBatch()
                {
                    CampusId = batch.CampusId,
                    CreatedByPersonId = batch.CreatedByPersonId,
                    CreatedDateTime = batch.CreatedDateTime,
                    EndDate = batch.BatchEndDateTime,
                    ModifiedDateTime = batch.ModifiedDateTime,
                    StartDate = batch.BatchStartDateTime,
                    Id = batch.Id,
                    ModifiedByPersonId = batch.ModifiedByPersonId,
                    Name = batch.Name
                };

                switch ( batch.Status )
                {
                    case BatchStatus.Pending:
                        exportBatch.Status = Slingshot.Core.Model.BatchStatus.Pending;
                        break;
                    case BatchStatus.Closed:
                        exportBatch.Status = Slingshot.Core.Model.BatchStatus.Closed;
                        break;
                    case BatchStatus.Open:
                    default:
                        exportBatch.Status = Slingshot.Core.Model.BatchStatus.Open;
                        break;
                }

                exportBatch.FinancialTransactions = new List<Slingshot.Core.Model.FinancialTransaction>();

                foreach ( var transaction in financialTransactions.Where( a => a.BatchId.HasValue && a.BatchId == batch.Id ) )
                {
                    var exportTransaction = new Slingshot.Core.Model.FinancialTransaction()
                    {
                        AuthorizedPersonId = transaction.AuthorizedPersonAlias.PersonId,
                        BatchId = transaction.BatchId ?? 0,
                        CreatedByPersonId = transaction.CreatedByPersonId,
                        CreatedDateTime = transaction.CreatedDateTime,
                        Id = transaction.Id,
                        ModifiedByPersonId = transaction.ModifiedByPersonId,
                        ModifiedDateTime = transaction.ModifiedDateTime,
                        Summary = transaction.Summary,
                        TransactionCode = transaction.TransactionCode,
                        TransactionDate = transaction.TransactionDateTime
                    };

                    if ( transaction.SourceTypeValueId.HasValue )
                    {
                        var sourceValue = transaction.SourceTypeValue.Guid;
                        if ( sourceValue == Rock.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_BANK_CHECK.AsGuid() )
                        {
                            exportTransaction.TransactionSource = Slingshot.Core.Model.TransactionSource.BankChecks;
                        }
                        else if ( sourceValue == Rock.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_KIOSK.AsGuid() )
                        {
                            exportTransaction.TransactionSource = Slingshot.Core.Model.TransactionSource.Kiosk;
                        }
                        else if ( sourceValue == Rock.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_MOBILE_APPLICATION.AsGuid() )
                        {
                            exportTransaction.TransactionSource = Slingshot.Core.Model.TransactionSource.MobileApplication;
                        }
                        else if ( sourceValue == Rock.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_ONSITE_COLLECTION.AsGuid() )
                        {
                            exportTransaction.TransactionSource = Slingshot.Core.Model.TransactionSource.OnsiteCollection;
                        }
                        else
                        {
                            exportTransaction.TransactionSource = Slingshot.Core.Model.TransactionSource.Website;
                        }
                    }
                    else
                    {
                        exportTransaction.TransactionSource = Slingshot.Core.Model.TransactionSource.BankChecks;
                    }

                    var transactionType = transaction.TransactionTypeValue.Guid;
                    if ( transactionType == Rock.SystemGuid.DefinedValue.TRANSACTION_TYPE_CONTRIBUTION.AsGuid() )
                    {
                        exportTransaction.TransactionType = Slingshot.Core.Model.TransactionType.Contribution;
                    }
                    else
                    {
                        exportTransaction.TransactionType = Slingshot.Core.Model.TransactionType.EventRegistration;
                    }

                    if ( transaction.FinancialPaymentDetailId.HasValue && transaction.FinancialPaymentDetail.CurrencyTypeValueId.HasValue )
                    {
                        var currencyType = transaction.FinancialPaymentDetail.CurrencyTypeValue.Guid;

                        if ( currencyType == Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_ACH.AsGuid() )
                        {
                            exportTransaction.CurrencyType = Slingshot.Core.Model.CurrencyType.ACH;
                        }
                        else if ( currencyType == Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CASH.AsGuid() )
                        {
                            exportTransaction.CurrencyType = Slingshot.Core.Model.CurrencyType.Cash;
                        }
                        else if ( currencyType == Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CHECK.AsGuid() )
                        {
                            exportTransaction.CurrencyType = Slingshot.Core.Model.CurrencyType.Check;
                        }
                        else if ( currencyType == Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CREDIT_CARD.AsGuid() )
                        {
                            exportTransaction.CurrencyType = Slingshot.Core.Model.CurrencyType.CreditCard;
                        }
                        else if ( currencyType == Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_NONCASH.AsGuid() )
                        {
                            exportTransaction.CurrencyType = Slingshot.Core.Model.CurrencyType.NonCash;
                        }
                        else if ( currencyType == Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_OTHER.AsGuid() )
                        {
                            exportTransaction.CurrencyType = Slingshot.Core.Model.CurrencyType.Other;
                        }
                        else
                        {
                            exportTransaction.CurrencyType = Slingshot.Core.Model.CurrencyType.Unknown;
                        }
                    }
                    else
                    {
                        exportTransaction.CurrencyType = Slingshot.Core.Model.CurrencyType.Unknown;
                    }

                    exportTransaction.FinancialTransactionDetails = new List<Slingshot.Core.Model.FinancialTransactionDetail>();
                    foreach ( var detail in transaction.TransactionDetails )
                    {
                        var exportDetail = new Slingshot.Core.Model.FinancialTransactionDetail()
                        {
                            AccountId = detail.AccountId,
                            Amount = detail.Amount,
                            CreatedByPersonId = detail.CreatedByPersonId,
                            CreatedDateTime = detail.CreatedDateTime,
                            Id = detail.Id,
                            ModifiedByPersonId = detail.ModifiedByPersonId,
                            ModifiedDateTime = detail.ModifiedDateTime,
                            Summary = detail.Summary,
                            TransactionId = detail.TransactionId
                        };
                        exportTransaction.FinancialTransactionDetails.Add( exportDetail );

                        if ( !distinctAccountIds.Contains( detail.AccountId ) )
                        {
                            distinctAccountIds.Add( detail.AccountId );
                        }
                    }
                    exportBatch.FinancialTransactions.Add( exportTransaction );
                }

                ImportPackage.WriteToPackage<Slingshot.Core.Model.FinancialBatch>( exportBatch );
            }

            var accounts = new FinancialAccountService( rockContext ).GetByIds( distinctAccountIds ).AsNoTracking();
            foreach ( var account in accounts )
            {
                Slingshot.Core.Model.FinancialAccount exportAccount = new Slingshot.Core.Model.FinancialAccount()
                {
                    CampusId = account.CampusId,
                    Id = account.Id,
                    Name = account.Name,
                    ParentAccountId = account.ParentAccountId,
                    IsTaxDeductible = account.IsTaxDeductible
                };

                ImportPackage.WriteToPackage<Slingshot.Core.Model.FinancialAccount>( exportAccount );
            }
            
        }

        /// <summary>
        /// Writes the progress message.
        /// </summary>
        /// <param name="message">The message.</param>
        private void WriteProgressMessage( string message, string results, int completed, int total )
        {
            _hubContext.Clients.All.receiveNotification( this.SignalRNotificationKey, message, results.ConvertCrLfToHtmlBr(), completed.ToString(), total.ToString() );
        }

        /// <summary>
        /// Tells the client that the processing is completed.
        /// </summary>
        private void ProcessingCompleted()
        {
            _hubContext.Clients.All.showDetails( this.SignalRNotificationKey, false );
        }
        #endregion
    }
}