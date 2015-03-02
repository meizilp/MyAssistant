using System;
using System.Collections.Generic;

using MySqliteHelper;

using System.Data.SQLite;

namespace MyAssistant.db
{
    internal class Guide : MyDbTreeItem
    {

        public const int GUIDE_TYPE_COLLECT = 0;
        public static Guide GetRootGuide(int type)
        {
            SQLiteCommand cmd = new SQLiteCommand(mDb.connection);
            cmd.CommandText = String.Format("SELECT * FROM {0} WHERE {1} IS NULL AND {2}=@{2} AND {3}=@{3}",
                mTable.TableName, FIELD_PARENT.name, FIELD_DELETE_TYPE.name, FIELD_TYPE.name);
            cmd.Parameters.Add(new SQLiteParameter(FIELD_DELETE_TYPE.name) { Value = DELETE_TYPE_NOT_DELETE });
            cmd.Parameters.Add(new SQLiteParameter(FIELD_TYPE.name) { Value = GUIDE_TYPE_COLLECT });                        
            SQLiteDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                Guide result = new Guide("");
                ReadObjectFromDB(result, reader);
                reader.Close();
                return result;
            }
            else
            {
                reader.Close();
                return CreateRootGuide(type);
            }
        }

        public List<Guide> GetChildGuides()
        {
            SQLiteDataReader reader = GetChildren();
            List<Guide> results = new List<Guide>();
            do
            {
                for (; reader.Read(); )
                {
                    results.Add(ReadObjectFromDB(new Guide(""), reader) as Guide);
                }
            } while (reader.NextResult());
            reader.Close();
            return results;
        }       

        private void InsertToDB()
        {
            InsertToDB(new SQLiteParameter[] { 
                new SQLiteParameter(FIELD_ID.name){Value = id},
                new SQLiteParameter(FIELD_PARENT.name){Value = parent},
                new SQLiteParameter(FIELD_CHILD_NO.name){Value = child_no},
                new SQLiteParameter(FIELD_TEXT.name){Value = text},
                new SQLiteParameter(FIELD_TYPE.name){Value = type},
                new SQLiteParameter(FIELD_ID_DIR.name){Value = id_dir},
            });
        }

        public void AppendNewChild(Guide newChild)
        {            
            this.next_child_no += MyAssistantDbHelper.CHILD_ITEM_SPAN;
            this.child_guide_num += 1;
            newChild.parent = this.id;
            newChild.child_no = this.next_child_no;
            newChild.id_dir = this.GetFullIdPath();
            //保存
            SQLiteTransaction trans = mDb.BeginTransaction();
            newChild.InsertToDB();
            this.UpdateToDB(new SQLiteParameter[] { 
                new SQLiteParameter(FIELD_NEXT_CHILD_NO.name){Value = this.next_child_no},
                new SQLiteParameter(FIELD_CHILD_GUIDE_NUM.name){Value = this.child_guide_num},
            });            
            trans.Commit();
        }

        public void InsertNewChildBetween(Guide newChild, Guide before, Guide after)
        {
            if (newChild == null) return;
            if (after == null)
            {//后面没有子节点
                AppendNewChild(newChild);
                return;
            }
            else
            {
                this.child_guide_num += 1;
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
                    new SQLiteParameter(FIELD_CHILD_GUIDE_NUM.name){Value = this.child_guide_num},
                });
                trans.Commit();
            }
        }

        public void DeleteChild(Guide child)
        {
            if (child == null) return;
            this.child_guide_num -= 1;
            child.delete_type = DELETE_TYPE_BY_USER;
            SQLiteTransaction trans = mDb.BeginTransaction();
            //update parent
            this.UpdateToDB(new SQLiteParameter[] {                 
                    new SQLiteParameter(FIELD_CHILD_GUIDE_NUM.name){Value = this.child_guide_num},
            });
            //update child
            child.UpdateToDB(new SQLiteParameter[] {                 
                    new SQLiteParameter(FIELD_DELETE_TYPE.name){Value = child.delete_type},
                });
            //update descendants of child
            if (child.child_guide_num != 0)
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
        
        private static Guide CreateRootGuide(int type)
        {
            Guide result;
            if(type == GUIDE_TYPE_COLLECT) 
            {
                result = new Guide(COLLECT_GUIDE_ROOT_ID);
                result.type = type;
                result.text = "收集向导";                
                result.InsertToDB();
                return result;  
            }
            return null;
        }

        internal Guide(string initId = null) : base(initId)
        {            
        }

        private const string COLLECT_GUIDE_ROOT_ID = "collect0root0guide0id88888888888";

        protected override void ReadFieldValue(string fieldName, SQLiteDataReader reader, int valueIndex)
        {            
            switch (fieldName)
            {                
                case NAME_OF_FIELD_TEXT:
                    text = reader.GetString(valueIndex);
                    break;             
                case NAME_OF_FIELD_CHILD_GUIDE_NUM:
                    child_guide_num = reader.GetInt32(valueIndex);
                    break;                
                case NAME_OF_FIELD_TYPE:
                    type = reader.GetInt32(valueIndex);
                    break;
                default:
                    base.ReadFieldValue(fieldName, reader, valueIndex);
                    break;
            }
        }
                
        public string text { get; set; }
        private const string NAME_OF_FIELD_TEXT = "text";
        public static MyDbField FIELD_TEXT = new MyDbField(NAME_OF_FIELD_TEXT, MyDbField.TYPE_TEXT, null);
        
        public int child_guide_num { get; set; }
        private const string NAME_OF_FIELD_CHILD_GUIDE_NUM = "child_guide_num";
        private static MyDbField FIELD_CHILD_GUIDE_NUM = new MyDbField(NAME_OF_FIELD_CHILD_GUIDE_NUM, MyDbField.TYPE_INTEGER, "DEFAULT 0");

        public int type { get; set; }
        private const string NAME_OF_FIELD_TYPE = "type";
        private static MyDbField FIELD_TYPE = new MyDbField(NAME_OF_FIELD_TYPE, MyDbField.TYPE_INTEGER, "NOT NULL");

        private static MyDbField[] mFields = {
                                                FIELD_ID,
                                                FIELD_DELETE_TYPE,
                                                FIELD_PARENT,
                                                FIELD_CHILD_NO,
                                                FIELD_NEXT_CHILD_NO,
                                                FIELD_TEXT,                                                
                                                FIELD_CHILD_GUIDE_NUM,                                                
                                                FIELD_TYPE,    
                                                FIELD_ID_DIR,
                                             };
        private static MyDbIndex[] mIndexes = {
                                                  
                                              };
        internal static MyDbTable mTable = new MyDbTable("guide", mFields, mIndexes);
        public override MyDbTable GetMyDbTable()
        {
            return mTable;
        }
    }
}
