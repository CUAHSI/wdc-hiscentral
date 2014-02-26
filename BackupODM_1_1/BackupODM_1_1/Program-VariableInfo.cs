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
    class OD_VariableInfo
    {
        // Return number of records inserted or found.
        public static int HandleVariableInfo(SqlConnection sqlConn, VariableInfoType varInfo)
        {
            VariablesTableAdapter varAdapter = new VariablesTableAdapter();
            varAdapter.Connection = sqlConn;

            OD_1_1_1DataSet.VariablesDataTable tblVariables = new OD_1_1_1DataSet.VariablesDataTable();

            Console.WriteLine(">>>Parsing and inserting VARIABLES");

           InsertOneVariable(tblVariables, varInfo, sqlConn);

           try
           {
               varAdapter.Update(tblVariables);
           }
           catch (Exception e)
           {
               Console.WriteLine("Failed to insert VARIABLE {0}: {1}",
                   varInfo.variableCode[0].Value, e.Message);
               return 0;
           }

           return 1;
           //PrintTable(varAdapter, tblVariables);
        }


        static void InsertOneVariable(OD_1_1_1DataSet.VariablesDataTable tblVariables,
            VariableInfoType varInfo, SqlConnection sqlConn)
        {
            string cond;

            OD_1_1_1DataSet.VariablesRow row = tblVariables.NewVariablesRow();

            string[] vars = Regex.Split(varInfo.variableCode[0].Value, "/");
            row.DataType = varInfo.dataType;
            //row.DataType = OD_Utils.ConvertToString(varInfo.dataType);
            row.VariableCode = vars[0] + "_" + row.DataType;
            
            // Check if the variable is already in the table or not
            //??? need to add ValueType filter
            cond = "VariableCode = '" + row.VariableCode + "'";
            if (OD_Utils.Exists(row.Table.TableName, cond, sqlConn))
                return;

            // VariableName is a tricky one too
            //??? I think VariableCode is enough to differentiate different variables 
            cond = "Term = '" + varInfo.variableName + "'";
            if (OD_Utils.Exists("VariableNameCV", cond, sqlConn))
            {
                // Found it and use it
                row.VariableName = varInfo.variableName;
            }
            else
            {
                string myString = varInfo.variableName;
                System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(@"\bas\b");
                string[] splitString = reg.Split(myString);

                // Refer to the document, e.g.,
                //    including "as", eg., "Nitrogen, nitrate (NO3) as N, filtered"
                //    fix ing "Calcium, filtered as Ca"
                if (splitString.Length == 2)
                {
                    row.VariableName = splitString[0];
                }
                else
                {
                    // Remove the string after ","
                    vars = Regex.Split(varInfo.variableName, ",");
                    row.VariableName = vars[0];
                }
            }

            row.Speciation = varInfo.speciation; // "Not Applicable"; //?
            // Note: in DavtaValue Response Type units.unitsCode is actually unitsAbbreviation
            if (varInfo.unit.unitCode != null)
            {
                //cond = "UnitsName = '" + varInfo.unit.unitName + "'";
                //row.VariableUnitsID = OD_Utils.GetPrimaryKey("Units", "UnitsID", cond, sqlConn);
                //if (row.VariableUnitsID >= 143) row.VariableUnitsID = row.VariableUnitsID + 1;
                row.VariableUnitsID = Convert.ToInt32(varInfo.unit.unitCode);
            }
            else
            {
                row.VariableUnitsID = varInfo.unit.unitID;
            }
            
            
            row.SampleMedium = varInfo.sampleMedium;

            row.ValueType = varInfo.valueType;

            row.IsRegular = varInfo.timeScale.isRegular;

            row.TimeSupport = varInfo.timeScale.timeSupport;

            if (varInfo.timeScale.unit.unitCode != null)
            {
                row.TimeUnitsID = Convert.ToInt32(varInfo.timeScale.unit.unitCode);
            }
            else
            {
                row.TimeUnitsID = varInfo.timeScale.unit.unitID;
            }

            row.GeneralCategory = varInfo.generalCategory;

            if (varInfo.noDataValue != null)
                row.NoDataValue = varInfo.noDataValue;
            else
                row.NoDataValue = -9999;

            tblVariables.AddVariablesRow(row);            
        }

        static void PrintTable(VariablesTableAdapter dtAdapter, OD_1_1_1DataSet.VariablesDataTable dt)
        {
            dtAdapter.Fill(dt);

            // Print out the column names.
            for (int curCol = 0; curCol < dt.Columns.Count; curCol++)
            {
                Console.Write(dt.Columns[curCol].ColumnName + "\t");
            }
            Console.WriteLine("\n----------------------------------");

            // Print the DataTable.
            for (int curRow = 0; curRow < dt.Rows.Count; curRow++)
            {
                for (int curCol = 0; curCol < dt.Columns.Count; curCol++)
                {
                    Console.Write(dt.Rows[curRow][curCol].ToString() + "\t");
                }
                Console.WriteLine();
            }
        }
 
    }

}
