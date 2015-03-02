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

        //Get Root Node's ID
        public string GetRootID()
        {
            if (this.parent == null) return this.id;
            SQLiteCommand cmd = new SQLiteCommand(mDb.connection);
            cmd.CommandText = String.Format("SELECT substr({1},1,32) FROM {0} WHERE {2}=@{2}",
                GetMyDbTable().TableName, FIELD_ID_DIR.name, FIELD_ID.name);
            cmd.Parameters.Add(new SQLiteParameter(FIELD_ID.name) { Value = this.id });
            SQLiteDataReader reader = cmd.ExecuteReader();
            string result = null;
            if (reader.Read())
            {
                result = reader.GetString(0);
            }
            reader.Close();
            return result;
        }

        public void AppendNewChild(MyDbTreeItem newChild)
        {
            this.next_child_no += MyDbHelper.CHILD_ITEM_SPAN;
            this.child_count += 1;
            newChild.parent = this.id;
            newChild.child_no = this.next_child_no;
            newChild.id_dir = this.GetFullIdPath();
            //保存
            SQLiteTransaction trans = mDb.BeginTransaction();
            newChild.InsertToDB();
            this.UpdateToDB(new SQLiteParameter[] { 
                new SQLiteParameter(FIELD_NEXT_CHILD_NO.name){Value = this.next_child_no},
                new SQLiteParameter(FIELD_CHILD_COUNT.name){Value = this.child_count},
            });
            trans.Commit();
        }

        public void InsertNewChildBetween(MyDbTreeItem newChild, MyDbTreeItem before, MyDbTreeItem after)
        {
            if (newChild == null) return;
            if (after == null)
            {//后面没有子节点
                AppendNewChild(newChild);
                return;
            }
            else
            {
                this.child_count += 1;
                newChild.parent = this.id;
                newChild.id_dir = this.GetFullIdPath();
                if (before == null)
                {//在首部添加
                    newChild.child_no = after.child_no / 2;
                }
                else
                {//在两个对象之间添加
                    newChild.child_no = (before.child_no + after.child_no) / 2;
                }
                //保存
                SQLiteTransaction trans = mDb.BeginTransaction();
                newChild.InsertToDB();
                this.UpdateToDB(new SQLiteParameter[] {                 
                    new SQLiteParameter(FIELD_CHILD_COUNT.name){Value = this.child_count},
                });
                trans.Commit();
            }
        }


        public void DeleteChild(MyDbTreeItem child)
        {
            if (child == null) return;
            this.child_count -= 1;            
            SQLiteTransaction trans = mDb.BeginTransaction();
            //update parent
            this.UpdateToDB(new SQLiteParameter[] {                 
                    new SQLiteParameter(FIELD_CHILD_COUNT.name){Value = this.child_count},
            });
            //update child
            child.DeleteFromDB(DELETE_TYPE_BY_USER);            
            //update descendants of child
            if (child.child_count != 0)
            {//Update guide SET delete_type=delete_by_parent where id_dir like child.id_dir + "-" + child.id%
                SQLiteCommand cmd = new SQLiteCommand(mDb.connection);
                cmd.CommandText = String.Format("UPDATE {0} SET {1}=@{1} WHERE {2} LIKE @{2}",
                    GetMyDbTable().TableName, FIELD_DELETE_TYPE.name, FIELD_ID_DIR.name);
                cmd.Parameters.Add(new SQLiteParameter(FIELD_DELETE_TYPE.name) { Value = DELETE_TYPE_BY_PARENT });
                cmd.Parameters.Add(new SQLiteParameter(FIELD_ID_DIR.name) { Value = child.GetFullIdPath() + "%" });
                cmd.ExecuteNonQuery();
            }
            trans.Commit();
        }
        
        protected string GetFullIdPath()
        {
            if (this.id_dir == null) return this.id;
            return this.id_dir + "-" + this.id;
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

        public int child_count { get; set; }
        private const string NAME_OF_FIELD_CHILD_COUNT = "child_count";
        public static MyDbField FIELD_CHILD_COUNT = new MyDbField(NAME_OF_FIELD_CHILD_COUNT, MyDbField.TYPE_INTEGER, "DEFAULT 0");
        
        public long next_child_no { get; set; }
        private const string NAME_OF_FIELD_NEXT_CHILD_NO = "next_child_no";
        public static MyDbField FIELD_NEXT_CHILD_NO = new MyDbField(NAME_OF_FIELD_NEXT_CHILD_NO, MyDbField.TYPE_INTEGER, "DEFAULT 0");

        protected static List<MyDbField> CalFields(List<MyDbField> childFields)
        {
            List<MyDbField> myFields = new List<MyDbField>();
            myFields.Add(FIELD_PARENT);
            myFields.Add(FIELD_ID_DIR);            
            myFields.Add(FIELD_CHILD_COUNT);            
            myFields.Add(FIELD_NEXT_CHILD_NO);
            myFields.AddRange(childFields);
            return MyDbItem.CalFields(myFields);
        }

        protected static List<MyDbIndex> CalIndexes(List<MyDbIndex> childIndexes)
        {
            return MyDbItem.CalIndexes(childIndexes);
        }

        protected override void ReadFieldValue(string fieldName, System.Data.SQLite.SQLiteDataReader reader, int valueIndex)
        {
            switch (fieldName)
            {
                case NAME_OF_FIELD_CHILD_COUNT:
                    this.child_count = reader.GetInt32(valueIndex);
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
