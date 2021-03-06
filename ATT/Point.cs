﻿#region copyright
// Copyright 2013-2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Npgsql;
using PostGIS = LAIR.ResourceAPIs.PostGIS;
using LAIR.ResourceAPIs.PostgreSQL;
using NpgsqlTypes;
using LAIR.Collections.Generic;
using LAIR.MachineLearning;

namespace PTL.ATT
{
    public class Point : ClassifiableEntity
    {
        public class Columns
        {
            [Reflector.Insert, Reflector.Select(true)]
            public const string Id = "id";
            [Reflector.Insert, Reflector.Select(true)]
            public const string IncidentType = "incident_type";
            [Reflector.Insert, Reflector.Select(true)]
            public const string Location = "location";
            [Reflector.Insert, Reflector.Select(true)]
            public const string Time = "time";

            public static string StX(string table) { return "st_x(" + table + "." + Location + ") as " + X(table); }
            public static string X(string table) { return table + "_x"; }
            public static string StY(string table) { return "st_y(" + table + "." + Location + ") as " + Y(table); }
            public static string Y(string table) { return table + "_y"; }
            public static string StSRID(string table) { return "st_srid(" + table + "." + Location + ") as " + SRID(table); }
            public static string SRID(string table) { return table + "_srid"; }

            public static string Insert { get { return Reflector.GetInsertColumns(typeof(Columns)); } }
            public static string Select(string table) { return Reflector.GetSelectColumns(table, new string[] { StX(table), StY(table), StSRID(table) }, typeof(Columns)); }
        }

        public static string GetTableName(Prediction prediction)
        {
            return "point_" + prediction.Id;
        }

        internal static string CreateTable(Prediction prediction, int srid)
        {
            string table = GetTableName(prediction);

            DB.Connection.ExecuteNonQuery(
                "CREATE TABLE " + table + " (" +
                Columns.Id + " SERIAL PRIMARY KEY," +
                Columns.IncidentType + " VARCHAR," +
                Columns.Location + " GEOMETRY(GEOMETRY," + srid + ")," +
                Columns.Time + " TIMESTAMP);" +
                "CREATE INDEX ON " + table + " (" + Columns.IncidentType + ");" +
                "CREATE INDEX ON " + table + " USING GIST (" + Columns.Location + ");");

            return table;
        }

        internal static void DeleteTable(Prediction prediction)
        {
            DB.Connection.ExecuteNonQuery("DROP TABLE " + GetTableName(prediction) + " CASCADE");
        }

        public static void VacuumTable(Prediction prediction)
        {
            DB.Connection.ExecuteNonQuery("VACUUM ANALYZE " + GetTableName(prediction));
        }
        internal static void FilterByZipCode(NpgsqlConnection connection,string zipcodeShapeFile, Prediction prediction, int zipcode, bool vacuum, int maxIncidentID)
        {
            NpgsqlCommand cmd = DB.Connection.NewCommand(null, null, connection);
            string pointTable = GetTableName(prediction);
           cmd.CommandText = "DELETE FROM " + pointTable + " WHERE " + pointTable + "." + Columns.Id + " not in" +
                "((SELECT " + pointTable + "." + Columns.Id + "  FROM " + pointTable + " LEFT JOIN " + zipcodeShapeFile + " " +
                  "ON st_intersects(st_expand(" + pointTable + "." + Columns.Location + "," + prediction.PredictionPointSpacing / 2 + ")," + zipcodeShapeFile + ".geom) WHERE " + pointTable + "." + Columns.Id + ">" + maxIncidentID +
                "  AND  " + zipcodeShapeFile + ".zip ='" + zipcode + "') union (SELECT " + pointTable + "." + Columns.Id + "  FROM " + pointTable + " LEFT JOIN " + zipcodeShapeFile + " " +
                 "ON st_intersects( " + pointTable + "." + Columns.Location + "," + zipcodeShapeFile + ".geom) WHERE " + pointTable + "." + Columns.Id + "<=" + maxIncidentID +
                " AND  " + zipcodeShapeFile + ".zip ='" + zipcode + "'))";
            cmd.ExecuteNonQuery();

            if (vacuum)
                VacuumTable(prediction);
        }
       
