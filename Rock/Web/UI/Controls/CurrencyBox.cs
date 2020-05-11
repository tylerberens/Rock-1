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
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock.Web.Cache;

namespace Rock.Web.UI.Controls
{
    /// <summary>
    /// An implementation of a <see cref="T:System.Web.UI.WebControls.TextBox"/> control for displaying and editing currency values.
    /// </summary>
    [ToolboxData( "<{0}:CurrencyBox runat=server></{0}:CurrencyBox>" )]
    public class CurrencyBox: RockTextBox
    {
        private CustomValidator _customValidator;

        private string _currencySymbol = "$";
        private string _currencyDecimalSeparator = ".";
        private string _currencyGroupSeparator = ",";

        // By default, restrict the input value to (+/-) $99,999,999.99 to prevent an Int32 overflow if the amount is stored in cents.
        private const decimal _defaultRangeLimitValue = 100000000.00M;
        private const RangeBoundaryTypeSpecifier _defaultRangeBoundaryType = RangeBoundaryTypeSpecifier.Exclusive;

        #region Properties

        /// <summary>
        /// Gets or sets the name of the field (for range validation messages when Label is not provided)
        /// </summary>
        /// <value>
        /// The name of the field.
        /// </value>
        public string FieldName
        {
            get { return ViewState["FieldName"] as string ?? Label; }
            set { ViewState["FieldName"] = value; }
        }

        /// <summary>
        /// Gets or sets the currency value.
        /// </summary>
        /// <value>
        /// The amount.
        /// </value>
        public decimal? Value
        {
            get
            {
                // Remove the currency formatting characters and return the decimal value.
                var valueText = this.Text
                    .ToStringSafe()
                    .Replace( _currencySymbol, string.Empty )
                    .Replace( _currencyGroupSeparator, string.Empty );

                decimal value;

                if ( decimal.TryParse( valueText, out value ) )
                {
                    return value;
                }

                return null;
            }

            set
            {
                // Format the value as a fixed point decimal to 2 places.
                this.Text = value?.ToString( "F2" );
            }
        }

        /// <summary>
        /// Gets or sets the minimum value.
        /// </summary>
        /// <value>
        /// The minimum value.
        /// </value>
        public string MinimumValue
        {
            get { return ViewState["MinimumValue"] as string; }
            set { ViewState["MinimumValue"] = value; }
        }

        /// <summary>
        /// Gets or sets the maximum value.
        /// </summary>
        /// <value>
        /// The maximum value.
        /// </value>
        public string MaximumValue
        {
            get { return ViewState["MaximumValue"] as string; }
            set { ViewState["MaximumValue"] = value; }
        }

        /// <summary>
        /// Gets or sets the type of boundary imposed by the minimum value.
        /// </summary>
        /// <value>
        /// The minimum value boundary type.
        /// </value>
        public RangeBoundaryTypeSpecifier MinimumValueBoundaryType
        {
            get { return ViewState["MinimumValueBoundaryType"].ToStringSafe().ConvertToEnum<RangeBoundaryTypeSpecifier>( RangeBoundaryTypeSpecifier.Inclusive ); }
            set { ViewState["MinimumValueBoundaryType"] = value; }
        }

        /// <summary>
        /// Gets or sets the type of boundary imposed by the maximum value.
        /// </summary>
        /// <value>
        /// The maximum value boundary type.
        /// </value>
        public RangeBoundaryTypeSpecifier MaximumValueBoundaryType
        {
            get { return ViewState["MaximumValueBoundaryType"].ToStringSafe().ConvertToEnum<RangeBoundaryTypeSpecifier>( RangeBoundaryTypeSpecifier.Inclusive ); }
            set { ViewState["MaximumValueBoundaryType"] = value; }
        }

        /// <summary>
        /// Enables or disables the client-side filtering of keypresses and other character input by the client.
        /// </summary>
        public bool InputFilterEnabled
        {
            get { return ViewState["InputFilterEnabled"] as bool? ?? true; }
            set { ViewState["InputFilterEnabled"] = value; }
        }

