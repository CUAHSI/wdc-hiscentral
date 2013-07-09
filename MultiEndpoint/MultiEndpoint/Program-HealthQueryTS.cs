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
    class HealthQueryTimeseries
    {
        public QueryTimeseriesTableAdapter Adapter;
        public HealthQueryDataSet.QueryTimeseriesDataTable Table;

        public HealthQueryTimeseries(SqlConnection sqlConn)
        {
            Adapter = new QueryTimeseriesTableAdapter();
            Table = new HealthQueryDataSet.QueryTimeseriesDataTable();
            Adapter.Connection = sqlConn;
            Adapter.Adapter.UpdateBatchSize = Program.DbUpdateBatchSize;
            Adapter.Adapter.InsertCommand.UpdatedRowSource = UpdateRowSource.None; // required to do batch insertion
         }

        public void HandleQueryTimeseries(
            string siteCode, string varCode,
            int seriesCatalogID, int newID,
            int valueCount, int[] hasChanged,
            string queryParam, string errMsg)
        {
            HealthQueryDataSet.QueryTimeseriesRow row = Table.NewQueryTimeseriesRow();
            
            row.SiteCode = siteCode;
            row.VariableCode = varCode;
            if (seriesCatalogID != 0)
                row.SeriesCatalogID = seriesCatalogID;
            else
            {
                row.SeriesCatalogID = newID;
                row.NewSeries = true;
            }
            row.QueryParam = queryParam;
            row.ValueCount = valueCount;
            if (errMsg.Length > 0)
                row.ErrorMsg = errMsg;
            row.QueryDateTime = DateTime.Now;

            int count = Program.HDV.Table.Rows.Count;
            if ((errMsg.Length == 0) && (valueCount > 0) && (count > 0))
            {
                HealthQueryDataSet.DataValueRow first, last;
                first = Program.HDV.Table.ElementAt(0);
                last = Program.HDV.Table.ElementAt(count - 1);
                row.FirstrecValue = first.DataValue;
                row.FirstrecDateTime = first.DateTimeUTC;
                if (count > 1)
                {
                    row.LastrecValue = last.DataValue;
                    row.LastrecDateTime = last.DateTimeUTC;
                }
            }

            if (hasChanged != null)
            {
                row.First30recHasChanged = Convert.ToBoolean(hasChanged[0]);
                row.Mid30recHasChanged = Convert.ToBoolean(hasChanged[1]);
                row.Last30recHasChanged = Convert.ToBoolean(hasChanged[2]);
            }

            Table.AddQueryTimeseriesRow(row);
            if (valueCount > 10)
            {
                Adapter.Update(Table);
                Table.Clear();
            }
        }
   
    }
}
