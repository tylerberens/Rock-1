using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Rock.Attribute;
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
        public object GetLaunchPacket( [FromBody] IDictionary<string, object> deviceData, int applicationId )
        {
            var baseUrl = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Authority}";
            var platform = deviceData.ContainsKey( "DeviceType" ) ? ( int ) ( long ) deviceData["DeviceType"] : 0; // Phone

            var launchPacket = new Dictionary<string, object>()
            {
                { "LatestVersionId", RockDateTime.Now.ToJavascriptMilliseconds() / 1000 },
                { "LatestVersionSettingsUrl", $"{baseUrl}/api/mobile/GetLatestVersion?ApplicationId={applicationId}&Platform={platform}" }
            };

            // TODO: Set CurrentPerson

            return launchPacket;
        }

        [Route( "api/mobile/GetLatestVersion" )]
        [HttpGet]
        [Authenticate]
        public object GetLatestVersion( int applicationId, int platform )
        {
            var site = SiteCache.Get( 8 );
            var rockContext = new Rock.Data.RockContext();

            var package = new Dictionary<string, object>();
            var appearanceSettings = new Dictionary<string, object>();
            var layouts = new List<Dictionary<string, object>>();
            var pages = new List<Dictionary<string, object>>();
            var blocks = new List<Dictionary<string, object>>();

            var additionalSettings = site.AdditionalSettings.FromJsonOrNull<AdditionalSettings>();

            package.Add( "ApplicationType", 1 );
            package.Add( "ApplicationVersionId", RockDateTime.Now.ToJavascriptMilliseconds() / 1000 );
            package.Add( "AppearanceSettings", appearanceSettings );
            package.Add( "CssStyles", additionalSettings?.CssStyle ?? string.Empty );
            package.Add( "Layouts", layouts );
            package.Add( "Pages", pages );
            package.Add( "Blocks", blocks );

            appearanceSettings.Add( "BarTextColor", "#ffffff" );
            appearanceSettings.Add( "BarBackgroundColor", "#ee7725" );

            foreach ( var cachedLayout in LayoutCache.All().Where( l => l.SiteId == site.Id ) )
            {
                var layout = new LayoutService( rockContext ).Get( cachedLayout.Id );
                var mobileLayout = new Dictionary<string, object>();

                mobileLayout.Add( "LayoutGuid", layout.Guid );
                mobileLayout.Add( "Name", layout.Name );
                mobileLayout.Add( "LayoutXaml", platform == 1 ? layout.LayoutMobileTablet : layout.LayoutMobilePhone );

                layouts.Add( mobileLayout );
            }

            foreach ( var page in PageCache.All().Where( p => p.SiteId == site.Id ) )
            {
                var mobilePage = new Dictionary<string, object>();

                mobilePage.Add( "LayoutGuid", page.Layout.Guid );
                mobilePage.Add( "DisplayInNav", page.DisplayInNavWhen == DisplayInNavWhen.WhenAllowed );
                mobilePage.Add( "Title", page.PageTitle );
                mobilePage.Add( "PageGuid", page.Guid );

                pages.Add( mobilePage );
            }

            foreach ( var block in BlockCache.All().Where( b => b.Page != null && b.Page.SiteId == site.Id && b.BlockType.EntityTypeId.HasValue ).OrderBy( b => b.Order ) )
            {
                var blockEntityType = block.BlockType.EntityType.GetEntityType();

                if ( typeof( Rock.Blocks.IRockMobileBlockType ).IsAssignableFrom( blockEntityType ) )
                {
                    var mobileBlockEntity = ( Rock.Blocks.IRockMobileBlockType ) Activator.CreateInstance( blockEntityType );
                    mobileBlockEntity.BlockCache = block;
                    mobileBlockEntity.PageCache = block.Page;

                    var mobileBlock = new Dictionary<string, object>();

                    mobileBlock.Add( "PageGuid", block.Page.Guid );
                    mobileBlock.Add( "Zone", block.Zone );
                    mobileBlock.Add( "BlockGuid", block.Guid );
                    mobileBlock.Add( "BlockType", mobileBlockEntity.MobileBlockType );
                    mobileBlock.Add( "ConfigurationValues", mobileBlockEntity.GetMobileConfigurationValues() );

                    var values = block.Attributes
                        .Where( a => a.Value.Categories.Any( c => c.Name == "custommobile" ) )
                        .ToDictionary( a => a.Key, a => block.GetAttributeValue( a.Key ) );

                    mobileBlock.Add( "AttributeValues", values );

                    blocks.Add( mobileBlock );
                }
            }

            return package;
        }


        #region Support Classes

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
        }

        /// <summary>The type of application shell.</summary>
        public enum ShellType
        {
            Blank = 0,
            Flyout = 1,
            Tabbed = 2
        }

        public enum TabLocation
        {
            Top = 0,
            Bottom = 1,
        }

        #endregion
    }
}
