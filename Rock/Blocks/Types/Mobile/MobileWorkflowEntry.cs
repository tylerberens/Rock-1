using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.Blocks.Types.Mobile
{
    [DisplayName( "Mobile Workflow Entry" )]
    [Category( "Mobile" )]
    [Description( "Allows for filling out workflows from a mobile application." )]
    [IconCssClass( "fa fa-gears" )]

    #region Block Attributes

    [WorkflowTypeField( "Workflow Type",
        "The type of workflow to launch when viewing this.",
        false,
        true,
        order: 0 )]

    #endregion

    public class MobileWorkflowEntry : RockBlockType, IRockMobileBlockType
    {
        public static class AttributeKeys
        {
            public const string WorkflowType = "WorkflowType";
        }

        #region IRockMobileBlockType Implementation

        /// <summary>
        /// Gets the required mobile API version required to render this block.
        /// </summary>
        /// <value>
        /// The required mobile API version required to render this block.
        /// </value>
        int IRockMobileBlockType.RequiredMobileApiVersion => 1;

        /// <summary>
        /// Gets the class name of the mobile block to use during rendering on the device.
        /// </summary>
        /// <value>
        /// The class name of the mobile block to use during rendering on the device
        /// </value>
        string IRockMobileBlockType.MobileBlockType => "Rock.Mobile.Blocks.WorkflowEntry";

        /// <summary>
        /// Gets the property values that will be sent to the device in the application bundle.
        /// </summary>
        /// <returns>
        /// A collection of string/object pairs.
        /// </returns>
        object IRockMobileBlockType.GetMobileConfigurationValues()
        {
            return new { };
        }

        #endregion

        #region Action Methods

        /// <summary>
        /// Gets the current configuration for this block.
        /// </summary>
        /// <returns>A collection of string/string pairs.</returns>
        [BlockAction]
        public WorkflowForm GetNextForm( int? workflowId )
        {
            var workflowType = WorkflowTypeCache.Get( GetAttributeValue( AttributeKeys.WorkflowType ).AsGuid() );
            var rockContext = new RockContext();
            var workflowService = new WorkflowService( rockContext );

            var workflow = Rock.Model.Workflow.Activate( workflowType, $"New {workflowType.Name}" );
            workflowService.Process( workflow, null, out var errorMessages );
            var activity = workflow.ActiveActivities.First();
            var action = activity.ActiveActions.First();
            var form = action.ActionTypeCache.WorkflowForm;

            var mobileForm = new WorkflowForm
            {
                Message = "Workflow form summary message.",
                ButtonTitles = new List<string> { "Submit" },
                Fields = new List<MobileField>()
            };

            foreach ( var formAttribute in form.FormAttributes.OrderBy( a => a.Order ) )
            {
                if ( formAttribute.IsVisible )
                {
                    var attribute = AttributeCache.Get( formAttribute.AttributeId );

                    string value = attribute.DefaultValue;
                    if ( workflow != null && workflow.AttributeValues.ContainsKey( attribute.Key ) && workflow.AttributeValues[attribute.Key] != null )
                    {
                        value = workflow.AttributeValues[attribute.Key].Value;
                    }
                    // Now see if the key is in the activity attributes so we can get it's value
                    else if ( activity != null && activity.AttributeValues.ContainsKey( attribute.Key ) && activity.AttributeValues[attribute.Key] != null )
                    {
                        value = activity.AttributeValues[attribute.Key].Value;
                    }

                    //if ( !string.IsNullOrWhiteSpace( formAttribute.PreHtml ) )
                    //{
                    //    phAttributes.Controls.Add( new LiteralControl( formAttribute.PreHtml.ResolveMergeFields( mergeFields ) ) );
                    //}

                    var mobileField = new MobileField
                    {
                        Key = attribute.Key,
                        Title = attribute.Name,
                        IsRequired = formAttribute.IsRequired,
                        ConfigurationValues = attribute.QualifierValues.ToDictionary( kvp => kvp.Key, kvp => kvp.Value.Value ),
                        RockFieldType = attribute.FieldType.Class,
                        Value = value
                    };

                    if ( formAttribute.IsReadOnly )
                    {
                        var field = attribute.FieldType.Field;

                        string formattedValue = null;

                        // get formatted value 
                        if ( attribute.FieldType.Class == typeof( Rock.Field.Types.ImageFieldType ).FullName )
                        {
                            formattedValue = field.FormatValueAsHtml( null, attribute.EntityTypeId, activity.Id, value, attribute.QualifierValues, true );
                        }
                        else
                        {
                            formattedValue = field.FormatValueAsHtml( null, attribute.EntityTypeId, activity.Id, value, attribute.QualifierValues );
                        }

                        mobileField.Value = formattedValue;
                        mobileField.RockFieldType = string.Empty;

                        if ( formAttribute.HideLabel )
                        {
                            mobileField.Title = string.Empty;
                        }
                    }

                    mobileForm.Fields.Add( mobileField );

                    //if ( !string.IsNullOrWhiteSpace( formAttribute.PostHtml ) )
                    //{
                    //    phAttributes.Controls.Add( new LiteralControl( formAttribute.PostHtml.ResolveMergeFields( mergeFields ) ) );
                    //}

                }
            }

            return mobileForm;
        }

        #endregion

        public class WorkflowForm
        {
            public string Message { get; set; }

            public List<MobileField> Fields { get; set; }

            public List<string> ButtonTitles { get; set; }
        }

        public class MobileField
        {
            public string Title { get; set; }

            public string Key { get; set; }

            public string Value { get; set; }

            public string RockFieldType { get; set; }

            public Dictionary<string, string> ConfigurationValues { get; set; } = new Dictionary<string, string>();

            public bool IsRequired { get; set; }
        }
    }
}
