<%@ Control Language="C#" AutoEventWireup="true" CodeFile="TransactionFeeReport.ascx.cs" Inherits="RockWeb.Blocks.Finance.TransactionFeeReport" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-rocket"></i>
                    Transaction Fee Report
                </h1>
            </div>
            <div class="panel-body">

                <%-- Filter --%>
                <div class="row">
                    <div class="col-md-4">
                        <Rock:AccountPicker ID="apAccounts" AllowMultiSelect="true" CssClass="input-width-lg" Label="Account" runat="server" />
                    </div>
                    <div class="col-md-4">
                        <Rock:SlidingDateRangePicker ID="srpFilterDates" Label="Date Range" runat="server" />

                    </div>
                    <div class="col-md-4">
                        <Rock:BootstrapButton ID="bbtnApply" CssClass="btn btn-primary margin-t-lg" runat="server" Text="Apply" OnClick="bbtnApply_Click" />
                    </div>
                </div>

                <%-- Report Output --%>
                <div class="row">

                    <%-- Fee Coverage - Totals --%>
                    <div class="col-md-4">

                        <div class="fees-icon">
                            <i class="fa fa-rocket fa-2x"></i>
                        </div>
                        <div class="fee-details">
                            <h1>
                                <asp:Literal ID="lTotalFeeCoverageAmount" runat="server" Text="$100.00" /></h1>
                            <div class="fee-coverage-label">
                                <asp:Literal ID="lTotalFeesCoverageLabel" runat="server" Text="Total Fees" />
                            </div>
                            <div class="fee-coverage-count">
                                <asp:Literal ID="lTotalFeesCoverageCount" runat="server" Text="12345 Transactions" />
                            </div>
                        </div>
                    </div>

                    <%-- Fee Coverage - Credit Card --%>
                    <div class="col-md-4">

                        <div class="fees-icon">
                            <i class="fa fa-rocket fa-2x"></i>
                        </div>
                        <div class="fee-details">
                            <h1>
                                <asp:Literal ID="lCreditCardFeeCoverageAmount" runat="server" Text="$100.00" /></h1>
                            <div class="fee-coverage-label">
                                <asp:Literal ID="lCreditCardFeesCoverageLabel" runat="server" Text="Credit Card Fees" />
                            </div>
                            <div class="fee-coverage-count">
                                <asp:Literal ID="lCreditCardFeesCoverageCount" runat="server" Text="12345 Transactions" />
                            </div>
                        </div>
                    </div>

                    <div class="col-md-4">

                        <%-- Fee Coverage - ACH --%>
                        <div class="fees-icon">
                            <i class="fa fa-rocket fa-2x"></i>
                        </div>
                        <div class="fee-details">
                            <h1>
                                <asp:Literal ID="lACHFeeCoverageAmount" runat="server" Text="$100.00" /></h1>
                            <div class="fee-coverage-label">
                                <asp:Literal ID="lACHFeesCoverageLabel" runat="server" Text="ACH Fees" />
                            </div>
                            <div class="fee-coverage-count">
                                <asp:Literal ID="lACHFeesCoverageCount" runat="server" Text="12345 Transactions" />
                            </div>
                        </div>
                    </div>

                </div>

            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
