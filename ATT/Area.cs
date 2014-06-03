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
using LAIR.Collections.Generic;
using PTL.ATT.Models;

namespace PTL.ATT
{
    [Serializable]
    public class Area
    {
        #region static members
        internal const string Table = "area";

        internal class Columns
        {
            [Reflector.Select(true)]
            internal const string Id = "id";
            [Reflector.Insert, Reflector.Select(true)]
            internal const string Name = "name";
            [Reflector.Insert, Reflector.Select(true)]
            internal const string ShapefileId = "shapefile_id";
            [Reflector.Insert, Reflector.Select(true)]
            internal const string SRID = "srid";

            internal static string Insert { get { return Reflector.GetInsertColumns(typeof(Columns)); } }
            internal static string Select { get { return Reflector.GetSelectColumns(Table, typeof(Columns)); } }
        }

        [ConnectionPool.CreateTable(typeof(Shapefile))]
        private static string CreateTable(ConnectionPool connection)
        {
            return "CREATE TABLE IF NOT EXISTS " + Table + " (" +
                   Columns.Id + " SERIAL PRIMARY KEY," +
                   Columns.Name + " VARCHAR," +
                   Columns.ShapefileId + " INTEGER REFERENCES " + Shapefile.Table + " ON DELETE CASCADE," +
                   Columns.SRID + " INTEGER);";
        }

        public static int Create(Shapefile shapefile, string name, int pointContainmentBoundingBoxSize)
        {
            int areaId = -1;
            try
            {
                areaId = Convert.ToInt32(DB.Connection.ExecuteScalar("INSERT INTO " + Area.Table + " (" + Columns.Insert + ") VALUES ('" + name + "'," + shapefile.Id + "," + shapefile.SRID + ") RETURNING " + Columns.Id));

                Console.Out.WriteLine("Creating area geometry");
                AreaGeometry.Create(shapefile, areaId);

                Console.Out.WriteLine("Creating area bounding boxes");
                AreaBoundingBoxes.Create(areaId, shapefile.SRID, pointContainmentBoundingBoxSize);

                return areaId;
            }
            catch (Exception ex)
            {
                try { DB.Connection.ExecuteNonQuery("DELETE FROM " + Table + " WHERE " + Columns.Id + "=" + areaId); }
                catch (Exception ex2) { Console.Out.WriteLine("Failed to delete area from table:  " + ex2.Message); }

                throw ex;
            }
        }

        public static List<Area> GetAll()
        {
            List<Area> areas = new List<Area>();
            NpgsqlCommand cmd = DB.Connection.NewCommand("SELECT " + Columns.Select + " FROM " + Table);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
                areas.Add(new Area(reader));

            reader.Close();
            DB.Connection.Return(cmd.Connection);

            return areas;
        }

        public static List<Area> GetForSRID(int srid)
        {
            if (srid < 0)
                throw new ArgumentException("Invalid SRID:  " + srid + ". Must be >= 0.", "srid");

            List<Area> areas = new List<Area>();
            NpgsqlCommand cmd = DB.Connection.NewCommand("SELECT " + Columns.Select + " FROM " + Table + " WHERE " + Columns.SRID + "=" + srid);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
                areas.Add(new Area(reader));

            reader.Close();
            DB.Connection.Return(cmd.Connection);

            return areas;
        }

        public static List<Area> GetForShapefile(Shapefile shapefile)
        {
            List<Area> areas = new List<Area>();
            NpgsqlCommand cmd = DB.Connection.NewCommand("SELECT " + Columns.Select + " FROM " + Table + " WHERE " + Columns.ShapefileId + "=" + shapefile.Id);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
                areas.Add(new Area(reader));

            reader.Close();
            DB.Connection.Return(cmd.Connection);

            return areas;
        }
        #endregion

        private int _id;
        private string _name;
        private int _srid;
        private PostGIS.Polygon _boundingBox;
        private int _shapefileId;

        public int Id
        {
            get { return _id; }
        }

        public string Name
        {
            get { return _name; }
        }

        public int SRID
        {
            get { return _srid; }
        }

        public PostGIS.Polygon BoundingBox
        {
            get { return _boundingBox; }
        }

        public int ShapefileId
        {
            get { return _shapefileId; }
        }

        public Area(int id)
        {
            NpgsqlCommand cmd = DB.Connection.NewCommand("SELECT " + Columns.Select + " FROM " + Area.Table + " WHERE " + Columns.Id + "=" + id);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            reader.Read();
            Construct(reader);
            reader.Close();
            DB.Connection.Return(cmd.Connection);
        }

        private Area(NpgsqlDataReader reader)
        {
            Construct(reader);
        }

