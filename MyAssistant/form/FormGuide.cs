using System;
using System.Collections.Generic;
using System.Windows.Forms;

using MyAssistant.db;
using BrightIdeasSoftware;
using System.Data.SQLite;

namespace MyAssistant.form
{
    public partial class FormGuide : Form
    {
        public FormGuide()
        {
            InitializeComponent();

            treeGuides.CanExpandGetter = delegate(object row)
            {
                Guide guide = row as Guide;
                if (guide.child_count != 0 || 
                    (mNewGuide != null && mNewGuide.parentObject.Equals(guide))) return true;
                return false;
            };

            treeGuides.ChildrenGetter = delegate(object row) 
            {
                return LoadChildrenOfGuide(row as Guide);                
            };
        }

        private Guide mRoot;
        private void FormGuide_Load(object sender, EventArgs e)
        {
            mRoot = Guide.GetRootGuide(Guide.GUIDE_TYPE_COLLECT);
            List<Guide> roots = new List<Guide>();
            roots.Add(mRoot);
            treeGuides.Roots = roots;
            treeGuides.ExpandAll();

            SimpleDropSink sink = treeGuides.DropSink as SimpleDropSink;
            sink.CanDropBetween = true;
        }

        private void menuNewGuide_Click(object sender, EventArgs e)
        {            
            mNewGuide = new NewGuideWrapper();            
            Guide selectedGuide = treeGuides.SelectedObject as Guide;
            if (selectedGuide == null || selectedGuide.parent == null)
            {//没有选择任何节点或选择了根节点，在根节点的子节点列表最后添加。
                mNewGuide.siblingObject = null;
                mNewGuide.parentObject = mRoot;
                mNewGuide.index = mRoot.child_count;
            }
            else 
            {//在选中节点的后面添加
                mNewGuide.siblingObject = selectedGuide;
                mNewGuide.parentObject = treeGuides.GetParent(selectedGuide) as Guide;
                List<Guide> children = mNewGuide.parentObject.GetChildGuides();
                mNewGuide.index = children.IndexOf(mNewGuide.siblingObject) + 1;
                //如果是最后一个，那么转为尾部添加，可在ConfirmAddDir中减少一次查询子目录列表。
                if (mNewGuide.index == children.Count) mNewGuide.siblingObject = null;  
            }
            if (treeGuides.IsExpanded(mNewGuide.parentObject)) treeGuides.RefreshObject(mNewGuide.parentObject);
            else treeGuides.Expand(mNewGuide.parentObject);
            treeGuides.EditModel(mNewGuide);
        }

        private void menuNewChildGuide_Click(object sender, EventArgs e)
        {
            mNewGuide = new NewGuideWrapper();
            if (treeGuides.SelectedObject == null)
            {//没有选择任何节点，在根节点的最后添加。                
                mNewGuide.parentObject = mRoot;                
            }
            else
            {//在选中节点的子列表最后面添加                
                mNewGuide.parentObject = treeGuides.SelectedObject as Guide;                                
            }
            mNewGuide.index = mNewGuide.parentObject.child_count;

            if (treeGuides.IsExpanded(mNewGuide.parentObject)) treeGuides.RefreshObject(mNewGuide.parentObject);
            else treeGuides.Expand(mNewGuide.parentObject);

            treeGuides.EditModel(mNewGuide);
        }

        private void menuDelete_Click(object sender, EventArgs e)
        {
            Guide target = treeGuides.SelectedObject as Guide;
            if (target == null || target.parent == null) 
            {
                MessageBox.Show("空节点或根节点不能删除！");
                return; 
            }
            Guide parent = treeGuides.GetParent(target) as Guide;
            parent.DeleteChild(target);
            treeGuides.RefreshObject(parent);
        }

