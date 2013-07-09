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

using MultiEndpoint_1_0.WaterML10;
using MultiEndpoint_1_0.HealthQueryDataSetTableAdapters;


namespace MultiEndpoint_1_0
{
    class HealthDataValue
    {
        public DataValueTableAdapter Adapter;
        public HealthQueryDataSet.DataValueDataTable Table;

        public HealthDataValue(SqlConnection sqlConn)
        {
            Adapter = new DataValueTableAdapter();
            Adapter.Connection = sqlConn;
            Adapter.Adapter.UpdateBatchSize = Program.DbUpdateBatchSize;
            Adapter.Adapter.InsertCommand.UpdatedRowSource = UpdateRowSource.None; // required to do batch insertion
            Adapter.Adapter.UpdateCommand.UpdatedRowSource = UpdateRowSource.None; // required to do batch insertion
            Table = new HealthQueryDataSet.DataValueDataTable();
        }

        // Return 1: records changed
        //        0: records unchanged
        public int[] HandleDataValueInfoSplit(int scID, int newID, WaterML10.TimeSeriesResponseType[] tsRtAll)
        {
            //Console.WriteLine("Parsing and inserting data value info.");

            WaterML10.ValueSingleVariable dvInfo;
            HealthQueryDataSet.DataValueRow row;
            int[] hasChanged = new int[3];  //0 or 1
            int[] valueCount = new int[3];

            for (int iflag = 0; iflag < 3; iflag++)
            {
                hasChanged[iflag] = 0;
                // Where WaterML1.1 and WaterML1.0 differ
                if (tsRtAll[iflag].timeSeries.values.value == null)
                {
                    valueCount[iflag] = 0;
                }
                else
                {
                    valueCount[iflag] = Convert.ToInt32(tsRtAll[iflag].timeSeries.values.count);
                }

                // WaterXML 1.0
                // (tsRt.timeSeries.values.count == null) || (tsRt.timeSeries.values.value == null)
                // int valueCount = int.Parse(tsRt.timeSeries.values.count);
                if (valueCount[iflag] == 1 &&
                    tsRtAll[iflag].timeSeries.values.value.ToString() == tsRtAll[iflag].timeSeries.variable.NoDataValue)
                {
                    Console.WriteLine("No values in WS response.");
                    // there are values in SC, but not in the response from WS
                }
            }

            string cond;
            if (scID > 0)
                cond = string.Format("SeriesCatalogID = {0} order by RecordID", scID);
            else
                cond = string.Format("NewSCID = {0} order by RecordID", newID);

            Adapter.FillWhere(Table, cond);

            for (int iflag = 0; iflag < 3; iflag++)
            {
                int recordID = iflag * Program.CompareRecordCount + 1;
                int limit = Math.Min(valueCount[iflag], Program.CompareRecordCount);

                if ((iflag > 0) && valueCount[iflag - 1] < Program.CompareRecordCount)
                {
                    // Skip next loops if previous records are not filled.
                    break;
                }

                for (int j = 0; j < limit; j++)
                {
                    if (iflag < 2)
                    {
                        if (j >= valueCount[iflag])
                            break;
                        dvInfo = tsRtAll[iflag].timeSeries.values.value[j];
                    }
                    else
                    {
                        if (valueCount[iflag] - 1 - j >=0)
                            dvInfo = tsRtAll[iflag].timeSeries.values.value[valueCount[iflag] - 1 - j];
                        else
                            break;
                    }

                    if (recordID + j > Table.Rows.Count)
                    {
                        row = Table.NewDataValueRow();
                        row.DataValue = (double)dvInfo.Value;
                        row.ValueAccuracy = dvInfo.accuracyStdDev;
                        row.DateTimeUTC = dvInfo.dateTime.ToUniversalTime();
                        row.SeriesCatalogID = scID;
                        row.NewSCID = newID;
                        row.RecordID = recordID + j;

                        Table.AddDataValueRow(row);
                        hasChanged[iflag] = 1;
                        continue;
                    }

                    if (iflag < 2)
                    {
                        row = Table.ElementAt(recordID - 1 + j);
                    }
                    else
                    {
                        row = Table.ElementAt(recordID - 1 + Program.CompareRecordCount - 1 - j);
                    }

                    if ((row.DataValue != (double)dvInfo.Value) ||
                        (row.ValueAccuracy != dvInfo.accuracyStdDev) ||
                        (DateTime.Compare(row.DateTimeUTC, dvInfo.dateTime.ToUniversalTime()) != 0))
                    {
                        row.DataValue = (double)dvInfo.Value;
                        row.ValueAccuracy = dvInfo.accuracyStdDev;
                        row.DateTimeUTC = dvInfo.dateTime.ToUniversalTime();
                        hasChanged[iflag] = 1;
                    }

                }

            }

            Adapter.Update(Table);
            return hasChanged;
        }

        public int[] HandleDataValueInfo(int scID, int newID, WaterML10.TimeSeriesResponseType tsRt)
        {
            //Console.WriteLine("Parsing and inserting data value info.");

            WaterML10.ValueSingleVariable dvInfo;
            HealthQueryDataSet.DataValueRow row;
            int[] hasChanged = new int[3] { 0, 0, 0 };  //0 or 1
            int valueCount = Convert.ToInt32(tsRt.timeSeries.values.count);

            if (valueCount == 1 &&
                tsRt.timeSeries.values.value.ToString() == tsRt.timeSeries.variable.NoDataValue)
            {
                Console.WriteLine("No values in WS response.");
                // there are values in SC, but not in the response from WS
            }

            string cond;
            if (scID > 0)
                cond = string.Format("SeriesCatalogID = {0} order by RecordID", scID);
            else
                cond = string.Format("NewSCID = {0} order by RecordID", newID);

            Adapter.FillWhere(Table, cond);

            int[] indices = new int[] { 0, valueCount / 2, valueCount - Program.CompareRecordCount };
            for (int i = 0; i < 3; i++)
            {
                int recordID = i * Program.CompareRecordCount + 1;
                int idx = indices[i];
                int limit = Math.Min(Program.CompareRecordCount, valueCount - indices[i]);

                if ((i > 0) && (indices[i] < Program.CompareRecordCount * i))
                {
                    // Record overlap, all records are put into table, no further action
                    break;
                }

                for (int j = 0; j < limit; j++)
                {
                    dvInfo = tsRt.timeSeries.values.value[idx + j];

                    if (recordID + j > Table.Rows.Count)
                    {
                        row = Table.NewDataValueRow();
                        row.DataValue = (double)dvInfo.Value;
                        row.ValueAccuracy = dvInfo.accuracyStdDev;
                        row.DateTimeUTC = dvInfo.dateTime.ToUniversalTime();
                        row.SeriesCatalogID = scID;
                        row.NewSCID = newID;
                        row.RecordID = recordID + j;

                        Table.AddDataValueRow(row);
                        hasChanged[i] = 1;
                        continue;
                    }

                    row = Table.ElementAt(recordID - 1 + j);

                    if ((row.DataValue != (double)dvInfo.Value) ||
                        (row.ValueAccuracy != dvInfo.accuracyStdDev) ||
                        (DateTime.Compare(row.DateTimeUTC, dvInfo.dateTime.ToUniversalTime()) != 0))
                    {
                        row.DataValue = (double)dvInfo.Value;
                        row.ValueAccuracy = dvInfo.accuracyStdDev;
                        row.DateTimeUTC = dvInfo.dateTime.ToUniversalTime();
                        hasChanged[i] = 1;
                    }

                }
            }
            Adapter.Update(Table);

            return hasChanged;
        }

    }
}
