using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;

using System.IO;
using System.Xml;
using System.Xml.Serialization;

using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Configuration;

using BackupODM_1_1.WaterML11;
using BackupODM_1_1.OD_1_1_1DataSetTableAdapters;


namespace BackupODM_1_1
{
    class OD_SeriesCatalog
    {
        public SeriesCatalogTableAdapter Adapter;
        public OD_1_1_1DataSet.SeriesCatalogDataTable Table;

        public OD_SeriesCatalog(SqlConnection sqlConn)
        {
            Adapter = new SeriesCatalogTableAdapter();
            Table = new OD_1_1_1DataSet.SeriesCatalogDataTable();
            Adapter.Connection = sqlConn;
        }

        /*
         * This routine is to add SQL WHERE clause to limit records to be loaded.
         * Be sure to copy this routine to SeriesCatalogTableAdapter class when
         * OD_1_1_DataSet.Designer.cs is regenerated to make GetRow() work.
         * Copied from http://www.codeproject.com/Articles/17324/Extending-TableAdapters-for-Dynamic-SQL.
        public int FillWhere(OD_1_1_DataSet.SeriesCatalogDataTable dataTable, string whereExpression)
        {
            string text1 = this._commandCollection[0].CommandText;
            try
            {
                this._commandCollection[0].CommandText += " WHERE " + whereExpression;
                return this.Fill(dataTable);
            }
            finally { this._commandCollection[0].CommandText = text1; }
        }

        public OD_1_1DataSet.SeriesCatalogRow GetRow(string siteCode, string varCode)
        {
            string cond = "SiteCode = '" + siteCode + "' and ";
            cond += "VariableCode = '" + varCode + "'";

            Adapter.FillWhere(Table, cond);
            if (Table.Rows.Count == 0)
                return null;
            else {
                if (Table.Rows.Count > 1)
                    Console.WriteLine(
                        "SeriesCatalog table contains {0} records.", 
                        Table.Rows.Count);
                return (OD_1_1DataSet.SeriesCatalogRow)Table.Rows[0];
            }           
        }
        */

        public OD_1_1_1DataSet.SeriesCatalogRow GetOrCreateSeriesCatalog(
            string siteCode, string varCode,
            WaterML11.seriesCatalogTypeSeries scts)
        {
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
            Adapter.FillWhere(Table, query);
            if (Table.Rows.Count > 0)
            {
                if (Table.Rows.Count > 1)
                    Console.WriteLine(
                        "SeriesCatalog table contains {0} records.", Table.Rows.Count);
                return (OD_1_1_1DataSet.SeriesCatalogRow)Table.Rows[0];
            }

            Console.WriteLine(">>>Parsing and inserting a new SeriesCatalog");
            OD_1_1_1DataSet.SeriesCatalogRow row = CreateNewSeriesCatalog(siteCode, varCode, scts);
            Table.AddSeriesCatalogRow(row);
            Adapter.Update(Table);

            return row;
        }


        // This routine is to fill in the data what stinfo and scts have. The IDs will
        // be updated when handling DataValues.
        OD_1_1_1DataSet.SeriesCatalogRow CreateNewSeriesCatalog(
            string siteCode, string varCode,
            seriesCatalogTypeSeries scts)
        {
            OD_1_1_1DataSet.SeriesCatalogRow row = Table.NewSeriesCatalogRow();
            VariableInfoType varInfo = scts.variable;

            row.SiteID = -1;
            row.SiteCode = siteCode;
            row.SiteName = null;

            row.VariableID = -1;
            row.VariableCode = varInfo.variableCode[0].Value;
            row.VariableName = varInfo.variableName;

            row.Speciation = varInfo.speciation;

            row.VariableUnitsID = Convert.ToInt32(varInfo.unit.unitCode);
            row.VariableUnitsName = varInfo.unit.unitName;

            row.SampleMedium = varInfo.sampleMedium;
            row.ValueType = varInfo.valueType;

            row.TimeSupport = varInfo.timeScale.timeSupport;
            row.TimeUnitsID = Convert.ToInt32(varInfo.timeScale.unit.unitCode);
            row.TimeUnitsName = varInfo.timeScale.unit.unitName;

            row.DataType = varInfo.dataType;
            row.GeneralCategory = varInfo.generalCategory;

            if (scts.method != null)
            {
                row.MethodID = scts.method.methodID;
                row.MethodDescription = scts.method.methodDescription;
            }

            row.SourceID = scts.source.sourceID;
            row.Organization = scts.source.organization;
            row.SourceDescription = scts.source.sourceDescription;

            row.SetCitationNull();

            row.QualityControlLevelID = scts.qualityControlLevel.qualityControlLevelID;
            row.QualityControlLevelCode = scts.qualityControlLevel.qualityControlLevelCode;

            // This table is to track what we have in DataValues table.
            // We don't have anything in DataValues table at this moment.
            TimeIntervalType ti = (TimeIntervalType)scts.variableTimeInterval;
            row.BeginDateTime = ti.beginDateTime;
            row.EndDateTime = OD_Utils.GetDateTime(row.BeginDateTime, row.TimeUnitsID, -1);
            row.BeginDateTimeUTC = ti.beginDateTime.ToUniversalTime();
            row.EndDateTimeUTC = row.EndDateTime.ToUniversalTime();
            row.ValueCount = 0;

            return row;
        }

    }

}
