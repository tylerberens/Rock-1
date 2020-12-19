<%@ Control Language="C#" AutoEventWireup="true" CodeFile="AttendanceDetail.ascx.cs" Inherits="RockWeb.Blocks.CheckIn.Manager.AttendanceDetail" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <div class="row">
            <div class="col-xs-12 col-sm-4 col-lg-3">

                <!-- Photo, Name & Campus, Phone & Email -->
                <div class="panel panel-block">
                    <div class="profile-photo-container">
                        <asp:Literal ID="lPhoto" runat="server" />
                    </div>
                    <div class="d-flex flex-column align-items-center p-2 pb-3 py-lg-3 px-lg-4">
                        <h1 class="h3 title name js-checkin-person-name mt-0 text-center">
                            <asp:Literal ID="lName" runat="server" /></h1>
                        <Rock:HighlightLabel ID="hlCampus" runat="server" LabelType="Campus" />
                    </div>

                    <asp:Panel ID="pnlContact" runat="server" CssClass="border-top border-gray-400 p-2 p-lg-3">
                        <asp:Repeater ID="rptrPhones" runat="server">
                            <ItemTemplate>
                                <div class="d-flex justify-content-between align-items-center mb-3">
                                    <div>
                                        <%# Eval("NumberFormatted") %>
                                        <span class="d-block text-sm text-muted leading-snug">
                                            <%# Eval("NumberTypeValue.Value") %>
                                        </span>
                                    </div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>

                        <asp:Literal ID="lEmail" runat="server" />
                    </asp:Panel>
                </div>
            </div>

            <div class="col-sm-8 col-lg-9">
                <!-- Attendance Details -->
                <div class="panel panel-body">
                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockLiteral ID="lGroupName" runat="server" Label="Group" />
                            <Rock:RockLiteral ID="lLocationName" runat="server" Label="Location" />
                        </div>
                        <div class="col-md-6">
                            <Rock:RockLiteral ID="lTag" runat="server" Label="Tag" />
                            <Rock:RockLiteral ID="lScheduleName" runat="server" Label="Schedule" />
                        </div>
                    </div>

                    <hr />

                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockLiteral ID="lCheckinTime" runat="server" Label="Check-in" />
                        </div>
                        <div class="col-md-6">
                            <Rock:RockLiteral ID="lCheckinByPerson" runat="server" Label=" " />
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockLiteral ID="lPresentTime" runat="server" Label="Present" />
                        </div>
                        <div class="col-md-6">
                            <Rock:RockLiteral ID="lPresentByPerson" runat="server" Label=" " />
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockLiteral ID="lCheckedOutTime" runat="server" Label="Checked-out" />
                        </div>
                        <div class="col-md-6">
                            <Rock:RockLiteral ID="lCheckedOutByPerson" runat="server" Label=" " />
                        </div>
                    </div>
                </div>

                <!-- Attributes -->
                <asp:Panel ID="pnlAttributes" runat="server" CssClass="panel panel-block">
                    <div class="panel-heading">
                        <i class="fa fa-rocket"></i>
                        Attributes
                    </div>
                    <div class="panel-body">
                        #TODO#
                        <Rock:AttributeValuesContainer ID="avcAttributesDisplay" runat="server" />
                        <div class="actions">
                            <asp:LinkButton ID="btnEditAttributes" runat="server" CssClass="btn btn-primary btn-xs" CausesValidation="false" Text="Edit" OnClick="btnEditAttributes_Click" />
                        </div>

                        <Rock:AttributeValuesContainer ID="avcAttributesEdit" runat="server" ValidationGroup="vgAttributesEdit" />
                        #TODO#
                        <div class="actions">
                            <asp:LinkButton ID="btnSaveAttributes" runat="server" CssClass="btn btn-primary btn-xs" ValidationGroup="vgAttributesEdit" CausesValidation="true" Text="Save" OnClick="btnSaveAttributes_Click" />
                            <asp:LinkButton ID="btnCancelAttributes" runat="server" CssClass="btn btn-link btn-xs" Text="Cancel" CausesValidation="false" OnClick="btnCancelAttributes_Click" />
                        </div>
                    </div>
                </asp:Panel>

                <!-- Workflow Entry -->
                <asp:Panel ID="pnlWorkflowWithNote" runat="server" CssClass="panel panel-block">
                    <div class="panel-heading">
                    </div>
                    <div class="panel-body">
                        <Rock:RockTextBox ID="tbWorkflowNote" runat="server" TextMode="MultiLine" Rows="5" Label="#Some Workflow Prompt# -  Note To Group Director" ValidationGroup="vgLaunchWorkflow"/>
                        <div class="actions">
                            <asp:LinkButton ID="btnLaunchWorkflow" runat="server" CssClass="btn btn-primary btn-xs" ValidationGroup="vgLaunchWorkflow" CausesValidation="true" Text="Send" OnClick="btnLaunchWorkflow_Click" />
                        </div>
                    </div>
                </asp:Panel>
            </div>

        </div>

    </ContentTemplate>
</asp:UpdatePanel>
