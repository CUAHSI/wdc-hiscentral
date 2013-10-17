using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for DataAccess
/// </summary>
public static class DataAccess {
    public static List<String> SuperAdmins = new List<String> { "mmartinmass@gmail.com", "jpollak@gmail.com", "xxx@gmail.com" };

    public static void UpdateGmailAddress(string username, string gmailAddress) {
        var db = new hiscentralEntities();
        var recordsToUpdate = db.HISNetworks.Where(x => x.username == username);
        foreach (var record in recordsToUpdate) {
            if (record.GmailAddress != gmailAddress) {
                record.GmailAddress = gmailAddress;
                record.username = gmailAddress;
                db.Entry(record).State = System.Data.EntityState.Modified;
            }
        }
        db.SaveChanges();
    }

    public static void GatherUniqueLoginsAndInitializeTokens() {
        var db = new hiscentralEntities();
        //First delete existing data in the table if any
        var rows = db.ExistingUserTokens;
        foreach (var row in rows) {
            db.ExistingUserTokens.Remove(row);
        }
        db.SaveChanges();
        var hisNetworkRecords = db.HISNetworks.Where(x => !String.IsNullOrEmpty(x.username)).ToList();
        var logins = hisNetworkRecords.ConvertAll(x => x.username);
        var distinctLogins = logins.Distinct();
        distinctLogins.ToList().ForEach(x => {
            var token = Guid.NewGuid().ToString();
            var email = String.Empty;
            var emailRecord = hisNetworkRecords.FirstOrDefault(y => y.username == x);
            if (emailRecord != null) email = emailRecord.ContactEmail;
            db.ExistingUserTokens.Add(new ExistingUserToken { ExistingUsername = x, GeneratedToken = token, EmailAddressOnFile = email });
        });
        db.SaveChanges();
    }

    public static string AssociateSuppliedTokenToExistingLogin(string token) {
        var login = String.Empty;
        var db = new hiscentralEntities();
        var existingLogin = db.ExistingUserTokens.SingleOrDefault(x => x.GeneratedToken == token);
        if (existingLogin != null) login = existingLogin.ExistingUsername;
        return login;
    }

    public static List<string> GetExistingNetworkNames() {
        return new hiscentralEntities().HISNetworks.Select(x => x.NetworkName).Distinct().ToList();
    }
}

public static class DataAccess_Logging {
    private static hiscentral_loggingEntities _db = new hiscentral_loggingEntities();

    //public static List<string> GetStat1(string networkName) {
    //    //    --he number of unique ip addresses that accessed the GetValues service for this network for each month. 
    //    //--IP addresses originating from SDSC are not counted.

    //    //select  YEAR(querytime) ydate, MONTH(querytime) AS mdate, COUNT(distinct (userhost))
    //    //from log11service
    //    //where method = 'GetValues_Start' and network = 'LittleBearRiver' and userhost not in ('xxx')
    //    //group by YEAR(querytime), MONTH(querytime)


    //    var records = _db.log11Service.Where(x => x.method == "GetValues_Start" && x.network == networkName && x.userhost != "[SERVER_IP]").OrderBy(x => x.querytime).ToList();
    //    records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, 1, 0, 0, 0));
    //    var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, x.Select(y => y.userhost).Distinct().Count())).ToList();
    //    return result;
    //}

    //public static List<string> GetStat2(string networkName) {
    //    //--Requests per Month: The total number of requests for this network for each month.
    //    //select  YEAR(querytime) ydate, MONTH(querytime) AS mdate, COUNT(*)
    //    //from log11service
    //    //where method like '%_Start' and network = 'LittleBearRiver' 
    //    //group by YEAR(querytime), MONTH(querytime)
    //    var records = _db.log11Service.Where(x => x.method.Contains("_Start") && x.network == networkName).OrderBy(x => x.querytime).ToList();
    //    records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, 1, 0, 0, 0));
    //    var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, x.Count())).ToList();
    //    return result;
    //}

    //public static List<string> GetStat3(string networkName) {

    //    //--Values per Month: The total number of values retrieved from the Water One Flow webservices associated with this network. 
    //    //--This chart's y axis uses a log scale. 
    //    var records = _db.log11Service.Where(x => x.method == "GetValues_End" && x.network == networkName && x.userhost != "[SERVER_IP]").OrderBy(x => x.querytime).ToList();
    //    records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, 1, 0, 0, 0));
    //    var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, Math.Round(Math.Log10(Convert.ToDouble(x.Select(y => y.reccount).Sum())), 2))).ToList();
    //    return result;
    //}

    //public static List<string> GetStat1_GroupByDay(string networkName) {
    //    var records = _db.log11Service.Where(x => x.method == "GetValues_Start" && x.network == networkName && x.userhost != "[SERVER_IP]").OrderBy(x => x.querytime).ToList();
    //    records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, x.querytime.Day, 0, 0, 0));
        
