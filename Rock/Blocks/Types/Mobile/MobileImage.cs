using System.Collections.Generic;
using System.ComponentModel;

using Rock.Attribute;

namespace Rock.Blocks.Types.Mobile
{
    [DisplayName( "Mobile Image" )]
    [Category( "Mobile" )]
    [Description( "Places an image on the mobile device screen." )]
    [IconCssClass( "fa fa-image" )]
    [CodeEditorField( "Image Url", "The URL to use for displaying the image. <span class='tip tip-lava'></span>", Web.UI.Controls.CodeEditorMode.Lava )]
    public class MobileImage : RockBlockType, IRockMobileBlockType
    {
        private static string Xaml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<ContentView xmlns=""http://xamarin.com/schemas/2014/forms""
             xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
             xmlns:Rock=""clr-namespace:Rock.Mobile.Cms"">
  <ContentView.Content>
    <Rock:RockImage ImageUrl=""{Binding ConfigurationValues[Url]}"" />
  </ContentView.Content>
</ContentView>";

        public int RequiredMobileApiVersion => 1;

        public string MobileBlockType => "Rock.Mobile.Blocks.XamlContent";

        public object GetMobileConfigurationValues()
        {
            var mergeFields = new Dictionary<string, object>();

            return new Dictionary<string, object>
            {
                { "Xaml", Xaml },
                { "Url", GetAttributeValue( "ImageUrl" ).ResolveMergeFields( mergeFields, null ) }
            };
        }

        [BlockAction( "GetContent" )]
        public object GetContent()
        {
            return "dynamix";
        }
    }
}