        private void Construct(NpgsqlDataReader reader)
        {
            _id = Convert.ToInt32(reader[Table + "_" + Columns.Id]);
            _name = Convert.ToString(reader[Table + "_" + Columns.Name]);
            _shapefileId = Convert.ToInt32(reader[Table + "_" + Columns.ShapefileId]);
            _srid = Convert.ToInt32(reader[Table + "_" + Columns.SRID]);

            NpgsqlCommand cmd = DB.Connection.NewCommand("SELECT " +
                                                         "st_xmin(" + AreaGeometry.Columns.Geometry + ") as left," +
                                                         "st_xmax(" + AreaGeometry.Columns.Geometry + ") as right," +
                                                         "st_ymin(" + AreaGeometry.Columns.Geometry + ") as bottom," +
                                                         "st_ymax(" + AreaGeometry.Columns.Geometry + ") as top " +
                                                         "FROM " + AreaGeometry.GetTableName(_srid) + " " +
                                                         "WHERE " + AreaGeometry.Columns.AreaId + "=" + _id);
            reader = cmd.ExecuteReader();

            double left = double.MaxValue;
            double right = double.MinValue;
            double bottom = double.MaxValue;
            double top = double.MinValue;
            while (reader.Read())
            {
                double l = Convert.ToDouble(reader["left"]);
                if (l < left)
                    left = l;

                double r = Convert.ToDouble(reader["right"]);
                if (r > right)
                    right = r;

                double b = Convert.ToDouble(reader["bottom"]);
                if (b < bottom)
                    bottom = b;

                double t = Convert.ToDouble(reader["top"]);
                if (t > top)
                    top = t;
            }

            reader.Close();
            DB.Connection.Return(cmd.Connection);

            _boundingBox = new PostGIS.Polygon(new PostGIS.LineString(new List<PostGIS.Point>(new PostGIS.Point[]{
                               new PostGIS.Point(left, top, _srid),
                               new PostGIS.Point(right, top, _srid),
                               new PostGIS.Point(right, bottom, _srid),
                               new PostGIS.Point(left, bottom, _srid),
                               new PostGIS.Point(left, top, _srid)}), _srid), _srid);
        }

        public override bool Equals(object obj)
        {
            if(!(obj is Area))
                return false;

            Area other = obj as Area;

            return _id == other.Id;
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }

        public override string ToString()
        {
            return _name;
        }

        public string GetDetails(int indentLevel)
        {
            string indent = "";
            for (int i = 0; i < indentLevel; ++i)
                indent += "\t";

            return (indentLevel > 0 ? Environment.NewLine : "") + indent + "ID:  " + _id + Environment.NewLine +
                   indent + "Name:  " + _name + Environment.NewLine +
                   indent + "Bounding box:  " + _boundingBox.LowerLeft + "," + _boundingBox.UpperLeft + "," + _boundingBox.UpperRight + "," + _boundingBox.LowerRight + Environment.NewLine +
                   indent + "Shapefile:  " + new Shapefile(_shapefileId) + Environment.NewLine + 
                   indent + "SRID:  " + _srid;
        }

        public IEnumerable<int> Contains(IEnumerable<PostGIS.Point> points)
        {
            if (points.Count() == 0)
                return new int[0];

            NpgsqlCommand cmd = DB.Connection.NewCommand("CREATE TABLE temp (" +
                                                         "point GEOMETRY(POINT," + _srid + ")," +
                                                         "num INT);" +
                                                         "CREATE INDEX ON temp USING GIST(point);");
            cmd.ExecuteNonQuery();

            int pointNum = 0;
            int pointsPerBatch = 5000;
            StringBuilder cmdText = new StringBuilder();
            foreach (PostGIS.Point point in points)
            {
                if (point.SRID != _srid)
                    throw new Exception("Area SRID (" + _srid + ") does not match point SRID (" + point.SRID);

                cmdText.Append((cmdText.Length == 0 ? "INSERT INTO temp VALUES" : ",") + " (" + point.StGeometryFromText + "," + pointNum++ + ")");
                if ((pointNum % pointsPerBatch) == 0)
                {
                    cmd.CommandText = cmdText.ToString();
                    cmd.ExecuteNonQuery();
                    cmdText.Clear();
                }
            }

            if (cmdText.Length > 0)
            {
                cmd.CommandText = cmdText.ToString();
                cmd.ExecuteNonQuery();
                cmdText.Clear();
            }

            string areaGeometryTable = AreaGeometry.GetTableName(_srid);
            string areaBoundingBoxesTable = AreaBoundingBoxes.GetTableName(_srid);

            cmd.CommandText = "SELECT num " +
                              "FROM temp " +
                              "WHERE EXISTS (SELECT 1 " +
                                                "FROM " + areaGeometryTable + "," + areaBoundingBoxesTable + " " +
                                                "WHERE " + areaGeometryTable + "." + AreaGeometry.Columns.AreaId + "=" + _id + " AND " +
                                                           areaBoundingBoxesTable + "." + AreaBoundingBoxes.Columns.AreaId + "=" + _id + " AND " +
                                                           "(" +
                                                             "(" +
                                                                areaBoundingBoxesTable + "." + AreaBoundingBoxes.Columns.Relationship + "='" + AreaBoundingBoxes.Relationship.Within + "' AND " +
                                                                "st_intersects(temp.point," + areaBoundingBoxesTable + "." + AreaBoundingBoxes.Columns.BoundingBox + ")" +
                                                             ") " +
                                                             "OR " +
                                                             "(" +
                                                                areaBoundingBoxesTable + "." + AreaBoundingBoxes.Columns.Relationship + "='" + AreaBoundingBoxes.Relationship.Overlaps + "' AND " +
                                                                "st_intersects(temp.point," + areaBoundingBoxesTable + "." + AreaBoundingBoxes.Columns.BoundingBox + ") AND " +
                                                                "st_intersects(temp.point," + areaGeometryTable + "." + AreaGeometry.Columns.Geometry + ")" +
                                                             ")" +
                                                           ")" +
                                            ")";

            List<int> containedPointIndices = new List<int>(pointNum);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
                containedPointIndices.Add(Convert.ToInt32(reader["num"]));
            reader.Close();

            cmd.CommandText = "DROP TABLE temp";
            cmd.ExecuteNonQuery();
            DB.Connection.Return(cmd.Connection);

            return containedPointIndices;
        }
    }
}