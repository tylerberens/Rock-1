using System.Web.Http;
using System.Web.Http.Controllers;
using Rock.Data;
using Rock.Rest.Filters;

namespace Rock.Rest.Controllers.RestBlocks.Cms
{

    /// <summary>
    /// HTML Content Rest Block
    /// </summary>
    /// <seealso cref="Rock.Rest.Controllers.RestBlocks.RestBlockBase" />
    [RoutePrefix( "api/RestBlocks/Cms/HtmlContent/{blockId}" )]
    public class HtmlContentController : RestBlockBase
    {
        /// <summary>
        /// Contents the specified block identifier.
        /// </summary>
        /// <param name="blockId">The block identifier.</param>
        /// <returns></returns>
        [HttpGet]
        [System.Web.Http.Route( "Content" )]
        public string Content( int blockId )
        {
            int cacheDuration = GetBlockSetting( "CacheDuration" ).AsInteger();

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null, this.CurrentPerson );
            mergeFields.Add( "CurrentPage", this.Page );
            mergeFields.Add( "RockVersion", Rock.VersionInfo.VersionInfo.GetRockProductVersionNumber() );
            mergeFields.Add( "CurrentPersonCanEdit", this.UserCanEdit );
            mergeFields.Add( "CurrentPersonCanAdministrate", this.UserCanAdministrate );

            // mergeFields.Add( "CurrentBrowser", this.RockPage.BrowserClient );

            var htmlContentService = new Model.HtmlContentService( new RockContext() );
            var content = htmlContentService.GetActiveContent( this.BlockId, "" );

            var html = content.Content.ResolveMergeFields( mergeFields, GetBlockSetting( "EnabledLavaCommands" ) );

            return $"{Block.PreHtml}{html}{Block.PostHtml}";            ;
        }
    }
}
