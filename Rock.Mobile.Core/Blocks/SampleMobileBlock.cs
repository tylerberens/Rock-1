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
using System.Net;
using System.Web.Http;
using Rock.Attribute;
using Rock.Model;
using Rock.Security;

namespace Rock.Mobile.Core.Blocks
{
    [BooleanField(
        "Display Pledges",
        Key = AttributeKey.ShowConcise,
        Description = "Determines if pledges should be shown.",
        DefaultBooleanValue = true,
        Order = 1 )]
    [TextField(
        "Font",
        Key = AttributeKey.Font,
        Description = "The font to use on the phone.",
        DefaultValue = "Arial",
        Category = "Device",
        Order = 1
        )]
    public class SampleMobileBlock : MobileBlockBase
    {
        protected static class AttributeKey
        {
            public const string ShowConcise = "ShowConcise";
            public const string Font = "Font";
        }

        /// <summary>
        /// Use this to Login a user and return an AuthCookie which can be used in subsequent REST calls
        /// </summary>
        /// <param name="loginParameters">The login parameters.</param>
        /// <exception cref="System.Web.Http.HttpResponseException"></exception>
        [HttpGet]
        [System.Web.Http.Route( "api/Mobile/SampleMobileBlock/GetContent" )]
        public string GetContent(  )
        {
            var showConcise = GetAttributeValue( AttributeKey.ShowConcise ).AsBoolean();

            if ( showConcise )
            {
                return "Hello";
            }

            return "Hello World"; 
        }
    }
}
