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
using System.Globalization;
using System.Web.UI;
using Rock.Web.UI.Controls;

namespace Rock.Field.Types
{
    /// <summary>
    /// Field used to save and display a currency amount.
    /// </summary>
    /// <remarks>
    /// The currency field type is stored internally as a string representation of a decimal value formatted using the InvariantCulture,
    /// to ensure that it is consistently processed by clients operating with different culture settings.
    /// The invariant culture specifies a decimal number in the form "1,234.56" - other cultures may use a different
    /// group separator and/or decimal separator.
    /// </remarks>
    public class CurrencyFieldType : DecimalFieldType
    {
        #region Formatting

        /// <summary>
        /// Formats the value.
        /// </summary>
        /// <param name="parentControl">The parent control.</param>
        /// <param name="value">The value.</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="condensed">if set to <c>true</c> [condensed].</param>
        /// <returns></returns>
        public override string FormatValue( System.Web.UI.Control parentControl, string value, Dictionary<string, ConfigurationValue> configurationValues, bool condensed )
        {
            if ( !string.IsNullOrWhiteSpace( value ) )
            {
                var decimalValue = value.ToStringSafe().AsDecimalOrNull( CultureInfo.InvariantCulture );

                if ( decimalValue != null )
                {
                    return decimalValue.FormatAsCurrency();
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Returns the value using the most appropriate datatype
        /// </summary>
        /// <param name="parentControl">The parent control.</param>
        /// <param name="value">The value.</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <returns></returns>
        public override object ValueAsFieldType( System.Web.UI.Control parentControl, string value, Dictionary<string, ConfigurationValue> configurationValues )
        {
            return value.ToStringSafe().AsDecimalOrNull( CultureInfo.InvariantCulture );
        }

        /// <summary>
        /// Returns the value that should be used for sorting, using the most appropriate datatype
        /// </summary>
        /// <param name="parentControl">The parent control.</param>
        /// <param name="value">The value.</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <returns></returns>
        public override object SortValue( System.Web.UI.Control parentControl, string value, Dictionary<string, ConfigurationValue> configurationValues )
        {
            return value.ToStringSafe().AsDecimalOrNull( CultureInfo.InvariantCulture );
        }

        #endregion

        #region Edit Control

        /// <summary>
        /// Creates the control(s) necessary for prompting user for a new value
        /// </summary>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="id"></param>
        /// <returns>
        /// The control
        /// </returns>
        public override System.Web.UI.Control EditControl( System.Collections.Generic.Dictionary<string, ConfigurationValue> configurationValues, string id )
        {
            return new CurrencyBox { ID = id };
        }

        /// <summary>
        /// Tests the value to ensure that it is a valid value.  If not, message will indicate why
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="required">if set to <c>true</c> [required].</param>
        /// <param name="message">The message.</param>
        /// <returns>
        ///   <c>true</c> if the specified value is valid; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsValid( string value, bool required, out string message )
        {
            if ( !string.IsNullOrWhiteSpace( value ) )
            {
                decimal result;
                if ( !decimal.TryParse( value, out result ) )
                {
                    message = "The input provided is not a valid currency value.";
                    return true;
                }
            }

            return base.IsValid( value, required, out message );
        }

        /// <summary>
        /// Reads new values entered by the user for the field
        /// </summary>
        /// <param name="control">Parent control that controls were added to in the CreateEditControl() method</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <returns></returns>
        public override string GetEditValue( Control control, Dictionary<string, ConfigurationValue> configurationValues )
        {
            if ( control is CurrencyBox cb )
            {
                if ( cb.Value == null )
                {
                    return string.Empty;
                }
                else
                {
                    // Return the decimal value of the CurrencyBox as a string formatted using the InvariantCulture number format.
                    return cb.Value.Value.ToString( CultureInfo.InvariantCulture );
                }
            }

            return null;
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="value">A decimal value formatted with the InvariantCulture.</param>
        public override void SetEditValue( Control control, Dictionary<string, ConfigurationValue> configurationValues, string value )
        {
            if ( control is CurrencyBox cb )
            {
                // The value for the Currency Field is supplied as a string representation of a decimal formatted using the InvariantCulture number format.
                cb.Value = value.ToStringSafe().AsDecimalOrNull( CultureInfo.InvariantCulture );
            }
        }

        #endregion
    }
}