        /// <summary>
        /// Enables or disables input validation by the client.
        /// </summary>
        public bool ClientValidationEnabled
        {
            get { return ViewState["ClientValidationEnabled"] as bool? ?? true; }
            set { ViewState["ClientValidationEnabled"] = value; }
        }

        /// <summary>
        /// Gets or sets the display behavior of error messages generated by this control.
        /// </summary>
        public ValidatorDisplay ValidatorDisplayMode
        {
            get { return ViewState["ValidatorDisplayMode"].ToStringSafe().ConvertToEnum<ValidatorDisplay>( ValidatorDisplay.Dynamic ); }
            set { ViewState["ValidatorDisplayMode"] = value; }
        }

        /// <summary>
        /// Parse the input string and ensure that only a valid currency value is set.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ParseValue( string value )
        {
            if ( string.IsNullOrWhiteSpace( value ) )
            {
                base.Text = value;
            }

            var decimalValue = ParseToCurrencyOrNull( value );

            if ( decimalValue.HasValue )
            {
                base.Text = decimalValue.Value.ToString( "F2" );
            }
            else
            {
                base.Text = null;
            }

            return decimalValue.HasValue;
        }

        /// <summary>
        /// Gets a value indicating whether the current value of this field is valid.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </value>
        public override bool IsValid
        {
            get
            {
                EnsureChildControls();

                return base.IsValid && _customValidator.IsValid;
            }
        }

        /// <summary>
        /// Gets or sets the group of controls for which the <see cref="T:System.Web.UI.WebControls.TextBox" /> control causes validation when it posts back to the server.
        /// </summary>
        /// <returns>The group of controls for which the <see cref="T:System.Web.UI.WebControls.TextBox" /> control causes validation when it posts back to the server. The default value is an empty string ("").</returns>
        public override string ValidationGroup
        {
            get
            {
                return base.ValidationGroup;
            }
            set
            {
                base.ValidationGroup = value;

                EnsureChildControls();

                if ( _customValidator != null )
                {
                    _customValidator.ValidationGroup = value;
                }
            }
        }

        #endregion

        #region Base Control Implementation

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( System.EventArgs e )
        {
            base.OnInit( e );

            // Get the preferred currency symbol from the Rock global settings.
            var globalAttributes = GlobalAttributesCache.Get();

            if ( globalAttributes != null )
            {
                _currencySymbol = globalAttributes.GetValue( "CurrencySymbol" );

                if ( string.IsNullOrWhiteSpace( _currencySymbol ) )
                {
                    _currencySymbol = "$";
                }

                this.PrependText = _currencySymbol;
            }

            // Get the currency formatting options for the current thread culture.
            var numberFormat = System.Globalization.CultureInfo.CurrentCulture.NumberFormat;

            _currencyDecimalSeparator = numberFormat.CurrencyDecimalSeparator;
            _currencyGroupSeparator = numberFormat.CurrencyGroupSeparator;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( this.InputFilterEnabled )
            {
                AddClientKeyPressFilterScript();
            }
        }

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            // Create a validator for the field input, to verify currency format and acceptable range of values.
            // Use a CustomValidator because the RangeValidator does not correctly handle the currency symbol or separators.
            _customValidator = new CustomValidator();

            _customValidator.ID = this.ID + "_CV";
            _customValidator.ControlToValidate = this.ID;
            _customValidator.Enabled = true;
            _customValidator.ValidateEmptyText = true;
            _customValidator.CssClass = "validation-error help-inline";

            _customValidator.ServerValidate += _CustomRangeValidator_ServerValidate;

            Controls.Add( _customValidator );
        }