        internal static List<int> Insert(NpgsqlConnection connection,
                                         IEnumerable<Tuple<PostGIS.Point, string, DateTime>> points,
                                         Prediction prediction,
                                         Area area,
                                         bool vacuum)
        {
            NpgsqlCommand cmd = DB.Connection.NewCommand(null, null, connection);

            string pointTable = GetTableName(prediction);
            List<int> ids = new List<int>();
            StringBuilder pointValues = new StringBuilder();
            int pointNum = 0;
            int pointsPerBatch = 1000;
            foreach (Tuple<PostGIS.Point, string, DateTime> pointIncidentTime in points)
            {
                PostGIS.Point point = pointIncidentTime.Item1;
                string incidentType = pointIncidentTime.Item2;
                DateTime time = pointIncidentTime.Item3;

                if (point.SRID != area.Shapefile.SRID)
                    throw new Exception("Area SRID (" + area.Shapefile.SRID + ") does not match point SRID (" + point.SRID);

                pointValues.Append((pointValues.Length > 0 ? "," : "") + "(DEFAULT,'" + incidentType + "',st_geometryfromtext('POINT(" + point.X + " " + point.Y + ")'," + point.SRID + "),@time_" + pointNum + ")");
                ConnectionPool.AddParameters(cmd, new Parameter("time_" + pointNum, NpgsqlDbType.Timestamp, time));

                if ((++pointNum % pointsPerBatch) == 0)
                {
                    cmd.CommandText = "INSERT INTO " + pointTable + " (" + Columns.Insert + ") VALUES " + pointValues + " RETURNING " + Columns.Id;

                    NpgsqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                        ids.Add(Convert.ToInt32(reader[0]));

                    reader.Close();
                    pointValues.Clear();
                    cmd.Parameters.Clear();
                }
            }

            if (pointValues.Length > 0)
            {
                cmd.CommandText = "INSERT INTO " + pointTable + " (" + Columns.Insert + ") VALUES " + pointValues + " RETURNING " + Columns.Id;

                NpgsqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                    ids.Add(Convert.ToInt32(reader[0]));

                reader.Close();
                pointValues.Clear();
                cmd.Parameters.Clear();
            }

            if (vacuum)
                VacuumTable(prediction);

            return ids;
        }

        private int _id;
        private string _incidentType;
        private PostGIS.Point _location;
        private DateTime _time;

        public int Id
        {
            get { return _id; }
        }

        public string IncidentType
        {
            get { return _incidentType; }
        }

        public PostGIS.Point Location
        {
            get { return _location; }
        }

        public DateTime Time
        {
            get { return _time; }
            set { _time = value; }
        }

        internal Point(NpgsqlDataReader reader, string table)
        {
            Construct(reader, table);
        }

        internal Point(int id, string incidentType, PostGIS.Point location, DateTime time)
        {
            Construct(id, incidentType, location, time);
        }

        private void Construct(NpgsqlDataReader reader, string table)
        {
            Construct(Convert.ToInt32(reader[table + "_" + Columns.Id]),
                      Convert.ToString(reader[table + "_" + Columns.IncidentType]),
                      new PostGIS.Point(Convert.ToDouble(reader[Columns.X(table)]), Convert.ToDouble(reader[Columns.Y(table)]), Convert.ToInt32(reader[Columns.SRID(table)])),
                      Convert.ToDateTime(reader[table + "_" + Columns.Time]));
        }

        private void Construct(int id, string incidentType, PostGIS.Point location, DateTime time)
        {
            _id = id;
            _incidentType = incidentType;
            _location = location;
            _time = time;
        }
    }
}
