using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;

namespace MySqliteHelper
{
    public abstract class MyDbTreeItem : MyDbItem
    {
        /// <summary>
        /// 查询此节点的所有直接直接点。
        /// </summary>
        /// <returns></returns>
        protected SQLiteDataReader GetChildren()
        {//select * from table where parent = this.id and delete_type = 0 order by no
            SQLiteCommand cmd = new SQLiteCommand(mDb.connection);
            cmd.CommandText = String.Format("SELECT * FROM {0} WHERE {1}=@{1} AND {2}=@{2} ORDER BY {3}",
                GetMyDbTable().TableName, FIELD_PARENT.name, FIELD_DELETE_TYPE.name, FIELD_NO.name);
            cmd.Parameters.Add(new SQLiteParameter(FIELD_PARENT.name) { Value = this.id });
            cmd.Parameters.Add(new SQLiteParameter(FIELD_DELETE_TYPE.name) { Value = DELETE_TYPE_NOT_DELETE });
            return cmd.ExecuteReader();            
        }

        /// <summary>
        /// 获取此节点的根节点ID，现在是取路径的前32个字符。
        /// </summary>
        /// <returns></returns>
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
        /// <param name="newChild">要添加的新节点，必须是新建的对象，未在数据库中存储过。</param>
        public void AppendNewChild(MyDbTreeItem newChild)
        {
            if (newChild == null || newChild.delete_type != DELETE_TYPE_NOT_DELETE) return;                       
            newChild.parent = this.id;
            newChild.no = this.child_count;            
            newChild.id_dir = this.GetFullIdPath();
            this.child_count += 1;
            //保存
            SQLiteTransaction trans = mDb.BeginTransaction();
            newChild.InsertToDB();
            this.UpdateToDB(new SQLiteParameter(FIELD_CHILD_COUNT.name){Value = this.child_count});
            trans.Commit();
        }

        //UPDATE table_name SET no=no+delta where parent=parent.id and deleted=0 and no>=start_no
        private static void UpdateChildrenNo(MyDbTreeItem parent,int delta, int start_no, int end_no)
        {
            SQLiteCommand cmd = new SQLiteCommand(mDb.connection);
            StringBuilder cmdText = new StringBuilder();
            cmdText.Append(String.Format("UPDATE {0} SET {1}={1}{2} WHERE {3}=@{3} AND {4}=@{4} AND {1}>=@{5}",
                parent.GetMyDbTable().TableName, 
                FIELD_NO.name, delta>0 ? "+"+delta.ToString() : delta.ToString(), 
                FIELD_PARENT.name, 
                FIELD_DELETE_TYPE.name, 
                "start_no"));
            if (end_no >= 0)
            {//如果有截止no，那么就只修改指定的范围内的记录。
                cmdText.Append(String.Format(" AND {0}<=@{1}", FIELD_NO.name, "end_no"));
                cmd.Parameters.Add(new SQLiteParameter("end_no") { Value = end_no });
            }
            cmd.CommandText = cmdText.ToString();
            cmd.Parameters.Add(new SQLiteParameter(FIELD_PARENT.name) { Value = parent.id });
            cmd.Parameters.Add(new SQLiteParameter(FIELD_DELETE_TYPE.name) { Value = DELETE_TYPE_NOT_DELETE });
            cmd.Parameters.Add(new SQLiteParameter("start_no") { Value = start_no });
            cmd.ExecuteNonQuery();     
        }

        /// <summary>
        /// 插入一个新的子节点。
        /// 插入之后所有的直接点都应从数据库重新加载。
        /// </summary>
        /// <param name="newChild">要插入的新对象，必须是新建的对象。</param>
        /// <param name="before"></param>
        /// <param name="after"></param>
        public void InsertNewChild(MyDbTreeItem newChild, MyDbTreeItem sibling, bool isBefore)
        {
            if (newChild == null || newChild.delete_type != DELETE_TYPE_NOT_DELETE) return;
            if (sibling == null)
            {//在尾部添加
                AppendNewChild(newChild);
                return;
            }
            else
            {
                newChild.parent = this.id;
                newChild.id_dir = this.GetFullIdPath();
                SQLiteTransaction trans = mDb.BeginTransaction();
                if (isBefore)
                {//所有sibling之后以及sibling本身的no+1，腾出位置来。
                 //UPDATE table_name SET no=no+1 where parent=this.id and deleted=0 and no>=sibling.no
                    UpdateChildrenNo(this, 1, sibling.no, -1);
                    newChild.no = sibling.no;                    
                }
                else
                {//所有sibling之后记录的no+1，腾出位置来。
                 //UPDATE table_name SET no=no+1 where parent=this.id and deleted=0 and no>=sibling.no+1
                    UpdateChildrenNo(this, 1, sibling.no+1, -1);
                    newChild.no = sibling.no + 1;
                }
                this.child_count += 1;                                                
                newChild.InsertToDB();
                this.UpdateToDB(new SQLiteParameter(FIELD_CHILD_COUNT.name){Value = this.child_count});
                trans.Commit();
            }
        }

