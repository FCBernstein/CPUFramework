﻿using System;
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
            return DoExecuteSQL(cmd, true);
        }

        public static void SaveDataRow(DataRow row, string sprocname)
        {
            SqlCommand cmd = GetSQLCommand(sprocname);
            foreach (DataColumn col in row.Table.Columns)
            {
                string paramname = $"@{col.ColumnName}";
                if (cmd.Parameters.Contains(paramname))
                {
                    cmd.Parameters[paramname].Value = row[col.ColumnName];
                }
            }
            DoExecuteSQL(cmd, false);

            foreach (SqlParameter p in cmd.Parameters)
            {
                if (p.Direction == ParameterDirection.InputOutput)
                {
                    string colname = p.ParameterName.Substring(1);
                    if (row.Table.Columns.Contains(colname))
                    {

                        //5-27-2025
                        //JF: I added this in since the PK column was coming in as ReadOnly and therefore preventing the 'set'
                        //I overwrote it here, but should be looked into more if this is due to the new SQL package or something else.
                        //I did not review all the code so it may be due to something simple elsewhere, but I wanted her to be able to continue
                        if (row.Table.Columns[colname].ReadOnly)
                        {
                            row.Table.Columns[colname].ReadOnly = false;
                        }
                        row[colname] = p.Value;
                    }
                }
            }
        }
        private static DataTable DoExecuteSQL(SqlCommand cmd, bool loadtable)
        {
            DataTable dt = new();
            using (SqlConnection conn = new SqlConnection(SQLUtility.ConnectionString))
            {
                conn.Open();
                cmd.Connection = conn;
                Debug.Print(GetSQL(cmd));
                try
                {
                    SqlDataReader dr = cmd.ExecuteReader();
                    CheckReturnValue(cmd);
                    if (loadtable == true)
                    {
                        dt.Load(dr);
                    }
                }
                catch (SqlException ex)
                {
                    string msg = ParseConstraintMsg(ex.Message);
                    throw new Exception(msg);
                }
                catch (InvalidCastException ex)
                {
                    throw new Exception(cmd.CommandText + ": " + ex.Message, ex);
                }
            }
            SetAllColumnsAllowNull(dt);
            return dt;
        }

        private static void CheckReturnValue(SqlCommand cmd)
        {
            int returnvalue = 0;
            string msg = "";
            if (cmd.CommandType == CommandType.StoredProcedure)
            {
                foreach (SqlParameter p in cmd.Parameters)
                {
                    if (p.Direction == ParameterDirection.ReturnValue)
                    {
                        if (p.Value != null)
                        {
                            returnvalue = (int)p.Value;
                        }

                    }
                    else if (p.ParameterName.ToLower() == "@message")
                    {
                        if (p.Value != null)
                        {
                            msg = p.Value.ToString();
                        }
                    }
                }

                if (returnvalue == 1)
                {
                    if (msg == "")
                    {
                        msg = $"{cmd.CommandText} did not do action that was requested.";
                    }
                    throw new Exception(msg);
                }
            }
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
            return DoExecuteSQL(new SqlCommand(sqlstatement), true);
        }

        public static void ExecuteSQL(string sqlstatement)
        {
            GetDataTable(sqlstatement);
        }

        public static void ExecuteSQL(SqlCommand cmd)
        {
            DoExecuteSQL(cmd, false);
        }

        public static void SetParamValue(SqlCommand cmd, string paramname, object value)
        {
            try 
            { 
                cmd.Parameters[paramname].Value = value; 
            }
            catch(Exception ex)
            {
                throw new Exception(cmd.CommandText + ": " + ex.Message, ex);
            }
        }
        private static string ParseConstraintMsg(string msg)
        {
            string origmsg = msg;
            string prefix = "ck_";
            string msgend = "";
            if (msg.Contains(prefix) == false)
            {
                if (msg.Contains("u_"))
                {
                    prefix = "u_";
                    msgend = " must be unique.";
                }
                else if (msg.Contains("f_"))
                {
                    prefix = "f_";
                }
            }
            if (msg.Contains(prefix))
            {
                msg = msg.Replace("\"", "'");
                int pos = msg.IndexOf(prefix) + prefix.Length;
                msg = msg.Substring(pos);
                pos = msg.IndexOf("'");
                if (pos == -1)
                {
                    msg = origmsg;
                }
                else
                {
                    msg = msg.Substring(0, pos);
                    msg = msg.Replace("_", " ");
                    msg = msg + msgend;

                    if (prefix == "f_")
                    {
                        var words = msg.Split(" ");
                        if (words.Length > 1)
                        {
                            msg = $"Cannot delete {words[0]} because it has a related {words[1]} record."; 
                        }
                    }
                }
            }
            return msg;
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
            foreach (DataColumn c in dt1.Columns)
            {
                c.AllowDBNull = true;
            }
        }

        public static string GetSQL(SqlCommand cmd)
        {
            string val = "";
#if DEBUG
            StringBuilder sb = new StringBuilder();

            if (cmd.Connection != null)
            {
                sb.AppendLine($"--{cmd.Connection.DataSource}");
                sb.AppendLine($"use {cmd.Connection.Database}");
                sb.AppendLine("go");
            }

            if (cmd.CommandType == CommandType.StoredProcedure)
            {
                sb.AppendLine($"exec {cmd.CommandText}");
                int paramcount = cmd.Parameters.Count - 1;
                int paramnum = 0;
                string comma = ",";
                foreach (SqlParameter p in cmd.Parameters)
                {
                    if (p.Direction != ParameterDirection.ReturnValue)
                    {
                        if (paramnum == paramcount)
                        {
                            comma = "";
                        }
                        sb.AppendLine($"{p.ParameterName} = {(p.Value == null ? "null" : p.Value.ToString())}{comma}");

                    }
                    paramnum++;
                }
            }
            else
            {
                sb.AppendLine(cmd.CommandText);
            }
            val = sb.ToString();
#endif
            return val;
        }

        public static void DebugPrintDataTable(DataTable dt)
        {
            foreach (DataRow r in dt.Rows)
            {
                foreach (DataColumn c in dt.Columns)
                {
                    Debug.Print(c.ColumnName + " = " + r[c.ColumnName].ToString());
                }
            }
        }
    }
}
//