using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CPUFramework;

namespace CPUFramework
{
    public class SQLUtility
    {
        public static string ConnectionString = "";

        public static SqlCommand GetSQLCommand(string sprocname)
        {
            SqlCommand cmd;
            using (SqlConnection conn = new SqlConnection(SQLUtility.ConnectionString))
            {
                cmd = new SqlCommand(sprocname, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                conn.Open();
                SqlCommandBuilder.DeriveParameters(cmd);
            }
            return cmd;
        }

        public static DataTable GetDataTable(SqlCommand cmd)
        {
            Debug.Print("------" + Environment.NewLine + cmd.CommandText);
            DataTable dt = new();
            using (SqlConnection conn = new SqlConnection(SQLUtility.ConnectionString))
            {
                conn.Open();
                cmd.Connection = conn;
                SqlDataReader dr = cmd.ExecuteReader();
                dt.Load(dr);
            }
            SetAllColumnsAllowNull(dt);
            return dt;
        }

        public static DataTable GetDataTable(string sqlstatement) //- take a SQL statement and return a DataTable
        {
            //DataTable dt = new();
            //SqlConnection conn = new();
            //conn.ConnectionString = ConnectionString;
            //conn.Open();
            ////DisplayMessage("Conn Status ", conn.State.ToString());
            //var cmd = new SqlCommand();
            //cmd.Connection = conn;
            //cmd.CommandText = sqlstatement;
            //var dr = cmd.ExecuteReader();
            //dt.Load(dr);
            //SetAllColumnsAllowNull(dt);
            return GetDataTable(new SqlCommand(sqlstatement));
        }

        public static void ExecuteSQL(string sqlstatement)
        {
            GetDataTable(sqlstatement);
        }

        public static string GetFirstColumnFirstRowValueString(string sql)
        {
            string n = "";
            DataTable dt = GetDataTable(sql);
            if (dt.Rows.Count > 0 && dt.Columns.Count > 0)
            {
                if (dt.Rows[0][0] != DBNull.Value)
                {
                    n = dt.Rows[0][0].ToString();
                }
               
            }
            return n;
        }

        public static DateTime GetFirstColumnFirstRowValueDate(string sql)
        {
            DateTime n = DateTime.Now;
            DataTable dt = GetDataTable(sql);
            if (dt.Rows.Count > 0 && dt.Columns.Count > 0)
            {
                if (dt.Rows[0][0] != DBNull.Value)
                {
                    DateTime.TryParse(dt.Rows[0][0].ToString(), out n);
                }

            }
            return n;
        }

        public static int GetFirstColumnFirstRowValueInt(string sql)
        {
            int n = 0;
            DataTable dt = GetDataTable(sql);
            if (dt.Rows.Count > 0 && dt.Columns.Count > 0)
            {
                if (dt.Rows[0][0] != DBNull.Value)
                {
                    int.TryParse(dt.Rows[0][0].ToString(), out n);
                }

            }
            return n;
        }

        private static void SetAllColumnsAllowNull(DataTable dt1)
        {
            foreach(DataColumn c in dt1.Columns)
            {
                c.AllowDBNull = true;
            }
        }

        public static void DebugPrintDataTable(DataTable dt)
        {
            foreach(DataRow r in dt.Rows)
            {
                foreach(DataColumn c in dt.Columns)
                {
                    Debug.Print(c.ColumnName + " = " + r[c.ColumnName].ToString());
                }
            }
        }
    }
}
//