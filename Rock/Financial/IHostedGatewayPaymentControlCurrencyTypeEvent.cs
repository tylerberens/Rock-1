using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Web.Cache;

namespace Rock.Financial
{
    /// <summary>
    /// 
    /// </summary>
    public interface IHostedGatewayPaymentControlCurrencyTypeEvent
    {
        /// <summary>
        /// Gets the currency type value.
        /// </summary>
        /// <value>
        /// The currency type value.
        /// </value>
        DefinedValueCache CurrencyTypeValue { get; }

        /// <summary>
        /// Occurs when a payment token is received from the hosted gateway
        /// </summary>
        event EventHandler<HostedGatewayPaymentControlCurrencyTypeEventArgs> CurrencyTypeChange;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class HostedGatewayPaymentControlCurrencyTypeEventArgs : EventArgs
    {
        /// <summary>
        /// The hosted gateway payment control
        /// </summary>
        public IHostedGatewayPaymentControlCurrencyTypeEvent hostedGatewayPaymentControl;
    }


}
