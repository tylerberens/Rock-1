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
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Financial;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Finance
{
    #region Block Attributes

    /// <summary>
    /// Edit an existing scheduled transaction.
    /// </summary>
    [DisplayName( "Scheduled Transaction Edit" )]
    [Category( "Finance" )]
    [Description( "Edit an existing scheduled transaction." )]

    [BooleanField( "Impersonation", "Allow (only use on an internal page used by staff)", "Don't Allow",
        "Should the current user be able to view and edit other people's transactions?  IMPORTANT: This should only be enabled on an internal page that is secured to trusted users", false, "", 0 )]
    [AccountsField( "Accounts", "The accounts to display.  By default all active accounts with a Public Name will be displayed", false, "", "", 1 )]
    [BooleanField( "Additional Accounts", "Display option for selecting additional accounts", "Don't display option",
        "Should users be allowed to select additional accounts?  If so, any active account with a Public Name value will be available", true, "", 2 )]

    // Text Options

    [TextField( "Panel Title", "The text to display in panel heading", false, "Scheduled Transaction", "Text Options", 4 )]
    [TextField( "Contribution Info Title", "The text to display as heading of section for selecting account and amount.", false, "Contribution Information", "Text Options", 5 )]
    [TextField( "Add Account Text", "The button text to display for adding an additional account", false, "Add Another Account", "Text Options", 6 )]
    [TextField( "Payment Info Title", "The text to display as heading of section for entering credit card or bank account information.", false, "Payment Information", "Text Options", 7 )]
    [TextField( "Confirmation Title", "The text to display as heading of section for confirming information entered.", false, "Confirm Information", "Text Options", 8 )]
    [CodeEditorField( "Confirmation Header", "The text (HTML) to display at the top of the confirmation section. <span class='tip tip-lava'></span> <span class='tip tip-html'></span>",
        CodeEditorMode.Html, CodeEditorTheme.Rock, 200, false, @"
<p>
Please confirm the information below. Once you have confirmed that the information is accurate click the 'Finish' button to complete your transaction.
</p>
", "Text Options", 9 )]
    [CodeEditorField( "Confirmation Footer", "The text (HTML) to display at the bottom of the confirmation section. <span class='tip tip-lava'></span> <span class='tip tip-html'></span>",
        CodeEditorMode.Html, CodeEditorTheme.Rock, 200, false, @"
<div class='alert alert-info'>
By clicking the 'finish' button below I agree to allow {{ 'Global' | Attribute:'OrganizationName' }} to debit the amount above from my account. I acknowledge that I may
update the transaction information at any time by returning to this website. Please call the Finance Office if you have any additional questions.
</div>
", "Text Options", 10 )]
    [CodeEditorField( "Success Header", "The text (HTML) to display at the top of the success section. <span class='tip tip-lava'></span> <span class='tip tip-html'></span>",
        CodeEditorMode.Html, CodeEditorTheme.Rock, 200, false, @"
<p>
Thank you for your generous contribution.  Your support is helping {{ 'Global' | Attribute:'OrganizationName' }} actively
achieve our mission.  We are so grateful for your commitment.
</p>
", "Text Options", 11 )]
    [CodeEditorField( "Success Footer", "The text (HTML) to display at the bottom of the success section. <span class='tip tip-lava'></span> <span class='tip tip-html'></span>",
        CodeEditorMode.Html, CodeEditorTheme.Rock, 200, false, @"
", "Text Options", 12 )]

    #endregion

    public partial class ScheduledTransactionEdit : RockBlock
    {
        #region Fields

        private GatewayComponent _gateway;
        private bool _using3StepGateway = false;
        private bool _savedAccountSupported = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the accounts that are available for user to add to the list.
        /// </summary>
        protected List<AccountItem> AvailableAccounts { get; set; }

        /// <summary>
        /// Gets or sets the accounts that are currently displayed to the user
        /// </summary>
        protected List<AccountItem> SelectedAccounts { get; set; }

        /// <summary>
        /// Gets or sets the payment transaction code.
        /// </summary>
        protected string TransactionCode { get; set; }

        /// <summary>
        /// Gets or sets the scheduled transaction.
        /// </summary>
        /// <value>
        /// The scheduled transaction.
        /// </value>
        protected FinancialScheduledTransaction ScheduledTransaction { get; set; }

        // The URL for the Step-2 Iframe Url
        protected string Step2IFrameUrl { get; set; }

        #endregion

        #region base control methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            AvailableAccounts = ViewState["AvailableAccounts"] as List<AccountItem>;
            SelectedAccounts = ViewState["SelectedAccounts"] as List<AccountItem>;
            TransactionCode = ViewState["TransactionCode"] as string ?? string.Empty;
            var scheduleId = ViewState["TransactionId"] as int?;
            if ( scheduleId.HasValue )
            {
                ScheduledTransaction = new FinancialScheduledTransactionService( new RockContext() ).Get( scheduleId.Value );
            }

        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RockPage page = Page as RockPage;
            if ( page != null )
            {
                page.PageNavigate += page_PageNavigate;
            }

            RegisterScript();

        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            // Hide the error box on every postback
            nbMessage.Visible = false;
            nbSelectionMessage.Visible = false;
            nbConfirmationMessage.Visible = false;
            nbSuccessMessage.Visible = false;
            pnlDupWarning.Visible = false;

            hfStep2AutoSubmit.Value = "false";

            if ( ScheduledTransaction == null )
            {
                ScheduledTransaction = GetScheduledTransaction( true );
            }

            if ( ScheduledTransaction == null )
            {
                SetPage( 0 );
                ShowMessage( NotificationBoxType.Danger, "Invalid Transaction", "The transaction you are trying to edit could not be determined." );
                return;
            }

            if ( ScheduledTransaction.FinancialGateway == null )
            {
                SetPage( 0 );
                ShowMessage( NotificationBoxType.Danger, "Invalid Transaction", "The transaction you are trying to edit does not have a valid financial gateway." );
                return;
            }

            ScheduledTransaction.FinancialGateway.LoadAttributes();
            _gateway = ScheduledTransaction.FinancialGateway.GetGatewayComponent();
            if ( _gateway == null )
            {
                SetPage( 0 );
                ShowMessage( NotificationBoxType.Danger, "Invalid Gateway", "The transaction you are editing does not have a valid payment gateway, and cannot be updated." );
                return;
            }

            var threeStepGateway = _gateway as ThreeStepGatewayComponent;
            if ( threeStepGateway != null )
            {
                _using3StepGateway = true;
                Step2IFrameUrl = ResolveRockUrl( threeStepGateway.Step2FormUrl );
            }
            _savedAccountSupported = _gateway.SupportsSavedAccount( true );

            var testGatewayGuid = Rock.SystemGuid.EntityType.FINANCIAL_GATEWAY_TEST_GATEWAY.AsGuid();
            if ( _gateway.TypeGuid == testGatewayGuid )
            {
                ShowMessage( NotificationBoxType.Warning, "Testing", "Note, this scheduled transaction is configured to use the test gateway. No actual amounts will be charged to the card or bank account." );
            }

            if ( !Page.IsPostBack )
            {
                GetAccounts( ScheduledTransaction );
                SetFrequency( ScheduledTransaction );
                SetSavedAccounts();

                SetPage( 1 );

                SetControlOptions();

                // Get the list of accounts that can be used
                BindAccounts();
            }
            else
            {
                // Save amounts from controls to the viewstate list
                foreach ( RepeaterItem item in rptAccountList.Items )
                {
                    var accountAmount = item.FindControl( "txtAccountAmount" ) as RockTextBox;
                    if ( accountAmount != null )
                    {
                        if ( SelectedAccounts != null && SelectedAccounts.Count > item.ItemIndex )
                        {
                            decimal amount = decimal.MinValue;
                            if ( !decimal.TryParse( accountAmount.Text, out amount ) )
                            {
                                amount = 0.0M;
                            }

                            SelectedAccounts[item.ItemIndex].Amount = amount;
                        }
                    }
                }
            }

            // Update the total amount
            lblTotalAmount.Text = GlobalAttributesCache.Value( "CurrencySymbol" ) + SelectedAccounts.Sum( f => f.Amount ).ToString( "F2" );

            // Show or Hide the Credit card entry panel based on if a saved account exists and it's selected or not.
            divNewPayment.Style[HtmlTextWriterStyle.Display] = ( rblSavedAccount.Items.Count == 0 || rblSavedAccount.Items[rblSavedAccount.Items.Count - 1].Selected ) ? "block" : "none";

            if ( hfPaymentTab.Value == "ACH" )
            {
                liCreditCard.RemoveCssClass( "active" );
                liACH.AddCssClass( "active" );
                divCCPaymentInfo.RemoveCssClass( "active" );
                divACHPaymentInfo.AddCssClass( "active" );
            }
            else
            {
                liCreditCard.AddCssClass( "active" );
                liACH.RemoveCssClass( "active" );
                divCCPaymentInfo.AddCssClass( "active" );
                divACHPaymentInfo.RemoveCssClass( "active" );
            }

        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState["AvailableAccounts"] = AvailableAccounts;
            ViewState["SelectedAccounts"] = SelectedAccounts;
            ViewState["TransactionCode"] = TransactionCode;
            ViewState["TransactionId"] = ScheduledTransaction != null ? ScheduledTransaction.Id : (int?)null;

            return base.SaveViewState();
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the PageNavigate event of the page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="HistoryEventArgs"/> instance containing the event data.</param>
        protected void page_PageNavigate( object sender, HistoryEventArgs e )
        {
            int pageId = e.State["GivingDetail"].AsInteger();
            if ( pageId > 0 )
            {
                SetPage( pageId );
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the btnAddAccount control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnAddAccount_SelectionChanged( object sender, EventArgs e )
        {
            var selected = AvailableAccounts.Where( a => a.Id == ( btnAddAccount.SelectedValueAsId() ?? 0 ) ).ToList();
            AvailableAccounts = AvailableAccounts.Except( selected ).ToList();
            SelectedAccounts.AddRange( selected );

            BindAccounts();
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            var qryParams = new Dictionary<string, string>();

            string personParam = PageParameter( "Person" );
            if ( !string.IsNullOrWhiteSpace( personParam ) )
            {
                qryParams.Add( "Person", personParam );
            }

            string txnParam = PageParameter( "ScheduledTransactionId" );
            if ( !string.IsNullOrWhiteSpace( txnParam ) )
            {
                qryParams.Add( "ScheduledTransactionId", txnParam );
            }

            NavigateToParentPage( qryParams );
        }

        /// <summary>
        /// Handles the Click event of the btnPaymentInfoNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnPaymentInfoNext_Click( object sender, EventArgs e )
        {
            var errorMessages = new List<string>();

            // Validate that an amount was entered
            if ( SelectedAccounts.Sum( a => a.Amount ) <= 0 )
            {
                errorMessages.Add( "Make sure you've entered an amount for at least one account" );
            }

            // Validate that no negative amounts were entered
            if ( SelectedAccounts.Any( a => a.Amount < 0 ) )
            {
                errorMessages.Add( "Make sure the amount you've entered for each account is a positive amount" );
            }

            // Make sure a repeating payment starts in the future
            DateTime when = DateTime.MinValue;
            if ( dtpStartDate.SelectedDate.HasValue && dtpStartDate.SelectedDate > RockDateTime.Today )
            {
                when = dtpStartDate.SelectedDate.Value;
            }
            else
            {
                errorMessages.Add( "Make sure the Next  Gift date is in the future (after today)" );
            }

            if ( errorMessages.Any() )
            {
                ShowMessage( NotificationBoxType.Danger, "Before we finish...", errorMessages.AsDelimited( "<br/>" ) );
            }
            else
            {
                string errorMessage = string.Empty;

                if ( _using3StepGateway && cbChangePaymentMethod.Checked )
                {
                    if ( ProcessStep1( out errorMessage ) )
                    {
                        this.AddHistory( "GivingDetail", "1", null );
                        if ( rblSavedAccount.Items.Count > 0 && ( rblSavedAccount.SelectedValueAsId() ?? 0 ) > 0 )
                        {
                            hfStep2AutoSubmit.Value = "true";
                        }

                        if ( hfStep2Url.Value.IsNotNullOrWhiteSpace() )
                        {
                            SetPage( 2 );
                        }
                        else
                        {
                            SetPage( 3 );
                        }
                    }
                    else
                    {
                        ShowMessage( NotificationBoxType.Danger, "Before we finish...", errorMessage );
                    }
                }
                else
                {
                    if ( ProcessPaymentInfo( out errorMessage ) )
                    {
                        this.AddHistory( "GivingDetail", "1", null );
                        SetPage( 3 );
                    }
                    else
                    {
                        ShowMessage( NotificationBoxType.Danger, "Before we finish...", errorMessage );
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnStep2Payment control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnStep2PaymentPrev_Click( object sender, EventArgs e )
        {
            this.AddHistory( "GivingDetail", "2", null );
            SetPage( 1 );
        }

        /// <summary>
        /// Handles the Click event of the lbStep2Return control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbStep2Return_Click( object sender, EventArgs e )
        {
            SetPage( 3 );
        }

        /// <summary>
        /// Handles the Click event of the btnConfirmationPrev control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnConfirmationPrev_Click( object sender, EventArgs e )
        {
            SetPage( 1 );
        }

        /// <summary>
        /// Handles the Click event of the btnConfirmationNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnConfirmationNext_Click( object sender, EventArgs e )
        {
            string errorMessage = string.Empty;
            if ( _using3StepGateway && cbChangePaymentMethod.Checked )
            {
                string resultQueryString = hfStep2ReturnQueryString.Value;
                if ( ProcessStep3( resultQueryString, out errorMessage ) )
                {
                    this.AddHistory( "GivingDetail", "3", null );
                    SetPage( 4 );
                }
                else
                {
                    ShowMessage( NotificationBoxType.Danger, "Payment Error", errorMessage );
                }
            }
            else
            {
                if ( ProcessConfirmation( out errorMessage ) )
                {
                    this.AddHistory( "GivingDetail", "2", null );
                    SetPage( 4 );
                }
                else
                {
                    ShowMessage( NotificationBoxType.Danger, "Payment Error", errorMessage );
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnConfirm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnConfirm_Click( object sender, EventArgs e )
        {
            TransactionCode = string.Empty;

            string errorMessage = string.Empty;
            if ( ProcessConfirmation( out errorMessage ) )
            {
                SetPage( 4 );
            }
            else
            {
                ShowMessage( NotificationBoxType.Danger, "Payment Error", errorMessage );
            }
        }


        #endregion

        #region  Methods

        #region Initialization

        /// <summary>
        /// Gets the scheduled transaction.
        /// </summary>
        /// <param name="refresh">if set to <c>true</c> [refresh].</param>
        /// <returns></returns>
        private FinancialScheduledTransaction GetScheduledTransaction( bool refresh = false )
        {
            Person targetPerson = null;
            using ( var rockContext = new RockContext() )
            {
                // If impersonation is allowed, and a valid person key was used, set the target to that person
                bool allowImpersonation = GetAttributeValue( "Impersonation" ).AsBoolean();
                if ( allowImpersonation )
                {
                    string personKey = PageParameter( "Person" );
                    if ( !string.IsNullOrWhiteSpace( personKey ) )
                    {
                        targetPerson = new PersonService( rockContext ).GetByUrlEncodedKey( personKey );
                    }
                }

                if ( targetPerson == null )
                {
                    targetPerson = CurrentPerson;
                }

                // Verify that transaction id is valid for selected person
                if ( targetPerson != null )
                {
                    int txnId = int.MinValue;
                    if ( int.TryParse( PageParameter( "ScheduledTransactionId" ), out txnId ) )
                    {
                        var personService = new PersonService( rockContext );

                        var validGivingIds = new List<string> { targetPerson.GivingId };
                        validGivingIds.AddRange( personService.GetBusinesses( targetPerson.Id ).Select( b => b.GivingId ) );

                        var service = new FinancialScheduledTransactionService( rockContext );
                        var scheduledTransaction = service
                            .Queryable( "AuthorizedPersonAlias.Person,ScheduledTransactionDetails,FinancialGateway,FinancialPaymentDetail.CurrencyTypeValue,FinancialPaymentDetail.CreditCardTypeValue" )
                            .Where( t =>
                                t.Id == txnId &&
                                t.AuthorizedPersonAlias != null &&
                                t.AuthorizedPersonAlias.Person != null &&
                                validGivingIds.Contains( t.AuthorizedPersonAlias.Person.GivingId ) )
                            .FirstOrDefault();

                        if ( scheduledTransaction != null )
                        {
                            if ( refresh )
                            {
                                string errorMessages = string.Empty;
                                service.GetStatus( scheduledTransaction, out errorMessages );
                                rockContext.SaveChanges();
                            }

                            return scheduledTransaction;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the accounts.
        /// </summary>
        /// <param name="scheduledTransaction">The scheduled transaction.</param>
        private void GetAccounts( FinancialScheduledTransaction scheduledTransaction )
        {
            var selectedGuids = GetAttributeValues( "Accounts" ).Select( Guid.Parse ).ToList();
            bool showAll = !selectedGuids.Any();

            bool additionalAccounts = GetAttributeValue( "AdditionalAccounts" ).AsBoolean( true );

            SelectedAccounts = new List<AccountItem>();
            AvailableAccounts = new List<AccountItem>();

            // Enumerate through all active accounts that are public
            foreach ( var account in new FinancialAccountService( new RockContext() ).Queryable()
                .Where( f =>
                    f.IsActive &&
                    f.IsPublic.HasValue &&
                    f.IsPublic.Value &&
                    ( f.StartDate == null || f.StartDate <= RockDateTime.Today ) &&
                    ( f.EndDate == null || f.EndDate >= RockDateTime.Today ) )
                .OrderBy( f => f.Order ) )
            {
                var accountItem = new AccountItem( account.Id, account.Order, account.Name, account.CampusId, account.PublicName );
                if ( showAll )
                {
                    SelectedAccounts.Add( accountItem );
                }
                else
                {
                    if ( selectedGuids.Contains( account.Guid ) )
                    {
                        SelectedAccounts.Add( accountItem );
                    }
                    else
                    {
                        if ( additionalAccounts )
                        {
                            AvailableAccounts.Add( accountItem );
                        }
                    }
                }
            }

            foreach ( var txnDetail in scheduledTransaction.ScheduledTransactionDetails )
            {
                var selectedAccount = SelectedAccounts.Where( a => a.Id == txnDetail.AccountId ).FirstOrDefault();
                if ( selectedAccount != null )
                {
                    selectedAccount.Amount = txnDetail.Amount;
                }
                else
                {
                    var selected = AvailableAccounts.Where( a => a.Id == txnDetail.AccountId ).ToList();
                    if ( selected != null )
                    {
                        selected.ForEach( a => a.Amount = txnDetail.Amount );
                        AvailableAccounts = AvailableAccounts.Except( selected ).ToList();
                        SelectedAccounts.AddRange( selected );
                    }
                }
            }

            BindAccounts();
        }

        /// <summary>
        /// Sets the frequency.
        /// </summary>
        /// <param name="scheduledTransaction">The scheduled transaction.</param>
        private void SetFrequency( FinancialScheduledTransaction scheduledTransaction )
        {
            // Enable payment options based on the configured gateways
            bool ccEnabled = false;
            bool achEnabled = false;

            if ( scheduledTransaction != null && _gateway != null )
            {
                if ( scheduledTransaction.FinancialPaymentDetail != null &&
                    scheduledTransaction.FinancialPaymentDetail.CurrencyTypeValueId == DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CREDIT_CARD ).Id )
                {
                    ccEnabled = true;
                    var authorizedPerson = scheduledTransaction.AuthorizedPersonAlias.Person;
                    txtCardFirstName.Text = authorizedPerson.FirstName;
                    txtCardLastName.Text = authorizedPerson.LastName;
                    txtCardName.Text = authorizedPerson.FullName;

                    var groupLocation = new PersonService( new RockContext() ).GetFirstLocation(
                        authorizedPerson.Id, DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() ).Id );
                    if ( groupLocation != null )
                    {
                        acBillingAddress.SetValues( groupLocation.Location );
                    }
                    else
                    {
                        acBillingAddress.SetValues( null );
                    }

                    mypExpiration.MinimumYear = RockDateTime.Now.Year;
                }

                if ( scheduledTransaction.FinancialPaymentDetail != null &&
                    scheduledTransaction.FinancialPaymentDetail.CurrencyTypeValueId == DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_ACH ).Id )
                {
                    achEnabled = true;
                }

                if ( _gateway.SupportedPaymentSchedules.Any() )
                {
                    var oneTimeFrequency = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME );
                    divRepeatingPayments.Visible = true;

                    btnFrequency.DataSource = _gateway.SupportedPaymentSchedules;
                    btnFrequency.DataBind();

                    btnFrequency.SelectedValue = scheduledTransaction.TransactionFrequencyValueId.ToString();
                    dtpStartDate.SelectedDate = scheduledTransaction.NextPaymentDate ?? scheduledTransaction.StartDate;
                }

                liCreditCard.Visible = ccEnabled;
                divCCPaymentInfo.Visible = ccEnabled;

                liACH.Visible = achEnabled;
                divACHPaymentInfo.Visible = achEnabled;

                hfPaymentTab.Value = achEnabled ? "ACH" : "CreditCard";

                if ( ccEnabled )
                {
                    divCCPaymentInfo.AddCssClass( "tab-pane" );
                }

                if ( achEnabled )
                {
                    divACHPaymentInfo.AddCssClass( "tab-pane" );
                }
            }
        }

        /// <summary>
        /// Binds the saved accounts.
        /// </summary>
        private void SetSavedAccounts()
        {
            rblSavedAccount.Items.Clear();

            if ( ScheduledTransaction.AuthorizedPersonAlias != null &&
                ScheduledTransaction.AuthorizedPersonAlias != null &&
                CurrentPerson != null &&
                ScheduledTransaction.AuthorizedPersonAlias.PersonId == CurrentPerson.Id )
            {
                // Get the saved accounts for the target person
                var savedAccounts = new FinancialPersonSavedAccountService( new RockContext() )
                    .GetByPersonId( CurrentPerson.Id )
                    .ToList();

                if ( _gateway != null && _gateway.SupportsSavedAccount( true ) )
                {
                    // Find the saved accounts that are valid for the selected CC gateway
                    var ccSavedAccountIds = new List<int>();
                    var ccCurrencyType = DefinedValueCache.Get( new Guid( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CREDIT_CARD ) );
                    if ( _gateway.SupportsSavedAccount( ccCurrencyType ) )
                    {
                        ccSavedAccountIds = savedAccounts
                            .Where( a =>
                                a.FinancialGatewayId == ScheduledTransaction.FinancialGatewayId.Value &&
                                a.FinancialPaymentDetail != null &&
                                a.FinancialPaymentDetail.CurrencyTypeValueId == ccCurrencyType.Id )
                            .Select( a => a.Id )
                            .ToList();
                    }

                    // Find the saved accounts that are valid for the selected ACH gateway
                    var achSavedAccountIds = new List<int>();
                    var achCurrencyType = DefinedValueCache.Get( new Guid( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_ACH ) );
                    if ( _gateway.SupportsSavedAccount( achCurrencyType ) )
                    {
                        achSavedAccountIds = savedAccounts
                            .Where( a =>
                                a.FinancialGatewayId == ScheduledTransaction.FinancialGatewayId.Value &&
                                a.FinancialPaymentDetail != null &&
                                a.FinancialPaymentDetail.CurrencyTypeValueId == achCurrencyType.Id )
                            .Select( a => a.Id )
                            .ToList();
                    }

                    rblSavedAccount.DataSource = savedAccounts
                        .Where( a =>
                            ccSavedAccountIds.Contains( a.Id ) ||
                            achSavedAccountIds.Contains( a.Id ) )
                        .OrderBy( a => a.Name )
                        .Select( a => new
                        {
                            Id = a.Id,
                            Name = "Use " + a.Name + " (" + a.FinancialPaymentDetail.AccountNumberMasked + ")"
                        } ).ToList();
                    rblSavedAccount.DataBind();
                    if ( rblSavedAccount.Items.Count > 0 )
                    {
                        rblSavedAccount.Items.Add( new ListItem( "Use a different payment method", "0" ) );
                        if ( rblSavedAccount.SelectedValue == "" )
                        {
                            rblSavedAccount.Items[0].Selected = true;
                        }
                    }
                }
            }
        }

        private void SetControlOptions()
        {
            hfCurrentPage.Value = "1";

            btnAddAccount.Title = GetAttributeValue( "AddAccountText" );
            dtpStartDate.SelectedDate = ScheduledTransaction.NextPaymentDate ?? ScheduledTransaction.StartDate;
            divRepeatingPayments.Visible = btnFrequency.Items.Count > 0;

            lPanelTitle1.Text = GetAttributeValue( "PanelTitle" );
            lPanelTitle2.Text = GetAttributeValue( "PanelTitle" );
            lContributionInfoTitle.Text = GetAttributeValue( "ContributionInfoTitle" );
            lPaymentInfoTitle.Text = GetAttributeValue( "PaymentInfoTitle" );
            lConfirmationTitle.Text = GetAttributeValue( "ConfirmationTitle" );

            var commonMergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );

            lConfirmationHeader.Text = GetAttributeValue( "ConfirmationHeader" ).ResolveMergeFields( commonMergeFields );
            lConfirmationFooter.Text = GetAttributeValue( "ConfirmationFooter" ).ResolveMergeFields( commonMergeFields );

            lSuccessHeader.Text = GetAttributeValue( "SuccessHeader" ).ResolveMergeFields( commonMergeFields );
            lSuccessFooter.Text = GetAttributeValue( "SuccessFooter" ).ResolveMergeFields( commonMergeFields );

            // Determine if and how Name on Card should be displayed
            bool splitName = _gateway != null && _gateway.PromptForNameOnCard( ScheduledTransaction.FinancialGateway ) && _gateway.SplitNameOnCard;
            txtCardFirstName.Visible = splitName;
            txtCardLastName.Visible = splitName;
            txtCardName.Visible = !splitName;

            // Set cc expiration min/max
            mypExpiration.MinimumYear = RockDateTime.Now.Year;
            mypExpiration.MaximumYear = mypExpiration.MinimumYear + 15;

            // Determine if account name should be displayed for bank account
            txtAccountName.Visible = _gateway != null && _gateway.PromptForBankAccountName( ScheduledTransaction.FinancialGateway );

            // Determine if billing address should be displayed
            divBillingAddress.Visible = _gateway != null && _gateway.PromptForBillingAddress( ScheduledTransaction.FinancialGateway );
        }

        #endregion

        #region Process User Actions

        /// <summary>
        /// Processes the payment information.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        private bool ProcessPaymentInfo( out string errorMessage )
        {
            var rockContext = new RockContext();

            errorMessage = string.Empty;

            var errorMessages = new List<string>();

            if ( cbChangePaymentMethod.Checked && !_using3StepGateway  )
            {
                if ( rblSavedAccount.Items.Count <= 0 || ( rblSavedAccount.SelectedValueAsInt() ?? 0 ) <= 0 )
                { 
                    bool isACHTxn = hfPaymentTab.Value == "ACH";
                    if ( isACHTxn )
                    {
                        if ( string.IsNullOrWhiteSpace( txtRoutingNumber.Text ) )
                        {
                            errorMessages.Add( "Make sure to enter a valid routing number" );
                        }

                        if ( string.IsNullOrWhiteSpace( txtAccountNumber.Text ) )
                        {
                            errorMessages.Add( "Make sure to enter a valid account number" );
                        }
                    }
                    else
                    {
                        if ( _gateway.PromptForNameOnCard( ScheduledTransaction.FinancialGateway ) )
                        {
                            if ( _gateway.SplitNameOnCard )
                            {
                                if ( string.IsNullOrWhiteSpace( txtCardFirstName.Text ) || string.IsNullOrWhiteSpace( txtCardLastName.Text ) )
                                {
                                    errorMessages.Add( "Make sure to enter a valid first and last name as it appears on your credit card" );
                                }
                            }
                            else
                            {
                                if ( string.IsNullOrWhiteSpace( txtCardName.Text ) )
                                {
                                    errorMessages.Add( "Make sure to enter a valid name as it appears on your credit card" );
                                }
                            }
                        }

                        var rgx = new System.Text.RegularExpressions.Regex( @"[^\d]" );
                        string ccNum = rgx.Replace( txtCreditCard.Text, "" );
                        if ( string.IsNullOrWhiteSpace( ccNum ) )
                        {
                            errorMessages.Add( "Make sure to enter a valid credit card number" );
                        }

                        var currentMonth = RockDateTime.Today;
                        currentMonth = new DateTime( currentMonth.Year, currentMonth.Month, 1 );
                        if ( !mypExpiration.SelectedDate.HasValue || mypExpiration.SelectedDate.Value.CompareTo( currentMonth ) < 0 )
                        {
                            errorMessages.Add( "Make sure to enter a valid credit card expiration date" );
                        }

                        if ( string.IsNullOrWhiteSpace( txtCVV.Text ) )
                        {
                            errorMessages.Add( "Make sure to enter a valid credit card security code" );
                        }
                    }
                }
            }

            if ( errorMessages.Any() )
            {
                errorMessage = errorMessages.AsDelimited( "<br/>" );
                return false;
            }

            rptAccountListConfirmation.DataSource = SelectedAccounts.Where( a => a.Amount != 0 );
            rptAccountListConfirmation.DataBind();

            return true;
        }

        /// <summary>
        /// Processes the step1.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        private bool ProcessStep1( out string errorMessage )
        {
            var rockContext = new RockContext();

            var threeStepGateway = _gateway as ThreeStepGatewayComponent;
            if ( threeStepGateway == null )
            {
                errorMessage = "There was a problem creating the payment gateway information";
                return false;
            }

            PaymentInfo paymentInfo = GetPaymentInfo( new PersonService( rockContext ), ScheduledTransaction );
            paymentInfo.IPAddress = GetClientIpAddress();
            paymentInfo.AdditionalParameters = threeStepGateway.GetStep1Parameters( ResolveRockUrlIncludeRoot( "~/GatewayStep2Return.aspx" ) );

            string result = string.Empty;

            var howOften = DefinedValueCache.Get( btnFrequency.SelectedValueAsId().Value );
            DateTime when = DateTime.MinValue;

            var schedule = new PaymentSchedule();
            schedule.TransactionFrequencyValue = howOften;
            schedule.StartDate = when;

            result = threeStepGateway.UpdateScheduledPaymentStep1( ScheduledTransaction, schedule, paymentInfo, out errorMessage );

            if ( string.IsNullOrWhiteSpace( errorMessage ) && !string.IsNullOrWhiteSpace( result ) )
            {
                hfStep2Url.Value = result;
            }

            return string.IsNullOrWhiteSpace( errorMessage );
        }

        private void ShowConfirmationDetails()
        {
            PaymentInfo paymentInfo = GetPaymentInfo( new PersonService( new RockContext() ), ScheduledTransaction );
            if ( paymentInfo != null )
            {
                tdName.Description = paymentInfo.FullName;
                tdTotal.Description = paymentInfo.Amount.ToString( "C" );

                if ( paymentInfo.CurrencyTypeValue != null )
                {
                    tdPaymentMethod.Description = paymentInfo.CurrencyTypeValue.Description;
                    tdPaymentMethod.Visible = true;
                }
                else
                {
                    tdPaymentMethod.Visible = false;
                }

                if ( string.IsNullOrWhiteSpace( paymentInfo.MaskedNumber ) )
                {
                    tdAccountNumber.Visible = false;
                }
                else
                {
                    tdAccountNumber.Visible = true;
                    tdAccountNumber.Description = paymentInfo.MaskedNumber;
                }

                string nextDate = dtpStartDate.SelectedDate.HasValue ? dtpStartDate.SelectedDate.Value.ToShortDateString() : "?";
                string frequency = DefinedValueCache.Get( btnFrequency.SelectedValueAsInt() ?? 0 ).Description;
                tdWhen.Description = frequency + " starting on " + nextDate;
            }
        }

        /// <summary>
        /// Processes the confirmation.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        private bool ProcessConfirmation( out string errorMessage )
        {
            var rockContext = new RockContext();
            errorMessage = string.Empty;

            if ( string.IsNullOrWhiteSpace( TransactionCode ) )
            {
                var personService = new PersonService( rockContext );
                var transactionService = new FinancialScheduledTransactionService( rockContext );
                var transactionDetailService = new FinancialScheduledTransactionDetailService( rockContext );

                var scheduledTransaction = transactionService
                        .Queryable( "AuthorizedPersonAlias.Person,FinancialGateway" )
                        .FirstOrDefault( s => s.Id == ScheduledTransaction.Id );

                if ( scheduledTransaction == null )
                {
                    errorMessage = "There was a problem getting the transaction information";
                    return false;
                }

                if ( scheduledTransaction.FinancialPaymentDetail == null )
                {
                    scheduledTransaction.FinancialPaymentDetail = new FinancialPaymentDetail();
                }

                if ( scheduledTransaction.FinancialGateway != null )
                {
                    scheduledTransaction.FinancialGateway.LoadAttributes();
                }

                if ( scheduledTransaction.AuthorizedPersonAlias == null || scheduledTransaction.AuthorizedPersonAlias.Person == null )
                {
                    errorMessage = "There was a problem determining the person associated with the transaction";
                    return false;
                }

                var changeSummary = new StringBuilder();

                // Get the payment schedule
                scheduledTransaction.TransactionFrequencyValueId = btnFrequency.SelectedValueAsId().Value;
                changeSummary.Append( DefinedValueCache.Get( scheduledTransaction.TransactionFrequencyValueId, rockContext ) );

                if ( dtpStartDate.SelectedDate.HasValue && dtpStartDate.SelectedDate > RockDateTime.Today )
                {
                    scheduledTransaction.StartDate = dtpStartDate.SelectedDate.Value;
                    changeSummary.AppendFormat( " starting {0}", scheduledTransaction.StartDate.ToShortDateString() );
                }
                else
                {
                    scheduledTransaction.StartDate = DateTime.MinValue;
                }

                changeSummary.AppendLine();

                PaymentInfo paymentInfo = GetPaymentInfo( personService, scheduledTransaction );
                if ( paymentInfo == null )
                {
                    errorMessage = "There was a problem creating the payment information";
                    return false;
                }
                else
                {
                }

                // If transaction is not active, attempt to re-activate it first
                if ( !scheduledTransaction.IsActive )
                {
                    if ( !transactionService.Reactivate( scheduledTransaction, out errorMessage ) )
                    {
                        return false;
                    }
                }

                string ScheduleId = string.Empty;
                if ( _gateway.UpdateScheduledPayment( scheduledTransaction, paymentInfo, out errorMessage ) )
                {
                    if ( _gateway.UpdateScheduledPaymentMethodSupported )
                    {
                        if ( hfPaymentTab.Value == "CreditCard" || hfPaymentTab.Value == "ACH" )
                        {
                            scheduledTransaction.FinancialPaymentDetail.SetFromPaymentInfo( paymentInfo, _gateway, rockContext );
                        }
                    }

                    var selectedAccountIds = SelectedAccounts
                        .Where( a => a.Amount > 0 )
                        .Select( a => a.Id ).ToList();

                    var deletedAccounts = scheduledTransaction.ScheduledTransactionDetails
                        .Where( a => !selectedAccountIds.Contains( a.AccountId ) ).ToList();

                    foreach ( var deletedAccount in deletedAccounts )
                    {
                        scheduledTransaction.ScheduledTransactionDetails.Remove( deletedAccount );
                        transactionDetailService.Delete( deletedAccount );
                    }

                    foreach ( var account in SelectedAccounts
                        .Where( a => a.Amount > 0 ) )
                    {
                        var detail = scheduledTransaction.ScheduledTransactionDetails
                            .Where( d => d.AccountId == account.Id ).FirstOrDefault();
                        if ( detail == null )
                        {
                            detail = new FinancialScheduledTransactionDetail();
                            detail.AccountId = account.Id;
                            scheduledTransaction.ScheduledTransactionDetails.Add( detail );
                        }

                        detail.Amount = account.Amount;

                        changeSummary.AppendFormat( "{0}: {1}", account.Name, account.Amount.FormatAsCurrency() );
                        changeSummary.AppendLine();
                    }

                    rockContext.SaveChanges();

                    // Add a note about the change
                    var noteType = NoteTypeCache.Get( Rock.SystemGuid.NoteType.SCHEDULED_TRANSACTION_NOTE.AsGuid() );
                    if ( noteType != null )
                    {
                        var noteService = new NoteService( rockContext );
                        var note = new Note();
                        note.NoteTypeId = noteType.Id;
                        note.EntityId = scheduledTransaction.Id;
                        note.Caption = "Updated Transaction";
                        note.Text = changeSummary.ToString();
                        noteService.Add( note );
                    }
                    rockContext.SaveChanges();

                    ScheduleId = scheduledTransaction.GatewayScheduleId;
                    TransactionCode = scheduledTransaction.TransactionCode;

                    if ( transactionService.GetStatus( scheduledTransaction, out errorMessage ) )
                    {
                        rockContext.SaveChanges();
                    }
                }
                else
                {
                    return false;
                }

                tdTransactionCode.Description = TransactionCode;
                tdTransactionCode.Visible = !string.IsNullOrWhiteSpace( TransactionCode );

                tdScheduleId.Description = ScheduleId;
                tdScheduleId.Visible = !string.IsNullOrWhiteSpace( ScheduleId );

                return true;
            }
            else
            {
                pnlDupWarning.Visible = true;
                return false;
            }
        }

        private bool ProcessStep3( string resultQueryString, out string errorMessage )
        {
            var threeStepGateway = _gateway as ThreeStepGatewayComponent;
            if ( threeStepGateway == null )
            {
                errorMessage = "There was a problem creating the payment gateway information";
                return false;
            }

            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            var transactionService = new FinancialScheduledTransactionService( rockContext );
            var transactionDetailService = new FinancialScheduledTransactionDetailService( rockContext );

            var scheduledTransaction = transactionService
                    .Queryable( "AuthorizedPersonAlias.Person,FinancialGateway" )
                    .FirstOrDefault( s => s.Id == ScheduledTransaction.Id );

            if ( scheduledTransaction == null )
            {
                errorMessage = "There was a problem getting the transaction information";
                return false;
            }

            if ( scheduledTransaction.FinancialPaymentDetail == null )
            {
                scheduledTransaction.FinancialPaymentDetail = new FinancialPaymentDetail();
            }

            if ( scheduledTransaction.FinancialGateway != null )
            {
                scheduledTransaction.FinancialGateway.LoadAttributes();
            }

            if ( scheduledTransaction.AuthorizedPersonAlias == null || scheduledTransaction.AuthorizedPersonAlias.Person == null )
            {
                errorMessage = "There was a problem determining the person associated with the transaction";
                return false;
            }

            var changeSummary = new StringBuilder();

            // Get the payment schedule
            scheduledTransaction.TransactionFrequencyValueId = btnFrequency.SelectedValueAsId().Value;
            changeSummary.Append( DefinedValueCache.Get( scheduledTransaction.TransactionFrequencyValueId, rockContext ) );

            if ( dtpStartDate.SelectedDate.HasValue && dtpStartDate.SelectedDate > RockDateTime.Today )
            {
                scheduledTransaction.StartDate = dtpStartDate.SelectedDate.Value;
                changeSummary.AppendFormat( " starting {0}", scheduledTransaction.StartDate.ToShortDateString() );
            }
            else
            {
                scheduledTransaction.StartDate = DateTime.MinValue;
            }

            changeSummary.AppendLine();

            PaymentInfo paymentInfo = GetPaymentInfo( personService, scheduledTransaction );
            if ( paymentInfo == null )
            {
                errorMessage = "There was a problem creating the payment information";
                return false;
            }

            paymentInfo.AdditionalParameters = threeStepGateway.GetStep3Parameters( paymentInfo );
            if ( !threeStepGateway.UpdateScheduledPaymentStep3( scheduledTransaction, paymentInfo, resultQueryString, out errorMessage ) )
            {
                return false;
            }

            if ( _gateway.UpdateScheduledPaymentMethodSupported )
            {
                if ( hfPaymentTab.Value == "CreditCard" || hfPaymentTab.Value == "ACH" )
                {
                    scheduledTransaction.FinancialPaymentDetail.SetFromPaymentInfo( paymentInfo, _gateway, rockContext );
                }
            }

            var selectedAccountIds = SelectedAccounts
                .Where( a => a.Amount > 0 )
                .Select( a => a.Id ).ToList();

            var deletedAccounts = scheduledTransaction.ScheduledTransactionDetails
                .Where( a => !selectedAccountIds.Contains( a.AccountId ) ).ToList();

            foreach ( var deletedAccount in deletedAccounts )
            {
                scheduledTransaction.ScheduledTransactionDetails.Remove( deletedAccount );
                transactionDetailService.Delete( deletedAccount );
            }

            foreach ( var account in SelectedAccounts
                .Where( a => a.Amount > 0 ) )
            {
                var detail = scheduledTransaction.ScheduledTransactionDetails
                    .Where( d => d.AccountId == account.Id ).FirstOrDefault();
                if ( detail == null )
                {
                    detail = new FinancialScheduledTransactionDetail();
                    detail.AccountId = account.Id;
                    scheduledTransaction.ScheduledTransactionDetails.Add( detail );
                }

                detail.Amount = account.Amount;

                changeSummary.AppendFormat( "{0}: {1}", account.Name, account.Amount.FormatAsCurrency() );
                changeSummary.AppendLine();
            }

            rockContext.SaveChanges();

            // Add a note about the change
            var noteType = NoteTypeCache.Get( Rock.SystemGuid.NoteType.SCHEDULED_TRANSACTION_NOTE.AsGuid() );
            if ( noteType != null )
            {
                var noteService = new NoteService( rockContext );
                var note = new Note();
                note.NoteTypeId = noteType.Id;
                note.EntityId = scheduledTransaction.Id;
                note.Caption = "Updated Transaction";
                note.Text = changeSummary.ToString();
                noteService.Add( note );
            }
            rockContext.SaveChanges();

            var ScheduleId = scheduledTransaction.GatewayScheduleId;
            TransactionCode = scheduledTransaction.TransactionCode;

            tdTransactionCode.Description = TransactionCode;
            tdTransactionCode.Visible = !string.IsNullOrWhiteSpace( TransactionCode );

            tdScheduleId.Description = ScheduleId;
            tdScheduleId.Visible = !string.IsNullOrWhiteSpace( ScheduleId );

            errorMessage = string.Empty;
            return true;
        }
        #endregion

        #region Build PaymentInfo

        /// <summary>
        /// Gets the payment information.
        /// </summary>
        /// <returns></returns>
        private PaymentInfo GetPaymentInfo( PersonService personService, FinancialScheduledTransaction scheduledTransaction )
        {
            PaymentInfo paymentInfo = null;
            if ( rblSavedAccount.Items.Count > 0 && ( rblSavedAccount.SelectedValueAsId() ?? 0 ) > 0 )
            {
                paymentInfo = GetReferenceInfo( rblSavedAccount.SelectedValueAsId().Value );
            }
            else
            {
                if ( hfPaymentTab.Value == "ACH" )
                {
                    paymentInfo = GetACHInfo();
                }
                else
                {
                    paymentInfo = GetCCInfo();
                }
            }

            if ( paymentInfo != null )
            {
                paymentInfo.Amount = SelectedAccounts.Sum( a => a.Amount );

                var authorizedPerson = scheduledTransaction.AuthorizedPersonAlias.Person;
                paymentInfo.FirstName = authorizedPerson.FirstName;
                paymentInfo.LastName = authorizedPerson.LastName;
                paymentInfo.Email = authorizedPerson.Email;

                bool displayPhone = GetAttributeValue( "DisplayPhone" ).AsBoolean();
                if ( displayPhone )
                {
                    var phoneNumber = personService.GetPhoneNumber( authorizedPerson, DefinedValueCache.Get( new Guid( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME ) ) );
                    paymentInfo.Phone = phoneNumber != null ? phoneNumber.ToString() : string.Empty;
                }

                Guid addressTypeGuid = Guid.Empty;
                if ( !Guid.TryParse( GetAttributeValue( "AddressType" ), out addressTypeGuid ) )
                {
                    addressTypeGuid = new Guid( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME );
                }

                var groupLocation = personService.GetFirstLocation( authorizedPerson.Id, DefinedValueCache.Get( addressTypeGuid ).Id );
                if ( groupLocation != null && groupLocation.Location != null )
                {
                    paymentInfo.Street1 = groupLocation.Location.Street1;
                    paymentInfo.Street2 = groupLocation.Location.Street2;
                    paymentInfo.City = groupLocation.Location.City;
                    paymentInfo.State = groupLocation.Location.State;
                    paymentInfo.PostalCode = groupLocation.Location.PostalCode;
                    paymentInfo.Country = groupLocation.Location.Country;
                }
            }

            return paymentInfo;
        }

        /// <summary>
        /// Gets the credit card information.
        /// </summary>
        /// <returns></returns>
        private CreditCardPaymentInfo GetCCInfo()
        {
            var cc = new CreditCardPaymentInfo( txtCreditCard.Text, txtCVV.Text, mypExpiration.SelectedDate ?? DateTime.MinValue );
            cc.NameOnCard = _gateway.SplitNameOnCard ? txtCardFirstName.Text : txtCardName.Text;
            cc.LastNameOnCard = txtCardLastName.Text;
            cc.BillingStreet1 = acBillingAddress.Street1;
            cc.BillingStreet2 = acBillingAddress.Street2;
            cc.BillingCity = acBillingAddress.City;
            cc.BillingState = acBillingAddress.State;
            cc.BillingPostalCode = acBillingAddress.PostalCode;
            cc.BillingCountry = acBillingAddress.Country;

            return cc;
        }

        /// <summary>
        /// Gets the ACH information.
        /// </summary>
        /// <returns></returns>
        private ACHPaymentInfo GetACHInfo()
        {
            var ach = new ACHPaymentInfo( txtAccountNumber.Text, txtRoutingNumber.Text, rblAccountType.SelectedValue == "Savings" ? BankAccountType.Savings : BankAccountType.Checking );
            return ach;
        }

        /// <summary>
        /// Gets the reference information.
        /// </summary>
        /// <param name="savedAccountId">The saved account unique identifier.</param>
        /// <returns></returns>
        private ReferencePaymentInfo GetReferenceInfo( int savedAccountId )
        {
            var savedAccount = new FinancialPersonSavedAccountService( new RockContext() ).Get( savedAccountId );
            if ( savedAccount != null )
            {
                return savedAccount.GetReferencePayment();
            }

            return null;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Binds the accounts.
        /// </summary>
        private void BindAccounts()
        {
            rptAccountList.DataSource = SelectedAccounts.OrderBy( a => a.Order ).ToList();
            rptAccountList.DataBind();

            btnAddAccount.Visible = AvailableAccounts.Any();
            btnAddAccount.DataSource = AvailableAccounts;
            btnAddAccount.DataBind();
        }

        /// <summary>
        /// Sets the page.
        /// </summary>
        /// <param name="page">The page.</param>
        private void SetPage( int page )
        {
            // Page 0 = Only message box is displayed
            // Page 1 = Payment Info
            // Page 2 = Step 2 (of three-step charge)
            // Page 3 = Confirmation
            // Page 4 = Success

            pnlSelection.Visible = page == 1 || page == 2;
            pnlPaymentInfo.Visible = page == 1;

            if ( _gateway != null && _gateway.UpdateScheduledPaymentMethodSupported )
            {
                pnlPaymentMethod.Visible = page == 1 || page == 2;
                cbChangePaymentMethod.Visible = page == 1;
                rblSavedAccount.Visible = page == 1 && cbChangePaymentMethod.Checked;
                divNewPayment.Visible = ( page == 1 && !_using3StepGateway ) || ( page == 2 );
            }
            else
            {
                pnlPaymentMethod.Visible = false;
            }

            btnPaymentInfoNext.Visible = page == 1;
            btnStep2PaymentPrev.Visible = page == 2;
            aStep2Submit.Visible = page == 2;

            pnlConfirmation.Visible = page == 3;
            pnlSuccess.Visible = page == 4;

            hfCurrentPage.Value = page.ToString();

            switch ( page )
            {
                case 3:
                    {
                        ShowConfirmationDetails();
                        break;
                    }
            }
        }

        /// <summary>
        /// Shows the message.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="title">The title.</param>
        /// <param name="text">The text.</param>
        private void ShowMessage( NotificationBoxType type, string title, string text )
        {
            if ( !string.IsNullOrWhiteSpace( text ) )
            {
                NotificationBox nb = nbMessage;
                switch ( hfCurrentPage.Value.AsInteger() )
                {
                    case 1: nb = nbSelectionMessage; break;
                    case 2: nb = nbSelectionMessage; break;
                    case 3: nb = nbConfirmationMessage; break;
                    case 4: nb = nbSuccessMessage; break;
                }

                nb.Text = text;
                nb.Title = string.IsNullOrWhiteSpace( title ) ? "" : string.Format( "<p>{0}</p>", title );
                nb.NotificationBoxType = type;
                nb.Visible = true;
            }
        }

        /// <summary>
        /// Formats the value as currency (called from markup)
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public string FormatValueAsCurrency( decimal value )
        {
            return value.FormatAsCurrency();
        }

        /// <summary>
        /// Registers the startup script.
        /// </summary>
        private void RegisterScript()
        {
            RockPage.AddScriptLink( ResolveUrl( "~/Scripts/jquery.creditCardTypeDetector.js" ) );

            int oneTimeFrequencyId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME ).Id;

            string scriptFormat = @"
    Sys.Application.add_load(function () {{
        // As amounts are entered, validate that they are numeric and recalc total
        $('.account-amount').on('change', function() {{
            var totalAmt = Number(0);

            $('.account-amount .form-control').each(function (index) {{
                var itemValue = $(this).val();
                if (itemValue != null && itemValue != '') {{
                    if (isNaN(itemValue)) {{
                        $(this).parents('div.input-group').addClass('has-error');
                    }}
                    else {{
                        $(this).parents('div.input-group').removeClass('has-error');
                        var num = Number(itemValue);
                        $(this).val(num.toFixed(2));
                        totalAmt = totalAmt + num;
                    }}
                }}
                else {{
                    $(this).parents('div.input-group').removeClass('has-error');
                }}
            }});
            $('.total-amount').html('{3}' + totalAmt.toFixed(2));
            return false;
        }});

        // Set the date prompt based on the frequency value entered
        $('#ButtonDropDown_btnFrequency .dropdown-menu a').click( function () {{
            var $when = $(this).parents('div.form-group:first').next();
            if ($(this).attr('data-id') == '{2}') {{
                $when.find('label:first').html('When');
            }} else {{
                $when.find('label:first').html('First Gift');

                // Set date to tomorrow if it is equal or less than today's date
                var $dateInput = $when.find('input');
                var dt = new Date(Date.parse($dateInput.val()));
                var curr = new Date();
                if ( (dt-curr) <= 0 ) {{
                    curr.setDate(curr.getDate() + 1);
                    var dd = curr.getDate();
                    var mm = curr.getMonth()+1;
                    var yy = curr.getFullYear();
                    $dateInput.val(mm+'/'+dd+'/'+yy);
                    $dateInput.data('datePicker').value(mm+'/'+dd+'/'+yy);
                }}
            }};
        }});

        // Save the state of the selected payment type pill to a hidden field so that state can
        // be preserved through postback
        $('a[data-toggle=""pill""]').on('shown.bs.tab', function (e) {{
            var tabHref = $(e.target).attr(""href"");
            if (tabHref == '#{0}') {{
                $('#{1}').val('CreditCard');
            }} else {{
                $('#{1}').val('ACH');
            }}
        }});

        // Detect credit card type
        $('.credit-card').creditCardTypeDetector({{ 'credit_card_logos': '.card-logos' }});

        if ( typeof {21} != 'undefined' ) {{
            //// Toggle credit card display if saved card option is available
            $('#{21}').unbind('click').on('click', function () {{

                var radioDisplay = $('#{22}').css('display');
                var selectedVal = $('#{21}').find('input:checked').first().val();

                if ( selectedVal == 0 && radioDisplay == 'none') {{
                    $('#{22}').slideToggle();
                }}
                else if (selectedVal != 0 && radioDisplay != 'none') {{
                    $('#{22}').slideToggle();
                }}
            }});
        }}

        // Hide or show a div based on selection of checkbox
        $('input:checkbox.toggle-input').unbind('click').on('click', function () {{
            $(this).parents('.checkbox').next('.toggle-content').slideToggle();
        }});

        // Disable the submit button as soon as it's clicked to prevent double-clicking
        $('a[id$=""btnNext""]').click(function() {{
            $(this).unbind('click');
            if (typeof (Page_ClientValidate) == 'function') {{
                if (Page_IsValid) {{
                    Page_ClientValidate();
                }}
            }}
            if (Page_IsValid) {{
			    $(this).addClass('disabled');
			    $(this).click(function () {{
				    return false;
			    }});
            }}
        }});
    }});

    // Posts the iframe (step 2)
    $('#aStep2Submit').on('click', function(e) {{
        e.preventDefault();
        if (typeof (Page_ClientValidate) == 'function') {{
            if (Page_IsValid && Page_ClientValidate('{7}') ) {{
                $(this).prop('disabled', true);
                $('#updateProgress').show();
                var src = $('#{4}').val();
                var $form = $('#iframeStep2').contents().find('#Step2Form');

                {16}
                $form.find('.js-billing-address1').val( $('#{17}_tbStreet1').val() );
                $form.find('.js-billing-city').val( $('#{17}_tbCity').val() );
                if ( $('#{17}_ddlState').length ) {{
                    $form.find('.js-billing-state').val( $('#{17}_ddlState').val() );
                }} else {{
                    $form.find('.js-billing-state').val( $('#{17}_tbState').val() );
                }}
                $form.find('.js-billing-postal').val( $('#{17}_tbPostalCode').val() );
                $form.find('.js-billing-country').val( $('#{17}_ddlCountry').val() );

                if ( $('#{1}').val() == 'CreditCard' ) {{
                    $form.find('.js-cc-first-name').val( $('#{18}').val() );
                    $form.find('.js-cc-last-name').val( $('#{19}').val() );
                    $form.find('.js-cc-full-name').val( $('#{20}').val() );
                    $form.find('.js-cc-number').val( $('#{8}').val() );
                    var mm = $('#{9}_monthDropDownList').val();
                    var yy = $('#{9}_yearDropDownList_').val();
                    mm = mm.length == 1 ? '0' + mm : mm;
                    yy = yy.length == 4 ? yy.substring(2,4) : yy;
                    $form.find('.js-cc-expiration').val( mm + yy );
                    $form.find('.js-cc-cvv').val( $('#{10}').val() );
                }} else {{
                    $form.find('.js-account-name').val( $('#{11}').val() );
                    $form.find('.js-account-number').val( $('#{12}').val() );
                    $form.find('.js-routing-number').val( $('#{13}').val() );
                    $form.find('.js-account-type').val( $('#{14}').find('input:checked').val() );
                    $form.find('.js-entity-type').val( 'personal' );
                }}

                $form.attr('action', src );
                $form.submit();
            }}
        }}
    }});

    // Evaluates the current url whenever the iframe is loaded and if it includes a qrystring parameter
    // The qry parameter value is saved to a hidden field and a post back is performed
    $('#iframeStep2').on('load', function(e) {{
        var location = this.contentWindow.location;
        var qryString = this.contentWindow.location.search;
        if ( qryString && qryString != '' && qryString.startsWith('?token-id') ) {{
            $('#{5}').val(qryString);
            window.location = ""javascript:{6}"";
        }} else {{
            if ( $('#{15}').val() == 'true' ) {{
                $('#updateProgress').show();
                var src = $('#{4}').val();
                var $form = $('#iframeStep2').contents().find('#Step2Form');
                $form.attr('action', src );
                $form.submit();
                $('#updateProgress').hide();
            }}
        }}
    }});
";
            string script = string.Format(
                scriptFormat,
                divCCPaymentInfo.ClientID,      // {0}
                hfPaymentTab.ClientID,          // {1}
                oneTimeFrequencyId,             // {2}
                GlobalAttributesCache.Value( "CurrencySymbol" ), // {3)
                hfStep2Url.ClientID,            // {4}
                hfStep2ReturnQueryString.ClientID,   // {5}
                this.Page.ClientScript.GetPostBackEventReference( lbStep2Return, "" ), // {6}
                this.BlockValidationGroup,      // {7}
                txtCreditCard.ClientID,         // {8}
                mypExpiration.ClientID,         // {9}
                txtCVV.ClientID,                // {10}
                txtAccountName.ClientID,        // {11}
                txtAccountNumber.ClientID,      // {12}
                txtRoutingNumber.ClientID,      // {13}
                rblAccountType.ClientID,        // {14}
                hfStep2AutoSubmit.ClientID,     // {15}
                "",      // {16}
                acBillingAddress.ClientID,      // {17}
                txtCardFirstName.ClientID,      // {18}
                txtCardLastName.ClientID,       // {19}
                txtCardName.ClientID,           // {20}
                rblSavedAccount.ClientID,       // {21}
                divNewPayment.ClientID         // {22}
            );

            ScriptManager.RegisterStartupScript( upPayment, this.GetType(), "giving-profile", script, true );
        }

        #endregion

        #endregion

        #region Helper Classes

        /// <summary>
        /// Lightweight object for each contribution item
        /// </summary>
        [Serializable]
        protected class AccountItem
        {
            public int Id { get; set; }

            public int Order { get; set; }

            public string Name { get; set; }

            public int? CampusId { get; set; }

            public decimal Amount { get; set; }

            public string PublicName { get; set; }

            public string AmountFormatted
            {
                get
                {
                    return Amount > 0 ? Amount.ToString( "F2" ) : string.Empty;
                }
            }

            public AccountItem( int id, int order, string name, int? campusId, string publicName )
            {
                Id = id;
                Order = order;
                Name = name;
                CampusId = campusId;
                PublicName = publicName;
            }
        }

        #endregion

        protected void cbChangePaymentMethod_CheckedChanged( object sender, EventArgs e )
        {
            SetPage( 1 );
        }
    }
}