using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Rock.Attribute;
using Rock.Mobile.Common;
using Rock.Mobile.Common.Enums;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.Mobile
{
    public static class MobileHelper
    {
        /// <summary>
        /// Gets the base URL.
        /// </summary>
        /// <returns></returns>
        public static string GetBaseUrl()
        {
            var request = HttpContext.Current.Request;
            var headerKeys = request.Headers.AllKeys;

            if ( request.Headers.AllKeys.Contains( "X-Forwarded-Host" ) && request.Headers.AllKeys.Contains( "X-Forwarded-Proto" ) )
            {
                var proto = request.Headers["X-Forwarded-Proto"];
                var host = request.Headers["X-Forwarded-Host"];

                return $"{proto}://{host}/";
            }
            else
            {
                return $"{request.Url.Scheme}://{request.Url.Authority}/";
            }
        }

        /// <summary>
        /// Get the current site as specified by the X-Rock-App-Id header and optionally
        /// validate the X-Rock-Mobile-Api-Key against that site.
        /// </summary>
        /// <param name="validateApiKey"><c>true</c> if the X-Rock-Mobile-Api-Key header should be validated.</param>
        /// <param name="rockContext">The Rock context to use when accessing the database.</param>
        /// <returns>A SiteCache object or null if the request was not valid.</returns>
        public static SiteCache GetCurrentApplicationSite( bool validateApiKey = true, Data.RockContext rockContext = null )
        {
            var appId = HttpContext.Current?.Request?.Headers?["X-Rock-App-Id"];

            if ( !appId.AsIntegerOrNull().HasValue )
            {
                return null;
            }

            //
            // Lookup the site from the App Id.
            //
            var site = SiteCache.Get( appId.AsInteger() );
            if ( site == null )
            {
                return null;
            }

            //
            // If we have been requested to validate the Api Key then do so.
            //
            if ( validateApiKey )
            {
                var appApiKey = System.Web.HttpContext.Current?.Request?.Headers?["X-Rock-Mobile-Api-Key"];
                var additionalSettings = site.AdditionalSettings.FromJsonOrNull<AdditionalSettings>();

                //
                // Ensure we have valid site configuration.
                //
                if ( additionalSettings == null || !additionalSettings.ApiKeyId.HasValue )
                {
                    return null;
                }

                rockContext = rockContext ?? new Data.RockContext();
                var userLogin = new UserLoginService( rockContext ).GetByApiKey( appApiKey ).FirstOrDefault();

                if ( userLogin != null && userLogin.Id == additionalSettings.ApiKeyId )
                {
                    return site;
                }
#if DEBUG
                //
                // Check against our temporary development api key or a real api key.
                //
                else if ( appApiKey == "PUT_ME_IN_COACH!" )
                {
                    return site;
                }
#endif
                else
                {
                    return null;
                }
            }
            else
            {
                return site;
            }
        }

        /// <summary>
        /// Get the MobilePerson object for the specified Person.
        /// </summary>
        /// <param name="person">The person to be converted into a MobilePerson object.</param>
        /// <param name="site">The site to use for configuration data.</param>
        /// <returns>A MobilePerson object.</returns>
        public static MobilePerson GetMobilePerson( Person person, SiteCache site )
        {
            var baseUrl = GetBaseUrl();
            var homePhoneTypeId = DefinedValueCache.Get( SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() ).Id;
            var mobilePhoneTypeId = DefinedValueCache.Get( SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).Id;

            var additionalSettings = site.AdditionalSettings.FromJsonOrNull<AdditionalSettings>();

            if ( person.Attributes == null )
            {
                person.LoadAttributes();
            }

            var personAttributes = person.Attributes
                .Select( a => a.Value )
                .Where( a => a.Categories.Any( c => additionalSettings.PersonAttributeCategories.Contains( c.Id ) ) );

            return new MobilePerson
            {
                FirstName = person.FirstName,
                NickName = person.NickName,
                LastName = person.LastName,
                Gender = ( Common.Enums.Gender ) person.Gender,
                BirthDate = person.BirthDate,
                Email = person.Email,
                HomePhone = person.PhoneNumbers.Where( p => p.NumberTypeValueId == homePhoneTypeId ).Select( p => p.NumberFormatted ).FirstOrDefault(),
                MobilePhone = person.PhoneNumbers.Where( p => p.NumberTypeValueId == mobilePhoneTypeId ).Select( p => p.NumberFormatted ).FirstOrDefault(),
                HomeAddress = GetMobileAddress( person.GetHomeLocation() ),
                CampusGuid = person.GetCampus()?.Guid,
                PersonAliasId = person.PrimaryAliasId.Value,
                PhotoUrl = ( person.PhotoId.HasValue ? $"{baseUrl}{person.PhotoUrl}" : null ),
                SecurityGroupGuids = new List<Guid>(),
                PersonalizationSegmentGuids = new List<Guid>(),
                PersonGuid = person.Guid,
                AttributeValues = GetMobileAttributeValues( person, personAttributes )
            };
        }

        /// <summary>
        /// Generate an authentication token (.ROCK Cookie) for the given username.
        /// </summary>
        /// <param name="username">The username whose token should be generated for.</param>
        /// <returns>A string that represents the user's authentication token.</returns>
        public static string GetAuthenticationToken( string username )
        {
            var ticket = new System.Web.Security.FormsAuthenticationTicket( 1,
                username,
                RockDateTime.Now,
                RockDateTime.Now.Add( System.Web.Security.FormsAuthentication.Timeout ),
                true,
                false.ToString() );

            return System.Web.Security.FormsAuthentication.Encrypt( ticket );
        }

        /// <summary>
        /// Gets the mobile address.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns></returns>
        public static MobileAddress GetMobileAddress( Location location )
        {
            if ( location == null )
            {
                return null;
            }

            return new MobileAddress
            {
                Street1 = location.Street1,
                City = location.City,
                State = location.State,
                PostalCode = location.PostalCode,
                Country = location.Country
            };
        }

        /// <summary>
        /// Gets the mobile attribute values.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="attributes">The attributes.</param>
        /// <returns></returns>
        public static Dictionary<string, MobileAttributeValue> GetMobileAttributeValues( IHasAttributes entity, IEnumerable<AttributeCache> attributes )
        {
            var mobileAttributeValues = new Dictionary<string, MobileAttributeValue>();

            if ( entity.Attributes == null )
            {
                entity.LoadAttributes();
            }

            foreach ( var attribute in attributes )
            {
                var value = entity.GetAttributeValue( attribute.Key );
                var formattedValue = entity.AttributeValues.ContainsKey( attribute.Key ) ? entity.AttributeValues[attribute.Key].ValueFormatted : attribute.DefaultValueAsFormatted;

                var mobileAttributeValue = new MobileAttributeValue
                {
                    Value = value,
                    FormattedValue = formattedValue
                };

                mobileAttributeValues.AddOrReplace( attribute.Key, mobileAttributeValue );
            }

            return mobileAttributeValues;
        }

    }

    /// <summary>
    /// This class is used to store and retrieve
    /// Additional Setting for Mobile against the Site Entity
    /// </summary>
    public class AdditionalSettings
    {
        /// <summary>
        /// Gets or sets the type of the shell.
        /// </summary>
        /// <value>
        /// The type of the shell.
        /// </value>
        public ShellType? ShellType { get; set; }

        /// <summary>
        /// Gets or sets the tab location.
        /// </summary>
        /// <value>
        /// The tab location.
        /// </value>
        public TabLocation? TabLocation { get; set; }

        /// <summary>
        /// Gets or sets the CSS style.
        /// </summary>
        /// <value>
        /// The CSS style.
        /// </value>
        public string CssStyle { get; set; }

        /// <summary>
        /// Gets or sets the API key identifier.
        /// </summary>
        /// <value>
        /// The API key identifier.
        /// </value>
        public int? ApiKeyId { get; set; }

        /// <summary>
        /// Gets or sets the profile page identifier.
        /// </summary>
        /// <value>
        /// The profile page identifier.
        /// </value>
        public int? ProfilePageId { get; set; }

        /// <summary>
        /// Gets or sets the person attribute categories.
        /// </summary>
        /// <value>
        /// The person attribute categories.
        /// </value>
        public List<int> PersonAttributeCategories { get; set; } = new List<int>();
    }

    public enum TabLocation
    {
        Top = 0,
        Bottom = 1,
    }
}