        /// <summary>
        /// Renders the data validators for this IRockControl instance.
        /// </summary>
        /// <param name="writer">The writer.</param>
        protected override void RenderDataValidator( HtmlTextWriter writer )
        {
            writer.RenderBeginTag( HtmlTextWriterTag.Div );

            _customValidator.ValidationGroup = this.ValidationGroup;
            _customValidator.Display = this.ValidatorDisplayMode;
            _customValidator.ClientValidationFunction = "validateCurrencyValue_" + this.ClientID;

            _customValidator.RenderControl( writer );

            writer.RenderEndTag();

            AddClientValidationScript();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Add a client script to filter keystrokes in the currency field.
        /// </summary>
        private void AddClientKeyPressFilterScript()
        {
            // Add a script to limit the allowable keystrokes for currency data entry: number keys, currency format keys, and all shortcut Ctrl/Alt key combinations.
            // Note that the currency symbol itself is not permitted, because it is already prepended to the control text display.
            // However, this does not prevent invalid text being copied/pasted into the field, so we need to rely on regular expression validation to catch the exceptions.
            var changeScript = @"
$('<controlId>').keydown( function (e)
{
    var key = e.which || e.keyCode;
    var validRegularCodes = [<regularKeyList>];
    var validSpecialCodes = [<specialKeyList>];
    if (e.shiftKey)
    {
        return (validSpecialCodes.indexOf(key) >= 0);
    }
    else if (e.altKey || e.ctrlKey)
    {
        return true;
    }
    else
    {
        return (validRegularCodes.indexOf(key) >= 0) || (validSpecialCodes.indexOf(key) >= 0);
    }
} );
";

            changeScript = changeScript.Replace( "<controlId>", ".js-currency-field input" );

            // Define Regular Keys, which are standard printable characters.
            var validRegularKeyCodes = new List<int>();

            // Add keys: Numbers 0-9, keyboard and keypad.
            for ( int i = 48; i <= 57; i++ )
            {
                validRegularKeyCodes.Add( i );
                validRegularKeyCodes.Add( i + 48 );
            }

            // Add format character keys for international group separators and decimal separators:
            // Space, Comma, Period, keypad Period.
            validRegularKeyCodes.AddRange( new int[] { 32, 110, 188, 190 } );

            // Add keys: Minus/NumpadSubtract if Minimum Value is either undefined or less than 0.
            decimal boundaryValue;
            RangeBoundaryTypeSpecifier boundaryType;

            GetActiveMinimumRangeComparison( out boundaryValue, out boundaryType );

            if ( boundaryValue <= 0 )
            {
                validRegularKeyCodes.AddRange( new int[] { 109, 189 } );
            }

            changeScript = changeScript.Replace( "<regularKeyList>", validRegularKeyCodes.AsDelimited( "," ) );

            // Define Special Keys, which can be used alone or in combination with Shift/Alt/Ctrl.
            var validSpecialKeyCodes = new List<int>();

            // Add keys: Backspace, Tab, Enter.
            validSpecialKeyCodes.AddRange( new int[] { 8, 9, 13 } );
            // Add keys: Home, End.
            validSpecialKeyCodes.AddRange( new int[] { 35, 36 } );
            // Add keys: Left Arrow, Right Arrow.
            validSpecialKeyCodes.AddRange( new int[] { 37, 39 } );
            // Add keys: Insert, Delete.
            validSpecialKeyCodes.AddRange( new int[] { 45, 46 } );

            changeScript = changeScript.Replace( "<specialKeyList>", validSpecialKeyCodes.AsDelimited( "," ) );

            this.AddCssClass( "js-currency-field" );

            ScriptManager.RegisterStartupScript( this, this.GetType(), "CurrencyFieldFilterKeyDownScript", changeScript, true );
        }

        /// <summary>
        /// Gets the effective minimum value and comparison type for validating data entry.
        /// </summary>
        /// <returns></returns>
        private void GetActiveMinimumRangeComparison( out decimal boundaryValue, out RangeBoundaryTypeSpecifier boundaryType )
        {
            var specifiedLimitValue = ParseToCurrencyOrNull( this.MinimumValue );

            boundaryValue = specifiedLimitValue ?? ( _defaultRangeLimitValue * -1 );

            if ( specifiedLimitValue == null )
            {
                boundaryType = _defaultRangeBoundaryType;
            }
            else
            {
                boundaryType = this.MinimumValueBoundaryType;
            }
        }

        /// <summary>
        /// Gets the effective maximum value and comparison type for validating data entry.
        /// </summary>
        /// <returns></returns>
        private void GetActiveMaximumRangeComparison( out decimal boundaryValue, out RangeBoundaryTypeSpecifier boundaryType )
        {
            var specifiedLimitValue = ParseToCurrencyOrNull( this.MaximumValue );

            boundaryValue = specifiedLimitValue ?? _defaultRangeLimitValue;

            if ( specifiedLimitValue == null )
            {
                boundaryType = _defaultRangeBoundaryType;
            }
            else
            {
                boundaryType = this.MaximumValueBoundaryType;
            }
        }

        /// <summary>
        /// Add a client script to validate the format and value range of input for the currency field.
        /// </summary>
        private void AddClientValidationScript()
        {
            // Add a generic currency input validation script that can be used for all controls on the page.
            // Parameters are passed in to customise the behavior for each control instance.
            var validationScript = @"
function validateCurrencyValue( sender, args )
{
    var input = args.Value.replace( /^\s+|\s+/g, '' );
    if ( input === '' )
    {
        return;
    }
    var summaryMessage = '';
    var isValid = true;
    var reCurrency = new RegExp( '<RX-CURRENCY>', 'g' );
    var isCurrency = reCurrency.test(input);
    if ( !isCurrency )
    {
        summaryMessage = args.fieldName + ' entry is not a recognized currency value.';
        isValid = false;
    }
    if ( isValid )
    {
        var reSymbols = new RegExp( '<RX-REPLACE>', 'g' );
        var numericInput = input.replace( reSymbols, '' );
        numericInput = numericInput.replace( '<DECIMAL-SEPARATOR>', '.');
        var number = parseFloat( numericInput );
        if ( Number.isNaN( number ) )
        {
            summaryMessage = args.fieldName + ' entry is not a recognized currency value.';
            isValid = false;
        }
        else if ( ( args.minCompare == '<' && number < args.minValue )
                  || ( args.minCompare == '<=' && number <= args.minValue )
                  || ( args.maxCompare == '>' && number > args.maxValue )
                  || ( args.maxCompare == '>=' && number >= args.maxValue ) )
        {
            summaryMessage = args.rangeErrorMessage;
            isValid = false;
        }
    }
    sender.errormessage = summaryMessage;
    args.IsValid = isValid;
    return;
}
";

            var currencyRegEx = GetRegExForCurrency().Replace( @"\", @"\\" );

            validationScript = validationScript.Replace( "<RX-CURRENCY>", currencyRegEx );
            validationScript = validationScript.Replace( "<RX-CURRENCY>", currencyRegEx );
            validationScript = validationScript.Replace( "<RX-REPLACE>", GetRegExForCurrencySymbol() + @"|\\" + _currencyGroupSeparator );
            validationScript = validationScript.Replace( "<DECIMAL-SEPARATOR>", _currencyDecimalSeparator );

            ScriptManager.RegisterStartupScript( this, this.GetType(), "CurrencyFieldValidation", validationScript, true );

            // Add a control-specific script for each CurrencyBox instance on the page.
            var controlScript = @"
function validateCurrencyValue_<ID>( sender, args )
{
    args.fieldName = '<FIELD_NAME>';
    args.minValue = <MIN_VALUE>;
    args.minCompare = '<MIN_COMPARE>';
    args.maxValue = <MAX_VALUE>;
    args.maxCompare = '<MAX_COMPARE>';
    args.rangeErrorMessage = '<MSG-RANGE-ERROR>';

    validateCurrencyValue( sender, args );
}
";

            decimal minBoundaryValue;
            decimal maxBoundaryValue;
            RangeBoundaryTypeSpecifier minBoundaryType;
            RangeBoundaryTypeSpecifier maxBoundaryType;

            GetActiveMinimumRangeComparison( out minBoundaryValue, out minBoundaryType );
            GetActiveMaximumRangeComparison( out maxBoundaryValue, out maxBoundaryType );

            // Make sure we stringify the comparison values using US-culture to avoid a syntax error because they are being injected as a literal part of the script.
            var cultures = new CultureInfo( "en-US" );

            controlScript = controlScript.Replace( "<ID>", this.ClientID );
            controlScript = controlScript.Replace( "<FIELD_NAME>", string.IsNullOrWhiteSpace( FieldName ) ? "Value" : FieldName );
            controlScript = controlScript.Replace( "<MIN_VALUE>", minBoundaryValue.ToString( cultures ) );

            if ( minBoundaryType == RangeBoundaryTypeSpecifier.Inclusive )
            {
                controlScript = controlScript.Replace( "<MIN_COMPARE>", "<" );
            }
            else
            {
                controlScript = controlScript.Replace( "<MIN_COMPARE>", "<=" );
            }

            controlScript = controlScript.Replace( "<MAX_VALUE>", maxBoundaryValue.ToString( cultures ) );

            if ( maxBoundaryType == RangeBoundaryTypeSpecifier.Inclusive )
            {
                controlScript = controlScript.Replace( "<MAX_COMPARE>", ">" );
            }
            else
            {
                controlScript = controlScript.Replace( "<MAX_COMPARE>", ">=" );
            }

            controlScript = controlScript.Replace( "<MSG-RANGE-ERROR>", GetRangeErrorMessage() );

            ScriptManager.RegisterStartupScript( this, this.GetType(), "CurrencyFieldValidation_" + this.ClientID, controlScript, true );
        }

        /// <summary>
        /// Gets the regular expression for the local currency symbol.
        /// </summary>
        /// <returns></returns>
        private string GetRegExForCurrencySymbol()
        {
            if ( _currencySymbol == "$" )
            {
                return @"\\$";
            }

            return _currencySymbol;
        }

        /// <summary>
        /// Get a regular expression to validate a currency amount, including culture-specific symbols.
        /// </summary>
        /// <returns></returns>
        private string GetRegExForCurrency()
        {
            // Create a regular expression that includes culture-specific currency format characters.
            var regexExpression = @"^\-?<CS>?([1-9]{1}[0-9]{0,2}(\<GS>?[0-9]{3})*(\<DS>[0-9]{0,2})?|[1-9]{1}[0-9]{0<GS>}(\<DS>[0-9]{0,2})?|0(\<DS>[0-9]{0,2})?|(\<DS>[0-9]{1,2})?)$";

            // Replace the currency symbol, escaping special Regex characters if necessary.
            var currencySymbol = _currencySymbol;

            if ( currencySymbol == "$" )
            {
                currencySymbol = @"\" + currencySymbol;
            }

            regexExpression = regexExpression.Replace( "<CS>", currencySymbol );

            regexExpression = regexExpression.Replace( "<DS>", _currencyDecimalSeparator );
            regexExpression = regexExpression.Replace( "<GS>", _currencyGroupSeparator );

            return regexExpression;
        }

        /// <summary>
        /// Processes server-side validation for the custom range validation control.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        protected void _CustomRangeValidator_ServerValidate( object source, ServerValidateEventArgs args )
        {
            string errorMessage = null;

            // If the field is empty, no range validation is needed.
            var inputValue = args.Value.ToStringSafe().Trim();

            if ( string.IsNullOrWhiteSpace( inputValue ) )
            {
                return;
            }

            // Get the field content as a numeric value.
            var numericValue = ParseToCurrencyOrNull( inputValue );

            if ( numericValue == null )
            {
                errorMessage = string.Format( "{0} entry is not a recognized currency amount.", GetFieldName() );
            }
            else
            {
                decimal minBoundaryValue;
                decimal maxBoundaryValue;
                RangeBoundaryTypeSpecifier minBoundaryType;
                RangeBoundaryTypeSpecifier maxBoundaryType;

                GetActiveMinimumRangeComparison( out minBoundaryValue, out minBoundaryType );
                GetActiveMaximumRangeComparison( out maxBoundaryValue, out maxBoundaryType );

                if ( ( ( minBoundaryType == RangeBoundaryTypeSpecifier.Inclusive && numericValue < minBoundaryValue )
                       || ( minBoundaryType == RangeBoundaryTypeSpecifier.Exclusive && numericValue <= minBoundaryValue ) )
                     ||
                     ( ( maxBoundaryType == RangeBoundaryTypeSpecifier.Inclusive && numericValue > maxBoundaryValue )
                       || ( maxBoundaryType == RangeBoundaryTypeSpecifier.Exclusive && numericValue >= maxBoundaryValue ) )
                   )
                {
                    errorMessage = this.GetRangeErrorMessage();
                }
            }

            if ( !string.IsNullOrWhiteSpace( errorMessage ) )
            {
                _customValidator.ErrorMessage = errorMessage;

                args.IsValid = false;
            }
        }

        /// <summary>
        /// Get a name for the field that is suitable for use in user interactions.
        /// </summary>
        /// <returns></returns>
        private string GetFieldName()
        {
            return string.IsNullOrWhiteSpace( this.FieldName ) ? "Value" : FieldName;
        }

        /// <summary>
        /// Parse a string value to a currency value using the current culture settings.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private decimal? ParseToCurrencyOrNull( string value )
        {
            if ( value != null )
            {
                // Explicitly remove the Rock-specified currency symbol because it may not be the same as the current culture symbol.
                value = value.Replace( _currencySymbol, string.Empty );

                // Parse the remaining input text using the current culture settings for currency values.
                decimal decimalValue;

                var isValid = decimal.TryParse( value, NumberStyles.Currency, null, out decimalValue );

                if ( isValid )
                {
                    return decimalValue;
                }
            }

            return null;
        }

        /// <summary>
        /// Get the validation message that summarises the range restrictions.
        /// </summary>
        /// <returns>A formatted error message.</returns>
        private string GetRangeErrorMessage()
        {
            decimal minBoundaryValue;
            decimal maxBoundaryValue;
            RangeBoundaryTypeSpecifier minBoundaryType;
            RangeBoundaryTypeSpecifier maxBoundaryType;

            GetActiveMinimumRangeComparison( out minBoundaryValue, out minBoundaryType );
            GetActiveMaximumRangeComparison( out maxBoundaryValue, out maxBoundaryType );

            string minExpression;
            string maxExpression;

            // Get expression for minimum value.
            if ( minBoundaryType == RangeBoundaryTypeSpecifier.Inclusive )
            {
                minExpression = string.Format( "at least {0:n2}", minBoundaryValue );
            }
            else
            {
                minExpression = string.Format( "greater than {0:n2}", minBoundaryValue );
            }

            // Get expression for maximum value.
            if ( maxBoundaryType == RangeBoundaryTypeSpecifier.Inclusive )
            {
                maxExpression = string.Format( "not more than {0:n2}", maxBoundaryValue );
            }
            else
            {
                maxExpression = string.Format( "less than {0:n2}", maxBoundaryValue );
            }

            var comparisonExpression = string.Format( "{0} value must be {1} and {2}",
                string.IsNullOrWhiteSpace( FieldName ) ? "Value" : FieldName,
                minExpression,
                maxExpression );

            return comparisonExpression;
        }

        #endregion

        #region Enumerations

        /// <summary>
        /// Specifies the type of limit imposed by a boundary value in a range.
        /// </summary>
        public enum RangeBoundaryTypeSpecifier
        {
            /// <summary>
            /// The boundary value is included in the range of valid values.
            /// </summary>
            Inclusive = 0,

            /// <summary>
            /// The boundary value is excluded from the range of valid values.
            /// </summary>
            Exclusive = 1,
        }

        #endregion
    }
}