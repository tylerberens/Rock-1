<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MobileContent.ascx.cs" Inherits="RockWeb.BlockConfig.MobileContent" %>

<p>My custom settings!</p>

<asp:Button ID="btnTest" runat="server" CssClass="btn btn-primary" Text="Test" OnClick="btnTest_Click" />

<asp:Literal ID="ltText" runat="server" />

<Rock:RockTextBox ID="tbCustomValue" runat="server" Label="Custom Value" />
