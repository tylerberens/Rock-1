// <copyright>
// Copyright 2013 by the Spark Development Network
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
using System.IO;
using Rock;
using Rock.MyWell;
using System.Net;
using System.Web;
using Rock.Model;
using Rock.Data;
using System.Linq;
using System.Data.Entity;
using Rock.Security;
using Rock.Financial;

namespace RockWeb.Webhooks
{
    /// <summary>
    /// </summary>
    public class MyWellCardSync : IHttpHandler
    {
        public void ProcessRequest( HttpContext context )
        {
            // see https://sandbox.gotnpgateway.com/docs/webhooks/#core-webhook-response-format for example payload
            HttpRequest request = context.Request;
            var response = context.Response;
            response.ContentType = "text/plain";


            // Signature https://sandbox.gotnpgateway.com/docs/webhooks/#security ??
            var signature = request.Headers["Signature"];

            string postedData = string.Empty;
            using ( var reader = new StreamReader( request.InputStream ) )
            {
                postedData = reader.ReadToEnd();
            }

            var cardSyncWebhookResponse = postedData.FromJsonOrNull<CardSyncWebhookResponse>();

            if ( cardSyncWebhookResponse == null )
            {
                response.StatusCode = ( int ) HttpStatusCode.BadRequest;
                return;
            }

            var paymentMethodData = cardSyncWebhookResponse.PaymentMethodData;

            if ( paymentMethodData == null )
            {
                response.StatusCode = ( int ) HttpStatusCode.BadRequest;
                return;
            }

            var rockContext = new RockContext();
            FinancialPersonSavedAccountService financialPersonSavedAccountService = new FinancialPersonSavedAccountService( rockContext );
            var financialPersonSavedAccountQuery = financialPersonSavedAccountService.Queryable()
                .Where( a => a.GatewayPersonIdentifier == paymentMethodData.RecordId );

            var savedAccounts = financialPersonSavedAccountQuery.Include( a => a.FinancialPaymentDetail ).ToList();

            foreach ( var savedAccount in savedAccounts )
            {
                var financialPaymentDetail = savedAccount.FinancialPaymentDetail;
                if ( financialPaymentDetail == null )
                {
                    // shouldn't happen
                    continue;
                }

                financialPaymentDetail.AccountNumberMasked = paymentMethodData.MaskedNumber;

                if ( paymentMethodData.ExpirationDate.IsNotNullOrWhiteSpace() && paymentMethodData.ExpirationDate.Length == 5 )
                {
                    financialPaymentDetail.ExpirationMonthEncrypted = Encryption.EncryptString( paymentMethodData.ExpirationDate.Substring( 0, 2 ) );
                    financialPaymentDetail.ExpirationYearEncrypted = Encryption.EncryptString( paymentMethodData.ExpirationDate.Substring( 3, 2 ) );
                }

                /*
                // See if we can figure it out from the CC Type (Amex, Visa, etc)
                var creditCardTypeValue = CreditCardPaymentInfo.GetCreditCardTypeFromName( paymentMethodData.CardType );
                if ( creditCardTypeValue == null )
                {
                    // GetCreditCardTypeFromName should have worked, but just in case, see if we can figure it out from the MaskedCard using RegEx
                    creditCardTypeValue = CreditCardPaymentInfo.GetCreditCardTypeFromCreditCardNumber( paymentMethodData.masked_number );
                }

                financialPaymentDetail.CreditCardTypeValueId = creditCardTypeValue.Id;

                */
            }


            // todo

            response.StatusCode = ( int ) HttpStatusCode.OK;
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}