        //同一个parent，向前移动。
        private void MoveForward(MyDbTreeItem parent, MyDbTreeItem sibling, bool isBefore)
        {
            if (isBefore)
            {//f(this)在b(sibling)之后，并且移动到b之前。f-->b之前
                //那么f从队列移出，从b开始到f之前的元素位置都往后一个，b原来的位置空出来，f放入，完成。
                UpdateChildrenNo(parent, 1, sibling.no, this.no - 1);
                this.no = sibling.no;
            }
            else
            {//f在b之后，并且移动到b之后。
                //那么f从队列移出，从b之后一个位置开始到f之前的元素位置都往后一个，b之后一个的位置空出来，f放入，完成。
                UpdateChildrenNo(parent, 1, sibling.no+1, this.no - 1);
                this.no = sibling.no + 1;
            }
            this.UpdateToDB(new SQLiteParameter(FIELD_NO.name) { Value = this.no });
        }

        //同一个parent，向后移动。
        private void MoveBackward(MyDbTreeItem parent, MyDbTreeItem sibling, bool isBefore)
        {
            if (isBefore)
            {//b(this)在f(sibling)之前，并且移动到f之前。
                //那么b从队列移出，b之后到f之前的所有元素位置提前一个，f之前的位置空出来，b放入，完成。
                UpdateChildrenNo(parent, -1, this.no + 1, sibling.no - 1);
                this.no = sibling.no - 1;
            }
            else
            {//b在f之前，并且移动到f之后。
                //那么b从队列移出，b之后到f所有元素位置提前一个，f原来的位置空出来，b放入，完成。
                UpdateChildrenNo(parent, -1, this.no + 1, sibling.no);
                this.no = sibling.no;
            }
            this.UpdateToDB(new SQLiteParameter(FIELD_NO.name) { Value = this.no });
        }

        //向上移动一个位置。前一个的no+1，自己的no-1
        public void MoveUp()
        {
            if (no == 0) return;
            SQLiteTransaction trans = mDb.BeginTransaction();
            //UPDATE guide SET no=no+1 WHERE parent = this.parent and deleted = 0 and no = this.no -1 
            SQLiteCommand cmd = new SQLiteCommand(mDb.connection);
            cmd.CommandText = String.Format("UPDATE {0} SET {1}={1}+1 WHERE {2}=@{2} AND {3}=@{3} AND {1}=@{1}",
                GetMyDbTable().TableName,
                FIELD_NO.name,
                FIELD_PARENT.name,
                FIELD_DELETE_TYPE.name
                );
            cmd.Parameters.Add(new SQLiteParameter(FIELD_PARENT.name) { Value = this.parent });
            cmd.Parameters.Add(new SQLiteParameter(FIELD_DELETE_TYPE.name) { Value = DELETE_TYPE_NOT_DELETE });
            cmd.Parameters.Add(new SQLiteParameter(FIELD_NO.name) { Value = no - 1 });
            if (cmd.ExecuteNonQuery() != 0)
            {//确实修改了前一个节点
                --no;
                UpdateToDB(new SQLiteParameter(FIELD_NO.name) { Value = this.no });
            }
            trans.Commit();
        }

        //向下移动一个位置。后一个的no-1，自己的no+1
        public void MoveDown()
        {
            SQLiteTransaction trans = mDb.BeginTransaction();
            //UPDATE guide SET no=no-1 WHERE parent = this.parent and deleted = 0 and no = this.no + 1 
            SQLiteCommand cmd = new SQLiteCommand(mDb.connection);
            cmd.CommandText = String.Format("UPDATE {0} SET {1}={1}-1 WHERE {2}=@{2} AND {3}=@{3} AND {1}=@{1}",
                GetMyDbTable().TableName,
                FIELD_NO.name,
                FIELD_PARENT.name,
                FIELD_DELETE_TYPE.name
                );
            cmd.Parameters.Add(new SQLiteParameter(FIELD_PARENT.name) { Value = this.parent });
            cmd.Parameters.Add(new SQLiteParameter(FIELD_DELETE_TYPE.name) { Value = DELETE_TYPE_NOT_DELETE });
            cmd.Parameters.Add(new SQLiteParameter(FIELD_NO.name) { Value = no + 1 });
            if (cmd.ExecuteNonQuery() != 0)
            {//确实修改了后一个节点
                ++no;
                UpdateToDB(new SQLiteParameter(FIELD_NO.name) { Value = this.no });
            }
            trans.Commit();
        }

