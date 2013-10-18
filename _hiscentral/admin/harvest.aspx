<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Harvest.aspx.cs" Inherits="admin_Harvest" %>

<%@ Register Src="HeaderControl.ascx" TagName="HeaderControl" TagPrefix="uc2" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Untitled Page</title>
    <link href="../styles/his.css" rel="stylesheet" type="text/css" />
    <script type="text/javascript" src="//ajax.googleapis.com/ajax/libs/jquery/1.10.2/jquery.min.js"></script>
    <style type="text/css">
        .progress {
            display: none;
        }
    </style>
</head>
<body>
    <script type="text/javascript">
        function showProgressBar() {
            $('.progress').show();
        }
    </script>
    <form id="form1" runat="server">
        <div>
            &nbsp;
      <asp:Button ID="Button1" runat="server" Text="Begin Harvest" Style="z-index: 100; left: 187px; position: absolute; top: 215px" Height="36px" OnClick="Button1_Click" OnClientClick="showProgressBar();" Width="129px" />
            &nbsp;
      <uc2:HeaderControl ID="HeaderControl1" runat="server" />
            <asp:Panel CssClass="progress" runat="server" ID="pnlWait" Style="z-index: 102; right: 10px; position: absolute; top: 140px">
                <asp:Image ID="imgWait" runat="server" ImageUrl="../images/wait.gif" />
            </asp:Panel>

            <asp:HyperLink ID="HyperLink1" runat="server" Style="z-index: 102; left: 203px; position: absolute; top: 284px"
                Target="_blank" Visible="False">Harvest log</asp:HyperLink>

        </div>
    </form>
</body>
</html>
