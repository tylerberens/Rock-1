using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Rock.Attribute;
using Rock.Mobile.Common;
using Rock.Mobile.Common.Enums;
using Rock.Model;
using Rock.Rest.Filters;
using Rock.Web.Cache;

namespace Rock.Rest.Controllers
{
    /// <summary>
    /// Provides API interfaces for mobile applications to use when communicating with Rock.
    /// </summary>
    /// <seealso cref="Rock.Rest.ApiControllerBase" />
    public class MobileController : ApiControllerBase
    {
        /// <summary>
        /// Gets the launch packet.
        /// </summary>
        /// <param name="deviceData">The device data.</param>
        /// <param name="applicationId">The application (site) identifier.</param>
        /// <returns></returns>
        [Route( "api/mobile/GetLaunchPacket" )]
        [HttpPost]
        [Authenticate]
        public object GetLaunchPacket( [FromBody] DeviceData deviceData, int applicationId )
        {
            var baseUrl = GetBaseUrl();

            var launchPacket = new LaunchPackage
            {
                LatestVersionId = ( int ) ( RockDateTime.Now.ToJavascriptMilliseconds() / 1000 ),
                LatestVersionSettingsUrl = $"{baseUrl}api/mobile/GetLatestVersion?ApplicationId={applicationId}&Platform={deviceData.DevicePlatform.ConvertToInt()}"
            };

            using ( var rockContext = new Data.RockContext() )
            {
                var homePhoneTypeId = DefinedValueCache.Get( SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() ).Id;
                var mobilePhoneTypeId = DefinedValueCache.Get( SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE ).Id;

                var person = GetPerson( rockContext ) ?? new PersonService( rockContext ).Get( 1 );
                person.LoadAttributes( rockContext );

                var personAttributes = person.Attributes
                    .Select( a => a.Value )
                    .Where( a => a.Categories.Any( c => c.Name == "Mobile" ) );

                if ( person != null )
                {
                    var mobilePerson = new MobilePerson
                    {
                        FirstName = person.FirstName,
                        LastName = person.LastName,
                        Email = person.Email,
                        HomePhone = person.PhoneNumbers.Where( p => p.NumberTypeValueId == homePhoneTypeId ).Select( p => p.NumberFormatted ).FirstOrDefault(),
                        MobilePhone = person.PhoneNumbers.Where( p => p.NumberTypeValueId == mobilePhoneTypeId ).Select( p => p.NumberFormatted ).FirstOrDefault(),
                        AuthToken = null,
                        PersonAliasId = person.PrimaryAliasId.Value,
                        PhotoUrl = person.PhotoUrl,
                        SecurityGroupGuids = new List<Guid>(),
                        PersonalizationSegmentGuids = new List<Guid>(),
                        PersonGuid = person.Guid,
                        AttributeValues = GetMobileAttributeValues( person, personAttributes )
                    };

                    launchPacket.CurrentPerson = mobilePerson;
                }
            }

            return launchPacket;
        }

        /// <summary>
        /// Gets the latest version of an application. This is temporary and should probably go away at some point.
        /// </summary>
        /// <param name="applicationId">The application identifier.</param>
        /// <param name="platform">The platform.</param>
        /// <returns></returns>
        [Route( "api/mobile/GetLatestVersion" )]
        [HttpGet]
        [Authenticate]
        public object GetLatestVersion( int applicationId, DeviceType platform )
        {
            var rockContext = new Rock.Data.RockContext();
            var site = SiteCache.Get( applicationId );
            var additionalSettings = site.AdditionalSettings.FromJsonOrNull<AdditionalSettings>();

            var package = new UpdatePackage
            {
                ApplicationType = additionalSettings.ShellType ?? ShellType.Blank,
                ApplicationVersionId = ( int ) ( RockDateTime.Now.ToJavascriptMilliseconds() / 1000 ),
                CssStyles = additionalSettings?.CssStyle ?? string.Empty
            };

            package.AppearanceSettings.BarTextColor = "#ffffff";
            package.AppearanceSettings.BarBackgroundColor = "#ee7725";

            //
            // Load all the campuses.
            //
            foreach ( var cachedLayout in LayoutCache.All().Where( l => l.SiteId == site.Id ) )
            {
                var layout = new LayoutService( rockContext ).Get( cachedLayout.Id );
                var mobileLayout = new Mobile.Common.Layout
                {
                    LayoutGuid = layout.Guid,
                    Name = layout.Name,
                    LayoutXaml = platform == DeviceType.Tablet ? layout.LayoutMobileTablet : layout.LayoutMobilePhone
                };

                package.Layouts.Add( mobileLayout );
            }

