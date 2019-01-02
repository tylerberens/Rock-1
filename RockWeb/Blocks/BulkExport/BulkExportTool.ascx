<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BulkExportTool.ascx.cs" Inherits="RockWeb.Blocks.BulkExport.BulkExportTool" %>

<script src="/SignalR/hubs"></script>
<script type="text/javascript">
    $(function ()
    {
        var proxy = $.connection.rockMessageHub;

        proxy.client.receiveNotification = function (name, message, results, completed, total)
        {
            if (name == '<%=this.SignalRNotificationKey %>') {
                $('#<%=pnlProgress.ClientID%>').show();

                if (message) {
                    $('#<%=lProgressMessage.ClientID %>').html(message);
                }

                var $bar = $('#<%= pnlProgress.ClientID %> .js-progress-bar');
                $bar.prop('aria-valuenow', completed);
                $bar.prop('aria-valuemax', total);
                $bar.css('width', (completed.replace(',','') / total.replace(',','') * 100) + '%');
                $bar.text(completed + '/' + total);

                if (results) {
                    $('#<%=lProgressResults.ClientID %>').html(results);
                }
            }
        }

        proxy.client.showDetails = function (name, visible)
        {
            if (name == '<%=this.SignalRNotificationKey %>') {

                if (visible) {
                    $('#<%=lProgressResults.ClientID%>').parent().show();
                }
                else {
                    $('#<%=lProgressResults.ClientID%>').parent().hide();
                }
            }
        }

        $.connection.hub.start().done(function ()
        {
            // hub started... do stuff here if you want to let the user know something
        });
    })
</script>

<asp:UpdatePanel ID="upnlContent" runat="server" ChildrenAsTriggers="false" UpdateMode="Conditional">
    <ContentTemplate>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-download"></i>&nbsp;Bulk Export Tool</h1>
            </div>
            <div class="panel-body">
                <asp:ValidationSummary ID="vsBulkExport" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" />
                <Rock:NotificationBox ID="nbPersonsAlert" runat="server" NotificationBoxType="Validation" />
                <Rock:DataViewItemPicker ID="dpDataView" runat="server" Label="Person Dataview" Help="The dataview that will return a list of people that meet the criteria." Required="true" />
                <Rock:RockTextBox ID="tbFileName" runat="server" Label="Filename for the *.slingshot file" Help="The tool will automatically append the .slingshot suffix." Placeholder="MySetOfPeople" Required="true" />

                <asp:Panel ID="pnlProgress" runat="server" CssClass="js-messageContainer" Style="display: none">
                    <strong>Progress</strong><br />
                    <div class="alert alert-info">
                        <asp:Label ID="lProgressMessage" CssClass="js-progressMessage" runat="server" />
                        <div class="progress">
                          <div class="progress-bar progress-bar-info js-progress-bar" role="progressbar" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100" style="width: 0%;">
                            0%
                          </div>
                        </div>

                        <pre><asp:Label ID="lProgressResults" CssClass="js-progressResults" runat="server" /></pre>

                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlActions" runat="server" CssClass="actions">
                    <asp:LinkButton ID="btnExport" runat="server" CssClass="btn btn-primary" Text="Export" OnClick="btnExport_Click" />
                </asp:Panel>
            </div>

        </asp:Panel>

    </ContentTemplate>
    <Triggers>
        <asp:PostBackTrigger ControlID="btnExport" />
    </Triggers>
</asp:UpdatePanel>
