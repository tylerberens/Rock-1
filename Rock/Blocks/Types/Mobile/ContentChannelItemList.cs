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
    
    [ContentChannelField(
        "Content Channel",
        Description = "The content channel to retrieve the items for.",
        Key = "",
        Order = 1,
        Category = "CustomSettings" )]

    [TextField(
        "Page Size",
        Description = "The number of items to send per page.",
        Key = AttributeKeys.PageSize,
        DefaultValue = "50",
        Order = 2,
        Category = "CustomSettings" )]

    [BooleanField(
        "Include Following",
        Description = "Determines if following data should be sent along with the results.",
        Key = AttributeKeys.IncludeFollowing,
        Order = 3,
        Category = "CustomSettings" )]

    [TextField(
        "Field Settings",
        Description = "JSON object of the configured fields to show.",
        Key = AttributeKeys.FieldSettings,
        Order = 4,
        Category = "CustomSettings")]

    #endregion

    public class ContentChannelItemList : RockBlockType, IRockMobileBlockType
    {
        public static class AttributeKeys
        {
            public const string LavaTemplate = "LavaTemplate";

            public const string ContentChannel = "ContentChannel";

            public const string FieldSettings = "FieldSettings";

            public const string PageSize = "PageSize";

            public const string IncludeFollowing = "IncludeFollowing";
        }

        #region IRockMobileBlockType Implementation

        /// <summary>
        /// Gets the class name of the mobile block to use during rendering on the device.
        /// </summary>
        /// <value>
        /// The class name of the mobile block to use during rendering on the device
        /// </value>
        public string MobileBlockType => "Rock.Mobile.Blocks.ContentChannelItemList";

        public int RequiredMobileAbiVersion => 1;

        #endregion

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


        #region Custom Settings

        [TargetType( typeof( ContentChannelItemList ) )]
        public class MobileContentCustomSettingsProvider : RockCustomSettingsUserControlProvider
        {
            protected override string UserControlPath => "~/BlockConfig/ContentChannelListSettings.ascx";

            public override string CustomSettingsTitle => "Basic Settings";
        }

        #endregion
    }
}
