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

        /// <summary>
        /// 在尾部添加一个新的子节点。
        /// </summary>
        /// <param name="newChild"></param>
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

        /// <summary>
        /// 在两个对象之间添加添加一个新的子节点。
        /// </summary>
        /// <param name="newChild"></param>
        /// <param name="before"></param>
        /// <param name="after"></param>
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

        /// <summary>
        /// 移动一个节点。
        /// </summary>
        /// <param name="oriParent"></param>
        /// <param name="newParent"></param>
        /// <param name="newBefore"></param>
        /// <param name="newAfter"></param>
        public void Move(MyDbTreeItem oriParent, MyDbTreeItem newParent, MyDbTreeItem newBefore, MyDbTreeItem newAfter)
        {
            if (!oriParent.id.Equals(newParent.id))
            {//跨父节点移动
                oriParent.child_count -= 1;
                newParent.child_count += 1;
                this.parent = newParent.id;
                this.id_dir = newParent.GetFullIdPath();
            }
            if (newAfter == null)
            {//后面没有节点
                newParent.next_child_no += MyDbHelper.CHILD_ITEM_SPAN;
                this.child_no = newParent.next_child_no;                
            }
            else
            {
                if (newBefore == null)
                {
                    this.child_no = newAfter.child_no / 2;
                }
                else
                {
                    this.child_no = (newBefore.child_no + newAfter.child_no) / 2;
                }
            }
            SQLiteTransaction trans = mDb.BeginTransaction();
            //update newParent
            newParent.UpdateToDB(new SQLiteParameter[] {
                new SQLiteParameter(FIELD_CHILD_COUNT.name){Value=newParent.child_count},
                new SQLiteParameter(FIELD_NEXT_CHILD_NO.name){Value=newParent.next_child_no},
            });
            if (!oriParent.Equals(newParent))
            {//update oriParent                
                oriParent.UpdateToDB(new SQLiteParameter(FIELD_CHILD_COUNT.name){Value=oriParent.child_count});
            }            
            //update this
            this.UpdateToDB(new SQLiteParameter[] {
                new SQLiteParameter(FIELD_PARENT.name){Value=this.parent},
                new SQLiteParameter(FIELD_ID_DIR.name){Value=this.id_dir},
                new SQLiteParameter(FIELD_CHILD_NO.name){Value=this.child_no},
            });
            trans.Commit();
        }

        /// <summary>
        /// 删除一个子节点。
        /// 子节点移动不要调用此函数来组合，直接使用Move函数。
        /// 操作完成后父节点的子节点介绍一个；子节点本身被标记删除；子节点的子节点们也被标记删除。
        /// </summary>
        /// <param name="child"></param>
        public virtual void DeleteChild(MyDbTreeItem child)
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
            {//Update guide SET delete_type=delete_by_parent where delete_type=no_delete and id_dir like child.id_dir + "-" + child.id%
                SQLiteCommand cmd = new SQLiteCommand(mDb.connection);
                cmd.CommandText = String.Format("UPDATE {0} SET {1}=@{1} WHERE {1}=@{1}2 AND {2} LIKE @{2}",
                    GetMyDbTable().TableName, FIELD_DELETE_TYPE.name, FIELD_ID_DIR.name);
                cmd.Parameters.Add(new SQLiteParameter(FIELD_DELETE_TYPE.name) { Value = DELETE_TYPE_BY_PARENT });
                cmd.Parameters.Add(new SQLiteParameter(FIELD_DELETE_TYPE.name+"2") { Value = DELETE_TYPE_NOT_DELETE });
                cmd.Parameters.Add(new SQLiteParameter(FIELD_ID_DIR.name) { Value = child.GetFullIdPath() + "%" });
                cmd.ExecuteNonQuery();
            }
            trans.Commit();
        }
        
        /// <summary>
        /// 得到完成的ID路径。
        /// </summary>
        /// <returns></returns>
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
        
        
        //计算列信息，把本类定义的插入到子类定义的前面
        protected new static List<MyDbField> CalFields(List<MyDbField> childFields)
        {
            List<MyDbField> myFields = new List<MyDbField>();
            myFields.Add(FIELD_PARENT);
            myFields.Add(FIELD_ID_DIR);            
            myFields.Add(FIELD_CHILD_COUNT);                        
            myFields.AddRange(childFields);
            return MyDbItem.CalFields(myFields);
        }

        //计算索引信息
        protected new static List<MyDbIndex> CalIndexes(List<MyDbIndex> childIndexes)
        {
            return MyDbItem.CalIndexes(childIndexes);
        }

        //读取本类中定义的字段
        protected override void ReadFieldValue(string fieldName, System.Data.SQLite.SQLiteDataReader reader, int valueIndex)
        {
            switch (fieldName)
            {
                case NAME_OF_FIELD_CHILD_COUNT:
                    this.child_count = reader.GetInt32(valueIndex);
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
