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
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Web;
using Rock.Web.Cache;

namespace Rock.Security.ExternalAuthentication
{
    /// <summary>
    /// Authenticates a username/password using a phone number and pin
    /// </summary>
    [Description( "SMS Authentication Provider" )]
    [Export( typeof( AuthenticationComponent ) )]
    [ExportMetadata( "ComponentName", "SMS Authentication" )]

    #region Attributes

    [LinkedPage(
        "SMS Login Page",
        Key = AttributeKeys.SMSLoginPage,
        Description = "Page that contains the SMS login block.",
        DefaultValue = "C137E7F2-DDB6-404F-AFD3-4D741E0DA43A",
        Order = 0 )]

    [IntegerField(
        "BCrypt Cost Factor",
        Key = AttributeKeys.BCryptCostFactor,
        Description = "The higher this number, the more secure BCrypt can be. However it also will be slower.",
        DefaultIntegerValue = 11,
        IsRequired = true,
        Order = 1 )]

    [DefinedValueField(
        "From",
        Key = AttributeKeys.From,
        Description = "The number to originate message from (configured under Admin Tools > Communications > SMS From Values).",
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.COMMUNICATION_SMS_FROM,
        IsRequired = true,
        Order = 2 )]

    [TextField(
        "Message",
        Key = AttributeKeys.Message,
        Description = "Message that will be sent along with the login code.",
        DefaultValue = "Use {{ password }} to log in to {{ 'Global' | Attribute:'OrganizationName' }}.",
        IsRequired = true,
        Order = 3 )]

    [IntegerField(
        "Minimum Age",
        Key = AttributeKeys.MinimumAge,
        Description = "Minimum age which someone is allowed to log in.",
        DefaultIntegerValue = 13,
        IsRequired = true,
        Order = 4 )]

    #endregion Attributes

    public class SMSAuthentication : AuthenticationComponent
    {
        private static class AttributeKeys
        {
            public const string SMSLoginPage = "SMSLoginPage";
            public const string BCryptCostFactor = "BCryptCostFactor";
            public const string From = "From";
            public const string Message = "Message";
            public const string MinimumAge = "MinimumAge";
        }

        #region Override Methods
        /// <summary>
        /// Gets the type of the service.
        /// </summary>
        /// <value>
        /// The type of the service.
        /// </value>
        public override AuthenticationServiceType ServiceType
        {
            get { return AuthenticationServiceType.External; }
        }

        /// <summary>
        /// Determines if user is directed to another site (i.e. Facebook, Gmail, Twitter, etc) to confirm approval of using
        /// that site's credentials for authentication.
        /// </summary>
        /// <value>
        /// The requires remote authentication.
        /// </value>
        public override bool RequiresRemoteAuthentication
        {
            get { return true; }
        }

        /// <summary>
        /// Initializes the class.
        /// </summary>
        static SMSAuthentication()
        {
        }

        /// <summary>
        /// Authenticates the specified user name.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <returns>True if the authentication attempt is successful.</returns>
        public override Boolean Authenticate( UserLogin user, string password )
        {
            try
            {
                // Security code must only be under 30 minutes old
                if ( user.FailedPasswordAttemptWindowStartDateTime == null
                    || user.FailedPasswordAttemptWindowStartDateTime < Rock.RockDateTime.Now.AddMinutes( -30 ) )
                {
                    ExceptionLogService.LogException( new TimeoutException( $"SMS Authentication:  Security code timed out (UserName: {user.UserName})." ) );
                    return false;
                }

                //Limit to 5 attempts
                if ( user.FailedPasswordAttemptCount > 4 )
                {
                    ExceptionLogService.LogException( new Exception( $"SMS Authentication:  Maximum login attempts exceeded (UserName: {user.UserName})." ) );
                    return false;
                }

                using ( var rockContext = new RockContext() )
                {
                    var userLoginService = new UserLoginService( rockContext );
                    var userLogin = userLoginService.Get( user.Id );
                    if ( userLogin == null )
                    {
                        throw new Exception( $"SMS Authentication:  Internal error occurred while attempting to load user with Id {user.Id}." );
                    }

                    // If the security code is blank or doesn't match the password, authentication should fail.
                    bool isAuthenticated = !string.IsNullOrWhiteSpace( user.Password ) && AuthenticateBcrypt( user, password );
                    if ( isAuthenticated )
                    {
                        // Reset the user's failed attempt counts.
                        user.FailedPasswordAttemptCount = 0;
                    }
                    else
                    {
                        // Add one failed attempt.
                        userLogin.FailedPasswordAttemptCount++;
                    }
                    rockContext.SaveChanges();
                    return isAuthenticated;
                }
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex );
                return false;
            }

        }