        private NewGuideWrapper mNewGuide;
        private List<Guide> LoadChildrenOfGuide(Guide guide)
        {
            List<Guide> results;
            if (guide.GetType() == typeof(NewGuideWrapper)) results = new List<Guide>();
            else results = guide.GetChildGuides();
            if (mNewGuide != null && mNewGuide.parentObject.GetHashCode() == guide.GetHashCode())
            {//正在新建节点，并且新建节点的parent在查询子节点，那么要把新建节点加入结果中，才能在tree中显示。
                results.Insert(mNewGuide.index, mNewGuide);
            }
            return results;
        }

        private void CancelNewGuide(NewGuideWrapper tmpGuide)
        {
            mNewGuide = null;
            treeGuides.RefreshObject(tmpGuide.parentObject);
        }

        private void ConfirmNewGuide(NewGuideWrapper tmpGuide, string newValue)
        {
            mNewGuide = null;
            Guide parent = tmpGuide.parentObject;
            Guide newGuide = new Guide();            
            newGuide.text = newValue;

            if (tmpGuide.siblingObject == null)
            {//兄弟对象为空，在子对象列表尾部追加
                parent.AppendNewChild(newGuide);
            }
            else
            {//在兄弟对象之后追加
                List<Guide> children = parent.GetChildGuides();
                Guide before = tmpGuide.siblingObject;
                Guide after;
                if (tmpGuide.index == children.Count) after = null; //新节点后面不再有兄弟节点
                else after = children[tmpGuide.index];      //原来这个位置的对象会被挤到后面
                parent.InsertNewChildBetween(newGuide, before, after);
            }
            
            //刷新父对象
            treeGuides.RefreshObject(parent);
            treeGuides.SelectedObject = newGuide;
        }

        private void ConfirmEditGuide(Guide guide, CellEditEventArgs e)
        {
            if (guide.parent == null)
            {
                e.Cancel = true;
                MessageBox.Show("根节点不能修改名称！");
                return;
            }
            guide.text = e.NewValue as string;
            guide.UpdateToDB(new SQLiteParameter[]{
                new SQLiteParameter(Guide.FIELD_TEXT.name){Value = guide.text},
            });
        }

        private void treeGuides_CellEditFinishing(object sender, BrightIdeasSoftware.CellEditEventArgs e)
        {
            if (mNewGuide != null)
            {//新增的Guide编辑完成
                if (e.Cancel)
                {//放弃新增
                    CancelNewGuide(mNewGuide);
                }
                else
                {//确认新增
                    ConfirmNewGuide(mNewGuide, e.NewValue as string);
                }
            }
            else
            {//编辑了原有的Guide
                if (e.Cancel == false)
                {//确认编辑生效
                    ConfirmEditGuide(e.RowObject as Guide, e);
                }//else 放弃编辑则没有任何影响
            }
        }

        private void treeGuides_ModelCanDrop(object sender, ModelDropEventArgs e)
        {            
            if (e.TargetModel != null)
            {                
                Guide target = e.TargetModel as Guide;
                foreach (Guide src in e.SourceModels)
                {
                    if( src.parent == null ||
                        target.id.Equals(src.id) ||
                        (src.parent != null && target.id.Equals(src.parent))||
                        (target.id_dir != null && target.id_dir.Contains(src.id))
                      )
                    {//不允许拖动根节点；不允许拖动到自身、自身的直接parent、自身的子孙
                        e.Effect = DragDropEffects.None;
                        return;
                    }
                }                
            }
            e.Effect = DragDropEffects.Move;
        }

        private void treeGuides_ModelDropped(object sender, ModelDropEventArgs e)
        {
            if (e.TargetModel == null) return;
            switch (e.DropTargetLocation)
            {
                case DropTargetLocation.Item:
                    
                    break;
                case DropTargetLocation.AboveItem:
                    break;
                case DropTargetLocation.BelowItem:
                    break;
            }
            e.RefreshObjects();
        }

        private void menuInsertGuide_Click(object sender, EventArgs e)
        {

        }
    }

    class NewGuideWrapper : Guide
    {
        internal Guide parentObject;
        internal Guide siblingObject;
        internal bool isBeforeSibling;
        internal int index;
        internal NewGuideWrapper()
            : base("")
        {

        }
    }
}
