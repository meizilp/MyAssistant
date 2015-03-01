using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqliteHelper
{
    public abstract class MyDbTreeItem : MyDbItem
    {

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
