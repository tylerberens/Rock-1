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
using System.Collections.Generic;
using System.Linq;
using Rock.Data;
using Rock.Model;

namespace Rock.Tests.Integration.TestData
{
    public static class TestDataHelper
    {
        private static List<PersonIdPersonAliasId> _PersonIdToAliasIdMap = null;

        /// <summary>
        /// Gets a list of the Id and AliasId of all Person records in the database
        /// </summary>
        /// <param name="dataContext"></param>
        /// <returns></returns>
        public static List<PersonIdPersonAliasId> GetPersonIdWithAliasIdList()
        {
            if ( _PersonIdToAliasIdMap == null )
            {
                var aliasService = new PersonAliasService( new RockContext() );

                _PersonIdToAliasIdMap = aliasService.Queryable()
                    .Where( x => !x.Person.IsSystem )
                    .GroupBy( x => x.PersonId )
                    .Select( a => new PersonIdPersonAliasId
                    {
                        PersonId = a.Key,
                        PersonAliasId = a.FirstOrDefault().Id
                    } )
                    .ToList();
            }

            return _PersonIdToAliasIdMap;
        }
        
        /// <summary>
        /// A hardcoded Device Id of 2 (Main Campus Checkin)
        /// </summary>
        public const int KioskDeviceId = 2;

        public class PersonIdPersonAliasId
        {
            public int PersonId { get; set; }
            public int PersonAliasId { get; set; }
        }
    }

}
