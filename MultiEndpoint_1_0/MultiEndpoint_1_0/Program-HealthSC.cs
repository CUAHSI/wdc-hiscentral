using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using System.Data.SqlClient;

using MultiEndpoint_1_0.WaterML10;
using MultiEndpoint_1_0.HealthQueryDataSetTableAdapters;


namespace MultiEndpoint_1_0
{
    class HealthSeriesCatalog
    {
        public SeriesCatalogTableAdapter Adapter;
        public HealthQueryDataSet.SeriesCatalogDataTable Table;

        public HealthSeriesCatalog(SqlConnection sqlConn)
        {
            Adapter = new SeriesCatalogTableAdapter();
            Table = new HealthQueryDataSet.SeriesCatalogDataTable();
            Adapter.Connection = sqlConn;
            Adapter.Adapter.UpdateBatchSize = Program.DbUpdateBatchSize;
            Adapter.Adapter.InsertCommand.UpdatedRowSource = UpdateRowSource.None; // required to do batch insertion
         }

        // The seriesCatalogID is not found in hiscental database, create one here before
        // hiscental database adopts the new variable.
        public int GetOrCreateSeriesID(string siteCode, string varCode,
            WaterML10.seriesCatalogTypeSeries scts)
        {
            int scID;
            string query;
            int undefined = -99;

//            Console.WriteLine(@"......hiscentral.SeriesCatalog has no record for site {0} and 
//                variable {1}, dataType '{2}' methodID {3} qualityID {4} sourceID {5}.",
//                siteCode, varCode, scts.dataType, scts.method.methodID,
//                scts.qualityControlLevel.qualityControlLevelID,
//                scts.source.sourceID);

            if (scts.Method.methodID < 0)
                scts.Method.methodID = undefined;
            if (scts.QualityControlLevel.QualityControlLevelID < 0)
                scts.QualityControlLevel.QualityControlLevelID = undefined;
            if (scts.Source.sourceID < 0)
                scts.Source.sourceID = undefined;

            query = string.Format(@"SiteCode='{0}' and VariableCode='{1}' and 
                                MethodID = {2} and QualityControlLevelID = {3} and
                                SourceID = {4}", siteCode, varCode,
                                               scts.Method.methodID,
                                               scts.QualityControlLevel.QualityControlLevelID,
                                               scts.Source.sourceID);
            scID = OD_Utils.GetPrimaryKey("dbo.SeriesCatalog", "SeriesID", query, Adapter.Connection);
            if (scID >= 0)
                return scID;

            HealthQueryDataSet.SeriesCatalogRow row = Table.NewSeriesCatalogRow();

            row.SiteCode = siteCode;
            row.VariableCode = varCode;

            row.MethodID = scts.Method.methodID;
            row.SourceID = scts.Source.sourceID;
            row.QualityControlLevelID = scts.QualityControlLevel.QualityControlLevelID;

            if (OD_Utils.ConvertToString(scts.variable.dataType) != null)
                row.DataType = OD_Utils.ConvertToString(scts.variable.dataType);

            row.ValueCount = scts.valueCount.Value;

            WaterML10.TimeIntervalType ti = (WaterML10.TimeIntervalType)scts.variableTimeInterval;
            row.BeginDateTimeUTC = ti.beginDateTime.ToUniversalTime();
            row.EndDateTimeUTC = ti.endDateTime.ToUniversalTime();
            
            Table.AddSeriesCatalogRow(row);
            Adapter.Update(Table);
            Table.Clear();

            scID = OD_Utils.GetPrimaryKey("dbo.SeriesCatalog", "SeriesID", query, Adapter.Connection);
            Console.WriteLine(
                    "....Site {0} and variable {1} with ID {2} is saved in a private catalog table.",
                    siteCode, varCode, scID);

            return scID;
        }
   
    }
}
