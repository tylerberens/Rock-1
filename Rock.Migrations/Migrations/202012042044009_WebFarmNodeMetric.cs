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
    using System;
    using System.Data.Entity.Migrations;
    
    /// <summary>
    ///
    /// </summary>
    public partial class WebFarmNodeMetric : Rock.Migrations.RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            CreateTable(
                "dbo.WebFarmNodeMetric",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        WebFarmNodeId = c.Int(nullable: false),
                        MetricType = c.Int(nullable: false),
                        MetricValue = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Note = c.String(),
                        MetricValueDateTime = c.DateTime(nullable: false),
                        MetricValueDateKey = c.Int(nullable: false),
                        CreatedDateTime = c.DateTime(),
                        ModifiedDateTime = c.DateTime(),
                        CreatedByPersonAliasId = c.Int(),
                        ModifiedByPersonAliasId = c.Int(),
                        Guid = c.Guid(nullable: false),
                        ForeignId = c.Int(),
                        ForeignGuid = c.Guid(),
                        ForeignKey = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PersonAlias", t => t.CreatedByPersonAliasId)
                .ForeignKey("dbo.PersonAlias", t => t.ModifiedByPersonAliasId)
                .ForeignKey("dbo.WebFarmNode", t => t.WebFarmNodeId, cascadeDelete: true)
                .Index(t => new { t.WebFarmNodeId, t.MetricType, t.MetricValueDateTime }, name: "IX_WebFarmNode_MetricType_MetricValueDateTime")
                .Index(t => t.CreatedByPersonAliasId)
                .Index(t => t.ModifiedByPersonAliasId)
                .Index(t => t.Guid, unique: true);
            
        }
        
        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            DropForeignKey("dbo.WebFarmNodeMetric", "WebFarmNodeId", "dbo.WebFarmNode");
            DropForeignKey("dbo.WebFarmNodeMetric", "ModifiedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo.WebFarmNodeMetric", "CreatedByPersonAliasId", "dbo.PersonAlias");
            DropIndex("dbo.WebFarmNodeMetric", new[] { "Guid" });
            DropIndex("dbo.WebFarmNodeMetric", new[] { "ModifiedByPersonAliasId" });
            DropIndex("dbo.WebFarmNodeMetric", new[] { "CreatedByPersonAliasId" });
            DropIndex("dbo.WebFarmNodeMetric", "IX_WebFarmNode_MetricType_MetricValueDateTime");
            DropTable("dbo.WebFarmNodeMetric");
        }
    }
}
