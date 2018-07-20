<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ScheduledTransactionEdit.ascx.cs" Inherits="RockWeb.Blocks.Finance.ScheduledTransactionEdit" %>

<asp:UpdatePanel ID="upPayment" runat="server">
    <ContentTemplate>

        <asp:HiddenField ID="hfCurrentPage" runat="server" />

        <Rock:NotificationBox ID="nbMessage" runat="server" Visible="false"></Rock:NotificationBox>

        <asp:Panel ID="pnlSelection" cssClass=" panel panel-block" runat="server">

            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-calendar"></i> <asp:Literal ID="lPanelTitle1" runat="server" /></h1>
            </div>
            <div class="panel-body">

                <asp:Panel ID="pnlPaymentInfo" runat="server" >

                    <div class="panel panel-default contribution-info">
                        <div class="panel-heading">
                            <h3 class="panel-title"><asp:Literal ID="lContributionInfoTitle" runat="server" /></h3>
                        </div>
                        <div class="panel-body">
                            <fieldset>

                                <asp:Repeater ID="rptAccountList" runat="server">
                                    <ItemTemplate>
                                        <Rock:CurrencyBox ID="txtAccountAmount" runat="server" Label='<%# Eval("PublicName") %>' Text='<%# Eval("AmountFormatted") %>' Placeholder="0.00" CssClass="account-amount" />
                                    </ItemTemplate>
                                </asp:Repeater>
                                <Rock:ButtonDropDownList ID="btnAddAccount" runat="server" Visible="false" Label=" "
                                    DataTextField="PublicName" DataValueField="Id" OnSelectionChanged="btnAddAccount_SelectionChanged" />

                                <div class="form-group">
                                    <label>Total</label>
                                    <asp:Label ID="lblTotalAmount" runat="server" CssClass="form-control-static total-amount" />
                                </div>

                                <div id="divRepeatingPayments" runat="server" visible="false">
                                    <Rock:ButtonDropDownList ID="btnFrequency" runat="server" CssClass="btn btn-primary" Label="Frequency"
                                        DataTextField="Value" DataValueField="Id" />
                                    <Rock:DatePicker ID="dtpStartDate" runat="server" Label="Next Gift" />
                                </div>

                            </fieldset>
                        </div>
                    </div>

                </asp:Panel>

                <asp:Panel ID="pnlContributionPayment" runat="server">

                    <asp:Panel ID="pnlPaymentMethod" runat="server" CssClass="panel panel-default contribution-payment">

                        <div class="panel-heading">
                            <h3 class="panel-title"><asp:Literal ID="lPaymentInfoTitle" runat="server" /></h3>
                        </div>

                        <div class="panel-body">

                            <div id="divNewPayment" runat="server" class="radio-content">

                                <asp:HiddenField ID="hfPaymentTab" runat="server" />
                                <asp:PlaceHolder ID="phPills" runat="server">
                                    <ul class="nav nav-pills">
                                        <li id="liNone" runat="server"><a href='#<%=divNonePaymentInfo.ClientID%>' data-toggle="pill">No Change</a></li>
                                        <li id="liCreditCard" runat="server"><a href='#<%=divCCPaymentInfo.ClientID%>' data-toggle="pill">New Card</a></li>
                                        <li id="liACH" runat="server"><a href='#<%=divACHPaymentInfo.ClientID%>' data-toggle="pill">New Bank Account</a></li>
                                    </ul>
                                </asp:PlaceHolder>

                                <div class="tab-content">

                                    <div id="divNonePaymentInfo" runat="server" class="tab-pane">
                                        Keep the same payment info
                                    </div>

                                    <div id="divCCPaymentInfo" runat="server" visible="false" class="tab-pane">
                                        <Rock:RockRadioButtonList ID="rblSavedCC" runat="server" Label=" " CssClass="radio-list" RepeatDirection="Vertical" DataValueField="Id" DataTextField="Name" />
                                        <div id="divNewCard" runat="server" class="radio-content">
                                            <Rock:RockTextBox ID="txtCardFirstName" runat="server" Label="First Name on Card" Visible="false"></Rock:RockTextBox>
                                            <Rock:RockTextBox ID="txtCardLastName" runat="server" Label="Last Name on Card" Visible="false"></Rock:RockTextBox>
                                            <Rock:RockTextBox ID="txtCardName" runat="server" Label="Name on Card" Visible="false"></Rock:RockTextBox>
                                            <Rock:RockTextBox ID="txtCreditCard" runat="server" Label="Card Number" MaxLength="19" CssClass="credit-card" />
                                            <ul class="card-logos">
                                                <li class="card-visa"></li>
                                                <li class="card-mastercard"></li>
                                                <li class="card-amex"></li>
                                                <li class="card-discover"></li>
                                            </ul>
                                            <div class="row">
                                                <div class="col-md-6">
                                                    <Rock:MonthYearPicker ID="mypExpiration" runat="server" Label="Expiration Date" />
                                                </div>
                                                <div class="col-md-6">
                                                    <Rock:NumberBox ID="txtCVV" Label="Card Security Code" runat="server" MaxLength="4" />
                                                </div>
                                            </div>
                                            <div id="divBillingAddress" runat="server">
                                                <Rock:AddressControl ID="acBillingAddress" runat="server" Label="Billing Address" UseStateAbbreviation="true" UseCountryAbbreviation="false" />
                                            </div>
                                        </div>
                                    </div>

                                    <div id="divACHPaymentInfo" runat="server" visible="false" class="tab-pane">
                                        <Rock:RockRadioButtonList ID="rblSavedAch" runat="server" Label=" " CssClass="radio-list" RepeatDirection="Vertical" DataValueField="Id" DataTextField="Name" />
                                        <div id="divNewBank" runat="server" class="radio-content">
                                            <Rock:RockTextBox ID="txtAccountName" runat="server" Label="Name on Account" />
                                            <Rock:RockTextBox ID="txtRoutingNumber" runat="server" Label="Routing Number" />
                                            <Rock:RockTextBox ID="txtAccountNumber" runat="server" Label="Account Number" />
                                            <Rock:RockRadioButtonList ID="rblAccountType" runat="server" RepeatDirection="Horizontal" Label="Account Type">
                                                <asp:ListItem Text="Checking" Selected="true" />
                                                <asp:ListItem Text="Savings" />
                                            </Rock:RockRadioButtonList>
                                            <asp:Image ID="imgCheck" runat="server" ImageUrl="<%$ Fingerprint:~/Assets/Images/check-image.png %>" />
                                        </div>
                                    </div>

                                </div>
                            </div>
                        </div>
                    </asp:Panel>

                </asp:Panel>

            <div class="panel panel-default no-border">
                <div class="panel-body">
                    <Rock:NotificationBox ID="nbSelectionMessage" runat="server" Visible="false"></Rock:NotificationBox>

                    <div class="actions clearfix">
                        <asp:LinkButton ID="btnPaymentInfoNext" runat="server" Text="Next" CssClass="btn btn-primary pull-right" OnClick="btnPaymentInfoNext_Click" />
                        <asp:LinkButton ID="btnStep2PaymentPrev" runat="server" Text="Previous" CssClass="btn btn-link" OnClick="btnStep2PaymentPrev_Click" />
                        <asp:LinkButton ID="btnCancel" runat="server" Text="Cancel" CssClass="btn btn-link" OnClick="btnCancel_Click" />
                        <asp:Label ID="aStep2Submit" runat="server" ClientIDMode="Static" CssClass="btn btn-primary pull-right" Text="Next" />
                    </div>
                </div>
            </div>
            
            <iframe id="iframeStep2" src="<%=this.Step2IFrameUrl%>" style="display:none"></iframe>

            <asp:HiddenField ID="hfStep2AutoSubmit" runat="server" Value="false" />
            <asp:HiddenField ID="hfStep2Url" runat="server" />
            <asp:HiddenField ID="hfStep2ReturnQueryString" runat="server" />
            <span style="display:none" >
                <asp:LinkButton ID="lbStep2Return" runat="server" Text="Step 2 Return" OnClick="lbStep2Return_Click" CausesValidation="false" ></asp:LinkButton>
            </span>

        </asp:Panel>

        <asp:Panel ID="pnlConfirmation" CssClass="panel panel-block" runat="server" Visible="false">

            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-credit-card"></i> <asp:Literal ID="lPanelTitle2" runat="server" /></h1>
            </div>

            <div class="panel-body">
                <div class="panel panel-default">

                    <div class="panel-heading">
                        <h1 class="panel-title"><asp:Literal ID="lConfirmationTitle" runat="server" /></h1>
                    </div>
                    <div class="panel-body">
                        <asp:Literal ID="lConfirmationHeader" runat="server"></asp:Literal>
                        <dl class="dl-horizontal gift-confirmation">
                            <Rock:TermDescription ID="tdName" runat="server" Term="Name" />
                            <Rock:TermDescription runat="server" />
                            <asp:Repeater ID="rptAccountListConfirmation" runat="server">
                                <ItemTemplate>
                                    <Rock:TermDescription ID="tdAddress" runat="server" Term='<%# Eval("Name") %>' Description='<%# this.FormatValueAsCurrency((decimal)Eval("Amount")) %>' />
                                </ItemTemplate>
                            </asp:Repeater>
                            <Rock:TermDescription ID="tdTotal" runat="server" Term="Total" />
                            <Rock:TermDescription runat="server" />
                            <Rock:TermDescription ID="tdPaymentMethod" runat="server" Term="Payment Method" />
                            <Rock:TermDescription ID="tdAccountNumber" runat="server" Term="Account Number" />
                            <Rock:TermDescription ID="tdWhen" runat="server" Term="When" />
                        </dl>

                        <asp:Literal ID="lConfirmationFooter" runat="server"></asp:Literal>
                        <asp:Panel ID="pnlDupWarning" runat="server" CssClass="alert alert-block">
                            <h4>Warning!</h4>
                            <p>
                                You have already submitted a transaction that has been processed.  Are you sure you want
                            to submit another possible duplicate transaction?
                            </p>
                            <asp:LinkButton ID="btnConfirm" runat="server" Text="Yes, submit another transaction" CssClass="btn btn-primary" OnClick="btnConfirm_Click" />
                        </asp:Panel>

                        <Rock:NotificationBox ID="nbConfirmationMessage" runat="server" Visible="false" />

                        <div class="actions clearfix">
                            <asp:LinkButton ID="btnConfirmationPrev" runat="server" Text="Previous" CssClass="btn btn-link" OnClick="btnConfirmationPrev_Click" Visible="false" />
                            <Rock:BootstrapButton ID="btnConfirmationNext" runat="server" Text="Finish" CssClass="btn btn-primary pull-right" OnClick="btnConfirmationNext_Click" />
                        </div>
                    </div>
                </div>
            </div>

        </asp:Panel>

        <asp:Panel ID="pnlSuccess" runat="server" Visible="false">
            <div class="well">
                <asp:Literal ID="lSuccessHeader" runat="server"></asp:Literal>
                <dl class="dl-horizontal gift-success">
                    <Rock:TermDescription ID="tdScheduleId" runat="server" Term="Payment Schedule ID" />
                    <Rock:TermDescription ID="tdTransactionCode" runat="server" Term="Confirmation Code" />
                </dl>
            </div>

            <asp:Literal ID="lSuccessFooter" runat="server"></asp:Literal>

            <Rock:NotificationBox ID="nbSuccessMessage" runat="server" Visible="false"></Rock:NotificationBox>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