        /// <summary>
        /// Encodes the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override String EncodePassword( UserLogin user, string password )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Authenticates the user based on a request from a third-party provider.  Will set the username and returnUrl values.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns></returns>
        public override bool Authenticate( HttpRequest request, out string userName, out string returnUrl )
        {
            userName = string.Empty;
            returnUrl = request.QueryString["State"];
            return false;
        }

        /// <summary>
        /// Generates the login URL.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>A <see cref="Uri"/> that represents the login URL.</returns>
        public override Uri GenerateLoginUrl( HttpRequest request )
        {
            var uri = new Uri( request.Url.ToString() );
            var pageReference = new PageReference( GetAttributeValue( AttributeKeys.SMSLoginPage ), null );
            var host = uri.Scheme + "://" + uri.GetComponents( UriComponents.HostAndPort, UriFormat.UriEscaped );
            return new Uri( host + pageReference.BuildUrl() + uri.Query );
        }

        /// <summary>
        /// Tests the <see cref="HttpRequest"/> to determine if authentication should be tested by this
        /// authentication provider.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>False.</returns>
        public override bool IsReturningFromAuthentication( HttpRequest request )
        {
            return false;
        }

        /// <summary>
        /// Gets the URL of an image that should be displayed.
        /// </summary>
        /// <returns></returns>
        public override string ImageUrl()
        {
            return "";
        }

        /// <summary>
        /// Gets a value indicating whether [supports change password].
        /// </summary>
        /// <value>
        /// <c>true</c> if [supports change password]; otherwise, <c>false</c>.
        /// </value>
        public override bool SupportsChangePassword
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Changes the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="oldPassword">The old password.</param>
        /// <param name="newPassword">The new password.</param>
        /// <param name="warningMessage">The warning message.</param>
        /// <returns>
        /// A <see cref="Boolean" /> value that indicates if the password change was successful. <c>true</c> if successful; otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="NotImplementedException">Cannot change password on external service type.</exception>
        public override bool ChangePassword( UserLogin user, string oldPassword, string newPassword, out string warningMessage )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <exception cref="NotImplementedException"></exception>
        public override void SetPassword( UserLogin user, string password )
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Creates the user login for the SMS message and sends the text message with the code.
        /// </summary>
        /// <param name="phoneNumber">The phone number.</param>
        /// <returns>True if an SMS message was generated.</returns>
        public bool SendSMSAuthentication( string phoneNumber )
        {
            using ( var rockContext = new RockContext() )
            {
                string error;
                var person = GetNumberOwner( phoneNumber, rockContext, out error );
                if ( person == null )
                {
                    return false;
                }

                UserLoginService userLoginService = new UserLoginService( rockContext );
                var userLogin = userLoginService.Queryable()
                    .Where( u => u.UserName == ( "SMS_" + person.Id.ToString() ) )
                    .FirstOrDefault();

                //Create user login if does not exist
                if ( userLogin == null )
                {
                    var entityTypeId = EntityTypeCache.Get( "Rock.Security.ExternalAuthentication.SMSAuthentication" ).Id;

                    userLogin = new UserLogin()
                    {
                        UserName = "SMS_" + person.Id.ToString(),
                        EntityTypeId = entityTypeId,
                    };
                    userLoginService.Add( userLogin );
                }

                //Update user login
                userLogin.PersonId = person.Id;
                userLogin.LastPasswordChangedDateTime = Rock.RockDateTime.Now;
                userLogin.FailedPasswordAttemptWindowStartDateTime = Rock.RockDateTime.Now;
                //userLogin.FailedPasswordAttemptCount = 0;
                userLogin.IsConfirmed = true;

                var password = new Random().Next( 100000, 999999 ).ToString();
                userLogin.Password = EncodeBcrypt( password );
                rockContext.SaveChanges();


                var mergeFields = new Dictionary<string, object> { { "password", password } };
                var recipient = new RockSMSMessageRecipient( person, phoneNumber, mergeFields );
                var recipients = new List<RockSMSMessageRecipient> { recipient };

                var smsMessage = new RockSMSMessage
                {
                    CreateCommunicationRecord = false
                };
                smsMessage.SetRecipients( recipients );

                // Get the From value
                Guid? fromGuid = GetAttributeValue( AttributeKeys.From ).AsGuidOrNull();
                if ( fromGuid.HasValue )
                {
                    var fromValue = DefinedValueCache.Get( fromGuid.Value, rockContext );
                    if ( fromValue != null )
                    {
                        smsMessage.FromNumber = DefinedValueCache.Get( fromValue.Id, rockContext );
                    }
                }

                smsMessage.AdditionalMergeFields = mergeFields;
                smsMessage.Message = GetAttributeValue( AttributeKeys.Message );

                var request = HttpContext.Current.Request.RequestContext.HttpContext.Request;
                var ipAddress = Rock.Utility.WebRequestHelper.GetClientIpAddress( request );

                //Reserve items rate limits the text messages
                if ( SMSAuthRateLimiter.ReserveItems( ipAddress, phoneNumber ) )
                {
                    var delay = SMSAuthRateLimiter.GetDelay( ipAddress, phoneNumber );
                    Task.Run( () => { SendSMS( smsMessage, ipAddress, phoneNumber, delay ); } );
                }
                else
                {
                    ExceptionLogService.LogException( new Exception( string.Format( "Rate limiting reached for SMS authentication: IP: {0} PhoneNumber: {1}", ipAddress, phoneNumber ) ) );
                }

                return true;
            }
        }

