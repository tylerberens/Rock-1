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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Microsoft.AspNet.SignalR;
using OpenXmlPowerTools;
using Rock;
using Rock.Configuration;
using Rock.Data;
using Rock.SampleData;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.SampleData
{
    /// <summary>
    /// Block for managing sample data.
    /// </summary>
    [DisplayName( "Sample Data Manager" )]
    [Category( "Examples" )]
    [Description( "Create and remove sample data." )]

    public partial class SampleDataManager : RockBlock
    {
        #region Fields

        private CustomValidator _PageValidator = new CustomValidator();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the index of the current wizard page.
        /// </summary>
        /// <value>
        /// The index of the current category.
        /// </value>
        protected int CurrentPageIndex { get; set; }

        protected string SelectedActionKey { get; set; }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            _ProcessingComplete = ViewState["ProcessingComplete"] as bool? ?? false;

            CurrentPageIndex = ViewState["CurrentPageIndex"] as int? ?? 0;

            SelectedActionKey = ViewState["SelectedActionKey"] as string;
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState["ProcessingComplete"] = _ProcessingComplete;

            ViewState["CurrentPageIndex"] = this.CurrentPageIndex;
            ViewState["SelectedActionKey"] = this.SelectedActionKey;

            return base.SaveViewState();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            _PageValidator.ValidationGroup = this.BlockValidationGroup;

            Page.Validators.Add( _PageValidator );

            // Prevent browser cache, to avoid stale wizard pages.
            var cache = Page.Response.Cache;

            cache.SetCacheability( System.Web.HttpCacheability.NoCache );
            cache.SetExpires( DateTime.UtcNow.AddHours( -1 ) );
            cache.SetNoStore();

            RockPage.AddScriptLink( "~/Scripts/jquery.signalR-2.2.0.min.js", fingerprint: false );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            RegisterStartupScript();

            gActions.ShowHeaderWhenEmpty = true;
            gActions.EmptyDataText = "There are no actions to show in this view.";

            if ( Page.IsPostBack )
            {
                string parameter = Request["__EVENTARGUMENT"];

                if ( parameter == "TaskCompleted" )
                {
                    _Step = ActiveWizardStep.Finished;

                    _ProcessingComplete = true;

                    this.RefreshActiveWizardPage();
                }
            }
            else
            {
                SetActiveWizardStep( ActiveWizardStep.SelectAction );
            }
        }

        /// <summary>
        /// Registers the startup script.
        /// </summary>
        private void RegisterStartupScript()
        {
            // Set timeout for up to 30 minutes to allow for long-running tasks.
            Server.ScriptTimeout = 1800;

            var mgr = ScriptManager.GetCurrent( Page );

            if ( mgr == null )
            {
                return;
            }

            mgr.AsyncPostBackTimeout = 1800;

            // Disable asynchronous postback for the file download or it will not work correctly.
            mgr.RegisterPostBackControl( this.lbDownloadLogFile );
        }

        #endregion

        #region Wizard Navigation

        private ActiveWizardStep _Step;

        private enum ActiveWizardStep
        {
            SelectAction,
            ConfigureAction,
            Confirmation,
            Processing,
            Finished
        }

        /// <summary>
        /// Set the active step in the wizard.
        /// </summary>
        /// <param name="step"></param>
        private void SetActiveWizardStep( ActiveWizardStep step )
        {
            _Step = step;

            RefreshWizardNavigationBar();
            RefreshWizardPanelVisibility();
            RefreshActiveWizardPage();
            RefreshWizardButtons();
        }

        /// <summary>
        /// Refresh the display of the wizard navigation bar.
        /// </summary>
        /// <param name="step">Indicates which step is being displayed.</param>
        private void RefreshWizardNavigationBar()
        {
            pnlWizard.Visible = true;

            var panelControls = new Dictionary<ActiveWizardStep, HtmlGenericControl>();

            panelControls.Add( ActiveWizardStep.SelectAction, divSelectAction );
            panelControls.Add( ActiveWizardStep.ConfigureAction, divConfigureAction );
            panelControls.Add( ActiveWizardStep.Confirmation, divConfirmAction );
            panelControls.Add( ActiveWizardStep.Processing, divProcessAction );

            bool activeFound = false;

            foreach ( var panel in panelControls )
            {
                bool isActive = false;

                if ( panel.Key == _Step )
                {
                    isActive = true;
                    activeFound = true;
                }

                var control = panel.Value;

                control.Attributes.Remove( "class" );

                if ( activeFound )
                {
                    if ( isActive )
                    {
                        control.Attributes.Add( "class", "wizard-item active" );
                    }
                    else
                    {
                        control.Attributes.Add( "class", "wizard-item complete" );
                    }
                }
                else
                {
                    control.Attributes.Add( "class", "wizard-item" );
                }
            }
        }

        /// <summary>
        /// Refresh the active wizard panel.
        /// </summary>
        /// <param name="step">Indicates which step is being displayed.</param>
        private void RefreshWizardPanelVisibility()
        {
            var panelControls = new Dictionary<ActiveWizardStep, Tuple<Panel, Panel>>();

            panelControls.Add( ActiveWizardStep.SelectAction, new Tuple<Panel, Panel>( pnlSelectAction, wizSelectAction ) );
            panelControls.Add( ActiveWizardStep.ConfigureAction, new Tuple<Panel, Panel>( pnlConfigureAction, wizActionSettings ) );
            panelControls.Add( ActiveWizardStep.Confirmation, new Tuple<Panel, Panel>( pnlConfirmAction, wizConfirm ) );
            panelControls.Add( ActiveWizardStep.Processing, new Tuple<Panel, Panel>( pnlProcessAction, wizProcess ) );

            foreach ( var panel in panelControls )
            {
                var isActive = ( panel.Key == _Step );

                panel.Value.Item1.Visible = isActive;
                panel.Value.Item2.Visible = isActive;
            }
        }
       
        /// <summary>
        /// Set the status of the wizard navigation buttons.
        /// </summary>
        /// <param name="step">Indicates which step is being displayed.</param>
        private void RefreshWizardButtons()
        {
            lbConfigureAction.Enabled = false;
            lbConfirmAction.Enabled = false;
            lbProcessAction.Enabled = false;

            switch ( _Step )
            {
                case ActiveWizardStep.SelectAction:
                    break;
                case ActiveWizardStep.ConfigureAction:
                    break;
                case ActiveWizardStep.Confirmation:
                    lbConfigureAction.Enabled = true;
                    break;
                case ActiveWizardStep.Finished:
                    break;
                default:
                    break;
            }
        }

        #region Wizard LinkButton Event Handlers
        protected void lbSelectAction_Click( object sender, EventArgs e )
        {
            SetActiveWizardStep( ActiveWizardStep.SelectAction );
        }

        protected void lbConfigureAction_Click( object sender, EventArgs e )
        {
            SetActiveWizardStep( ActiveWizardStep.ConfigureAction );
        }

        protected void lbConfirmAction_Click( object sender, EventArgs e )
        {
            SetActiveWizardStep( ActiveWizardStep.Confirmation );
        }

        protected void lbProcessAction_Click( object sender, EventArgs e )
        {
            SetActiveWizardStep( ActiveWizardStep.Processing );
        }

        protected void lbSettingsNext_Click( object sender, EventArgs e )
        {
            SetActiveWizardStep( ActiveWizardStep.Confirmation );
        }

        protected void lbSettingsPrevious_Click( object sender, EventArgs e )
        {
            SetActiveWizardStep( ActiveWizardStep.SelectAction );
        }

        protected void lbConfirmPrevious_Click( object sender, EventArgs e )
        {
            SetActiveWizardStep( ActiveWizardStep.ConfigureAction );
        }

        protected void lbConfirmNext_Click( object sender, EventArgs e )
        {
            bool confirmed = chkConfirmAction.Checked;

            if ( confirmed )
            {
                _PageValidator.IsValid = true;

                SetActiveWizardStep( ActiveWizardStep.Processing );
            }
            else
            {
                _PageValidator.ErrorMessage = "Confirm that you intend to proceed with this action.";
                _PageValidator.IsValid = false;

                SetActiveWizardStep( ActiveWizardStep.Confirmation );
            }
        }

        protected void lbNext_Summary_Click( object sender, EventArgs e )
        {
            SetActiveWizardStep( ActiveWizardStep.Finished );
        }

        #endregion Wizard LinkButton Event Handlers

        private void RefreshConfirmationPanel()
        {
            RefreshSelectedActionName();
            RefreshTargetDatabaseName();
        }

        private void RefreshActiveWizardPage()
        {
            if ( _Step == ActiveWizardStep.SelectAction )
            {
                chkConfirmAction.Checked = false;

                RefreshActionsPanel();
            }
            else if ( _Step == ActiveWizardStep.ConfigureAction )
            {
                RefreshSettingsPanel();
            }
            else if ( _Step == ActiveWizardStep.Confirmation )
            {
                RefreshConfirmationPanel();
            }
            else if ( _Step == ActiveWizardStep.Processing )
            {
                if ( _ProcessingComplete )
                {
                    this.ShowResult();
                }
                else
                {
                    this.ProcessActionAsync();
                }
            }
            else
            {
                throw new Exception( "Active Wizard Page is invalid." );
            }
        }

        private void ShowResult()
        {
            pnlResult.Style.Remove( "display" );

            // Get the last result from the Log.
            var message = _Monitor.GetResultMessages().LastOrDefault();

            if ( message == null )
            {
                //taskInfo.StatusMessage = "Task failed.";
            }
            else
            {
                //taskInfo.StatusMessage = message.Message;

                if ( message.EventType == TaskLogEventTypeSpecifier.ResultSuccess )
                {
                    nbResult.NotificationBoxType = NotificationBoxType.Success;
                    nbResult.Text = "<strong>Import completed.</strong><p>" + message.Message + "</p>";
                }
                else
                {
                    nbResult.NotificationBoxType = NotificationBoxType.Danger;
                    nbResult.Text = "<strong>Import failed.</strong><p>" + message.Message + "</p>";
                }
            }
        }

        #endregion Wizard Navigation Control

        #region Wizard Panel: Select Action

        private void RefreshActionsPanel()
        {
            gActions.DataSource = this.GetActions();

            gActions.ItemType = "Action";

            gActions.DataBind();
        }

        private List<SampleDataChangeAction> _Actions = null;

        public string SelectedActionName { get; set; }

        private List<SampleDataChangeAction> GetActions()
        {
            if ( _Actions == null )
            {
                var manager = new SampleDataProcessorFactory();

                _Actions = manager.GetActions();
            }

            return _Actions;
        }

        private SampleDataChangeAction GetSelectedAction()
        {
            var actions = this.GetActions();

            var selectedAction = actions.FirstOrDefault( x => x.Key == this.SelectedActionKey );

            return selectedAction;
        }
        protected void gActions_RowSelected( object sender, RowEventArgs e )
        {
            this.SelectedActionKey = e.RowKeyValue.ToStringSafe();

            this.SetActiveWizardStep( ActiveWizardStep.ConfigureAction );
        }

        #endregion

        #region Wizard Panel: Settings

        private void RefreshSettingsPanel()
        {
            RefreshSelectedActionName();
        }

        #endregion        

        #region Wizard Panel: Process Action

        protected void lbRestart_Click( object sender, EventArgs e )
        {
            SetActiveWizardStep( ActiveWizardStep.SelectAction );
        }

        protected void lbCancel_Click( object sender, EventArgs e )
        {
            SetActiveWizardStep( ActiveWizardStep.SelectAction );
        }

        protected void lbDownloadLogFile_Click( object sender, EventArgs e )
        {
            // Get the file content as a stream of bytes.
            var fi = new FileInfo( this.GetLogFilePath() );

            if ( !fi.Exists )
            {
                return;
            }

            byte[] byteArray;

            using ( var ms = new MemoryStream() )
            {
                var reader = fi.OpenRead();

                reader.CopyTo( ms );

                byteArray = ms.ToArray();
            }

            string fileName = string.Format( "SampleDataManager_ActivityLog_{0:yyyyMMdd_hhmmss}.html", this.TaskStartTime.GetValueOrDefault() );

            // Transmit the file to the client browser.
            this.Page.EnableViewState = false;

            var response = this.Page.Response;

            response.Clear();
            response.ContentType = "text/plain";
            response.AppendHeader( "Content-Disposition", string.Format( "attachment; filename=\"{0}\"", fileName ) );

            response.Charset = string.Empty;

            response.BinaryWrite( byteArray );
            response.Flush();
            response.End();
        }

        #endregion

        #region Processing and Results

        private bool _ProcessingComplete = false;
        private StreamWriter _FileStream = null;

        public TaskMonitor OpenTaskMonitorWithDebugOutput()
        {
            var monitor = new TaskMonitor();

            // Set the log level.
            monitor.LogEventLevelFilter = _ClientNotificationLevel;

            return monitor;
        }

        private void ProcessActionAsync()
        {
            this.TaskID = Guid.NewGuid().ToString();

            Task.Factory.StartNew( () =>
            {
                this.ProcessAction();
            },
                                   TaskCreationOptions.LongRunning ).ContinueWith( ( x ) => { this.ProcessActionComplete( x ); } );

            pnlResult.Style.Add( "display", "none" );

            this.UpdateTaskLog();
        }

        /// <summary>
        /// A unique identifier for the running task instance.
        /// </summary>
        public string TaskID
        {
            get
            {
                return ViewState["TaskID"].ToStringSafe();
            }
            set
            {
                ViewState["TaskID"] = value;
            }
        }

        /// <summary>
        /// The time at which the active task was started.
        /// </summary>
        public DateTime? TaskStartTime
        {
            get
            {
                return ViewState["TaskStartTime"] as DateTime?;
            }
            set
            {
                ViewState["TaskStartTime"] = value;
            }
        }

        private string GetLogFilePath()
        {
            return string.Format( "{0}SampleDataManager_{1}.log", Path.GetTempPath(), this.TaskID );
        }

        private void ProcessActionComplete( Task task )
        {
            RefreshActiveWizardPage();
        }

        private void ProcessAction()
        {
            this.TaskStartTime = DateTime.Now;

            var selectedAction = GetSelectedAction();

            if ( selectedAction == null )
            {
                return;
            }

            InitializeTaskMonitor();

            _ProcessingComplete = false;

            try
            {
                _FileStream = new StreamWriter( this.GetLogFilePath() );

                var factory = new SampleDataProcessorFactory();

                var processor = factory.GetProcessorForAction( selectedAction.Key );

                // Force log a message to indicate the current user interface log level.
                _Monitor.LogMessage( new TaskMessage { EventLevel = TaskLogEventLevelSpecifier.Critical, EventType = TaskLogEventTypeSpecifier.Information, Message = string.Format( "Log Message Level: {0}.", _ClientNotificationLevel ) } );

                // Execute Action
                using ( var activity = _Monitor.AddActivity( selectedAction.Name ) )
                {
                    activity.LogInformation( "Initializing..." );

                    // Remove the processor key prefix to get the action key.
                    var actionKey = selectedAction.Key.SplitDelimitedValues( "::" ).Last();

                    processor.ExecuteAction( actionKey, null, activity );

                    if ( _Monitor.HasErrors )
                    {
                        _Monitor.LogActionFailure( "Processing completed with one or more errors [{0} processed, {1} errors, {2} warnings]",
                                                    activity.Activities.Count(),
                                                    _Monitor.GetErrorMessages().Count,
                                                    _Monitor.GetWarningMessages().Count );

                    }
                    else
                    {
                        _Monitor.LogActionSuccess( "Processing completed successfully." );

                        nbResult.NotificationBoxType = NotificationBoxType.Success;
                        nbResult.Title = "Processing Completed.";
                        nbResult.Text = string.Format( "Refer to the activity log for details." );
                    }
                }

            }
            catch ( Exception ex )
            {
                _Monitor.LogException( ex );
            }
            finally
            {
                this.UpdateTaskLog();

                _FileStream.Close();
            }

            _ProcessingComplete = true;

            this.NotifyTaskComplete();
        }

        #endregion

        #region Task Log

        private const TaskLogEventLevelSpecifier _LogNotificationLevel = TaskLogEventLevelSpecifier.Low;
        private const TaskLogEventLevelSpecifier _ClientNotificationLevel = TaskLogEventLevelSpecifier.Low;

        private readonly IHubContext<ITaskMonitorMessageHub> _MessageHub = GlobalHost.ConnectionManager.GetHubContext<TaskMonitorMessageHub, ITaskMonitorMessageHub>();
        private readonly List<TaskMessage> _NewMessages = new List<TaskMessage>();
        private TaskActivityMessage _LastClientReport = null;
        private TaskMonitor _Monitor;

        public event EventHandler TaskCompleted;

        private void UpdateTaskProgress( TaskStatusReport report )
        {
            var clientReport = new TaskActivityMessage();

            clientReport.CompletionPercentage = Math.Round( report.TaskRelativeProgress, 1 );

            var activities = _Monitor.Activities.Flatten();

            int actionCount = 0;

            // Get statistics for Actions.
            var actions = activities.ToList();

            int completedActions = actions.Count( a => a.ExecutionState == TaskActivityExecutionStateSpecifier.Completed );

            int errorCount = activities.Count( a => a.Result == TaskActivityResultSpecifier.Failed );
            int warningCount = activities.Count( a => a.Result == TaskActivityResultSpecifier.CompletedWithWarnings );

            string message = "Completed ";

            message += string.Format( "{0} of {1} actions.", completedActions, actionCount );

            if ( errorCount > 0
                 || warningCount > 0 )
            {
                message += string.Format( " ({0} errors, {1} warnings)", errorCount, warningCount );
            }

            clientReport.ProgressSummary = message;

            // Ignore the report if it is the same as the previous update.
            if ( _LastClientReport != null && _LastClientReport.Equals( clientReport ) )
            {
                return;
            }

            _LastClientReport = clientReport;

            _MessageHub.Clients.All.UpdateTaskProgress( clientReport );
        }

        private void NotifyTaskComplete()
        {
            var taskInfo = new TaskInfoMessage();

            taskInfo.IsFinished = true;

            // Get the last result from the Log.
            var message = _Monitor.GetResultMessages().LastOrDefault();

            if ( message == null )
            {
                taskInfo.StatusMessage = "Task failed.";
                taskInfo.HasErrors = true;
            }
            else
            {
                taskInfo.StatusMessage = message.Message;

                if ( message.EventType == TaskLogEventTypeSpecifier.ResultSuccess )
                {
                    taskInfo.StatusMessage = "<strong>Import completed.</strong><p>" + message.Message + "</p>";
                }
                else
                {
                    taskInfo.StatusMessage = "<strong>Import failed.</strong><p>" + message.Message + "</p>";
                    taskInfo.HasErrors = true;
                }
            }

            _MessageHub.Clients.All.NotifyTaskComplete( taskInfo );
        }

        private void UpdateTaskLog()
        {
            int count = _NewMessages.Count;

            if ( count == 0 )
            {
                return;
            }

            const int maxVisibleMessages = 50;

            List<TaskMessage> showMessages;

            if ( count > maxVisibleMessages )
            {
                showMessages = _NewMessages.Skip( count - maxVisibleMessages ).Take( maxVisibleMessages ).ToList();
            }
            else
            {
                showMessages = _NewMessages.ToList();
            }

            _NewMessages.Clear();

            var logMessages = new List<string>();
            var clientMessages = new List<string>();

            foreach ( var message in showMessages )
            {
                // Ignore messages that are below the notification threshold set for both the log file and the client page.
                if ( message.EventLevel < _LogNotificationLevel
                     && message.EventLevel < _ClientNotificationLevel )
                {
                    continue;
                }

                // Write to Trace Output.
                //string messageText = message.Message;

                string style = null;

                switch ( message.EventType )
                {
                    case TaskLogEventTypeSpecifier.Error:
                        style = "color: red";
                        break;
                    case TaskLogEventTypeSpecifier.Warning:
                        style = "color: orange";
                        break;
                    case TaskLogEventTypeSpecifier.ResultSuccess:
                        style = "color: green; font-weight: bold";
                        break;

                    case TaskLogEventTypeSpecifier.ResultFailure:
                        style = "color: red; font-weight: bold";
                        break;
                    default:
                        // Use the default style.
                        break;
                }

                if ( !string.IsNullOrWhiteSpace( style ) )
                {
                    style = string.Format( " style='{0}'", style );
                }

                string simpleMessage = string.Format( "<div{0}>{1} {2}</div>", style, message.TimeStamp.ToString( "HH:mm:ss.fff" ), message.Message );
                string detailMessage = simpleMessage;

                if ( !string.IsNullOrWhiteSpace( message.Details ) )
                {
                    detailMessage += "<div>" + message.Details + "</div>";
                }

                if ( message.EventLevel >= _LogNotificationLevel )
                {
                    logMessages.Add( detailMessage );
                }

                if ( message.EventLevel >= _ClientNotificationLevel )
                {
                    clientMessages.Add( simpleMessage );
                }
            }

            // Write messages to the log file.
            if ( logMessages.Count > 0 )
            {
                _FileStream.Write( logMessages.StringConcatenate() );
            }

            // Write messages to the client page.
            if ( clientMessages.Count > 0 )
            {
                _MessageHub.Clients.All.UpdateTaskLog( clientMessages.StringConcatenate() );
            }
        }

        private void InitializeTaskMonitor()
        {
            _Monitor = new TaskMonitor();

            _Monitor.TaskUpdated += _Monitor_TaskUpdated;

            _Monitor.LogUpdated += ( sender, args ) =>
            {
                _NewMessages.AddRange( args.NewMessages );

                this.UpdateTaskLog();
            };
        }

        void _Monitor_TaskUpdated( object sender, TaskMonitorUpdateEventArgs args )
        {
            this.UpdateTaskProgress( args.TaskStatus );
        }

        protected void tmrTaskProgress_OnTick( object sender, EventArgs e )
        {
            this.UpdateTaskLog();
        }

        #endregion

        #region ViewModel Properties

        public string TargetDatabaseName { get; set; }

        private void RefreshSelectedActionName()
        {
            var selectedAction = GetSelectedAction();

            if ( selectedAction != null )
            {
                this.SelectedActionName = string.Format( "{0} > {1}", selectedAction.Category, selectedAction.Name );
            }
            else
            {
                this.SelectedActionName = "(none)";
            }
        }

        private void RefreshTargetDatabaseName()
        {
            RefreshSelectedActionName();

            var dataContext = new RockContext();

            this.TargetDatabaseName = ApplicationConfigurationHelper.GetDatabaseDescription( dataContext.Database.Connection.ConnectionString );
        }

        #endregion
    }
}