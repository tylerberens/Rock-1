<%@ Control Language="C#" AutoEventWireup="true" CodeFile="RoomSettings.ascx.cs" Inherits="RockWeb.Blocks.CheckIn.Manager.RoomSettings" %>

<Rock:RockUpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <Rock:NotificationBox ID="nbWarning" runat="server" NotificationBoxType="Warning" />

        <asp:Panel ID="pnlContent" runat="server">

            <div class="clearfix">
                <div class="pull-left">
                    <Rock:LocationPicker ID="lpLocation" runat="server" AllowedPickerModes="Named" IncludeInactiveNamedLocations="true" CssClass="picker-lg" OnSelectLocation="lpLocation_SelectLocation" />
                </div>
                <asp:Panel ID="pnlSubPageNav" runat="server" class="pull-right">
                    <Rock:PageNavButtons ID="pbSubPages" runat="server" IncludeCurrentQueryString="true" />
                </asp:Panel>
            </div>

            <asp:Panel ID="pnlSettings" runat="server" CssClass="panel panel-block">
                <div class="panel-heading clearfix">
                    <h1 class="panel-title pull-left">Room Details</h1>
                    <div class="pull-right">
                        <Rock:Toggle ID="tglRoom" runat="server" OnText="Open" OffText="Close" ButtonSizeCssClass="btn-xs" OnCssClass="btn-success" OffCssClass="btn-danger" OnCheckedChanged="tglRoom_CheckedChanged" />
                    </div>
                </div>
            </asp:Panel>

        </asp:Panel>

    </ContentTemplate>
</Rock:RockUpdatePanel>