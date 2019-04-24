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
namespace Rock.Migrations
{
    /// <summary>
    ///
    /// </summary>
    public partial class SmsGive : RockMigration
    {
        private const string SmsGiveBlockTypeGuid = "597F3E62-8FCA-45AA-9502-BBBA3C5CA181";
        private const string SmsGiveBlockGuid = "22B0428C-5C39-4C92-8CBE-F64F7A045FF1";

        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.AddPage(
                true,
                SystemGuid.Page.GIVE_NOW,
                SystemGuid.Layout.FULL_WIDTH,
                "Text To Give",
                "Configuration that allows a user to give using a text message",
                SystemGuid.Page.TEXT_TO_GIVE_SETUP );

            RockMigrationHelper.AddPageRoute(
                SystemGuid.Page.TEXT_TO_GIVE_SETUP,
                "TextToGive",
                SystemGuid.PageRoute.TEXT_TO_GIVE_SETUP );

            RockMigrationHelper.AddBlockType(
                "Text To Give Setup",
                "Allow an SMS sender to configure their SMS based giving.",
                "~/Blocks/Finance/TextToGiveSetup.ascx",
                "Finance",
                SmsGiveBlockTypeGuid );

            RockMigrationHelper.AddBlock(
                true,
                SystemGuid.Page.TEXT_TO_GIVE_SETUP,
                null,
                SmsGiveBlockTypeGuid,
                "Text To Give Setup",
                "Main",
                string.Empty,
                string.Empty,
                0,
                SmsGiveBlockGuid );
        }

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            RockMigrationHelper.DeleteBlockType( SmsGiveBlockTypeGuid );
            RockMigrationHelper.DeletePage( SystemGuid.Page.TEXT_TO_GIVE_SETUP );
        }
    }
}