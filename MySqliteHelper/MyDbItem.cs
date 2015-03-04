using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

using System.Reflection;

using System.Data.SQLite;

namespace MySqliteHelper
{    
    /// <summary>
    /// 提供数据库公共操作的基类。
    /// </summary>
    public abstract class MyDbItem
    {
        /// <summary>
        /// 当前被打开的数据库实例。
        /// </summary>
        public static MyDbHelper mDb;

        /// <summary>
        /// 两个对象的id只要一样就认为是同样的对象。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            MyDbItem other = (MyDbItem)obj;
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(other.id)) return base.Equals(other);
            return id.Equals(other.id);
        }

        /// <summary>
        /// 实现了Equals就必须实现hashcode，因为有些代码是通过hashcode来判断两个对象是否一样。
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if (id == null) return base.GetHashCode();
            return id.GetHashCode();
        }

        /// <summary>
        /// 获取插入对象时需要的参数，用来配合InsertToDB()。
        /// 插入对象时的字段不像Update变化那么大，所以可以提供一个公用函数，以简化子类实现。
        /// </summary>
        /// <returns></returns>
        protected abstract SQLiteParameter[] GetParamsOfInsertToDB();
                       
        /// <summary>
        /// 把一个对象插入到数据库。Create。
        /// 会通过抽象函数获得对象真正要插入数据库的字段。
        /// INSERT INTO table_name (c1,c2,c3) VALUES({@c1,@c2,@c3})
        /// </summary>
        public virtual void InsertToDB()
        {
            SQLiteParameter[] fields = GetParamsOfInsertToDB();
            SQLiteCommand cmd = new SQLiteCommand(mDb.connection);
            StringBuilder sname = new StringBuilder();
            StringBuilder svar = new StringBuilder();
            for (int i = 0; i < fields.Length; ++i)
            {
                if (i != 0)
                {
                    sname.Append(", ");
                    svar.Append(", ");
                }
                sname.Append(fields[i].ParameterName);
                svar.Append("@" + fields[i].ParameterName);
                cmd.Parameters.Add(fields[i]);

            }
            cmd.CommandText = String.Format("INSERT INTO {0} ({1}) VALUES({2})", 
                GetMyDbTable().TableName, sname.ToString(), svar.ToString());
            cmd.ExecuteNonQuery();            
        }

        /// <summary>
        /// 实现从读取某个字段的值。类似于：
        /// switch (fieldName) 
        /// {
        ///     case FIELD1:
        ///         field1_var = reader.GetXXX(valueIndex);
        ///         break;
        /// }
        /// 子类必须要实现读取自己所声明的字段。
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="reader"></param>
        /// <param name="valueIndex"></param>
        protected virtual void ReadFieldValue(string fieldName, SQLiteDataReader reader, int valueIndex)
        {            
            switch(fieldName) 
            {
                case NAME_OF_FIELD_ID:
                    this.id = reader.GetString(valueIndex);
                    break;
                case NAME_OF_FIELD_DELETE_TYPE:
                    this.delete_type = reader.GetInt32(valueIndex);
                    break;
                case NAME_OF_FIELD_CHILD_NO:
                    this.child_no = reader.GetInt64(valueIndex);
                    break;
                case NAME_OF_FIELD_NEXT_CHILD_NO:
                    this.next_child_no = reader.GetInt64(valueIndex);
                    break;
            }            
        }

        /// <summary>
        /// 从查询结果中读取字段的值填充对象实例。
        /// 和ReadFieldValue()函数一起完成从数据库加载数据。        
        /// </summary>
        /// <param name="item"></param>
        /// <param name="reader"></param>
        protected static MyDbItem ReadObjectFromDB(MyDbItem item, SQLiteDataReader reader)
        {
            for (int i = 0; i < reader.FieldCount; ++i)
            {
                if(!reader.IsDBNull(i)) item.ReadFieldValue(reader.GetName(i), reader, i);
            }
            return item;
        }

        /// <summary>
        /// 更新给定的字段和值到数据库。
        /// UPDATE table_name SET c1=@c2, c2=@c2 WHERE id=@id
        /// </summary>
        /// <param name="fields"></param>
        public virtual void UpdateToDB(SQLiteParameter[] fields)
        {
            SQLiteCommand cmd = new SQLiteCommand(mDb.connection);

            StringBuilder paramClms = new StringBuilder();
            for (int i = 0; i < fields.Length; ++i)
            {
                if (i != 0) paramClms.Append(", ");
                paramClms.Append(String.Format("{0}=@{0}", fields[i].ParameterName));
                cmd.Parameters.Add(fields[i]);
            }
            cmd.CommandText = String.Format("UPDATE {0} SET {1} WHERE {2}=@{2}", 
                GetMyDbTable().TableName, paramClms.ToString(), FIELD_ID.name);
            cmd.Parameters.Add(new SQLiteParameter(FIELD_ID.name) { Value = this.id });            
            cmd.ExecuteNonQuery();            
        }    

        protected const int DELETE_TYPE_NOT_DELETE = 0;
        protected const int DELETE_TYPE_BY_USER = 1;
        protected const int DELETE_TYPE_BY_PARENT = 2;
        /// <summary>
        /// 删除对象。只是标记删除。
        /// Delete类型默认为1。当因为Host或者Parent删除而导致的删除，比如删除任务时子任务被删除，
        /// 设置为2.可以保证不为0，避免被选出，将来还原时又可以还原回来。
        /// UPDATE table SET delete_type = type WHERE id = this.id
        /// </summary>
        public virtual void DeleteFromDB(int type = 1)
        {
            delete_type = type;
            UpdateToDB(new SQLiteParameter[] { new SQLiteParameter(FIELD_DELETE_TYPE.name) { Value = type } });            
        }
        
        /// <summary>
        /// 彻底从数据库中删除记录。
        /// DELETE FROM table WHERE id = this.id
        /// </summary>
        protected virtual void RealDeleteFromDB() 
        {
            MyDbTable t = GetMyDbTable();            
            SQLiteCommand cmd = new SQLiteCommand(mDb.connection);
            cmd.CommandText = String.Format("DELETE FROM {0} WHERE {1}=@{1}", t.TableName, FIELD_ID.name);
            cmd.Parameters.Add(new SQLiteParameter(FIELD_ID.name) { Value = this.id });            
            cmd.ExecuteNonQuery();            
        }

        /// <summary>
        /// id字段的名称。每个类别的对象都有唯一的id。
        /// </summary>                
        public string id { get; set; }
        private const string NAME_OF_FIELD_ID = "id";
        public static MyDbField FIELD_ID = new MyDbField(NAME_OF_FIELD_ID, MyDbField.TYPE_TEXT, MyDbField.CONSTRAINT_PRIMARY_KEY);

        /// <summary>
        /// 用来排序的序号。
        /// </summary>
        public long child_no { get; set; }
        private const string NAME_OF_FIELD_CHILD_NO = "child_no";
        public static MyDbField FIELD_CHILD_NO = new MyDbField(NAME_OF_FIELD_CHILD_NO, MyDbField.TYPE_INTEGER, "DEFAULT 0");

        public long next_child_no { get; set; }
        private const string NAME_OF_FIELD_NEXT_CHILD_NO = "next_child_no";
        public static MyDbField FIELD_NEXT_CHILD_NO = new MyDbField(NAME_OF_FIELD_NEXT_CHILD_NO, MyDbField.TYPE_INTEGER, "DEFAULT 0");
                 
        /// <summary>
        /// 标记此对象是否已经被删除。0,1,2。
        /// </summary>        
        protected int delete_type { get; set; }
        private const string NAME_OF_FIELD_DELETE_TYPE = "delete_type";
        public static MyDbField FIELD_DELETE_TYPE = new MyDbField(NAME_OF_FIELD_DELETE_TYPE, MyDbField.TYPE_INTEGER, "DEFAULT " + DELETE_TYPE_NOT_DELETE);



        /// <summary>
        /// 把子类传递过来的所有字段信息合并到一起。
        /// </summary>
        /// <param name="childFields"></param>
        /// <returns></returns>
        protected static List<MyDbField> CalFields(List<MyDbField> childFields)
        {
            List<MyDbField> results = new List<MyDbField>();
            results.Add(FIELD_ID);
            results.Add(FIELD_CHILD_NO);
            results.Add(FIELD_NEXT_CHILD_NO);
            results.Add(FIELD_DELETE_TYPE);
            results.AddRange(childFields);
            return results;
        }

        /// <summary>
        /// 把子类传递过来的索引信息合并到一起。
        /// </summary>
        /// <param name="childIndexes"></param>
        /// <returns></returns>
        protected static List<MyDbIndex> CalIndexes(List<MyDbIndex> childIndexes)
        {
            return childIndexes;
        }

        /// <summary>
        /// 获取本类对象的数据库表信息。
        /// 必须要实现的函数，每个子类都会有一个静态MyDbTable对象。
        /// </summary>
        /// <returns></returns>
        public abstract MyDbTable GetMyDbTable();

        /// <summary>
        /// 构造一个数据对象。
        /// </summary>
        /// <param name="initId">
        /// 用来分配给对象的id，如果为null则产生一个新的Guid。
        /// 如果要构造一个对象，但又不想指定及分配id，可以用""。
        /// </param>
        protected MyDbItem(string initId)
        {
            if (initId == null) this.id = Guid.NewGuid().ToString("N");
            else this.id = initId;
        }
    }
}
