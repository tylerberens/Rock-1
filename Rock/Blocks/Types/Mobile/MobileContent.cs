using System.Collections.Generic;
using System.ComponentModel;

using Rock.Attribute;

namespace Rock.Blocks.Types.Mobile
{
    [DisplayName( "Mobile Content" )]
    [Category( "Mobile" )]
    [Description( "Demo Mobile Block" )]
    [IconCssClass( "fa fa-align-center" )]
    [CodeEditorField( "Content", "The XAML to use when rendering the block. <span class='tip tip-lava'></span>", Web.UI.Controls.CodeEditorMode.Xml )]
    public class MobileContent : RockBlockType, IRockMobileBlockType
    {
        public int RequiredMobileApiVersion => 1;

        public string MobileBlockType => "Rock.Mobile.Blocks.XamlContent";

        public object GetMobileConfigurationValues()
        {
            var content = GetAttributeValue( "Content" );

            var mergeFields = new Dictionary<string, object>();

            return new Dictionary<string, object>
            {
                { "Xaml", content.ResolveMergeFields( mergeFields, null ) }
            };
        }
    }
}
