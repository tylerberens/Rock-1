using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace Rock.Rest.Controllers.RestBlocks
{
    /// <summary>
    /// dd
    /// </summary>
    /// <seealso cref="System.Web.Http.ApiController" />
    public class RestBlockBase : System.Web.Http.ApiController
    {
        #region Properties
        /// <summary>
        /// Gets or sets the block id.
        /// </summary>
        /// <value>
        /// The block id.
        /// </value>
        public int BlockId { get; set; }

        /// <summary>
        /// Gets or sets the current person.
        /// </summary>
        /// <value>
        /// The current person.
        /// </value>
        public Person CurrentPerson {
            get
            {
                if ( _currentPerson == null )
                {
                    _currentPerson = GetPerson();
                }

                return _currentPerson;
            }
        }
        Person _currentPerson = null;

        /// <summary>
        /// Gets the current person alias.
        /// </summary>
        /// <value>
        /// The current person alias.
        /// </value>
        public PersonAlias CurrentPersonAlias
        {
            get
            {
                return this.CurrentPerson?.PrimaryAlias;
            }
        }

        /// <summary>
        /// Gets the current person alias id.
        /// </summary>
        /// <value>
        /// The current person alias id.
        /// </value>
        public int? CurrentPersonAliasId
        {
            get
            {
                return this.CurrentPersonAlias?.Id;
            }
        }

        /// <summary>
        /// Gets the current user.
        /// </summary>
        /// <value>
        /// The current user.
        /// </value>
        public UserLogin CurrentUser
        {
            get
            {
                return this.CurrentPerson?.Users?.FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets the block cache.
        /// </summary>
        /// <value>
        /// The block cache.
        /// </value>
        public BlockCache Block
        {
            get
            {
                if ( _blockCache == null )
                {
                    _blockCache = BlockCache.Get( this.BlockId );
                }
                return _blockCache;
            }
        }
        private BlockCache _blockCache = null;

        /// <summary>
        /// Gets the page.
        /// </summary>
        /// <value>
        /// The page.
        /// </value>
        public PageCache Page
        {
            get
            {
                return this.Block?.Page;
            }
        }

        /// <summary>
        /// Gets a value indicating whether user can view.
        /// </summary>
        /// <value>
        ///   <c>true</c> if user can view; otherwise, <c>false</c>.
        /// </value>
        public bool UserCanView
        {
            get
            {
                return Block.IsAuthorized( Security.Authorization.VIEW, CurrentPerson );
            }
        }

        /// <summary>
        /// Gets a value indicating whether user can edit.
        /// </summary>
        /// <value>
        ///   <c>true</c> if user can edit; otherwise, <c>false</c>.
        /// </value>
        public bool UserCanEdit
        {
            get
            {
                return Block.IsAuthorized( Security.Authorization.EDIT, CurrentPerson );
            }
        }

        /// <summary>
        /// Gets a value indicating whether user can administrate.
        /// </summary>
        /// <value>
        ///   <c>true</c> if user can administrate; otherwise, <c>false</c>.
        /// </value>
        public bool UserCanAdministrate
        {
            get
            {
                return Block.IsAuthorized( Security.Authorization.ADMINISTRATE, CurrentPerson );
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Initializes the <see cref="T:System.Web.Http.ApiController" /> instance with the specified controllerContext.
        /// </summary>
        /// <param name="controllerContext">The <see cref="T:System.Web.Http.Controllers.HttpControllerContext" /> object that is used for the initialization.</param>
        protected override void Initialize( HttpControllerContext controllerContext )
        {
            var segments = controllerContext?.Request?.RequestUri?.Segments;
            int blockId = 0;

            // Get the block id which will be the first segment of the uri that is all numeric
            if ( segments != null )
            {
                foreach( var segment in segments )
                {
                    if ( int.TryParse( segment.Replace("/",""), out blockId ) )
                    {
                        this.BlockId = blockId;
                        break;
                    } 
                }
            }

            // Ensure that the user has rights to view this block
            if ( !UserCanView )
            {
                var msg = new HttpResponseMessage( HttpStatusCode.Unauthorized ) { ReasonPhrase = "You are not authorized to view this block." };
                throw new HttpResponseException( msg );
            }

            base.Initialize( controllerContext );
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Gets the person.
        /// </summary>
        /// <returns></returns>
        protected Rock.Model.Person GetPerson()
        {
            /* in a webrequest there's no sharing of the request so this would never be loaded
            if ( controllerContext.Request.Properties.Keys.Contains( "Person" ) )
            {
                return controllerContext.Request.Properties["Person"] as Person;
            }*/

            var principal = RequestContext.Principal;
            if ( principal != null && principal.Identity != null )
            {
                if ( principal.Identity.Name.StartsWith( "rckipid=" ) )
                {
                    var personService = new Model.PersonService( new RockContext() );
                    Rock.Model.Person impersonatedPerson = personService.GetByImpersonationToken( principal.Identity.Name.Substring( 8 ), false, null );
                    if ( impersonatedPerson != null )
                    {
                        return impersonatedPerson;
                    }
                }
                else
                {
                    var userLoginService = new Rock.Model.UserLoginService( new RockContext() );
                    var userLogin = userLoginService.GetByUserName( principal.Identity.Name );

                    if ( userLogin != null )
                    {
                        var person = userLogin.Person;
                        //Request.Properties.Add( "Person", person ); in a webrequest there's no sharing of the request so no need to add the property
                        return userLogin.Person;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the block setting.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        protected string GetBlockSetting( string key )
        {
            if ( Block != null )
            {
                return Block.GetAttributeValue( key );
            }

            return null;
        }
        #endregion
    }
}
