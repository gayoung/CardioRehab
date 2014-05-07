using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace CardioRehab
{
    class DatabaseClass
    {
        #region variable declaration
        public String projectPath;
        public SQLiteConnection m_dbconnection;

        #endregion

        #region constructor
        public DatabaseClass()
        {
            projectPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            Console.WriteLine(projectPath);
        }
        #endregion

        public void ConnectToDB()
        {
            m_dbconnection = new SQLiteConnection("Data source=" + projectPath + "\\Django\\mysite\\db.sqlite3;Version=3;");
            m_dbconnection.Open();
        }

        /// <summary>
        /// For running INSERT, UPDATE or DELETE 
        /// </summary>
        /// <param name="sql">the SQL statement</param>
        public void ChangeDB(String sql)
        {
            SQLiteCommand command = new SQLiteCommand(sql, m_dbconnection);
            command.ExecuteNonQuery();
        }

        public List<DatabaseRecord> GetRecord(String sql)
        {
            SQLiteCommand command = new SQLiteCommand(sql, m_dbconnection);
            SQLiteDataReader reader = command.ExecuteReader();

        }
      
    }



}
