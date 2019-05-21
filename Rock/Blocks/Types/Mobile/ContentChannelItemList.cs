using System.Collections.Generic;
using System.ComponentModel;

using Rock.Attribute;

namespace Rock.Blocks.Types.Mobile
{
    [DisplayName( "Content Channel Item List" )]
    [Category( "Mobile" )]
    [Description( "Lists content channel items for a given channel." )]
    [IconCssClass( "fa fa-th-list" )]

    #region Block Attributes
    [CodeEditorField(
        "Lava Template",
        Description = "The Lava template to use to return the XAML for the CollectionList. <span class='tip tip-lava'></span>",
        EditorMode = Web.UI.Controls.CodeEditorMode.Xml,
        Key = "",
        Order = 0 )]

    [ContentChannelField(
        "Content Channel",
        Description = "The content channel to retrieve the items for.",
        Key = "",
        Order = 1 )]

    [IntegerField(
        "PageSize",
        Description = "The number of items to send per page.",
        DefaultIntegerValue = 50,
        Order = 2 )]
    #endregion

    public class ContentChannelItemList : RockBlockType, IRockMobileBlockType
    {
        private static class AttributeKeys
        {
            public const string LavaTemplate = "LavaTemplate";

            public const string ContentChannel = "ContentChannel";
        }

        /// <summary>
        /// Gets the required mobile API version.
        /// </summary>
        /// <value>
        /// The required mobile API version.
        /// </value>
        public int RequiredMobileApiVersion => 1;

        /// <summary>
        /// Gets the class name of the mobile block to use during rendering on the device.
        /// </summary>
        /// <value>
        /// The class name of the mobile block to use during rendering on the device
        /// </value>
        public string MobileBlockType => "Rock.Mobile.Blocks.ContentChannelItemList";

        /// <summary>
        /// Gets the property values that will be sent to the device in the application bundle.
        /// </summary>
        /// <returns>
        /// A collection of string/object pairs.
        /// </returns>
        public object GetMobileConfigurationValues()
        {
            return new Dictionary<string, object>();
        }

        #region Actions
        [BlockAction( "GetContentChannelItems" )]
        public string GetContentChannelItems()
        {
            return "hello";
        }
        #endregion

    }
}
