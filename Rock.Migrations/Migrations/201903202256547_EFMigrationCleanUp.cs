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
    using Rock.Plugin.HotFixes;

    /// <summary>
    ///
    /// </summary>
    public partial class EFMigrationCleanUp : Rock.Migrations.RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            // 60 

            // 61 
            UpdateStatementGeneratorInstallerLocation();
            ReAddMissingVisitorConnectionStatus();

            // 062_MigrationRollupsForV8_7
            FixWordCloudShortCodeUp();
            FixParallaxShortCodeUp();

            // 064_MigrationRollupsForV8_7_2
            UpdateCommunicationSettings();
            UpdatedCheckInSuccessTemplate();
            UpdatedAttendanceSummaryEmail();
            UpdateContentChannelView();
            RemoveDuplicateIndexes();

            // 066_MigrationRollupsForV8_7_3
            UpdateStatementGenerator();
            UpdateCalendarEventField();
            UpdatePersonTokenCreate();
            UpdatePersonTokenCreateAttribute();

            // 67
            CreatePersonBirthdatePersistedIndexed();

            // 068_MigrationRollupsForV8_7_5
            UpdatedGoogleMapsShortCode();

            // 69
            EFCleanUpForFile69_MigrationRollupsForV8_7_6();

            // 70_MigrationRollupsForV8_7_7
            UpDatesPhotoRequestCommunicationTemplate();
            
            // 073_MigrationRollupsForV8_8
            RemoveWindowsJobSchedulerServiceValue();

        }

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
          
        }
        #region 58



        #endregion

        #region 59 Review With Mike

        /// <summary>
        /// MP: Fix Invalid Column error in spCheckin_AttendanceAnalyticsQuery_NonAttendees
        /// </summary>
        private void FixspCheckin_AttendanceAnalyticsQuery_NonAttendees()
        {
            Sql( HotFixMigrationResource._059_MigrationRollupsForV8_5_2_spCheckin_AttendanceAnalyticsQuery_NonAttendees );
        }

        #endregion

        #region 60 Review with DEVS

        /// <summary>
        /// ED: Date filtering is missing while calculating Gifts in spFinance_PledgeAnalyticsQuery
        /// </summary>
        private void FixPledgeAnalyticsGiftDateFilteringUp()
        {
            Sql( HotFixMigrationResource._060_MigrationRollupsForV8_6_spFinance_PledgeAnalyticsQuery_Up );
        }

        /// <summary>
        /// Reverts Change:
        /// ED: Date filtering is missing while calculating Gifts in spFinance_PledgeAnalyticsQuery
        /// </summary>
        private void FixPledgeAnalyticsGiftDateFilteringDown()
        {
            Sql( HotFixMigrationResource._060_MigrationRollupsForV8_6_spFinance_PledgeAnalyticsQuery_Down );
        }

        /// <summary>
        /// GJ: Fix Chart Shortcode
        /// </summary>
        private void FixChartShortcode()
        {
            Sql( HotFixMigrationResource._060_MigrationRollupsForV8_6_FixChartShortcode );
        }

        #endregion

        #region 61 Review with Devs (ReAddMissingVisitorConnectionStatus)

        /// <summary>
        /// MP: Update StatementGenerator.exe to 1.8.0
        /// </summary>
        private void UpdateStatementGeneratorInstallerLocation()
        {
            // update new location of statementgenerator installer
            Sql( @"
 UPDATE [AttributeValue] 
 SET [Value] = 'http://storage.rockrms.com/externalapplications/sparkdevnetwork/statementgenerator/1.8.0/statementgenerator.exe' 
 WHERE [Guid] = '10BE2E03-7827-41B5-8CB2-DEB473EA107A'
" );
        }

        /// <summary>
        /// NA: Re-add Missing Visitor Connection Status (Fixes #3497)
        /// </summary>
        private void ReAddMissingVisitorConnectionStatus()
        {
            // Fixes #3497
            Sql( @"
IF NOT EXISTS (SELECT [Id] FROM [DefinedValue] WHERE [Guid] = 'b91ba046-bc1e-400c-b85d-638c1f4e0ce2' )
BEGIN
    SET IDENTITY_INSERT [dbo].[DefinedValue] ON
    INSERT INTO [dbo].[DefinedValue] ([Id], [IsSystem], [DefinedTypeId], [Order], [Value], [Description], [Guid], [IsActive] ) VALUES (66, 1, 4, 2, N'Visitor', N'Used when a person first enters through your first-time visitor process. As they continue to attend they will become an attendee and possibly a member.', N'b91ba046-bc1e-400c-b85d-638c1f4e0ce2', 1)
    SET IDENTITY_INSERT [dbo].[DefinedValue] OFF
END" );
        }

        #endregion

        #region 62

        /// <summary>
        /// Fixes the word cloud short code CA9B54BF-EF0A-4B08-884F-7042A6B3EAF4
        /// </summary>
        private void FixWordCloudShortCodeUp()
        {
            Sql( @"UPDATE [LavaShortCode] 
SET [Markup] = '{% javascript id:''d3-layout-cloud'' url:''~/Scripts/d3-cloud/d3.layout.cloud.js'' %}{% endjavascript %}
{% javascript id:''d3-min'' url:''~/Scripts/d3-cloud/d3.min.js'' %}{% endjavascript %}

<div id=""{{ uniqueid }}"" style=""width: {{ width }}; height: {{ height }};""></div>

{%- assign anglecount = anglecount | Trim -%}
{%- assign anglemin = anglemin | Trim -%}
{%- assign anglemax = anglemax | Trim -%}

{% javascript disableanonymousfunction:''true'' %}
    $( document ).ready(function() {
        Rock.controls.wordcloud.initialize({
            inputTextId: ''hf-{{ uniqueid }}'',
            visId: ''{{ uniqueid }}'',
            width: ''{{ width }}'',
            height: ''{{ height }}'',
            fontName: ''{{ fontname }}'',
            maxWords: {{ maxwords }},
            scaleName: ''{{ scalename }}'',
            spiralName: ''{{ spiralname}}'',
            colors: [ ''{{ colors | Replace:'','',""'',''"" }}''],
            {%- if anglecount != '''' %}
            anglecount: {{ anglecount }}{%- if anglemin != '''' or anglemax != '''' -%},{%- endif -%}
            {%- endif -%}
            {%- if anglemin != '''' %}
            anglemin: {{ anglemin }}{%- if anglemax != '''' -%},{%- endif -%}
            {%- endif -%}
            {%- if anglemax != '''' %}
            anglemax: {{ anglemax }}
            {%- endif -%}
        });
    });
{% endjavascript %}

<input type=""hidden"" id=""hf-{{ uniqueid }}"" value=""{{ blockContent }}"" />' WHERE [Guid] = 'CA9B54BF-EF0A-4B08-884F-7042A6B3EAF4'" );
        }

        /// <summary>
        /// Fixes the parallax short code 4B6452EF-6FEA-4A66-9FB9-1A7CCE82E7A4
        /// </summary>
        private void FixParallaxShortCodeUp()
        {
            Sql( @"UPDATE [LavaShortCode]
SET [Markup] = '{{ ''https://cdnjs.cloudflare.com/ajax/libs/jarallax/1.9.2/jarallax.min.js'' | AddScriptLink }}
{% if videourl != '''' -%}
    {{ ''https://cdnjs.cloudflare.com/ajax/libs/jarallax/1.9.2/jarallax-video.min.js'' | AddScriptLink }}
{% endif -%}

{% assign id = uniqueid -%} 
{% assign bodyZindex = zindex | Plus:1 -%}

{% assign speed = speed | AsInteger %}

{% if speed > 0 -%}
    {% assign speed = speed | Times:''.01'' -%}
    {% assign speed = speed | Plus:''1'' -%}
{% elseif speed == 0 -%}
    {% assign speed = 1 -%}
{% else -%}
    {% assign speed = speed | Times:''.02'' -%}
    {% assign speed = speed | Plus:''1'' -%}
{% endif -%}


 
{% if videourl != ''''- %}
    <div id=""{{ id }}"" class=""jarallax"" data-jarallax-video=""{{ videourl }}"" data-type=""{{ type }}"" data-speed=""{{ speed }}"" data-img-position=""{{ position }}"" data-object-position=""{{ position }}"" data-background-position=""{{ position }}"" data-zindex=""{{ bodyZindex }}"" data-no-android=""{{ noandroid }}"" data-no-ios=""{{ noios }}"">
{% else- %} 
    <div id=""{{ id }}"" data-jarallax class=""jarallax"" data-type=""{{ type }}"" data-speed=""{{ speed }}"" data-img-position=""{{ position }}"" data-object-position=""{{ position }}"" data-background-position=""{{ position }}"" data-zindex=""{{ bodyZindex }}"" data-no-android=""{{ noandroid }}"" data-no-ios=""{{ noios }}"">
        <img class=""jarallax-img"" src=""{{ image }}"" alt="""">
{% endif -%}

        {% if blockContent != '''' -%}
            <div class=""parallax-content"">
                {{ blockContent }}
            </div>
        {% else- %}
            {{ blockContent }}
        {% endif -%}
    </div>

{% stylesheet %}
#{{ id }} {
    /* eventually going to change the height using media queries with mixins using sass, and then include only the classes I want for certain parallaxes */
    min-height: {{ height }};
    background: transparent;
    position: relative;
    z-index: 0;
}

#{{ id }} .jarallax-img {
    position: absolute;
    object-fit: cover;
    /* support for plugin https://github.com/bfred-it/object-fit-images */
    font-family: ''object-fit: cover;'';
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    z-index: -1;
}

#{{ id }} .parallax-content{
    display: inline-block;
    margin: {{ contentpadding }};
    color: {{ contentcolor }};
    text-align: {{ contentalign }};
	width: 100%;
}
{% endstylesheet %}'
WHERE [Guid] = '4B6452EF-6FEA-4A66-9FB9-1A7CCE82E7A4'" );
        }

        #endregion

        #region 64

        /// <summary>Updates the communication settings.
        ///SK: Updated Communication Settings Name and description
        /// </summary>
        private void UpdateCommunicationSettings()
        {
            Sql( @"
UPDATE 
	[Attribute]
SET
	[Name]='Enable Do Not Disturb'
	, [Description] = 'When enabled, communications will not be sent between the starting and ending ''Do Not Disturb'' hours.'
WHERE
	[Guid]='1BE30413-5C90-4B78-B324-BD31AA83C002'

UPDATE 
	[Attribute]
SET
	[Name]='Do Not Disturb Start'
	, [Description] = 'The hour that starts the Do Not Disturb window.'
WHERE
	[Guid]='4A558666-32C7-4490-B860-0F41358E14CA'

UPDATE 
	[Attribute]
SET
	[Name]='Do Not Disturb End'
	, [Description] = 'The hour that ends the Do Not Disturb window.'
WHERE
	[Guid]='661802FC-E636-4CE2-B75A-4AC05595A347'
" );
        }

        /// <summary>Updateds the check in success template.
        ///SK: Updated Check-in Success Template to include error messages when capacity increases threshold value
        /// </summary>
        private void UpdatedCheckInSuccessTemplate()
        {
            Sql( @"
DECLARE @CheckInSuccessTemplateId INT = (
        SELECT TOP 1 Id
        FROM Attribute
        WHERE [Guid] = 'F5BA6DCC-0A4D-4616-871D-ECBA7082C45F'
        )

UPDATE Attribute
SET [DefaultValue] = '<ol class=''checkin-messages checkin-body-container''>
{% for checkinMessage in Messages %}
    <li><div class=''alert alert-{{ checkinMessage.MessageType }}''>
        {{ checkinMessage.MessageText }}
        </div>
    </li>
    {% endfor %}
</ol>
' + [DefaultValue]
WHERE [Id] = @CheckInSuccessTemplateId AND [DefaultValue] NOT LIKE '%checkinMessage in Messages%'

UPDATE
	[AttributeValue]
SET [Value] = '<ol class=''checkin-messages checkin-body-container''>
{% for checkinMessage in Messages %}
    <li><div class=''alert alert-{{ checkinMessage.MessageType }}''>
        {{ checkinMessage.MessageText }}
        </div>
    </li>
    {% endfor %}
</ol>
' + [Value]
WHERE
[AttributeId] =@CheckInSuccessTemplateId AND [Value] NOT LIKE '%checkinMessage in Messages%'
" );

        }

        /// <summary>Updateds the attendance summary email.
        ///SK: Updated Attendance Summary Email to always include the notes if exists.
        /// </summary>
        private void UpdatedAttendanceSummaryEmail()
        {
            Sql( @"
UPDATE
	 SystemEmail
SET [Body] = Replace([Body],'{%- if AttendanceOccurrence.DidNotOccur == true -%}
The {{ Group.Name }} group did not meet.
{%- else -%}
{%- if AttendanceOccurrence.Notes -%}
<p>{{ AttendanceNoteLabel }}: <br/> {{ AttendanceOccurrence.Notes }}</p>
{%- endif -%}','{%- if AttendanceOccurrence.DidNotOccur == true -%}
The {{ Group.Name }} group did not meet.
{%- if AttendanceOccurrence.Notes -%}
<p>{{ AttendanceNoteLabel }}: <br/> {{ AttendanceOccurrence.Notes }}</p>
{%- endif -%}
{%- else -%}
{%- if AttendanceOccurrence.Notes -%}
<p>{{ AttendanceNoteLabel }}: <br/> {{ AttendanceOccurrence.Notes }}</p>
{%- endif -%}')
WHERE
	 [Guid]='CA794BD8-25C5-46D9-B7C2-AD8190AC27E6'
	 AND [Body] LIKE '%{%- if AttendanceOccurrence.DidNotOccur == true -%}
The {{ Group.Name }} group did not meet.
{%- else -%}
{%- if AttendanceOccurrence.Notes -%}
<p>{{ AttendanceNoteLabel }}: <br/> {{ AttendanceOccurrence.Notes }}</p>
{%- endif -%}%'
" );
        }

        /// <summary>Updates the content channel view.
        ///SK: Updated Content Channel view detail to use Page picker for detail page setting instead of directly providing Id
        /// </summary>
        private void UpdateContentChannelView()
        {
            Sql( @"
DECLARE @BlockTypeId int, @AttributeId int

SET @BlockTypeId = (SELECT [Id] FROM [BlockType] WHERE [Guid] = '63659EBE-C5AF-4157-804A-55C7D565110E')
SET @AttributeId = (SELECT [Id] FROM [Attribute] WHERE [Key]='DetailPage' AND [EntityTypeQualifierColumn]='BlockTypeId' AND [EntityTypeQualifierValue]=@BlockTypeId)

UPDATE [AttributeValue]
SET [AttributeValue].[Value] = [Page].[Guid]
FROM [AttributeValue]
INNER JOIN [Page] ON [AttributeValue].[Value] = CONVERT(nvarchar(10), [Page].[Id])
WHERE [AttributeValue].[AttributeId] = @AttributeId
" );
        }

        /// <summary>Removes the duplicate indexes.
        ///SK:  Remove duplicate indexes from BinaryFileData and FinancialTransactionRefund
        /// </summary>
        private void RemoveDuplicateIndexes()
        {
            Sql( @"
IF EXISTS( SELECT * FROM sys.indexes WHERE name = 'IX_Id' AND object_id = OBJECT_ID( N'[dbo].[BinaryFileData]' ) )
DROP INDEX[IX_Id] ON[dbo].[BinaryFileData]

IF EXISTS( SELECT * FROM sys.indexes WHERE name = 'IX_Id' AND object_id = OBJECT_ID( N'[dbo].[FinancialTransactionRefund]' ) )
DROP INDEX[IX_Id] ON[dbo].[FinancialTransactionRefund]
" );
        }

        #endregion

        #region 66

        /// <summary>Updates the Statement Generator.
        ///SK: Updated Statement Generator lava template to check for null StreetAddress2
        /// </summary>
        private void UpdateStatementGenerator()
        {
            Sql( @"
DECLARE @StatementDefinedTypeId int = ( SELECT TOP 1 [Id] FROM [DefinedType] WHERE [Guid] = '74A23516-A20A-40C9-93B5-1AB5FDFF6750' )
DECLARE @LavaTemplatAttributeId int = ( SELECT TOP 1 [Id] FROM [Attribute] WHERE [Key]='LavaTemplate' AND  [EntityTypeQualifierColumn]='DefinedTypeId' AND [EntityTypeQualifierValue]=@StatementDefinedTypeId )
select * from [AttributeValue] WHERE 
	[AttributeId]=@LavaTemplatAttributeId
UPDATE 
	[AttributeValue]
SET 
	[Value]=REPLACE([Value],'{% if StreetAddress2 != '''' %}','{% if StreetAddress2 != null && StreetAddress2 != '''' %}')
WHERE 
	[AttributeId]=@LavaTemplatAttributeId
" );
        }

        /// <summary>Updates the Calendar Event field.
        ///SK: Updated Calendar Event Field to have correct default value in block setting.
        /// </summary>
        private void UpdateCalendarEventField()
        {
            Sql( @"
UPDATE
	[Attribute]
SET
	[DefaultValue]='8A444668-19AF-4417-9C74-09F842572974'
 WHERE
  [Guid] 
  IN
  ('D4CD78A2-E893-46D3-A68A-2F4D0EFCA97A','BBD2DDE8-EDA0-4A54-8895-F56D55D6A450')
" );
        }

        /// <summary>Updates the PersonTokenCreate.
        /// SK: update the existing usage of PersonTokenCreate to use PersonActionIdentifier in several places.(Spilt the SQL into two(JM)
        /// This is specifically updating the Photo Upload workflow's Send Email Action body value
        /// </summary>
        private void UpdatePersonTokenCreate()
        {
            Sql( @"
                DECLARE @ActionTypeId int = (SELECT [Id] FROM [WorkflowActionType] WHERE [Guid] = '2C88DD58-40D3-4927-8A12-7968092C4929')
                DECLARE @AttributeId int = (SELECT [Id] FROM [Attribute] WHERE [Guid] = '4D245B9E-6B03-46E7-8482-A51FBA190E4D')
                DECLARE @AttributeValue nvarchar(MAX)

                IF @ActionTypeId IS NOT NULL AND @AttributeId IS NOT NULL
                BEGIN
                    SET @AttributeValue = (SELECT [Value] FROM [AttributeValue]  WHERE
                        [AttributeId] = @AttributeId
                    AND [EntityId] = @ActionTypeId)
                    
                    IF @ActionTypeId IS NOT NULL AND @AttributeId IS NOT NULL
                    SET @AttributeValue = Replace(@AttributeValue,'OptOut/{{ Person.UrlEncodedKey }}','OptOut/{{ Person | PersonActionIdentifier:''OptOut'' }}')
                    SET @AttributeValue = Replace(@AttributeValue,'Unsubscribe/{{ Person.UrlEncodedKey }}','Unsubscribe/{{ Person | PersonActionIdentifier:''Unsubscribe'' }}')
                    BEGIN
                        UPDATE
                            [AttributeValue]
                        SET
                           [Value]=@AttributeValue
                        WHERE
                            [AttributeId] = @AttributeId AND [EntityId] = @ActionTypeId
                    END
                END
" );


        }

        /// <summary>Updates the PersonTokenCreate Attribute.
        ///SK: update the existing usage of PersonTokenCreate to use PersonActionIdentifier in several places.(Spilt the SQL into two(JM))
        ///This is specifically updating the 'Unsubscribe HTML' and 'Non-HTML Content' attribute values for the Rock.Communication.Medium.Email
        /// </summary>
        private void UpdatePersonTokenCreateAttribute()
        {
            Sql( @"
                    UPDATE Attribute
                    SET DefaultValue = Replace(DefaultValue,'PersonTokenCreate:43200,3','PersonActionIdentifier:''Unsubscribe''')
                    WHERE [Guid] = '2942EFCB-9BCF-4A16-9A78-D6149E2EAAD3'

                    UPDATE AttributeValue 
                    SET [Value] = Replace([Value],'PersonTokenCreate:43200,3','PersonActionIdentifier:''Unsubscribe''')
                    WHERE AttributeId in (select Id from Attribute where [Guid] = '2942EFCB-9BCF-4A16-9A78-D6149E2EAAD3')

                    Update Attribute
                    SET DefaultValue = Replace(DefaultValue,'PersonTokenCreate:43200,3','PersonActionIdentifier:''Unsubscribe''')
                    where 
                    [Guid] = 'FDB3E4EB-DE16-4A43-AE92-B4EAA3D5DF88'

                    Update AttributeValue
                    SET [Value] = Replace([Value],'PersonTokenCreate:43200,3','PersonActionIdentifier:''Unsubscribe''')
                    where
                    AttributeId in (select Id from Attribute where [Guid] = 'FDB3E4EB-DE16-4A43-AE92-B4EAA3D5DF88')
" );
        }

        #endregion

        #region 67

        /// <summary>
        /// Re-creates the person BirthDate column as a computed-persisted column and adds an index on it
        /// (based on discussions on https://github.com/SparkDevNetwork/Rock/pull/3507)
        /// </summary>
        private void CreatePersonBirthdatePersistedIndexed()
        {
            Sql( @"
IF EXISTS(SELECT * FROM sys.indexes WHERE name = 'IX_BirthDate' AND object_id = OBJECT_ID('Person'))
BEGIN
    DROP INDEX [IX_BirthDate] ON [Person]
END

ALTER TABLE [Person]
DROP COLUMN BirthDate

-- Calculate Birthdate using TRY_CONVERT to guard against bad dates, and use 126 style so that it'll parse as style 126 (ISO-8601) which is deterministic which will let it be persisted
ALTER TABLE [Person] ADD [BirthDate] AS CASE 
		WHEN BirthYear IS NOT NULL
			THEN TRY_CONVERT(DATE, CONVERT(VARCHAR, [BirthYear]) + '-' + CONVERT(VARCHAR, [BirthMonth]) + '-' + CONVERT(VARCHAR, [BirthDay]), 126)
		ELSE NULL
		END PERSISTED

IF NOT EXISTS(SELECT * FROM sys.indexes WHERE name = 'IX_BirthDate' AND object_id = OBJECT_ID('Person'))
BEGIN
    CREATE INDEX [IX_BirthDate] ON [Person] ([BirthDate])
END
" );
        }

        #endregion

        #region 68

        /// <summary>
        /// SK: Fixed Typo in Google Maps Lava Shortcode Marker
        /// </summary>
        private void UpdatedGoogleMapsShortCode()
        {
            Sql( @"UPDATE
	[LavaShortcode]
SET
	[Documentation] = REPLACE([Documentation],'markerannimation','markeranimation'),
	[Markup] = REPLACE([Markup],'markerannimation','markeranimation'),
	[Parameters] = REPLACE([Parameters],'markerannimation','markeranimation')
WHERE
	[Guid]='FE298210-1307-49DF-B28B-3735A414CCA0'
" );
        }

        #endregion

        #region 69
        /// <summary>
        /// JM: Efs the clean up for file69 migration rollups for v8 7 6.
        /// </summary>
        private void EFCleanUpForFile69_MigrationRollupsForV8_7_6()
        {
            // Attrib for BlockType: Attendance Analytics:Filter Column Count
            RockMigrationHelper.UpdateBlockTypeAttribute( "3CD3411C-C076-4344-A9D5-8F3B4F01E31D", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Filter Column Count", "FilterColumnCount", "", @"The number of check boxes for each row.", 14, @"1", "244327E8-01EE-4860-9F12-4CF6144DFD61" );
            // Attrib for BlockType: Attendance Analytics:Filter Column Direction
            RockMigrationHelper.UpdateBlockTypeAttribute( "3CD3411C-C076-4344-A9D5-8F3B4F01E31D", "7525C4CB-EE6B-41D4-9B64-A08048D5A5C0", "Filter Column Direction", "FilterColumnDirection", "", @"Choose the direction for the checkboxes for filter selections.", 13, @"vertical", "0807FC61-26CE-41A1-9C54-8C4FA0DB6B5C" );
            // Attrib for BlockType: Giving Analytics:Filter Column Count
            RockMigrationHelper.UpdateBlockTypeAttribute( "48E4225F-8948-4FB0-8F00-1B43D3D9B3C3", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Filter Column Count", "FilterColumnCount", "", @"The number of check boxes for each row.", 4, @"1", "43B7025A-778A-4107-8243-D91A2FA74AA4" );
            // Attrib for BlockType: Giving Analytics:Filter Column Direction
            RockMigrationHelper.UpdateBlockTypeAttribute( "48E4225F-8948-4FB0-8F00-1B43D3D9B3C3", "7525C4CB-EE6B-41D4-9B64-A08048D5A5C0", "Filter Column Direction", "FilterColumnDirection", "", @"Choose the direction for the checkboxes for filter selections.", 3, @"vertical", "11C6953E-E176-40A2-9BF7-979344BD8FD8" );
            // Attrib Value for Block:Attendance Reporting, Attribute:Filter Column Count Page: Attendance Analytics, Site: Rock RMS
            RockMigrationHelper.AddBlockAttributeValue( "3EF007F1-6B46-4BCD-A435-345C03EBCA17", "244327E8-01EE-4860-9F12-4CF6144DFD61", @"3" );
            // Attrib Value for Block:Attendance Reporting, Attribute:Filter Column Direction Page: Attendance Analytics, Site: Rock RMS
            RockMigrationHelper.AddBlockAttributeValue( "3EF007F1-6B46-4BCD-A435-345C03EBCA17", "0807FC61-26CE-41A1-9C54-8C4FA0DB6B5C", @"horizontal" );
            // Attrib Value for Block:Giving Analysis, Attribute:Filter Column Count Page: Giving Analytics, Site: Rock RMS
            RockMigrationHelper.AddBlockAttributeValue( "784C58EF-B1B8-4237-BF12-E04DE8271A5A", "43B7025A-778A-4107-8243-D91A2FA74AA4", @"3" );
            // Attrib Value for Block:Giving Analysis, Attribute:Filter Column Direction Page: Giving Analytics, Site: Rock RMS
            RockMigrationHelper.AddBlockAttributeValue( "784C58EF-B1B8-4237-BF12-E04DE8271A5A", "11C6953E-E176-40A2-9BF7-979344BD8FD8", @"horizontal" );

        }

        #endregion

        #region 70

        /// <summary>
        ///NA: Updates the "Photo Request Template" communication template
        /// </summary>
        private void UpDatesPhotoRequestCommunicationTemplate()
        {
            // Updates the "Photo Request Template" communication template
            Sql( @"DECLARE @CommunicationTemplateId int = (SELECT [Id] FROM [CommunicationTemplate] WHERE [Guid] = 'B9A0489C-A823-4C5C-A9F9-14A206EC3B88')
    DECLARE @Message nvarchar(MAX)

    IF @CommunicationTemplateId IS NOT NULL
    BEGIN
        SET @Message = (SELECT [Message] FROM [CommunicationTemplate] WHERE [Id] = @CommunicationTemplateId)
        SET @Message = Replace(@Message,'OptOut/{{ Person.UrlEncodedKey }}','OptOut/{{ Person | PersonActionIdentifier:''OptOut'' }}')
        SET @Message = Replace(@Message,'Unsubscribe/{{ Person.UrlEncodedKey }}','Unsubscribe/{{ Person | PersonActionIdentifier:''Unsubscribe'' }}')
        BEGIN
            UPDATE
                [CommunicationTemplate]
            SET
                [Message] = @Message
            WHERE
                [Id] = @CommunicationTemplateId
        END
    END
" );
        }

        #endregion
        
        #region 73

        /// <summary>
        ///NA: Remove Windows Job Scheduler Service Defined Value
        /// </summary>
        private void RemoveWindowsJobSchedulerServiceValue()
        {
            RockMigrationHelper.DeleteDefinedValue( "98E421D8-0F9D-484B-B2EA-2F6FEE8E785D" );
        }

        #endregion
    }
}
