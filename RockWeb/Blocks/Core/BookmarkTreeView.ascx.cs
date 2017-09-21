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
using System.ComponentModel;
using System.Linq;
using System.Text;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;
using Rock.Web.Cache;

namespace RockWeb.Blocks.Core
{
    [DisplayName( "Bookmark Tree View" )]
    [Category( "Core" )]
    [Description( "Displays a tree of bookmarks for the configured entity type." )]

    public partial class BookmarkTreeView : RockBlock
    {
        public const string CategoryNodePrefix = "C";

        /// <summary>
        /// The RestParams (used by the Markup)
        /// </summary>
        protected string RestParms;

        /// <summary>
        /// Gets or sets the selected category identifier.
        /// </summary>
        /// <value>
        /// The selected category identifier.
        /// </value>
        protected int? SelectedCategoryId { get; set; }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            // Get EntityTypeName
            var cachedEntityType = Rock.Web.Cache.EntityTypeCache.Read( Rock.SystemGuid.EntityType.PERSON_BOOKMARK.AsGuid() );
            if ( cachedEntityType != null )
            {
                RestParms = string.Format( "?getCategorizedItems=true&showUnnamedEntityItems=false&entityTypeId={0}&includeInactiveItems=false", cachedEntityType.Id );
            }
        }

    }
}