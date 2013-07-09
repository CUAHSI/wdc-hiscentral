using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using System.Data.SqlClient;

using MultiEndpoint.WaterML10;
using MultiEndpoint.WaterML11;
using MultiEndpoint.HealthQueryDataSetTableAdapters;


namespace MultiEndpoint
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
            WaterML11.seriesCatalogTypeSeries scts)
        {
            int scID;
            string query;
            int undefined = -99;

//            Console.WriteLine(@"....No SeriesCatalog from hiscentral for site {0} and var {1},
//                dataType '{2}' methodID {3} sourcdID {4} qualityID {5}.",
//                siteCode, varCode, scts.dataType, scts.method.methodID,
//                scts.source.sourceID,
//                scts.qualityControlLevel.qualityControlLevelID);

            if (scts.method.methodID < 0)
                scts.method.methodID = undefined;
            if (scts.qualityControlLevel.qualityControlLevelID < 0)
                scts.qualityControlLevel.qualityControlLevelID = undefined;
            if (scts.source.sourceID < 0)
                scts.source.sourceID = undefined;

            query = string.Format(@"SiteCode='{0}' and VariableCode='{1}' and 
                                MethodID = {2} and QualityControlLevelID = {3} and
                                SourceID = {4}", siteCode, varCode,
                                               scts.method.methodID,
                                               scts.qualityControlLevel.qualityControlLevelID,
                                               scts.source.sourceID);
            scID = OD_Utils.GetPrimaryKey("dbo.SeriesCatalog", "SeriesID", query, Adapter.Connection);
            if (scID >= 0)
                return scID;

            HealthQueryDataSet.SeriesCatalogRow row = Table.NewSeriesCatalogRow();

            row.SiteCode = siteCode;
            row.VariableCode = varCode;

            row.MethodID = scts.method.methodID;
            row.SourceID = scts.source.sourceID;
            row.QualityControlLevelID = scts.qualityControlLevel.qualityControlLevelID;

            if (scts.variable.dataType != null)
                row.DataType = scts.variable.dataType;

            row.ValueCount = scts.valueCount.Value;

            WaterML11.TimeIntervalType ti = (WaterML11.TimeIntervalType)scts.variableTimeInterval;
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
