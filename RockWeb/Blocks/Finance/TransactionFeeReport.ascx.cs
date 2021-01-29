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
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Blocks.Finance
{
    /// <summary>
    /// </summary>
    [DisplayName( "Transaction Fee Report" )]
    [Category( "Finance" )]
    [Description( "Block that reports transaction fees." )]

    public partial class TransactionFeeReport : RockBlock
    {
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

            if ( !Page.IsPostBack )
            {
                // set default date range
                srpFilterDates.SlidingDateRangeMode = Rock.Web.UI.Controls.SlidingDateRangePicker.SlidingDateRangeType.Last;
                srpFilterDates.TimeUnit = Rock.Web.UI.Controls.SlidingDateRangePicker.TimeUnitType.Month;
                srpFilterDates.NumberOfTimeUnits = 3;
                var delimitedValues = srpFilterDates.DelimitedValues;


                ShowReportOutput();
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
            this.NavigateToCurrentPageReference();
        }

        /// <summary>
        /// Handles the Click event of the bbtnApply control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void bbtnApply_Click( object sender, EventArgs e )
        {
            ShowReportOutput();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Shows the report output.
        /// </summary>
        public void ShowReportOutput()
        {
            var rockContext = new RockContext();
            var financialTransactionDetailService = new FinancialTransactionDetailService( rockContext );

            var qry = financialTransactionDetailService.Queryable();

            var startDateTime = srpFilterDates.SelectedDateRange.Start;
            var endDateTime = srpFilterDates.SelectedDateRange.End;

            qry = qry.Where( a => a.Transaction.TransactionDateTime >= startDateTime && a.Transaction.TransactionDateTime < endDateTime );

            var selectedAccountIds = apAccounts.SelectedIds;
            if ( selectedAccountIds.Any() )
            {
                if ( selectedAccountIds.Count() == 1 )
                {
                    var accountId = selectedAccountIds[0];
                    qry = qry.Where( a => a.AccountId == accountId );
                }
                else
                {
                    qry = qry.Where( a => selectedAccountIds.Contains( a.AccountId ) );
                }
            }

            var totals = qry.Select( a => new { a.FeeCoverageAmount, a.TransactionId, a.Transaction.FinancialPaymentDetail.CurrencyTypeValueId } );
            var transactionFeeCoverageList = totals.ToList();

            var totalsByTransactionId = transactionFeeCoverageList
                .GroupBy( a => a.TransactionId )
                .Select( a => new
                {
                    TransactionId = a.Key,
                    // There is only only one currency per transaction, so FirstOrDefault works here
                    CurrencyTypeValueId = a.FirstOrDefault().CurrencyTypeValueId,
                    FeeCoverageAmount = a.Sum( x => x.FeeCoverageAmount ?? 0.00M ),
                } );

            var currencyTypeIdCreditCard = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CREDIT_CARD.AsGuid() );
            var currencyTypeIdACH = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_ACH.AsGuid() );

            var creditCardTransactions = totalsByTransactionId.Where( a => a.CurrencyTypeValueId == currencyTypeIdCreditCard ).ToList();
            var achTransactions = totalsByTransactionId.Where( a => a.CurrencyTypeValueId == currencyTypeIdACH ).ToList();

            var achCount = achTransactions.Count();
            var achFeeCoverageTotal = achTransactions.Sum( a => a.FeeCoverageAmount );
            lACHFeeCoverageAmount.Text = achFeeCoverageTotal.FormatAsCurrency();
            lACHFeeCoverageCount.Text = string.Format( "{0:N0} Transactions", achCount );

            var creditCardCount = creditCardTransactions.Count();
            var creditCardFeeCoverageTotal = creditCardTransactions.Sum( a => a.FeeCoverageAmount );
            lCreditCardFeeCoverageAmount.Text = creditCardFeeCoverageTotal.FormatAsCurrency();
            lCreditCardFeeCoverageCount.Text = string.Format( "{0:N0} Transactions", creditCardCount );


            lTotalFeeCoverageAmount.Text = ( achFeeCoverageTotal + creditCardFeeCoverageTotal ).FormatAsCurrency();
            lTotalFeeCoverageCount.Text = string.Format( "{0:N0} Transactions", achCount + creditCardCount );
        }

        #endregion
    }
}