        /// <summary>
        /// Finds the owner of the phone number dependant on only one being returned
        /// </summary>
        /// <param name="phoneNumber">The phone number.</param>
        /// <param name="rockContext">The <see cref="RockContext"/>.</param>
        /// <returns>The Person record associated with the phone number.</returns>
        public Person GetNumberOwner( string phoneNumber, RockContext rockContext, out string error )
        {
            error = string.Empty;
            var phoneNumberService = new PhoneNumberService( rockContext );
            var numberOwners = phoneNumberService.Queryable()
                .Where( pn => pn.Number == phoneNumber )
                .Select( pn => pn.Person )
                .DistinctBy( p => p.Id )
                .ToList();

            if ( numberOwners.Count == 0 || numberOwners.Count > 1 )
            {
                error = "There was an issue with your request";
                return null;
            }

            var person = numberOwners.FirstOrDefault();
            if ( person.IsDeceased )
            {
                error = "There was an issue with your request";
                return null;
            }

            var minimumAge = GetAttributeValue( AttributeKeys.MinimumAge ).AsInteger();
            if ( minimumAge != 0 )
            {
                if ( person.Age == null )
                {
                    error = string.Format( "We could not determine your age. You must be at least {0} years old to log in.", minimumAge );
                    return null;
                }
                if ( person.Age.Value < minimumAge )
                {
                    error = string.Format( "You must be at least {0} years old to log in.", minimumAge );
                    return null;
                }
            }

            return person;
        }

        /// <summary>
        /// Hashes the password.
        /// </summary>
        /// <param name="password">The password.</param>
        /// <returns>The hashed password.</returns>
        private string EncodeBcrypt( string password )
        {
            var workFactor = ( GetAttributeValue( AttributeKeys.BCryptCostFactor ).AsIntegerOrNull() ?? 11 );
            var salt = BCrypt.Net.BCrypt.GenerateSalt( workFactor );
            return BCrypt.Net.BCrypt.HashPassword( password, salt );
        }