    //    var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, x.Select(y => y.userhost).Distinct().Count())).ToList();

    //    return result;
    //}

    //public static List<string> GetStat2_GroupByDay(string networkName) {
    //    var records = _db.log11Service.Where(x => x.method.Contains("_Start") && x.network == networkName).OrderBy(x => x.querytime).ToList();
    //    records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, x.querytime.Day, 0, 0, 0));
    //    var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, x.Count())).ToList();
    //    return result;
    //}

    //public static List<string> GetStat3_GroupByDay(string networkName) {
    //    var records = _db.log11Service.Where(x => x.method == "GetValues_End" && x.network == networkName && x.userhost != "[SERVER_IP]").OrderBy(x => x.querytime).ToList();
    //    records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, x.querytime.Day, 0, 0, 0));
    //    //do the padding

    //    //then turn them into statisticmodel lists

    //    //then do the grouping
    //    var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, Math.Round(Math.Log10(Convert.ToDouble(x.Select(y => y.reccount).Sum())), 2))).ToList();
    //    return result;
    //}


    //public static List<string> GetStat1() {
    //    var records = _db.log11Service.Where(x => x.method == "GetValues_Start" && x.userhost != "[SERVER_IP]").OrderBy(x => x.querytime).ToList();
    //    records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, 1, 0, 0, 0));
    //    var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, x.Select(y => y.userhost).Distinct().Count())).ToList();
    //    return result;
    //}

    //public static List<string> GetStat2() {
    //    var records = _db.log11Service.Where(x => x.method.Contains("_Start")).OrderBy(x => x.querytime).ToList();
    //    records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, 1, 0, 0, 0));
    //    var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, x.Count())).ToList();
    //    return result;
    //}

    //public static List<string> GetStat3() {
    //    var records = _db.log11Service.Where(x => x.method == "GetValues_End" && x.userhost != "[SERVER_IP]").OrderBy(x => x.querytime).ToList();
    //    records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, 1, 0, 0, 0));
    //    var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, Math.Round(Math.Log10(Convert.ToDouble(x.Select(y => y.reccount).Sum())), 2))).ToList();
    //    return result;
    //}

    //public static List<string> GetStat1_GroupByDay() {
    //    var records = _db.log11Service.Where(x => x.method == "GetValues_Start" && x.userhost != "[SERVER_IP]").OrderBy(x => x.querytime).ToList();
    //    records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, x.querytime.Day, 0, 0, 0));

    //    var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, x.Select(y => y.userhost).Distinct().Count())).ToList();

    //    return result;
    //}

    //public static List<string> GetStat2_GroupByDay() {
    //    var records = _db.log11Service.Where(x => x.method.Contains("_Start")).OrderBy(x => x.querytime).ToList();
    //    records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, x.querytime.Day, 0, 0, 0));
    //    var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, x.Count())).ToList();
    //    return result;
    //}

    //public static List<string> GetStat3_GroupByDay() {
    //    var records = _db.log11Service.Where(x => x.method == "GetValues_End" && x.userhost != "[SERVER_IP]").OrderBy(x => x.querytime).ToList();
    //    records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, x.querytime.Day, 0, 0, 0));
    //    //do the padding

    //    //then turn them into statisticmodel lists

    //    //then do the grouping
    //    var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, Math.Round(Math.Log10(Convert.ToDouble(x.Select(y => y.reccount).Sum())), 2))).ToList();
    //    return result;
    //}