        //解除和一个child的关系。子节点数-1；所有此节点后的no-1
        private void DetachChild(MyDbTreeItem child)
        {
            this.child_count -= 1;
            UpdateChildrenNo(this, -1, child.no+1, -1);
            this.UpdateToDB(new SQLiteParameter(FIELD_CHILD_COUNT.name) { Value = this.child_count });
        }

        public void PasteChild(MyDbTreeItem child, MyDbTreeItem sibling)
        {
            SQLiteTransaction trans = mDb.BeginTransaction();
            AttachChild(child, sibling, false);
            trans.Commit();
        }

        //和一个child建立关系。子节点数+1；所有此节点后的no+1。此child已经已经被删除或者从别的节点上移除。
        private void AttachChild(MyDbTreeItem child, MyDbTreeItem sibling, bool isBefore)
        {            
            child.parent = this.id;                        
            //更新所有子孙节点的id_dir路径
            if (child.child_count != 0)
            {//Update guide SET id_dir=replace(old_id_dir,child_path,new_child_path) where id_dir like child.id_dir + "-" + child.id%
                SQLiteCommand cmd = new SQLiteCommand(mDb.connection);
                cmd.CommandText = String.Format("UPDATE {0} SET {1}=replace({1}, '{2}', '{3}') WHERE {1} LIKE @{1}",
                    GetMyDbTable().TableName, FIELD_ID_DIR.name, child.GetFullIdPath(), this.GetFullIdPath()+"-"+child.id);                
                cmd.Parameters.Add(new SQLiteParameter(FIELD_ID_DIR.name) { Value = child.GetFullIdPath() + "%" });
                cmd.ExecuteNonQuery();
            }
            child.id_dir = this.GetFullIdPath();
            if (child.delete_type != DELETE_TYPE_NOT_DELETE)
            {//已经被标记删除的对象还要执行恢复本身和所有非用户删除的子孙节点的操作。
                if (child.child_count != 0)
                {//Update guide SET deleted = 0 where deleted = 2 and id_dir like child.id_dir + "-" + child.id%
                    SQLiteCommand cmd = new SQLiteCommand(mDb.connection);
                    cmd.CommandText = String.Format("UPDATE {0} SET {1}=@{1} WHERE {1}=@{1}2 AND {2} LIKE @{2}",
                        GetMyDbTable().TableName, FIELD_DELETE_TYPE.name, FIELD_ID_DIR.name);
                    cmd.Parameters.Add(new SQLiteParameter(FIELD_DELETE_TYPE.name) { Value = DELETE_TYPE_NOT_DELETE });
                    cmd.Parameters.Add(new SQLiteParameter(FIELD_DELETE_TYPE.name + "2") { Value = DELETE_TYPE_BY_PARENT });
                    cmd.Parameters.Add(new SQLiteParameter(FIELD_ID_DIR.name) { Value = child.GetFullIdPath() + "%" });
                    cmd.ExecuteNonQuery();
                }
                child.Recovery();
            }

            if (sibling == null)
            {//在尾部追加
                child.no = this.child_count;
            }
            else
            {
                if (isBefore)
                {//抢占sibling的位置
                    UpdateChildrenNo(this, 1, sibling.no, -1);
                    child.no = sibling.no;
                }
                else
                {//抢占sibling后一个位置
                    UpdateChildrenNo(this, 1, sibling.no + 1, -1);
                    child.no = sibling.no + 1;
                }
            }
            this.child_count += 1;
            this.UpdateToDB(new SQLiteParameter(FIELD_CHILD_COUNT.name) { Value = this.child_count });
            child.UpdateToDB(new SQLiteParameter[] {
                new SQLiteParameter(FIELD_PARENT.name) { Value = child.parent},
                new SQLiteParameter(FIELD_ID_DIR.name) { Value = child.id_dir},
                new SQLiteParameter(FIELD_NO.name) { Value = child.no},
            });
        }
        
