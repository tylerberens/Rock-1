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
using System.Configuration;
using System.Data.Odbc;

namespace Rock.Configuration
{
    public class ApplicationConfigurationHelper
    {
        public static string GetCurrentDatabaseDescription()
        {
            return GetDatabaseDescription( GetCurrentDatabaseConnectionString() );
        }

        public static string GetCurrentDatabaseConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["RockContext"].ConnectionString;
        }

        /// <summary>
        /// Returns a user-friendly description of a database from a connection string.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static string GetDatabaseDescription( string connectionString )
        {
            var csBuilder = new OdbcConnectionStringBuilder( connectionString );

            object dataSource;
            object catalog;

            csBuilder.TryGetValue( "initial catalog", out catalog );

            if ( string.IsNullOrWhiteSpace( catalog.ToStringSafe() ) )
            {
                return "(unknown)";
            }

            string databaseDescription = catalog.ToStringSafe();

            csBuilder.TryGetValue( "data source", out dataSource );

            if ( !string.IsNullOrWhiteSpace( dataSource.ToStringSafe() ) )
            {
                databaseDescription = dataSource.ToStringSafe() + "/" + databaseDescription;
            }

            return databaseDescription;
        }
    }
}