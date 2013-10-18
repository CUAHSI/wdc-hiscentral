<%@ Page Language="C#" AutoEventWireup="true" CodeFile="ReportDailyStatistics.aspx.cs" Inherits="ReportDailyStatistics" %>

<%@ Register Src="HeaderControl.ascx" TagName="HeaderControl" TagPrefix="uc1" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <style>
        table {
            border: 1px solid black;
            overflow-y: auto;
            height: 200px;
            

        }

        tr {
            border: 1px solid black;
        }

        td {
            width: 150px;
        }

        h3 {
            font-weight: normal !important;
        }

            h3 span {
                font-weight: bold;
                color: black;
            }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <uc1:HeaderControl ID="HeaderControl1" runat="server" />
            Usage Report for Network: [<asp:Label ID="lblNetworkName" runat="server"></asp:Label>]
        </div>

        <div style="position: absolute; top: 150px;">
            <asp:HyperLink ID="lnkDownload" runat="server" Text="[Download Usage CSV]"></asp:HyperLink>
            
            <h3><span>Clients per Day: </span>The number of unique ip addresses that accessed the GetValues service for this network for each day. IP addresses originating from SDSC are not counted</h3>
            <asp:Repeater ID="rptStat1" runat="server">
                <HeaderTemplate>
                    <table>
                        <tr>
                            <td>Date</td>
                            <td>Total</td>
                            <td>.Com</td>
                            <td>.Org</td>
                            <td>.Edu</td>
                        </tr>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr>
                        <td>
                            <%# Eval("Date") %> 
                        </td>
                        <td>
                            <%# Eval("Count") %>
                        </td>
                        <td>
                            <%# Eval("Count_Com") %>
                        </td>
                        <td>
                            <%# Eval("Count_Org") %>
                        </td>
                        <td>
                            <%# Eval("Count_Edu") %>
                        </td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate>
                    </table>
                </FooterTemplate>
            </asp:Repeater>
            <h3><span>Requests per Day: </span>The total number of requests for this network for each day.</h3>
            <asp:Repeater ID="rptStat2" runat="server">
                <HeaderTemplate>
                    <table>
                        <tr>
                            <td>Date</td>
                            <td>Total</td>
                            <td>.Com</td>
                            <td>.Org</td>
                            <td>.Edu</td>
                        </tr>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr>
                        <td>
                            <%# Eval("Date") %> 
                        </td>
                        <td>
                            <%# Eval("Count") %>
                        </td>
                        <td>
                            <%# Eval("Count_Com") %>
                        </td>
                        <td>
                            <%# Eval("Count_Org") %>
                        </td>
                        <td>
                            <%# Eval("Count_Edu") %>
                        </td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate>
                    </table>
                </FooterTemplate>
            </asp:Repeater>
            <h3><span>Values per Day: </span>The total number of values retrieved from the Water One Flow webservices associated with this network. This chart's y axis uses a log scale.</h3>
            <asp:Repeater ID="rptStat3" runat="server">
                <HeaderTemplate>
                    <table>
                        <tr>
                            <td>Date</td>
                            <td>Total</td>
                            <td>.Com</td>
                            <td>.Org</td>
                            <td>.Edu</td>
                        </tr>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr>
                        <td>
                            <%# Eval("Date") %> 
                        </td>
                        <td>
                            <%# Eval("Count") %>
                        </td>
                        <td>
                            <%# Eval("Count_Com") %>
                        </td>
                        <td>
                            <%# Eval("Count_Org") %>
                        </td>
                        <td>
                            <%# Eval("Count_Edu") %>
                        </td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate>
                    </table>
                </FooterTemplate>
            </asp:Repeater>
            <br />
            <br />
            <p style="font-size: 10px;">
                We use reporting services to generate reports from the logging database. 
For each registered network we calculate monthly totals on the number of clients, requests, and values for the GetValues requests. We filter out requests that originate from SDSC. Since each Water One Flow request is logged twice, we divide the total number of requests and number of values by two. We generate CSV files of these reports every night.
            </p>
        </div>

    </form>
</body>
</html>
