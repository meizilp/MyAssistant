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

        protected override SQLiteParameter[] GetParamsOfInsertToDB()
        {
            return new SQLiteParameter[] { 
                new SQLiteParameter(FIELD_ID.name){Value = id},
                new SQLiteParameter(FIELD_PARENT.name){Value = parent},
                new SQLiteParameter(FIELD_CHILD_NO.name){Value = child_no},
                new SQLiteParameter(FIELD_TEXT.name){Value = text},
                new SQLiteParameter(FIELD_TYPE.name){Value = type},
                new SQLiteParameter(FIELD_ID_DIR.name){Value = id_dir}, 
            };
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

        //默认的收集向导根节点ID。
        private const string COLLECT_GUIDE_ROOT_ID = "collect0root0guide0id88888888888";

        protected override void ReadFieldValue(string fieldName, SQLiteDataReader reader, int valueIndex)
        {            
            switch (fieldName)
            {                
                case NAME_OF_FIELD_TEXT:
                    text = reader.GetString(valueIndex);
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
                
        public int type { get; set; }
        private const string NAME_OF_FIELD_TYPE = "type";
        private static MyDbField FIELD_TYPE = new MyDbField(NAME_OF_FIELD_TYPE, MyDbField.TYPE_INTEGER, "NOT NULL");

        
        private static List<MyDbField> mMyFields = new List<MyDbField>()
        {
            FIELD_TEXT,
            FIELD_TYPE,
        };

        private static List<MyDbIndex> mMyIndexes = new List<MyDbIndex>()
        {                                                  
        };

        internal static MyDbTable mTable = new MyDbTable(
            "guide", 
            MyDbTreeItem.CalFields(mMyFields), 
            MyDbTreeItem.CalIndexes(mMyIndexes)
            );

        public override MyDbTable GetMyDbTable()
        {
            return mTable;
        }
    }
}
