using System.Text;

namespace MySqliteHelper
{    
    /// <summary>
    /// 表信息对象，通过这些信息可以完成表在数据库中的创建、升级以及索引创建。
    /// </summary>
    public sealed class MyDbTable
    {
        private string _tableName;
        /// <summary>
        /// 表的名字。
        /// </summary>
        public string TableName
        {
            get { return _tableName; }
        }

        private MyDbField[] _columnInfos;
        /// <summary>
        /// 表的列信息。                
        /// </summary>
        public MyDbField[] ColumnInfos
        {
            get { return _columnInfos; }
        }

        private MyDbIndex[] _indexInfos;
        /// <summary>
        /// 在此表上创建的索引的信息。
        /// </summary>
        public MyDbIndex[] IndexInfos
        {
            get { return _indexInfos; }
        }

        /// <summary>
        /// 表信息。
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnInfos"></param>
        /// <param name="indexInfos"></param>
        public MyDbTable(string tableName, MyDbField[] columnInfos, MyDbIndex[] indexInfos)
        {
            _tableName = tableName;
            _columnInfos = columnInfos;
            _indexInfos = indexInfos;
        }

        private string mJoinedColumns = null;
        /// <summary>
        /// 把列的名称串起来。类似于 C1,C2,C3
        /// </summary>
        /// <returns></returns>
        public string JoinColumns()
        {
            if (mJoinedColumns != null) return mJoinedColumns;
            StringBuilder sname = new StringBuilder();                        
            bool isFirst = true;
            foreach (MyDbField f in ColumnInfos) 
            {
                if (isFirst) isFirst = false;
                else
                {
                    sname.Append(", ");
                }
                sname.Append(f.name);                
            }            
            mJoinedColumns = sname.ToString();
            return mJoinedColumns;
        }        
    }    

    public sealed class MyDbField
    {
        //内置类型
        public const string TYPE_NULL = "NULL";
        public const string TYPE_INTEGER = "INTEGER";
        public const string TYPE_REAL = "REAL";
        public const string TYPE_TEXT = "TEXT";
        public const string TYPE_BLOB = "BLOB";
        //扩展类型，实际上Sqlite会转为内部类型处理。
        public const string TYPE_DATETIME = "DATETIME";
        public const string TYPE_BOOL = "INTEGER";
        public const string TYPE_DOUBLE = "REAL";
        public const string TYPE_FLOAT = "REAL";

        //列约束
        public const string CONSTRAINT_PRIMARY_KEY = "PRIMARY KEY";
        public const string CONSTRAINT_UNIQUE = "UNIQUE";
        public const string CONSTRAINT_NOT_NULL = "NOT NULL";
        public const string CONSTRAINT_DEFAULT = "DEFAULT";

        private string _name;
        /// <summary>
        /// 列的名称。
        /// </summary>
        public string name { get { return _name; } }
        private string _type;
        /// <summary>
        /// 列的类型。
        /// </summary>
        public string type { get { return _type; } }
        /// <summary>
        /// 列的约束。
        /// </summary>
        private string _constraint;
        public string constraint { get { return _constraint; } }

        /// <summary>
        /// 列信息。
        /// 类似于 {"column_name", "TEXT", "NOT NULL"}
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldType"></param>
        /// <param name="fieldConstraint"></param>
        public MyDbField(string fieldName, string fieldType, string fieldConstraint)
        {
            _name = fieldName;
            _type = fieldType;
            _constraint = fieldConstraint;
        }
    }

    public sealed class MyDbIndex
    {
        private string _name;
        private string _columns;        
        
        /// <summary>
        /// 索引的名称。
        /// </summary>
        public string name { get { return _name; } }
        /// <summary>
        /// 索引牵扯到的列。
        /// 类似于：{ "index_name", "field1, field2" }，顺序要和查询时的顺序配合。
        /// </summary>
        public string columns { get { return _columns; } }

        /// <summary>
        /// 索引信息。
        /// </summary>
        /// <param name="indexTableName"></param>
        /// <param name="indexColumns"></param>
        public MyDbIndex(string indexTableName, string indexColumns)
        {
            _name = indexTableName;
            _columns = indexColumns;
        }
    }
}
