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
            SQLiteCommand cmd = new SQLiteCommand(mDb.connection);
            cmd.CommandText = String.Format("SELECT * FROM {0} WHERE {1}=@{1} AND {2}=@{2} ORDER BY {3}",
                mTable.TableName, FIELD_PARENT.name, FIELD_DELETE_TYPE.name, FIELD_CHILD_NO.name);
            cmd.Parameters.Add(new SQLiteParameter(FIELD_PARENT.name){Value= this.id});
            cmd.Parameters.Add(new SQLiteParameter(FIELD_DELETE_TYPE.name) { Value = DELETE_TYPE_NOT_DELETE });           

            SQLiteDataReader reader = cmd.ExecuteReader();
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

        private static Guide CreateRootGuide(int type)
        {
            Guide result;
            if(type == GUIDE_TYPE_COLLECT) 
            {
                result = new Guide(COLLECT_GUIDE_ROOT_ID);
                result.type = type;
                result.text = "收集向导";
                result.InsertToDB(new SQLiteParameter[]{
                    new SQLiteParameter(FIELD_ID.name, result.id),
                    new SQLiteParameter(FIELD_TYPE.name, result.type),
                    new SQLiteParameter(FIELD_TEXT.name, result.text),
                });
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
        private static MyDbField FIELD_TEXT = new MyDbField(NAME_OF_FIELD_TEXT, MyDbField.TYPE_TEXT, null);
        
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
