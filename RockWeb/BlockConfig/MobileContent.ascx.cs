using System;

using Rock.Attribute;
using Rock.Blocks;

namespace RockWeb.BlockConfig
{
    public partial class MobileContent : System.Web.UI.UserControl, IRockCustomSettingsUserControl
    {
        public void WriteSettingsToEntity( IHasAttributes attributeEntity )
        {
            attributeEntity.SetAttributeValue( Rock.Blocks.Types.Mobile.MobileContent.AttributeKeys.LavaRenderLocation, tbCustomValue.Text );
        }

        public void ReadSettingsFromEntity( IHasAttributes attributeEntity )
        {
            tbCustomValue.Text = attributeEntity.GetAttributeValue( Rock.Blocks.Types.Mobile.MobileContent.AttributeKeys.LavaRenderLocation );
        }

        protected void btnTest_Click( object sender, EventArgs e )
        {
            ltText.Text += "click!<br />";
            ltText.Visible = !ltText.Visible;
        }
    }
}
