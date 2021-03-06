﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace CardioRehab_WPF
{
    public class DatabaseClass
    {
        #region variable declaration
        public String projectPath;
        public SQLiteConnection m_dbconnection;

        #endregion

        #region constructor
        public DatabaseClass()
        {
            projectPath = System.AppDomain.CurrentDomain.BaseDirectory;
            projectPath = System.IO.Directory.GetParent(projectPath).FullName;
            projectPath = System.IO.Directory.GetParent(projectPath).FullName;
            projectPath = System.IO.Directory.GetParent(projectPath).FullName;
            projectPath = System.IO.Directory.GetParent(projectPath).FullName;
            m_dbconnection = new SQLiteConnection("Data source=" + projectPath + "\\cardio.db;Version=3;");
        }
        #endregion

        /// <summary>
        /// For running INSERT, UPDATE or DELETE 
        /// </summary>
        /// <param name="sql">the SQL statement</param>
        public void ChangeDB(String sql)
        {
            SQLiteCommand command = new SQLiteCommand(sql, m_dbconnection);
            command.ExecuteNonQuery();
        }

    }



}
