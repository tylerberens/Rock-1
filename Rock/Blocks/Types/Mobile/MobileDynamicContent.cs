using System.Collections.Generic;
using System.ComponentModel;

using Rock.Attribute;

namespace Rock.Blocks.Types.Mobile
{
    [DisplayName( "Mobile Dynamic Content" )]
    [Category( "Mobile" )]
    [Description( "Demo Mobile Block" )]
    [IconCssClass( "fa fa-magic" )]

    #region Block Attributes

    [CodeEditorField( "Content",
        "The XAML to use when rendering the block. <span class='tip tip-lava'></span>",
        Web.UI.Controls.CodeEditorMode.Xml,
        order: 0 )]

    [CustomDropdownListField( "Initial Content",
        "If the initial content should be static or dynamic.",
        "Static,Dynamic",
        true,
        "Static",
        order: 1 )]

    #endregion

    public class MobileDynamicContent : RockBlockType, IRockMobileBlockType
    {
        public static class AttributeKeys
        {
            public const string Content = "Content";
            public const string InitialContent = "InitialContent";
        }

        #region IRockMobileBlockType Implementation

        /// <summary>
        /// Gets the required mobile application binary interface version required to render this block.
        /// </summary>
        /// <value>
        /// The required mobile application binary interface version required to render this block.
        /// </value>
        int IRockMobileBlockType.RequiredMobileAbiVersion => 1;

        /// <summary>
        /// Gets the class name of the mobile block to use during rendering on the device.
        /// </summary>
        /// <value>
        /// The class name of the mobile block to use during rendering on the device
        /// </value>
        string IRockMobileBlockType.MobileBlockType => "Rock.Mobile.Blocks.DynamicContent";

        /// <summary>
        /// Gets the property values that will be sent to the device in the application bundle.
        /// </summary>
        /// <returns>
        /// A collection of string/object pairs.
        /// </returns>
        object IRockMobileBlockType.GetMobileConfigurationValues()
        {
            var content = GetAttributeValue( AttributeKeys.InitialContent ) == "Dynamic" ? null : GetStartupContent();

            return new {
                InitialContent = content
            };
        }

        #endregion

        #region Block Actions

        /// <summary>
        /// Gets the startup XAML content that should be rendered.
        /// </summary>
        /// <returns></returns>
        [BlockAction]
        public string GetStartupContent()
        {
            var content = GetAttributeValue( AttributeKeys.Content );

            var mergeFields = new Dictionary<string, object>
            {
                { "Action", string.Empty },
                { "Parameters", new Dictionary<string, object>() }
            };

            return content.ResolveMergeFields( mergeFields );
        }

        /// <summary>
        /// Gets the dynamic XAML content that should be rendered based upon the request.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        [BlockAction]
        public string GetDynamicContent( string action, Dictionary<string, object> parameters )
        {
            var content = GetAttributeValue( AttributeKeys.Content );

            var mergeFields = new Dictionary<string, object>
            {
                { "Action", action },
                { "Parameters", parameters }
            };

            return content.ResolveMergeFields( mergeFields );
        }

        #endregion
    }
}
