<%@ Control Language="C#" AutoEventWireup="true" CodeFile="TransactionFeeReport.ascx.cs" Inherits="RockWeb.Blocks.Finance.TransactionFeeReport" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-file-invoice-dollar"></i>
                    Transaction Fee Report
                </h1>
            </div>
            <div class="panel-body">

                <%-- Filter --%>
                <div class="row">
                    <div class="col-md-4">
                        <Rock:AccountPicker ID="apAccounts" AllowMultiSelect="true" Label="Account" runat="server" />
                    </div>
                    <div class="col-md-8 d-flex flex-wrap align-items-end">
                        <Rock:SlidingDateRangePicker ID="srpFilterDates" Label="Date Range" runat="server" FormGroupCssClass="mb-2" />
                        <div class="pb-2" style="margin-bottom:6px">
                            <Rock:BootstrapButton ID="bbtnApply" CssClass="btn btn-primary" CausesValidation="true" runat="server" Text="Apply" OnClick="bbtnApply_Click" />
                        </div>
                    </div>
                </div>

                <%-- Report Output --%>
                <div class="row">

                    <%-- Fee Coverage - Totals --%>
                    <div class="col-md-4 d-flex">
                        <div class="kpi mx-0 text-blue-500 border-blue-300" style="color:#2D8FF2;">
                            <div class="kpi-icon">
                                <img class="svg-placeholder" src="data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1 1'></svg>">
                                <div class="kpi-content"><i class="fa fa-list"></i></div>
                            </div>
                            <div class="kpi-stat">
                                <span class="kpi-value text-color"><asp:Literal ID="lTotalFeeCoverageAmount" runat="server" Text="$100.00" /></span>
                                <span class="kpi-label text-muted"><asp:Literal ID="lTotalFeeCoverageLabel" runat="server" Text="Total Fees" /><br><asp:Literal ID="lTotalFeeCoverageCount" runat="server" Text="12345 Transactions" /></span>
                            </div>
                        </div>
                    </div>

                    <%-- Fee Coverage - Credit Card --%>
                    <div class="col-md-4  d-flex">
                        <div class="kpi mx-0 text-green-500 border-green-300">
                            <div class="kpi-icon">
                                <img class="svg-placeholder" src="data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1 1'></svg>">
                                <div class="kpi-content"><i class="fa fa-credit-card"></i></div>
                            </div>
                            <div class="kpi-stat">
                                <span class="kpi-value text-color"><asp:Literal ID="lCreditCardFeeCoverageAmount" runat="server" Text="$100.00" /></span>
                                <span class="kpi-label text-muted"><asp:Literal ID="lCreditCardFeeCoverageLabel" runat="server" Text="Credit Card Fees" /><br><asp:Literal ID="lCreditCardFeeCoverageCount" runat="server" Text="12345 Transactions" /></span>
                            </div>
                        </div>
                    </div>

                    <%-- Fee Coverage - ACH --%>
                    <div class="col-md-4  d-flex">
                        <div class="kpi mx-0 text-indigo-500 border-indigo-300">
                            <div class="kpi-icon">
                                <img class="svg-placeholder" src="data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1 1'></svg>">
                                <div class="kpi-content"><i class="fa fa-money-check-alt"></i></div>
                            </div>
                            <div class="kpi-stat">
                                <span class="kpi-value text-color"><asp:Literal ID="lACHFeeCoverageAmount" runat="server" Text="$100.00" /></span>
                                <span class="kpi-label text-muted"><asp:Literal ID="lACHFeeCoverageLabel" runat="server" Text="ACH Fees" /><br><asp:Literal ID="lACHFeeCoverageCount" runat="server" Text="12345 Transactions" /></span>
                            </div>
                        </div>
                    </div>

                </div>

            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
