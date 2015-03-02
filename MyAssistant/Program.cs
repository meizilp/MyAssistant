using System;
using System.Windows.Forms;

using MySqliteHelper;

using MyAssistant.form;
using MyAssistant.db;

namespace MyAssistant
{
    static class Program
    {
        private static MyAssistantDbHelper db = null;
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {            
            db = new MyAssistantDbHelper("MyAssistant.db");
            db.Open();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormGuide());
            if (db != null) 
            { 
                db.Close(); 
                db = null; 
            }
        }
    }
}
