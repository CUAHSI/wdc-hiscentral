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
using System.Text.RegularExpressions;

using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Configuration;

using BackupODM_1_1.WaterML11;
using BackupODM_1_1.OD_1_1_1DataSetTableAdapters;


namespace BackupODM_1_1
{
    class OD_DataValues
    {
        // Return number of records inserted or found
        public static int HandleDataValueInfo(SqlConnection sqlConn,
            OD_SeriesCatalog odSC, OD_1_1_1DataSet.SeriesCatalogRow scRow,
            SiteInfoType siteInfo, seriesCatalogTypeSeries scts, TimeSeriesResponseType tsRt)
        {
            DataValuesTableAdapter dvAdapter = new DataValuesTableAdapter();
            dvAdapter.Connection = sqlConn;
            dvAdapter.Adapter.UpdateBatchSize = Program.DbUpdateBatchSize;
            dvAdapter.Adapter.InsertCommand.UpdatedRowSource = UpdateRowSource.None; // required to do batch insertion

            string siteCode = siteInfo.siteCode[0].network + "|" + siteInfo.siteCode[0].Value;
            string varCode = scts.variable.variableCode[0].Value;
            string cond;

            OD_1_1_1DataSet.DataValuesDataTable tblDataValues = new OD_1_1_1DataSet.DataValuesDataTable();

            Console.WriteLine(">>>Parsing and inserting DATAVALUE");

            // Get site ID
            if (scRow.SiteID == -1)
            {
                cond = "SiteCode = '" + siteCode + "'";
                scRow.SiteID = OD_Utils.GetPrimaryKey("Sites", "SiteID", cond, sqlConn);
            }

            // Get variable ID
            if (scRow.VariableID == -1)
            {
                string[] vars = Regex.Split(varCode, "/");
                string dataType = tsRt.timeSeries[0].variable.dataType;
                cond = "VariableCode = '" + vars[0] + "_" + dataType + "'";
                scRow.VariableID = OD_Utils.GetPrimaryKey("Variables", "VariableID", cond, sqlConn);
                if (scRow.VariableID == -1)
                {
                    Console.WriteLine("Failed to get variable ID from WS TimeSeries info (code: {0} type: {1}).",
                        varCode, dataType);
                    cond = "VariableCode = '" + vars[0] + "_" + scRow.DataType + "'";
                    scRow.VariableID = OD_Utils.GetPrimaryKey("Variables", "VariableID", cond, sqlConn);
                    if (scRow.VariableID == -1)
                    {
                        Console.WriteLine("Also failed to get variable ID with code: {0} and type: {1}. Give up.",
                            varCode, scRow.DataType);
                        return 0;
                    }
                    else
                        Console.WriteLine("Found variable ID {0} with code: {1} and type: {2} from database.",
                            scRow.VariableID, varCode, scRow.DataType);
                }
            }

            // Update IDs if modified OD_SeriesCatalog odSC, 
            if (scRow.RowState == DataRowState.Modified)
                odSC.Adapter.Update(scRow);

            // Walk through each data value
            if ((tsRt.timeSeries[0].values[0].value == null) || (tsRt.timeSeries[0].values[0].value.Count() == 0))
            {
                Console.WriteLine("No values in WS response.");
                return 0;
            }
            int valueCount = tsRt.timeSeries[0].values[0].value.Count();
            int currCount = 0, idx0 = 0, dupCount = 0;
            ValueSingleVariable dvInfo0 = null;
            bool dup;
            // Begin database transaction to make sure the end data time and value count
            // in SeriesCatalog and DataValues tables are consistent.
            SqlTransaction sqlTrans = sqlConn.BeginTransaction();
            for (int i = 0; i < valueCount; i++)
            {
                dup = false;
                ValueSingleVariable dvInfo = tsRt.timeSeries[0].values[0].value[i];
                if (dvInfo0 != null)
                {
                    // We have seen many duplicate dvInfo which caused following DataValue insertion failure.
                    // "Violation of UNIQUE KEY constraint 'UNIQUE_DataValues'. Cannot insert duplicate key in object 'dbo.DataValues'"
                    // Have to skip the duplicate to avoid the whole batch insertion failure.
                    // Simplify the dup check by only comparing dateTime.
                    if (dvInfo0.dateTime == dvInfo.dateTime)
                    {
                        dupCount++;
                        Console.WriteLine("* Index {0} has duplicate time {1:s} with {2}, skip count {3}!",
                            i, dvInfo.dateTime, idx0, dupCount);
                        dup = true;
                    }

                }

                if (!dup)
                {
                    InsertOneDataValue(tblDataValues, scRow.SiteID, scRow.VariableID, dvInfo);
                    idx0 = i;
                    dvInfo0 = dvInfo;
                    currCount++;
                }

                if ((currCount == Program.DbUpdateBatchSize) || (i + 1 == valueCount))
                {
                    try
                    {
                        dvAdapter.Transaction = sqlTrans;
                        dvAdapter.Update(tblDataValues);

                        scRow.EndDateTime = dvInfo.dateTime;
                        scRow.EndDateTimeUTC = scRow.EndDateTime.ToUniversalTime();
                        scRow.ValueCount += currCount;
                        odSC.Adapter.Transaction = sqlTrans;
                        odSC.Adapter.Update(scRow);

                        sqlTrans.Commit();
                    }                    
                    catch (Exception e)
                    {
                        Console.WriteLine("!!!!!! Got exception: {0}.", e.Message);
                        Console.WriteLine("* Inserted {0} of {1} records for site {2} variable {3}",
                            i + 1 - currCount, valueCount, siteCode, varCode);
                        Console.WriteLine("* Rollback {0} records with {0} of {1} completed!",
                            currCount, i + 1 - currCount, valueCount);

                        sqlTrans.Rollback();
                        tblDataValues.Clear();

                        return i + 1 - currCount;
                    }

                    currCount = 0;
                    tblDataValues.Clear();

                    if (i + 1 != valueCount)
                        sqlTrans = sqlConn.BeginTransaction();
                }
            }

            dvAdapter.Transaction = null;
            odSC.Adapter.Transaction = null;

            return valueCount - dupCount;
        }

 
        static void InsertOneDataValue(OD_1_1_1DataSet.DataValuesDataTable tblDataValues,
            int siteID, int varID, ValueSingleVariable dvInfo)
        {
            OD_1_1_1DataSet.DataValuesRow row = tblDataValues.NewDataValuesRow();
         
            row.DataValue = (double)dvInfo.Value;
            row.ValueAccuracy = dvInfo.accuracyStdDev;

            string[] vars = Regex.Split(dvInfo.timeOffset, ":");
            double v = 0, f = 1;
            for (int i = 0; i < vars.Count(); i++)
            {
                v += int.Parse(vars[i]) / f;
                f *= 60.0;
            }
            row.UTCOffset = v;
            row.LocalDateTime = dvInfo.dateTime;
            row.DateTimeUTC = dvInfo.dateTimeUTC;

            row.SiteID = siteID;
            row.VariableID = varID;

            row.OffsetValue = dvInfo.offsetValue;
            row.SetOffsetTypeIDNull();
           
            row.CensorCode = dvInfo.censorCode;
            row.SetQualifierIDNull();
            row.MethodID = Convert.ToInt32(dvInfo.methodCode);
            row.SourceID = Convert.ToInt32(dvInfo.sourceCode);
            row.SetSampleIDNull();
            row.SetDerivedFromIDNull();
            row.QualityControlLevelID = Convert.ToInt32(dvInfo.qualityControlLevelCode);

            tblDataValues.AddDataValuesRow(row);

       }

    }


}
