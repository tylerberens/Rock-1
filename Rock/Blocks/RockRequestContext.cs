using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;

using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Web.Cache;
using UAParser;

namespace Rock.Blocks
{
    public class RockRequestContext
    {
        #region Properties

        public virtual UserLogin CurrentUser { get; protected set; }

        public virtual Person CurrentPerson => CurrentUser?.Person;

        public virtual string RootUrlPath { get; protected set; }

        public virtual ClientInformation ClientInformation { get; protected set; }

        internal protected virtual IDictionary<string, string> PageParameters { get; set; }

        internal protected IDictionary<Type, Lazy<IEntity>> ContextEntities { get; set; }

        #endregion

        #region Constructors

        internal RockRequestContext()
        {
            PageParameters = new Dictionary<string, string>();
            ContextEntities = new Dictionary<Type, Lazy<IEntity>>();
        }

        internal RockRequestContext( HttpRequest request )
        {
            CurrentUser = UserLoginService.GetCurrentUser( false );

            var uri = new Uri( request.Url.ToString() );
            RootUrlPath = uri.Scheme + "://" + uri.GetComponents( UriComponents.HostAndPort, UriFormat.UriEscaped ) + request.ApplicationPath;

            ClientInformation = new ClientInformation( request );

            //
            // Setup the page parameters.
            //
            PageParameters = new Dictionary<string, string>();
            foreach ( var key in request.QueryString.AllKeys )
            {
                PageParameters.AddOrReplace( key, request.QueryString[key] );
            }
            foreach ( var kvp in request.RequestContext.RouteData.Values )
            {
                PageParameters.AddOrReplace( kvp.Key, kvp.Value.ToStringSafe() );
            }

            //
            // Todo: Setup the ContextEntities somehow.
            //
            ContextEntities = new Dictionary<Type, Lazy<IEntity>>();
            AddContextEntitiesFromHeaders( request.Headers.AllKeys.Select( k => new KeyValuePair<string, IEnumerable<string>>( k, request.Headers.GetValues( k ) ) ) );
        }

        internal RockRequestContext( HttpRequestMessage request )
        {
            CurrentUser = UserLoginService.GetCurrentUser( false );

            var uri = request.RequestUri;
            RootUrlPath = uri.Scheme + "://" + uri.GetComponents( UriComponents.HostAndPort, UriFormat.UriEscaped );

            ClientInformation = new ClientInformation( request );

            //
            // Setup the page parameters.
            //
            PageParameters = new Dictionary<string, string>();
            foreach ( var kvp in request.GetQueryNameValuePairs() )
            {
                PageParameters.AddOrReplace( kvp.Key, kvp.Value );
            }
            foreach ( var kvp in request.GetRouteData().Values )
            {
                PageParameters.AddOrReplace( kvp.Key, kvp.Value.ToStringSafe() );
            }

            //
            // Todo: Setup the ContextEntities somehow.
            //
            ContextEntities = new Dictionary<Type, Lazy<IEntity>>();
            AddContextEntitiesFromHeaders( request.Headers );
        }

        #endregion


        #region Methods

        protected virtual void AddContextEntitiesFromHeaders( IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers )
        {
            foreach ( var kvp in headers )
            {
                if ( kvp.Key.StartsWith( "X-ENTITYCONTEXT-", StringComparison.InvariantCultureIgnoreCase ) )
                {
                    string entityKey = kvp.Value.First();
                    var entityName = kvp.Key.Substring( 16 );
                    var type = EntityTypeCache.All().FirstOrDefault( a => a.Name == entityName )?.GetEntityType();

                    if ( type != null && entityKey.IsNotNullOrWhiteSpace() )
                    {
                        ContextEntities.AddOrReplace( type, new Lazy<IEntity>( () =>
                        {
                            int? entityId = entityKey.AsIntegerOrNull();
                            if ( entityId.HasValue )
                            {
                                return Reflection.GetIEntityForEntityType( type, entityId.Value );
                            }

                            Guid? entityGuid = entityKey.AsGuidOrNull();
                            if ( entityGuid.HasValue )
                            {
                                return Reflection.GetIEntityForEntityType( type, entityGuid.Value );
                            }

                            return null;
                        } ) );
                    }

                }
            }
        }

