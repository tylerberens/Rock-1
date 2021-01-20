<%@ Control Language="C#" AutoEventWireup="true" CodeFile="EvacReport.ascx.cs" Inherits="RockWeb.Plugins.org_northpoint.RoomCheckin.EvacReport" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <div class="page-title-inject d-flex flex-wrap justify-content-between align-items-center">
            <div class="my-2">
                <Rock:LocationPicker ID="lpLocation" runat="server" AllowedPickerModes="Named" IncludeInactiveNamedLocations="true" CssClass="picker-lg" OnSelectLocation="lpLocation_SelectLocation" />
            </div>
            <asp:Panel ID="pnlSubPageNav" runat="server" CssClass="my-2">
                <Rock:PageNavButtons ID="pbSubPages" runat="server" IncludeCurrentQueryString="true" />
            </asp:Panel>
        </div>

        <Rock:NotificationBox ID="nbWarning" runat="server" NotificationBoxType="Warning" />

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title">
                    <asp:Literal ID="lPanelTitle" runat="server" Text="##Customize Title##" />
                </h1>
                <div class="pull-right">
                    <asp:LinkButton ID="btnRefresh" runat="server" CssClass="btn btn-xs btn-link" OnClick="btnRefresh_Click">
                        <i class="fa fa-undo"></i>
                        Refresh
                    </asp:LinkButton>
                </div>
            </div>
            <div class="panel-body">
                <div class="grid grid-panel">
                    <Rock:Grid ID="gAttendees" runat="server" DisplayType="Light" UseFullStylesForLightGrid="true" OnRowDataBound="gAttendees_RowDataBound" DataKeyNames="PersonGuid,AttendanceIds" ShowActionRow="false">
                        <Columns>
                            <Rock:RockLiteralField ID="lPhoto" ItemStyle-CssClass="avatar-column" ColumnPriority="TabletSmall" />
                            <Rock:RockLiteralField ID="lName" HeaderText="Name" ItemStyle-CssClass="name js-name align-middle" />

                            <Rock:RockLiteralField ID="lGroupNameAndPath" HeaderText="Group" Visible="true" />
                            <Rock:RockBoundField DataField="Tag" HeaderText="Tag" HeaderStyle-CssClass="d-none d-sm-table-cell" ItemStyle-CssClass="tag d-none d-sm-table-cell align-middle" />
                            <Rock:RockBoundField DataField="ServiceTimes" HeaderText="Service Times" HeaderStyle-CssClass="d-none d-sm-table-cell" ItemStyle-CssClass="service-times d-none d-sm-table-cell align-middle" />
                            <Rock:RockLiteralField ID="lCheckbox" HeaderText="" Text="<i class='fa fa-square-o'></i>" ItemStyle-CssClass="tag d-none d-sm-table-cell align-middle" />
                        </Columns>
                    </Rock:Grid>
                </div>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
