<%@ Control Language="C#" AutoEventWireup="true" CodeFile="EnRoute.ascx.cs" Inherits="RockWeb.Plugins.org_northpoint.RoomCheckin.EnRoute" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title">People En Route
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
                        <Rock:RockCheckBoxList ID="cblGroups" runat="server" RepeatDirection="Horizontal" Label="Groups" ValidationGroup="vgFilterCriteria" />
                        <Rock:RockListBox ID="lbSchedules" runat="server" Label="Schedules" ValidationGroup="vgFilterCriteria" />
                    </div>
                </div>
                <div class="actions margin-t-md">
                    <asp:LinkButton ID="btnApplyFilter" runat="server" CssClass="filter btn btn-action btn-xs" Text="Apply Filter" OnClick="btnApplyFilter_Click" ValidationGroup="vgFilterCriteria" CausesValidation="true" />
                    <asp:LinkButton ID="btnClearFilter" runat="server" CssClass="filter-clear btn btn-default btn-xs" Text="Clear Filter" OnClick="btnClearFilter_Click" CausesValidation="false" />
                </div>
            </asp:Panel>
            <div class="panel-body">
                <div class="grid grid-panel">
                    <Rock:Grid ID="gAttendees" runat="server" DisplayType="Light" UseFullStylesForLightGrid="true" OnRowDataBound="gAttendees_RowDataBound" DataKeyNames="PersonGuid,AttendanceIds" ShowActionRow="false">
                        <Columns>
                            <Rock:RockLiteralField ID="lPhoto" ItemStyle-CssClass="avatar-column" ColumnPriority="TabletSmall" />
                            <Rock:RockLiteralField ID="lName" HeaderText="Name" ItemStyle-CssClass="name js-name align-middle" />

                            <Rock:RockLiteralField ID="lGroupNameAndPath" HeaderText="Group" Visible="true" />
                            <Rock:RockBoundField DataField="ServiceTimes" HeaderText="Service Times" HeaderStyle-CssClass="d-none d-sm-table-cell" ItemStyle-CssClass="service-times d-none d-sm-table-cell align-middle" />
                            <Rock:RockLiteralField ID="lLocation" HeaderText="Room" Visible="true" />

                            <Rock:LinkButtonField ID="btnChangeRoom" ItemStyle-CssClass="grid-columnaction" CssClass="btn btn-default btn-square" Text="<i class='fa fa-external-link-alt'></i>" ToolTip="Change Room" OnClick="btnChangeRoom_Click" />
                        </Columns>
                    </Rock:Grid>
                </div>
        </asp:Panel>

        <Rock:ModalDialog ID="mdChangeRoom" runat="server" Title="Change Room" SaveButtonText="Move" OnSaveClick="mdChangeRoom_SaveClick">
            <Content>
                <asp:HiddenField ID="hfChangeRoomAttendanceId" runat="server" />
                <asp:Literal ID="lChangeRoomInstructions" runat="server" Text="Select a new location to change where this person is checked into." />

                <Rock:RockDropDownList ID="ddlChangeRoomLocation" runat="server" Label="Location" Required="true" />

            </Content>
        </Rock:ModalDialog>

    </ContentTemplate>
</asp:UpdatePanel>
