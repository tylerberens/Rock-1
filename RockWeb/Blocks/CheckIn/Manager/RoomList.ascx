<%@ Control Language="C#" AutoEventWireup="true" CodeFile="RoomList.ascx.cs" Inherits="RockWeb.Blocks.CheckIn.Manager.RoomList" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <%-- TODO Temp Style --%>
        <style>
            .criteria-exists{color:#fff;background-color:#f0ad4e;border:1px solid #eea236}
        </style>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title">Room List
                </h1>
                <div class="pull-right">
                    <asp:LinkButton ID="btnShowFilter" runat="server" CssClass="btn btn-xs btn-link room-list-criteria-exists" OnClick="btnShowFilter_Click">
                        <i class="fa fa-filter"></i>
                        Filters
                    </asp:LinkButton>
                </div>
            </div>
            <asp:Panel ID="pnlFilterCriteria" runat="server" CssClass="panel-heading" Visible="false">
                <div class="row">
                    <div class="col-md-12">
                        <Rock:RockListBox ID="lbSchedules" runat="server" Label="Schedules" ValidationGroup="vgFilterCriteria" />
                    </div>
                </div>
                <div class="actions margin-t-md">
                    <asp:LinkButton ID="btnApplyFilter" runat="server" CssClass="filter btn btn-action btn-xs" Text="Apply Filter" OnClick="btnApplyFilter_Click" ValidationGroup="vgFilterCriteria" CausesValidation="true" />
                    <asp:LinkButton ID="btnClearFilter" runat="server" CssClass="filter-clear btn btn-default btn-xs" Text="Clear Filter" OnClick="btnClearFilter_Click" CausesValidation="false" />
                </div>
            </asp:Panel>
            <Rock:NotificationBox ID="nbWarning" runat="server" NotificationBoxType="Warning" />
            <div class="panel-body">
                <div class="grid grid-panel">
                    <Rock:Grid ID="gRoomList" runat="server" DisplayType="Light" UseFullStylesForLightGrid="true" ShowActionRow="false" OnRowDataBound="gRoomList_RowDataBound" OnRowSelected="gRoomList_RowSelected">
                        <Columns>
                            <Rock:RockLiteralField ID="lRoomName" HeaderText="Room" />
                            <Rock:RockLiteralField ID="lGroupName" HeaderText="Group" />
                            <Rock:RockLiteralField ID="lCheckedInCount" HeaderText="Checked-in" />
                            <Rock:RockLiteralField ID="lPresentCount" HeaderText="Present" />
                            <Rock:RockLiteralField ID="lCheckedOutCount" HeaderText="Out" />
                        </Columns>
                    </Rock:Grid>
                </div>

            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
