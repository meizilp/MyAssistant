using System;
using System.Collections;
using System.Collections.Generic;

using System.Data.SQLite;

namespace MySqliteHelper
{
    public abstract class MyDbTreeItem : MyDbItem
    {
        //Get Children
        protected SQLiteDataReader GetChildren()
        {//select * from table where parent = this.id and delete_type = 0 order by child_no
            SQLiteCommand cmd = new SQLiteCommand(mDb.connection);
            cmd.CommandText = String.Format("SELECT * FROM {0} WHERE {1}=@{1} AND {2}=@{2} ORDER BY {3}",
                GetMyDbTable().TableName, FIELD_PARENT.name, FIELD_DELETE_TYPE.name, FIELD_CHILD_NO.name);
            cmd.Parameters.Add(new SQLiteParameter(FIELD_PARENT.name) { Value = this.id });
            cmd.Parameters.Add(new SQLiteParameter(FIELD_DELETE_TYPE.name) { Value = DELETE_TYPE_NOT_DELETE });
            return cmd.ExecuteReader();            
        }

        //get all descendants。LIKE id_dir + self.id%

        //is ancestor。 if( other.id_dir.contains(self.id) ) then true。

        protected MyDbTreeItem(string initId) : base(initId) { }

        public string parent { get; set; }
        private const string NAME_OF_FIELD_PARENT = "parent";
        public static MyDbField FIELD_PARENT = new MyDbField(NAME_OF_FIELD_PARENT, MyDbField.TYPE_TEXT, null);

        public string id_dir { get; set; }
        private const string NAME_OF_FIELD_ID_DIR = "id_dir";
        public static MyDbField FIELD_ID_DIR = new MyDbField(NAME_OF_FIELD_ID_DIR, MyDbField.TYPE_TEXT, null);

        public long child_no { get; set; }
        private const string NAME_OF_FIELD_CHILD_NO = "child_no";
        public static MyDbField FIELD_CHILD_NO = new MyDbField(NAME_OF_FIELD_CHILD_NO, MyDbField.TYPE_INTEGER, "DEFAULT 0");

        public long next_child_no { get; set; }
        private const string NAME_OF_FIELD_NEXT_CHILD_NO = "next_child_no";
        public static MyDbField FIELD_NEXT_CHILD_NO = new MyDbField(NAME_OF_FIELD_NEXT_CHILD_NO, MyDbField.TYPE_INTEGER, "DEFAULT 0");

        protected override void ReadFieldValue(string fieldName, System.Data.SQLite.SQLiteDataReader reader, int valueIndex)
        {
            switch (fieldName)
            {
                case NAME_OF_FIELD_CHILD_NO:
                    this.child_no = reader.GetInt64(valueIndex); 
                    break;                
                case NAME_OF_FIELD_NEXT_CHILD_NO:
                    this.next_child_no = reader.GetInt64(valueIndex);
                    break;
                case NAME_OF_FIELD_PARENT:
                    this.parent = reader.GetString(valueIndex);
                    break;
                case NAME_OF_FIELD_ID_DIR:
                    this.id_dir = reader.GetString(valueIndex);
                    break;
                default:
                    base.ReadFieldValue(fieldName, reader, valueIndex);
                    break;
            }            
        }
        
    }
}
