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

namespace Rock.SampleData.Utility
{
    public static class RandomizedDataHelper
    {
        private static Random _Rng = new Random();

        /// <summary>
        /// Returns a random DateTime within a specified time window of a base date.
        /// </summary>
        /// <param name="baseDateTime"></param>
        /// <param name="days"></param>
        /// <returns></returns>
        public static DateTime GetRandomTimeWithinDayWindow( DateTime? baseDateTime, int days )
        {
            if ( baseDateTime == null )
            {
                baseDateTime = RockDateTime.Now;
            }

            var minutesToAdd = _Rng.Next( 1, ( Math.Abs( days ) * 1440 ) + 1 );

            if ( days < 0 )
            {
                minutesToAdd *= -1;
            }

            var newDateTime = baseDateTime.Value.AddMinutes( minutesToAdd );

            return newDateTime;
        }
    }

}
