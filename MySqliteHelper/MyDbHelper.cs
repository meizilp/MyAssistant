using System;
using System.Collections.Generic;
using System.Text;

using System.Data.SQLite;

namespace MySqliteHelper
{    
    /// <summary>
    /// 数据库访问辅助对象。
    /// </summary>
    public abstract class MyDbHelper
    {
        private SQLiteConnection mConnection = null;
        /// <summary>
        /// 实际的数据库连接
        /// </summary>
        public SQLiteConnection connection { get { return mConnection; } }

        /// <summary>
        /// 以指定的文件路径创建数据库对象，此时尚未打开数据库。
        /// </summary>
        /// <param name="dbPath"></param>
        public MyDbHelper(string dbPath)
        {
            string dbSource = "Data Source =" + dbPath;
            mConnection = new SQLiteConnection(dbSource);
            mConnection.StateChange += mConnection_StateChange;
        }

        /// <summary>
        /// 获取所有需要在数据库中创建的表信息。
        /// 因为每个应用的表都不一样，所以需要子类具体实现。
        /// </summary>
        protected abstract MyDbTable[] GetAllTableInfo();

        /// <summary>
        /// 查询数据库中已经存在的表。
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="type">"table"或者"index"</param>
        /// <returns></returns>
        private HashSet<string> QueryAllTablesInDb(SQLiteConnection connection, string type)
        {
            HashSet<string> results = new HashSet<string>();
            SQLiteCommand cmd = new SQLiteCommand(connection);
            cmd.CommandText = String.Format("SELECT name from sqlite_master WHERE type = '{0}'", type);
            SQLiteDataReader reader = cmd.ExecuteReader();
            do
            {
                for (; reader.Read(); )
                {                    
                    results.Add(reader.GetString(0));
                }
            } while (reader.NextResult());
            reader.Close();
            return results;
        }

        
        // 数据库的连接状态变为打开时，进行创建表、升级表、创建Index等操作。
        private void mConnection_StateChange(object sender, System.Data.StateChangeEventArgs e)
        {
            if (e.CurrentState == System.Data.ConnectionState.Open)
            {//数据库打开
                SQLiteConnection connection = sender as SQLiteConnection;
                HashSet<string> dbTables = QueryAllTablesInDb(connection, "table");
                HashSet<string> dbIndexes = QueryAllTablesInDb(connection, "index");
                //为每一个类型创建表、升级表并设置默认值
                foreach (MyDbTable t in GetAllTableInfo())
                {
                    //表存在则尝试升级表；表不存在则创建表
                    if (dbTables.Contains(t.TableName)) TryUpgradeTable(t);
                    else CreateTable(t);                    
                    //依次检测本表要创建的索引，不存在则创建索引                    
                    for (int i = 0; i < t.IndexInfos.Count; ++i)
                    {
                        if (!dbIndexes.Contains(t.IndexInfos[i].name))
                        {//索引表在库中尚不存在则创建
                            CreateIndex(t.TableName, t.IndexInfos[i]);
                        }
                    }                        
                }
            }
        }               
                     
        // 创建存储此类对象的表。        
        private void CreateTable(MyDbTable t)
        {
            StringBuilder s = new StringBuilder();            
            bool isFirst = true;
            foreach (MyDbField clm in t.ColumnInfos)
            {
                if (isFirst) isFirst = false;
                else s.Append(", ");
                s.Append(String.Format("{0} {1} {2}", clm.name, clm.type, clm.constraint));                
            }            
            string sql = String.Format("CREATE TABLE {0} ({1})", t.TableName, s.ToString());
            SQLiteCommand cmd = new SQLiteCommand(sql, mConnection);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 创建一个索引。
        /// </summary>
        /// <param name="tblName"></param>
        /// <param name="fieldName"></param>
        private void CreateIndex(string tblName, MyDbIndex index)
        {
            string sql = String.Format(@"CREATE INDEX {0} ON {1} ({2})", index.name, tblName, index.columns);
            SQLiteCommand cmd = new SQLiteCommand(mConnection);
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();            
        }

        /// <summary>
        /// 获取指定名称表包含的所有列的名称。
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private HashSet<string> QueryColumnsOfTableInDb(string tableName)
        {
            HashSet<string> results = new HashSet<string>();
            SQLiteCommand cmd = new SQLiteCommand(connection);
            cmd.CommandText = String.Format("PRAGMA table_info('{0}') ", tableName);
            SQLiteDataReader reader = cmd.ExecuteReader();

            int i = 0;
            for (; i < reader.FieldCount; ++i)
            {//找到name列的索引。一般是1。
                if(reader.GetName(i).Equals("name")) break;
            }
            if (i == reader.FieldCount) return results;
            do
            {
                for (; reader.Read(); )
                {
                    results.Add(reader.GetString(i));
                }
            } while (reader.NextResult());
            reader.Close();
            return results;
        }

        //升级表，增加表缺少的列
        private void TryUpgradeTable(MyDbTable t)
        {
            HashSet<string> dbColumns = QueryColumnsOfTableInDb(t.TableName);                                    
            SQLiteCommand addColumnCmd = new SQLiteCommand(mConnection);            
            //如果列的数量没有变化，那么不用升级。因为sqlite只支持增加列数，不支持删除。
            if (dbColumns.Count == t.ColumnInfos.Count) return;
            SQLiteTransaction trans = mConnection.BeginTransaction();
            foreach (MyDbField clm in t.ColumnInfos)
            {
                if (!dbColumns.Contains(clm.name))
                {//发现没有的列，创建
                    addColumnCmd.CommandText = String.Format(@"ALTER TABLE {0} ADD COLUMN {1} {2} {3}", 
                        t.TableName, clm.name, clm.type, clm.constraint);
                    addColumnCmd.ExecuteNonQuery();
                }
            }                  
            trans.Commit();
        }
        
        /// <summary>
        /// 真实打开数据库。然后创建已经注册的类型所需要的表，并进行升级，创建索引。
        /// </summary>
        public void Open()
        {
            if (mConnection == null || mConnection.State == System.Data.ConnectionState.Open) return;            
            mConnection.Open();
            //把实例保存到全局静态变量，这样操作时就不需要传数据库句柄了。
            MyDbItem.mDb = this;
        }
        
        /// <summary>
        /// 关闭数据库
        /// </summary>
        public void Close()
        {
            if (mConnection == null || mConnection.State == System.Data.ConnectionState.Closed) return;
            mConnection.Close();
            mConnection = null;
            MyDbItem.mDb = null;
        }

        /// <summary>
        /// 开始一个事务。
        /// </summary>
        /// <returns></returns>
        public SQLiteTransaction BeginTransaction()
        {
            return mConnection.BeginTransaction();
        }

        public const long CHILD_ITEM_SPAN = 1048576L;
    }
}