    #region FILTER BY DOMAIN NAME
    public static List<string> GetStat1(string networkName, string domainFilter) {
        //    --he number of unique ip addresses that accessed the GetValues service for this network for each month. 
        //--IP addresses originating from SDSC are not counted.

        //select  YEAR(querytime) ydate, MONTH(querytime) AS mdate, COUNT(distinct (userhost))
        //from log11service
        //where method = 'GetValues_Start' and network = 'LittleBearRiver' and userhost not in ('xxx')
        //group by YEAR(querytime), MONTH(querytime)


        var records = _db.log11Service.Where(x => x.method == "GetValues_Start" && x.network == networkName && x.userhost.Contains(domainFilter) && x.userhost != "[SERVER_IP]").OrderBy(x => x.querytime).ToList();
        records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, 1, 0, 0, 0));
        var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, x.Select(y => y.userhost).Distinct().Count())).ToList();
        return result;
    }

    public static List<string> GetStat2(string networkName, string domainFilter) {
        //--Requests per Month: The total number of requests for this network for each month.
        //select  YEAR(querytime) ydate, MONTH(querytime) AS mdate, COUNT(*)
        //from log11service
        //where method like '%_Start' and network = 'LittleBearRiver' 
        //group by YEAR(querytime), MONTH(querytime)
        var records = _db.log11Service.Where(x => x.method.Contains("_Start") && x.userhost.Contains(domainFilter) && x.network == networkName).OrderBy(x => x.querytime).ToList();
        records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, 1, 0, 0, 0));
        var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, x.Count())).ToList();
        return result;
    }

    public static List<string> GetStat3(string networkName, string domainFilter) {

        //--Values per Month: The total number of values retrieved from the Water One Flow webservices associated with this network. 
        //--This chart's y axis uses a log scale. 
        var records = _db.log11Service.Where(x => x.method == "GetValues_End" && x.network == networkName && x.userhost.Contains(domainFilter) && x.userhost != "[SERVER_IP]").OrderBy(x => x.querytime).ToList();
        records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, 1, 0, 0, 0));
        var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, Math.Round(Math.Log10(Convert.ToDouble(x.Select(y => y.reccount).Sum())), 2))).ToList();
        return result;
    }

    public static List<string> GetStat1_GroupByDay(string networkName, string domainFilter) {
        var records = _db.log11Service.Where(x => x.method == "GetValues_Start" && x.network == networkName && x.userhost.Contains(domainFilter) && x.userhost != "[SERVER_IP]").OrderBy(x => x.querytime).ToList();
        records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, x.querytime.Day, 0, 0, 0));

        var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, x.Select(y => y.userhost).Distinct().Count())).ToList();

        return result;
    }

    public static List<string> GetStat2_GroupByDay(string networkName, string domainFilter) {
        var records = _db.log11Service.Where(x => x.method.Contains("_Start") && x.userhost.Contains(domainFilter) && x.network == networkName).OrderBy(x => x.querytime).ToList();
        records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, x.querytime.Day, 0, 0, 0));
        var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, x.Count())).ToList();
        return result;
    }

    public static List<string> GetStat3_GroupByDay(string networkName, string domainFilter) {
        var records = _db.log11Service.Where(x => x.method == "GetValues_End" && x.network == networkName && x.userhost.Contains(domainFilter) && x.userhost != "[SERVER_IP]").OrderBy(x => x.querytime).ToList();
        records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, x.querytime.Day, 0, 0, 0));
        //do the padding

        //then turn them into statisticmodel lists

        //then do the grouping
        var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, Math.Round(Math.Log10(Convert.ToDouble(x.Select(y => y.reccount).Sum())), 2))).ToList();
        return result;
    }


    public static List<string> GetStat1(string domainFilter) {
        var records = _db.log11Service.Where(x => x.method == "GetValues_Start" && x.userhost.Contains(domainFilter) && x.userhost != "[SERVER_IP]").OrderBy(x => x.querytime).ToList();
        records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, 1, 0, 0, 0));
        var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, x.Select(y => y.userhost).Distinct().Count())).ToList();
        return result;
    }

    public static List<string> GetStat2(string domainFilter) {
        var records = _db.log11Service.Where(x => x.method.Contains("_Start") && x.userhost.Contains(domainFilter)).OrderBy(x => x.querytime).ToList();
        records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, 1, 0, 0, 0));
        var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, x.Count())).ToList();
        return result;
    }

    public static List<string> GetStat3(string domainFilter) {
        var records = _db.log11Service.Where(x => x.method == "GetValues_End" && x.userhost.Contains(domainFilter) && x.userhost != "[SERVER_IP]").OrderBy(x => x.querytime).ToList();
        records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, 1, 0, 0, 0));
        var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, Math.Round(Math.Log10(Convert.ToDouble(x.Select(y => y.reccount).Sum())), 2))).ToList();
        return result;
    }

    public static List<string> GetStat1_GroupByDay(string domainFilter) {
        var records = _db.log11Service.Where(x => x.method == "GetValues_Start" && x.userhost.Contains(domainFilter) && x.userhost != "[SERVER_IP]").OrderBy(x => x.querytime).ToList();
        records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, x.querytime.Day, 0, 0, 0));

        var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, x.Select(y => y.userhost).Distinct().Count())).ToList();

        return result;
    }

    public static List<string> GetStat2_GroupByDay(string domainFilter) {
        var records = _db.log11Service.Where(x => x.method.Contains("_Start") && x.userhost.Contains(domainFilter)).OrderBy(x => x.querytime).ToList();
        records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, x.querytime.Day, 0, 0, 0));
        var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, x.Count())).ToList();
        return result;
    }

    public static List<string> GetStat3_GroupByDay(string domainFilter) {
        var records = _db.log11Service.Where(x => x.method == "GetValues_End" && x.userhost.Contains(domainFilter) && x.userhost != "[SERVER_IP]").OrderBy(x => x.querytime).ToList();
        records.ForEach(x => x.querytime = new DateTime(x.querytime.Year, x.querytime.Month, x.querytime.Day, 0, 0, 0));
        //do the padding

        //then turn them into statisticmodel lists

        //then do the grouping
        var result = records.GroupBy(x => x.querytime).Select(x => String.Format("{0},{1}", x.Key, Math.Round(Math.Log10(Convert.ToDouble(x.Select(y => y.reccount).Sum())), 2))).ToList();
        return result;
    }
    #endregion


}