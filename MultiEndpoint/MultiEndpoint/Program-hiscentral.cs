using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using System.Data.SqlClient;

using MultiEndpoint.WaterML10;
using MultiEndpoint.WaterML11;
using MultiEndpoint.hiscentralDataSetTableAdapters;

namespace MultiEndpoint
{
    class hiscentralSeriesCatalog
    {
        public SeriesCatalogTableAdapter Adapter;
        public hiscentralDataSet.SeriesCatalogDataTable Table;

        public hiscentralSeriesCatalog(SqlConnection sqlConn)
        {
            Adapter = new SeriesCatalogTableAdapter();
            Table = new hiscentralDataSet.SeriesCatalogDataTable();
            Adapter.Connection = sqlConn;
        }

        /*
         * This routine is to add SQL WHERE clause to limit records to be loaded.
         * Be sure to copy this routine to SeriesCatalogTableAdapter class when
         * hiscentralDataSet.Designer.cs is regenerated to make GetRow() work.
         * Copied from http://www.codeproject.com/Articles/17324/Extending-TableAdapters-for-Dynamic-SQL.
        public int FillWhere(hiscentralDataSet.SeriesCatalogDataTable dataTable, string whereExpression)
        {
            string text1 = this._commandCollection[0].CommandText;
            try
            {
                this._commandCollection[0].CommandText += " WHERE " + whereExpression;
                return this.Fill(dataTable);
            }
            finally { this._commandCollection[0].CommandText = text1; }
        }
        */


        public hiscentralDataSet.SeriesCatalogRow GetRow(string siteCode, string varCode,
            WaterML11.seriesCatalogTypeSeries scts)
        {
            string cond = "SiteCode = '" + siteCode + "' and ";
            cond += "VariableCode = '" + varCode + "'";

            Adapter.FillWhere(Table, cond);
            if (Table.Rows.Count == 0)
                return null;
            else
            {
                if (Table.Rows.Count > 1)
                {
                    string filter = string.Format("SeriesCode like '%||{0}||{1}||{2}'",
                        scts.method.methodID, scts.source.sourceID,
                        scts.qualityControlLevel.qualityControlLevelID);
                    DataRow[] rows = Table.Select(filter);
                    if (rows.Length == 1)
                        return (hiscentralDataSet.SeriesCatalogRow)rows[0];
                    else
                    {
                        Console.WriteLine(
                          "SeriesCatalog table contains {0} records for site {1} var {2}, got {3} records with filter '{4}'.",
                          Table.Rows.Count, siteCode, varCode, rows.Length, filter);
                    }
                }
                return (hiscentralDataSet.SeriesCatalogRow)Table.Rows[0];
            }
        }
    }
}
