using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySqliteHelper;

namespace MyAssistant.db
{
    public class MyAssistantDbHelper : MyDbHelper
    {
        public MyAssistantDbHelper(string path)
            : base(path)
        {

        }

        private static MyDbTable[] mAllTables = {
                                                Guide.mTable
                                                };

        protected override MyDbTable[] GetAllTableInfo()
        {
            return mAllTables;
        }
    }
}