            //
            // Load all the pages.
            //
            foreach ( var page in PageCache.All().Where( p => p.SiteId == site.Id ) )
            {
                var mobilePage = new Mobile.Common.Page
                {
                    LayoutGuid = page.Layout.Guid,
                    DisplayInNav = page.DisplayInNavWhen == DisplayInNavWhen.WhenAllowed,
                    Title = page.PageTitle,
                    PageGuid = page.Guid,
                    Order = page.Order,
                    ParentPageGuid = page.ParentPage?.Guid,
                    IconUrl = page.IconFileId.HasValue ? $"" : null
                };

                package.Pages.Add( mobilePage );
            }

            foreach ( var block in BlockCache.All().Where( b => b.Page != null && b.Page.SiteId == site.Id && b.BlockType.EntityTypeId.HasValue ).OrderBy( b => b.Order ) )
            {
                var blockEntityType = block.BlockType.EntityType.GetEntityType();

                if ( typeof( Rock.Blocks.IRockMobileBlockType ).IsAssignableFrom( blockEntityType ) )
                {
                    var mobileBlockEntity = ( Rock.Blocks.IRockMobileBlockType ) Activator.CreateInstance( blockEntityType );
                    mobileBlockEntity.BlockCache = block;
                    mobileBlockEntity.PageCache = block.Page;

                    var attributes = block.Attributes
                        .Select( a => a.Value );
                    //.Where( a => a.Categories.Any( c => c.Name == "custommobile" ) );

                    var mobileBlock = new Mobile.Common.Block
                    {
                        PageGuid = block.Page.Guid,
                        Zone = block.Zone,
                        BlockGuid = block.Guid,
                        BlockType = mobileBlockEntity.MobileBlockType,
                        ConfigurationValues = ( Dictionary<string, object> ) mobileBlockEntity.GetMobileConfigurationValues(),
                        Order = block.Order,
                        AttributeValues = GetMobileAttributeValues( block, attributes )
                    };

                    package.Blocks.Add( mobileBlock );
                }
            }

            foreach ( var campus in CampusCache.All().Where( c => c.IsActive ?? true ) )
            {
                var mobileCampus = new MobileCampus
                {
                    Guid = campus.Guid,
                    Name = campus.Name
                };

                if ( campus.Location != null )
                {
                    if ( campus.Location.Latitude.HasValue && campus.Location.Longitude.HasValue )
                    {
                        mobileCampus.Latitude = campus.Location.Latitude;
                        mobileCampus.Longitude = campus.Location.Longitude;
                    }

                    if ( !string.IsNullOrWhiteSpace( campus.Location.Street1 ) )
                    {
                        mobileCampus.Street1 = campus.Location.Street1;
                        mobileCampus.City = campus.Location.City;
                        mobileCampus.State = campus.Location.State;
                        mobileCampus.PostalCode = campus.Location.PostalCode;
                    }
                }

                package.Campuses.Add( mobileCampus );
            }

            return package;
        }