        /// <summary>
        /// 移动一个节点。
        /// 移动操作完成后所有相关节点应重新载入。
        /// 如果sibling为null，那么就是加入到新parent的尾部。
        /// </summary>        
        public void Move(MyDbTreeItem oriParent, MyDbTreeItem newParent, MyDbTreeItem sibling, bool isBefore)
        {                        
            if (oriParent.Equals(newParent))
            {//同父节点移动
                if (isBefore)
                {//如果本来就在sibling的前一个则不需要处理。
                    if (sibling != null && this.no == sibling.no - 1) return;
                }
                else
                {//如果本来就在sibling的后一个则不需要处理。
                    if (sibling != null && this.no == sibling.no + 1) return;
                }
                //只是改变位置
                SQLiteTransaction trans = mDb.BeginTransaction();
                if (this.no < sibling.no)
                {//向后移动。比如B-》F
                    MoveBackward(oriParent, sibling, isBefore);
                }
                else
                {//向前移动。比如F-》B
                    MoveForward(oriParent, sibling, isBefore);
                }
                trans.Commit();
            }
            else
            {//跨父节点移动                
                SQLiteTransaction trans = mDb.BeginTransaction();
                oriParent.DetachChild(this);
                newParent.AttachChild(this, sibling, isBefore);
                trans.Commit();
            }            
        }

        /// <summary>
        /// 删除一个子节点。        
        /// 操作完成后父节点的子节点介绍一个；子节点本身被标记删除；子节点的子节点们也被标记删除。
        /// 所有其后的兄弟节点索引值减1. UPDATE table_name SET no=no-1 where parent=this.id and deleted=0 and no>=this.no+1
        /// </summary>
        /// <param name="child"></param>
        public virtual void DeleteChild(MyDbTreeItem child)
        {
            if (child == null) return;
            this.child_count -= 1;            
            SQLiteTransaction trans = mDb.BeginTransaction();
            //update parent
            this.UpdateToDB(new SQLiteParameter(FIELD_CHILD_COUNT.name){Value = this.child_count});
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
            //update sibling
            UpdateChildrenNo(this, -1, child.no+1, -1);
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

        //父节点
        public string parent { get; set; }
        private const string NAME_OF_FIELD_PARENT = "parent";
        public static MyDbField FIELD_PARENT = new MyDbField(NAME_OF_FIELD_PARENT, MyDbField.TYPE_TEXT, null);

        //id路径
        public string id_dir { get; set; }
        private const string NAME_OF_FIELD_ID_DIR = "id_dir";
        public static MyDbField FIELD_ID_DIR = new MyDbField(NAME_OF_FIELD_ID_DIR, MyDbField.TYPE_TEXT, null);

        //本节点在所有兄弟节点中的索引
        public int no { get; set; }
        private const string NAME_OF_FIELD_NO = "no";
        public static MyDbField FIELD_NO = new MyDbField(NAME_OF_FIELD_NO, MyDbField.TYPE_INTEGER, "DEFAULT 0");
        
        //子节点数量
        public int child_count { get; set; }
        private const string NAME_OF_FIELD_CHILD_COUNT = "child_count";
        public static MyDbField FIELD_CHILD_COUNT = new MyDbField(NAME_OF_FIELD_CHILD_COUNT, MyDbField.TYPE_INTEGER, "DEFAULT 0");
                
        //直接继承的子类会调用此函数，把子类的字段信息传递过来合并到一起。
        protected new static List<MyDbField> CalFields(List<MyDbField> childFields)
        {
            List<MyDbField> myFields = new List<MyDbField>();
            myFields.Add(FIELD_PARENT);
            myFields.Add(FIELD_ID_DIR);
            myFields.Add(FIELD_NO);
            myFields.Add(FIELD_CHILD_COUNT);                   
            myFields.AddRange(childFields);
            return MyDbItem.CalFields(myFields);
        }

        //直接继承的子类会调用此函数把索引信息传递过来合并到一起。
        protected new static List<MyDbIndex> CalIndexes(List<MyDbIndex> childIndexes)
        {
            return MyDbItem.CalIndexes(childIndexes);
        }

        //读取本类中定义的字段的值。
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
                case NAME_OF_FIELD_NO:
                    this.no = reader.GetInt32(valueIndex);
                    break;
                default:
                    base.ReadFieldValue(fieldName, reader, valueIndex);
                    break;
            }            
        }
        
    }
}
