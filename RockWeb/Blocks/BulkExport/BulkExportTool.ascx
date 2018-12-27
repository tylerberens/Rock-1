<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BulkExportTool.ascx.cs" Inherits="RockWeb.Blocks.BulkExport.BulkExportTool" %>


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
                <Rock:RockTextBox ID="tbFileName" runat="server" Label="The filename for Slingshot file." Required="true" />
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