        public virtual string GetPageParameter( string name )
        {
            if ( PageParameters.ContainsKey( name ) )
            {
                return PageParameters[name];
            }

            return string.Empty;
        }

        public virtual T GetContextEntity<T>() where T : IEntity
        {
            if ( ContextEntities.ContainsKey( typeof(T) ) )
            {
                return ( T ) ContextEntities[typeof( T )].Value;
            }

            return default( T );
        }

        public Dictionary<string, object> GetCommonMergeFields( CommonMergeFieldsOptions options = null, Person currentPersonOverride = null )
        {
            var mergeFields = new Dictionary<string, object>();

            options = options ?? new CommonMergeFieldsOptions();

            if ( options.GetPageContext )
            {
                var contextObjects = new Dictionary<string, object>();

                foreach ( var ctx in ContextEntities )
                {
                    contextObjects.Add( ctx.Key.Name, ctx.Value.Value );
                }

                if ( contextObjects.Any() )
                {
                    mergeFields.Add( "Context", contextObjects );
                }
            }

            if ( options.GetPageParameters )
            {
                mergeFields.Add( "PageParameter", PageParameters );
            }

            if ( options.GetOSFamily )
            {
                mergeFields.Add( "OSFamily", ClientInformation.Browser.OS.Family.ToLower() );
            }

            if ( options.GetDeviceFamily )
            {
                mergeFields.Add( "DeviceFamily", ClientInformation.Browser.Device.Family );
            }

            if ( options.GetCurrentPerson && CurrentPerson != null )
            {
                mergeFields.Add( "CurrentPerson", currentPersonOverride ?? CurrentPerson );
            }

            if ( options.GetCampuses )
            {
                mergeFields.Add( "Campuses", CampusCache.All() );
            }

            return mergeFields;
        }

        #endregion
    }

    public class ClientInformation
    {
        public string IpAddress { get; }

        public ClientInfo Browser { get; }

        internal ClientInformation( HttpRequest request )
        {
            //
            // Set IP Address.
            //
            IpAddress = string.Empty;

            // http://stackoverflow.com/questions/735350/how-to-get-a-users-client-ip-address-in-asp-net
            string ipAddress = request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if ( !string.IsNullOrEmpty( ipAddress ) )
            {
                string[] addresses = ipAddress.Split( ',' );
                if ( addresses.Length != 0 )
                {
                    IpAddress = addresses[0];
                }
            }
            else
            {
                IpAddress = request.ServerVariables["REMOTE_ADDR"];
            }

            // nicely format localhost
            if ( IpAddress == "::1" )
            {
                IpAddress = "localhost";
            }

            Parser uaParser = Parser.GetDefault();
            Browser = uaParser.Parse( request.UserAgent );
        }

        internal ClientInformation( HttpRequestMessage request )
        {
            //
            // Set IP Address.
            //
            IpAddress = string.Empty;

            // http://stackoverflow.com/questions/735350/how-to-get-a-users-client-ip-address-in-asp-net
            if ( request.Headers.Contains( "X-FORWARDED-FOR" ) )
            {
                IpAddress = request.Headers.GetValues( "X-FORWARDED-FOR" ).First();
            }
            else if ( request.Properties.ContainsKey( "MS_HttpContext" ) )
            {
                IpAddress = ( ( HttpContextWrapper ) request.Properties["MS_HttpContext"] )?.Request?.UserHostAddress ?? string.Empty;
            }

            // nicely format localhost
            if ( IpAddress == "::1" )
            {
                IpAddress = "localhost";
            }

            Parser uaParser = Parser.GetDefault();
            Browser = uaParser.Parse( request.Headers.UserAgent.ToString() );
        }
    }
}
