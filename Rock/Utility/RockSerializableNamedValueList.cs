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
using System.Linq;
using System.Text;

namespace Rock.Utility
{
    /// <summary>
    /// An unsorted collection of non-unique name/value pairs that can be serialized to a Rock-specific formatted string for portability and storage.
    /// </summary>
    /// <remarks>
    /// The serialization format for this collection is: Name1^Value1|Name2^Value2a,Value2b,Value2c|Name3^Value3
    /// </remarks>
    public class RockSerializableNamedValueList : List<KeyValuePair<string, string>>
    {
        private const string nameValuePairEntrySeparator = "|";
        private const string nameValuePairInternalSeparator = "^";

        #region Constructors

        /// <summary>
        /// Create a new instance with an empty NamedValueList.
        /// </summary>
        public RockSerializableNamedValueList()
        {
            //
        }

        /// <summary>
        /// Create a new instance from an existing NamedValueList.
        /// </summary>
        /// <param name="dictionary"></param>
        public RockSerializableNamedValueList( IDictionary<string, object> dictionary )
        {
            foreach ( var kvp in dictionary )
            {
                this.Add( new KeyValuePair<string, string>( kvp.Key, ( kvp.Value == null ? string.Empty : kvp.Value.ToString() ) ) );
            }
        }

        /// <summary>
        /// Create a new instance from an existing serialized string.
        /// </summary>
        /// <param name="uriEncodedString"></param>
        public RockSerializableNamedValueList( string uriEncodedString )
        {
            var nameValueStrings = uriEncodedString.Split( new string[] { nameValuePairEntrySeparator }, StringSplitOptions.None ).ToList();

            foreach ( var nameValueString in nameValueStrings )
            {
                var nameValueArray = nameValueString.Split( new string[] { nameValuePairInternalSeparator }, StringSplitOptions.None );

                // Decode the values. Use UnescapeDataString() because HttpUtility.UrlDecode() replaces "+" with " " which is unwanted behavior.
                if ( nameValueArray.Length == 2 )
                {
                    var name = System.Uri.UnescapeDataString( nameValueArray[0] );
                    var value = System.Uri.UnescapeDataString( nameValueArray[1] );

                    this.Add( new KeyValuePair<string, string>( name, value ) );
                }
            }
        }

        #endregion

        /// <summary>
        /// Creates a formatted string where the names and values are Uri-encoded.
        /// </summary>
        /// <returns></returns>
        public string ToUriEncodedString()
        {
            return RockSerializableNamedValueList.ToUriEncodedString( this );
        }

        #region Static methods

        /// <summary>
        /// Creates a formatted string representation of the provided Dictionary where the names and values are Uri-encoded.
        /// </summary>
        /// <returns></returns>
        public static string ToUriEncodedString<TValue>( IDictionary<string, TValue> dictionary )
        {
            var stringNamedValueList = dictionary.Select( x => new KeyValuePair<string, TValue>( x.Key, x.Value ) );

            return ToUriEncodedString( stringNamedValueList );
        }

        /// <summary>
        /// Creates a formatted string representation of the provided KeyValuePair collection where the names and values are Uri-encoded.
        /// </summary>
        /// <returns></returns>
        public static string ToUriEncodedString<TValue>( IEnumerable<KeyValuePair<string, TValue>> keyValuePairList )
        {
            var sb = new StringBuilder();

            var isFirstItem = true;

            foreach ( var kvp in keyValuePairList )
            {
                if ( isFirstItem )
                {
                    isFirstItem = false;
                }
                else
                {
                    sb.Append( nameValuePairEntrySeparator );
                }

                // Make sure that any special characters in the name and value strings are encoded, to prevent confusion with the NamedValueList string delimiters.
                sb.Append( System.Uri.EscapeDataString( kvp.Key ) );
                sb.Append( nameValuePairInternalSeparator );
                sb.Append( System.Uri.EscapeDataString( kvp.Value.ToStringSafe() ) );
            }

            return sb.ToString();
        }

        /// <summary>
        /// Create a new instance of the RockSerializableNamedValueList from a Uri-encoded string.
        /// </summary>
        /// <param name="uriEncodedString"></param>
        /// <returns></returns>
        public static RockSerializableNamedValueList FromUriEncodedString( string uriEncodedString )
        {
            return new RockSerializableNamedValueList( uriEncodedString );
        }

        #endregion
    }
}