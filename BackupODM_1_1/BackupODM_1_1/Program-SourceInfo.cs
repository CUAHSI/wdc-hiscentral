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
    class OD_SourceInfo
    {
        // No one single data source provides enough data for Sources table,
        // so try all resources to put them together.
        public static void HandleSourceInfo(SqlConnection sqlConn,
            SiteInfoType stinfo, seriesCatalogTypeSeries scts, TimeSeriesType tst)
        {
            string cond = "SourceID = " + scts.source.sourceID;
            if (OD_Utils.Exists("Sources", cond, sqlConn))
                return;

            SourcesTableAdapter srcAdapter = new SourcesTableAdapter();
            srcAdapter.Connection = sqlConn;
            int metadataID = 0;
            string title = "Unknown";

            OD_1_1_1DataSet.SourcesDataTable tblSources = new OD_1_1_1DataSet.SourcesDataTable();

            // We currently don't have any information about ISOMetaDataTable. Just create
            // an unkown entry to resolve foreign key dependency.
            if (scts.source.metadata != null)
            {
                title = scts.source.metadata.title;
            }
            cond = string.Format("Title = '{0}'", title);
            metadataID = OD_Utils.GetPrimaryKey("ISOMetadata", "MetadataID", cond, sqlConn);
            if (metadataID < 0)
            {
                InsertOneMetadata(scts.source.metadata, sqlConn);
                metadataID = OD_Utils.GetPrimaryKey("ISOMetadata", "MetadataID", cond, sqlConn);
            }


            Console.WriteLine(">>>Parsing and inserting SOURCES");
            InsertOneSource(tblSources, stinfo, scts, tst, metadataID, sqlConn);

            //srcAdapter.Update(tblSources);

            //PrintTable(srcAdapter, tblSites);            
        }

        static void InsertOneSource(OD_1_1_1DataSet.SourcesDataTable srcTable,
            SiteInfoType stinfo, seriesCatalogTypeSeries scts, TimeSeriesType tst,
            int metadataID, SqlConnection sqlConn)
        {
            OD_1_1_1DataSet.SourcesRow row = srcTable.NewSourcesRow();

            row.SourceID = scts.source.sourceID;
            row.Organization = scts.source.organization;
            row.SourceDescription = scts.source.sourceDescription;

            row.MetadataID = metadataID;

            string tbd = "TBD";

            row.SourceLink = tbd;
            if (scts.source.sourceLink != null)
                row.SourceLink = scts.source.sourceLink[0];

            row.ContactName = row.Phone = row.Email =
                row.Address = row.City = row.State =
                row.ZipCode = row.Citation = tbd;

            for (int i = 0; (stinfo.note != null) && (i < stinfo.note.Count()); i++)
            {
                NoteType note = stinfo.note[i];
                switch (note.title)
                {
                    case "ContactName":
                        row.ContactName = note.Value;
                        break;
                    case "Phone":
                        row.Phone = note.Value;
                        break;
                    case "Email":
                        row.Email = note.Value;
                        break;
                    case "Address":
                        row.Address = note.Value;
                        break;
                    case "City":
                        row.City = note.Value;
                        break;
                    case "State":
                        row.State = note.Value;
                        break;
                    case "ZipCode":
                        row.ZipCode = note.Value;
                        break;
                    case "Citation":
                        row.Citation = note.Value;
                        break;
                }
            }

            //srcTable.AddSourcesRow(row);
            string sql = string.Format(@"SET IDENTITY_INSERT [Sources] ON;
INSERT INTO [Sources] 
([SourceID], [Organization], [SourceDescription], [SourceLink],
[ContactName], [Phone], [Email], [Address], [City], [State], [ZipCode],
[Citation], [MetadataID])
VALUES ({0}, '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', {12});
SET IDENTITY_INSERT [Sources] OFF",
           row.SourceID, row.Organization, row.SourceDescription, row.SourceLink,
           row.ContactName, row.Phone, row.Email, row.Address, row.City, row.State, row.ZipCode,
           row.Citation, row.MetadataID);

           OD_Utils.RunNonQuery(sql, sqlConn);
        }


        static int InsertOneMetadata(MetaDataType m, SqlConnection sqlConn)
        {
            string sql;

            if (m != null)
                sql = string.Format(@"INSERT INTO ISOMetadata ([TopicCategory],[Title],[Abstract],[ProfileVersion],[MetadataLink]) 
                    VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')",
                    m.topicCategory,
                    m.title,
                    m.@abstract,
                    m.profileVersion,
                    m.metadataLink);
            else
                sql = @"INSERT INTO ISOMetadata ([TopicCategory],[Title],[Abstract],[ProfileVersion],[MetadataLink]) 
                    VALUES ('Unknown', 'Unknown', 'Unknown', 'Unknown', 'Unkown')";

            return OD_Utils.RunNonQuery(sql, sqlConn);
        }

    }
}
