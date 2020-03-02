<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SampleDataManager.ascx.cs" Inherits="RockWeb.Blocks.SampleData.SampleDataManager" %>
<%@ Import Namespace="RockWeb.Blocks.SampleData" %>
<%@ Register TagPrefix="Rock" Namespace="Rock.Web.UI.Controls" Assembly="Rock" %>

<%-- SignalR Functions --%>
<script src="/SignalR/hubs"></script>
<script type="text/javascript">
    $(function () {
        <%-- Create the SignalR proxy and message event handlers --%>
        var proxy = $.connection.TaskMonitorMessageHub;

        proxy.client.showLog = function () {
            $("div[id$='_messageContainer']").fadeIn();
        }

        <%-- ShowLog event handler --%>
        proxy.client.UpdateTaskLog = function (message) {
            var container = $("[id$='_messageContainer']");
            var maxBufferSize = 100;
            var messageCount = container.children().length;
            if (messageCount >= maxBufferSize) {
                container.children().slice(0, messageCount - maxBufferSize + 1).remove();
            }
            container.append(message);
            var height = container[0].scrollHeight;
            container.scrollTop(height);
        }

        <%-- Event Handler for Task Progress Updates --%>
        proxy.client.UpdateTaskProgress = function (taskActivityMessage) {
            var progressMessage = $("[id$='_ProgressMessage']");
            progressMessage.html(taskActivityMessage.Message);
            var progressMeter = $("[id$='_ProgressBar']");
            progressMeter.css("width", taskActivityMessage.CompletionPercentage + "%");
            var progressText = $("[id$='_ProgressPercentage']");
            progressText.text(taskActivityMessage.CompletionPercentage.toFixed(1) + "% complete");
            var progressSummary = $("[id$='_ProgressSummary']");
            progressSummary.html(taskActivityMessage.ProgressSummary);
        }

        proxy.client.NotifyTaskComplete = function (taskInfoMessage) {

            proxy.client.UpdateTaskProgress({ Message: "Task Complete.", CompletionPercentage: 100, ProgressSummary: "" });
            //var progressPanel = $("[id$='_ProgressPanel']");
            //progressPanel.hide();
            var resultMessage = $("[id$='_nbResult']");
            resultMessage.html(taskInfoMessage.StatusMessage);
            if (taskInfoMessage.HasErrors) {
                var resultBox = $("[id$='_nbResult']");
                resultBox.css("NotificationBoxType", "danger");
            }
            var resultPanel = $("[id$='_pnlResult']");
            resultPanel.show();
        }

        <%-- Hub disconnection handler --%>
        $.connection.hub.disconnected(function () {
            <%-- Restart the hub proxy if it is disconnected for any reason. --%>
            $.connection.hub.start();
        });

        <%-- Start the SignalR hub proxy --%>
        $.connection.hub.start();
    })
</script>
<style>
    .scrollableContainer {
        /*width: 200px;*/
        height: 300px;
        overflow-y: scroll;
    }

    .btn-selected {
        background: #afd074;
        color: #fff;
    }
</style>