        /// <summary>
        /// Posts the interactions that have been queued up by the mobile client.
        /// </summary>
        /// <param name="sessions">The sessions.</param>
        /// <returns></returns>
        [Route( "api/mobile/Interactions" )]
        [HttpPost]
        [Authenticate]
        public IHttpActionResult PostInteractions( [FromBody] List<MobileInteractionSession> sessions )
        {
            var person = GetPerson();
            var ipAddress = System.Web.HttpContext.Current?.Request?.UserHostAddress;
            var appApiKey = System.Web.HttpContext.Current?.Request?.Headers?["X-Rock-Mobile-Api-Key"];

            using ( var rockContext = new Data.RockContext() )
            {
                var interactionChannelService = new InteractionChannelService( rockContext );
                var interactionComponentService = new InteractionComponentService( rockContext );
                var interactionSessionService = new InteractionSessionService( rockContext );
                var interactionService = new InteractionService( rockContext );
                var userLoginService = new UserLoginService( rockContext );
                var channelMediumTypeValue = DefinedValueCache.Get( SystemGuid.DefinedValue.INTERACTIONCHANNELTYPE_WEBSITE );
                var pageEntityTypeId = EntityTypeCache.Get( typeof( Model.Page ) ).Id;

                //
                // Check against our temporary development api key or a real api key.
                // Do we need to somehow validate this api key against a site or is this enough? -dsh
                //
                if ( appApiKey != "PUT_ME_IN_COACH!" && !userLoginService.GetByApiKey( appApiKey ).Any() )
                {
                    return StatusCode( System.Net.HttpStatusCode.Forbidden );
                }

                rockContext.WrapTransaction( () =>
                {
                    foreach ( var mobileSession in sessions )
                    {
                        var interactionGuids = mobileSession.Interactions.Select( i => i.Guid ).ToList();
                        var existingInteractionGuids = interactionService.Queryable()
                            .Where( i => interactionGuids.Contains( i.Guid ) )
                            .Select( i => i.Guid )
                            .ToList();

                        //
                        // Loop through all interactions that don't already exist and add each one.
                        //
                        foreach ( var mobileInteraction in mobileSession.Interactions.Where( i => !existingInteractionGuids.Contains( i.Guid ) ) )
                        {
                            int? interactionComponentId = null;

                            //
                            // Lookup the interaction channel, and create it if it doesn't exist
                            //
                            if ( mobileInteraction.AppId.HasValue && mobileInteraction.PageGuid.HasValue )
                            {
                                var site = SiteCache.Get( mobileInteraction.AppId.Value );
                                var page = PageCache.Get( mobileInteraction.PageGuid.Value );

                                if ( site == null || page == null )
                                {
                                    continue;
                                }

                                //
                                // Try to find an existing interaction channel.
                                //
                                var interactionChannelId = interactionChannelService.Queryable()
                                    .Where( a =>
                                        a.ChannelTypeMediumValueId == channelMediumTypeValue.Id &&
                                        a.ChannelEntityId == site.Id )
                                    .Select( a => ( int? ) a.Id )
                                    .FirstOrDefault();

                                //
                                // If not found, create one.
                                //
                                if ( !interactionChannelId.HasValue )
                                {
                                    var interactionChannel = new InteractionChannel
                                    {
                                        Name = site.Name,
                                        ChannelTypeMediumValueId = channelMediumTypeValue.Id,
                                        ChannelEntityId = site.Id,
                                        ComponentEntityTypeId = pageEntityTypeId
                                    };

                                    interactionChannelService.Add( interactionChannel );
                                    rockContext.SaveChanges();

                                    interactionChannelId = interactionChannel.Id;
                                }

                                //
                                // Get an existing or create a new component.
                                //
                                var interactionComponent = interactionComponentService.GetComponentByEntityId( interactionChannelId.Value, page.Id, page.InternalName);
                                rockContext.SaveChanges();

                                interactionComponentId = interactionComponent.Id;
                            }
                            else if ( mobileInteraction.ChannelGuid.HasValue && !string.IsNullOrWhiteSpace( mobileInteraction.ComponentName ) )
                            {
                                //
                                // Try to find an existing interaction channel.
                                //
                                var interactionChannelId = interactionChannelService.Get( mobileInteraction.ChannelGuid.Value )?.Id;

                                //
                                // If not found, skip this interaction.
                                //
                                if ( !interactionChannelId.HasValue )
                                {
                                    continue;
                                }

                                //
                                // Get an existing or create a new component.
                                //
                                var interactionComponent = interactionComponentService.GetComponentByComponentName( interactionChannelId.Value, mobileInteraction.ComponentName );
                                rockContext.SaveChanges();

                                interactionComponentId = interactionComponent.Id;
                            }
                            else
                            {
                                continue;
                            }

                            //
                            // Add the interaction
                            //
                            if ( interactionComponentId.HasValue )
                            {
                                var interaction = interactionService.CreateInteraction( interactionComponentId.Value,
                                    null,
                                    mobileInteraction.Operation,
                                    mobileInteraction.Summary,
                                    mobileInteraction.Data,
                                    person?.PrimaryAliasId,
                                    mobileInteraction.DateTime,
                                    mobileSession.Application,
                                    mobileSession.OperatingSystem,
                                    mobileSession.ClientType,
                                    null,
                                    ipAddress,
                                    mobileSession.Guid );

                                interaction.Guid = mobileInteraction.Guid;
                                interactionService.Add( interaction );
                                rockContext.SaveChanges();
                            }
                        }
                    }
                } );
            }

            return Ok();
        }

        /// <summary>
        /// Gets the mobile attribute values.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="attributes">The attributes.</param>
        /// <returns></returns>
        private Dictionary<string, MobileAttributeValue> GetMobileAttributeValues( IHasAttributes entity, IEnumerable<AttributeCache> attributes )
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

        /// <summary>
        /// Gets the base URL.
        /// </summary>
        /// <returns></returns>
        private string GetBaseUrl()
        {
            if ( Request.Headers.Contains( "X-Forwarded-Host" ) && Request.Headers.Contains( "X-Forwarded-Proto" ) && Request.Headers.Contains( "X-Forwarded-Port" ) )
            {
                var proto = Request.GetHeader( "X-Forwarded-Proto" );
                var host = Request.GetHeader( "X-Forwarded-Host" );

                return $"{proto}://{host}/";
            }
            else
            {
                var url = GlobalAttributesCache.Value( "PublicApplicationRoot" );

                if ( !url.EndsWith( "/", StringComparison.CurrentCultureIgnoreCase ) )
                {
                    url += "/";
                }

                return url;
            }
        }

        #region Support Classes
#pragma warning disable 1591

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

            public int? ApiKeyId { get; set; }
        }

        public enum TabLocation
        {
            Top = 0,
            Bottom = 1,
        }

#pragma warning restore 1591
        #endregion
    }
}