        /// <summary>
        /// Tests the bcrypt hash
        /// </summary>
        /// <param name="user">The <see cref="UserLogin"/>./param>
        /// <param name="password">The password.</param>
        /// <returns>True if the authentication attempt was successful.</returns>
        private bool AuthenticateBcrypt( UserLogin user, string password )
        {
            try
            {
                var hash = user.Password;
                var currentCost = hash.Substring( 4, 2 ).AsInteger();
                return BCrypt.Net.BCrypt.Verify( password, hash );
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sends the SMS message so the user can be logged in
        /// </summary>
        /// <param name="smsMessage"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="ipAddress"></param>
        /// <param name="delay"></param>
        public async void SendSMS( RockSMSMessage smsMessage, string phoneNumber, string ipAddress, double delay )
        {
            await Task.Delay( ( int ) delay );
            try
            {
                smsMessage.Send();
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex );
            }
            SMSAuthRateLimiter.ReleaseItems( ipAddress, phoneNumber );
        }
        #endregion
    }
    #region Helper Classes

    /// <summary>
    /// Static class for rate limiting of SMS messages by IP address and phone number
    /// </summary>
    public static class SMSAuthRateLimiter
    {
        private static object _delayLock = new object();
        private static object _reserveLock = new object();
        private static List<SMSAuthAttempt> AuthAttempts { get; set; }
        private static List<string> ActiveItems { get; set; }

        /// <summary>
        /// Constructor method
        /// </summary>
        static SMSAuthRateLimiter()
        {
            AuthAttempts = new List<SMSAuthAttempt>();
            ActiveItems = new List<string>();
        }

        /// <summary>
        /// Holds IP addressses and phone numbers in reserve
        /// Keeps from flooding SMS messages
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public static bool ReserveItems( string ip, string phoneNumber )
        {
            lock ( _reserveLock )
            {
                if ( ActiveItems.Contains( ip ) || ActiveItems.Contains( phoneNumber ) )
                {
                    //If the list already contains these two items
                    return false;
                }
                ActiveItems.Add( ip );
                ActiveItems.Add( phoneNumber );

                return true;
            }
        }

        /// <summary>
        /// Releases IP addresses and phone numbers from reserve
        /// Allows users to have access to send SMS logins again
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="phoneNumber"></param>
        public static void ReleaseItems( string ip, string phoneNumber )
        {
            lock ( _delayLock )
            {
                if ( ActiveItems.Contains( ip ) )
                {
                    ActiveItems.Remove( ip );
                }
                if ( ActiveItems.Contains( phoneNumber ) )
                {
                    ActiveItems.Remove( phoneNumber );
                }
            }
        }


        /// <summary>
        /// Calculates a delay for sending the next SMS record
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public static double GetDelay( string ip, string phoneNumber )
        {
            lock ( _delayLock )
            {
                SMSAuthRateLimiter.AuthAttempts.Add( new SMSAuthAttempt( ip ) );
                SMSAuthRateLimiter.AuthAttempts.Add( new SMSAuthAttempt( phoneNumber ) );
                var hourAgo = Rock.RockDateTime.Now.AddHours( -1 );
                var toRemove = new List<SMSAuthAttempt>();
                toRemove.AddRange( AuthAttempts.Where( r => r.DateTime < hourAgo ).ToList() );
                foreach ( var item in toRemove )
                {
                    AuthAttempts.Remove( item );
                }
                double delay = 2; //2ms
                                  //Slow down exponentially.
                delay = Math.Pow( delay, AuthAttempts.Where( r => r.Value == ip || r.Value == phoneNumber ).Count() );

                return delay;
            }
        }
    }

    /// <summary>
    /// Helper class for rate limiting
    /// </summary>
    public class SMSAuthAttempt
    {
        /// <summary>
        /// Constructor method
        /// </summary>
        /// <param name="value"></param>
        public SMSAuthAttempt( string value )
        {
            Value = value;
            DateTime = Rock.RockDateTime.Now;
        }

        /// <summary>
        /// The value of the item being reserved
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Time at which the item was reserved
        /// </summary>
        public DateTime DateTime { get; set; }
    }
    #endregion
}