<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <div>
            <div class="panel panel-block">
                <asp:Panel ID="pnlSelectAction" runat="server" CssClass="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-clipboard"></i>
                        Sample Data Manager - Choose an Action</h1>
                </asp:Panel>
                <asp:Panel ID="pnlConfigureAction" runat="server" CssClass="panel-heading" Visible="false">
                    <h1 class="panel-title"><i class="fa fa-file-o"></i>
                        Sample Data Manager - Configure Action Settings</h1>
                </asp:Panel>
                <asp:Panel ID="pnlConfirmAction" runat="server" CssClass="panel-heading" Visible="false">
                    <h1 class="panel-title"><i class="fa fa-users"></i>
                        Sample Data Manager - Confirmation</h1>
                </asp:Panel>
                <asp:Panel ID="pnlProcessAction" runat="server" CssClass="panel-heading" Visible="false">
                    <h1 class="panel-title"><i class="fa fa-users"></i>
                        Sample Data Manager - Process Action</h1>
                </asp:Panel>

                <!-- Wizard Navigation Bar -->
                <asp:Panel ID="pnlWizard" runat="server" CssClass="wizard" Visible="false">

                    <div id="divSelectAction" runat="server" class="wizard-item">
                        <asp:LinkButton ID="lbSelectAction" runat="server" OnClick="lbSelectAction_Click" CausesValidation="false">
                            <%-- Placeholder needed for bug. See: http://stackoverflow.com/questions/5539327/inner-image-and-text-of-asplinkbutton-disappears-after-postback--%>
                            <asp:PlaceHolder runat="server">
                                <div class="wizard-item-icon">
                                    <i class="fa fa-fw fa-clipboard"></i>
                                </div>
                                <div class="wizard-item-label">
                                    Select Action
                                </div>
                            </asp:PlaceHolder>
                        </asp:LinkButton>
                    </div>

                    <div id="divConfigureAction" runat="server" class="wizard-item">
                        <asp:LinkButton ID="lbConfigureAction" runat="server" OnClick="lbConfigureAction_Click" CausesValidation="false">
                            <asp:PlaceHolder runat="server">
                                <div class="wizard-item-icon">
                                    <i class="fa fa-fw fa-file-o"></i>
                                </div>
                                <div class="wizard-item-label">
                                    Configure Settings
                                </div>
                            </asp:PlaceHolder>
                        </asp:LinkButton>
                    </div>

                    <div id="divConfirmAction" runat="server" class="wizard-item">
                        <asp:LinkButton ID="lbConfirmAction" runat="server" OnClick="lbConfirmAction_Click" CausesValidation="false">
                            <asp:PlaceHolder runat="server">
                                <div class="wizard-item-icon">
                                    <i class="fa fa-fw fa-users"></i>
                                </div>
                                <div class="wizard-item-label">
                                    Review and Confirm
                                </div>
                            </asp:PlaceHolder>
                        </asp:LinkButton>
                    </div>

                    <div id="divProcessAction" runat="server" class="wizard-item">
                        <asp:LinkButton ID="lbProcessAction" runat="server" OnClick="lbProcessAction_Click" CausesValidation="false">
                            <asp:PlaceHolder runat="server">
                                <div class="wizard-item-icon">
                                    <i class="fa fa-fw fa-calendar-check-o"></i>
                                </div>
                                <div class="wizard-item-label">
                                    Process
                                </div>
                            </asp:PlaceHolder>
                        </asp:LinkButton>
                    </div>
                </asp:Panel>

                <!-- Wizard Panels -->
                <div class="panel-body">

                    <%-- Panel: Select Action --%>
                    <asp:Panel ID="wizSelectAction" runat="server">
                        <fieldset>
                            <Rock:NotificationBox runat="server"
                                NotificationBoxType="Warning"
                                Title="Important">
                                <br>
                                This utility provides actions for creating and removing sample data in the current database.
                                <br>
                                It is intended for testing purposes only, and should not be used to modify data in a production database.
                            </Rock:NotificationBox>
                            <p>
                                Select an action to perform for the current database.
                            </p>


                            <Rock:Grid ID="gActions" runat="server" DisplayType="Full" AllowSorting="true" DataKeyNames="Key" OnRowSelected="gActions_RowSelected">
                                <Columns>
                                    <Rock:RockBoundField DataField="Category" SortExpression="Category" HeaderText="Category" />
                                    <Rock:RockTemplateField HeaderText="Action" SortExpression="Action">
                                        <ItemTemplate>
                                            <div><strong><u><%# Eval("Name") %></u></strong></div>
                                            <div><small><%# Eval("Description") %></small></div>
                                        </ItemTemplate>
                                    </Rock:RockTemplateField>
                                </Columns>
                            </Rock:Grid>

                        </fieldset>
                    </asp:Panel>

                    <%--Panel: Action Settings --%>
                    <asp:Panel ID="wizActionSettings" runat="server">
                        <asp:ValidationSummary ID="valSummaryTop" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />
                        <p>
                            Specify the configuration settings for action "<%= SelectedActionName %>".
                        </p>
                        <Rock:NotificationBox runat="server"
                            NotificationBoxType="Info"
                            Title="Action Configuration">
                                <br>
                                If the action has any configuration settings, they will be shown here using the Attribute Editor.
                        </Rock:NotificationBox>

                        <div class="row">
                            <div class="col-md-2 text-right"><strong>Setting 1:</strong></div>
                            <div class="col-md-10">(Value 1)</div>
                        </div>
                        <div class="row">
                            <div class="col-md-2 text-right"><strong>Setting 2:</strong></div>
                            <div class="col-md-10">(Value 2)</div>
                        </div>
                        <div class="row">
                            <div class="col-md-2 text-right"><strong>Setting 3:</strong></div>
                            <div class="col-md-10">(Value 2)</div>
                        </div>
                        <div class="actions">
                            <asp:LinkButton ID="lbSettingsPrevious" runat="server" AccessKey="p" ToolTip="Alt+p" Text="Previous" CssClass="btn btn-default js-wizard-navigation" CausesValidation="false" OnClick="lbSettingsPrevious_Click" />
                            <asp:LinkButton ID="lbSettingsNext" runat="server" AccessKey="n" Text="Next" DataLoadingText="Next" CssClass="btn btn-primary pull-right js-wizard-navigation" OnClick="lbSettingsNext_Click" />
                        </div>

                    </asp:Panel>

                    <%--Panel: Confirmation --%>
                    <asp:Panel ID="wizConfirm" runat="server">
                        <asp:ValidationSummary ID="vsConfirm" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-warning" />
                        <fieldset>
                            <p>
                                If you proceed, the action you have selected will be processed in the current database.
                            </p>
                            <div class="panel panel-body">
                                <div class="row">
                                    <div class="col-md-6">
                                        <div>
                                            <strong>Selected Action:</strong> <%= SelectedActionName %>
                                        </div>
                                        <div>
                                            <strong>Target Database:</strong> <%= TargetDatabaseName %>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="alert alert-danger">
                                <strong>Warning!</strong>

                                <p>If you continue, the changes applied to the target database may be irreversible.</p>
                                <p>
                                    Please ensure that you have a backup of your database before proceeding with this operation.
                                </p>

                            </div>
                            <Rock:RockCheckBox ID="chkConfirmAction" runat="server" Text="Yes, I want to apply the selected action to the target database." />

                            <div class="actions">
                                <asp:LinkButton ID="lbConfirmPrevious" runat="server" AccessKey="p" ToolTip="Alt+p" Text="Previous" CssClass="btn btn-default js-wizard-navigation" CausesValidation="false" OnClick="lbConfirmPrevious_Click" />
                                <asp:LinkButton ID="lbConfirmNext" runat="server" AccessKey="n" Text="Finish" DataLoadingText="Running" CssClass="btn btn-primary pull-right js-wizard-navigation" OnClick="lbConfirmNext_Click" />
                            </div>
                        </fieldset>
                    </asp:Panel>

                    <%--Panel: Processing Log and Result --%>
                    <asp:Panel ID="wizProcess" runat="server">
                        <asp:Panel ID="pnlProcessing" runat="server">
                            <div class="panel">
                                <div class="panel-heading">
                                    <p>Please wait while the requested actions are being processed.</p>
                                </div>
                                <div class="panel-body">
                                    <asp:UpdatePanel ID="pnlProcess" runat="server" Visible="true" UpdateMode="Conditional">
                                        <ContentTemplate>
                                            <div id="ProgressPanel" runat="server" class="progress" style="position: relative">
                                                <div id="ProgressBar" runat="server" class="progress-bar progress-bar-info progress-bar-striped active" role="progressbar"
                                                    aria-valuenow="0" aria-valuemin="0" aria-valuemax="100" style="width: 0%">
                                                    <span id="ProgressPercentage" runat="server" style="position: absolute; display: block; color: black; width: 100%; text-align: center">0.0% complete</span>
                                                </div>
                                            </div>
                                            <div id="ProgressMessage" runat="server" visible="true">Running...</div>
                                            <div id="ProgressSummary" runat="server" visible="true">(0 processed)</div>
                                        </ContentTemplate>

                                    </asp:UpdatePanel>
                                    <h3>Activity Log</h3>
                                    <div class="alert alert-info scrollableContainer" id="messageContainer" runat="server" visible="true">
                                    </div>
                                </div>
                            </div>
                        </asp:Panel>
                        <asp:Panel ID="pnlResult" runat="server">
                            <Rock:NotificationBox ID="nbResult" runat="server" NotificationBoxType="Success" Text="The Sample Data management task is finished." />
                            <div class="actions">
                                <asp:LinkButton ID="lbRestart" runat="server" Text="Restart" CssClass="btn btn-primary" OnClick="lbRestart_Click" CausesValidation="false" Visible="true" />
                                <asp:LinkButton ID="lbDownloadLogFile" runat="server" Text="Download Log File" CssClass="btn" OnClick="lbDownloadLogFile_Click" CausesValidation="false" Visible="true" />
                            </div>
                        </asp:Panel>
                    </asp:Panel>
                </div>
            </div>
        </div>

    </ContentTemplate>
</asp:UpdatePanel>
