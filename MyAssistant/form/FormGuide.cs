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

        //刷新新增的节点，并开始编辑。
        private void BeginEditNewGuide()
        {
            if (treeGuides.IsExpanded(mNewGuide.parentObject)) treeGuides.RefreshObject(mNewGuide.parentObject);
            else treeGuides.Expand(mNewGuide.parentObject);
            //开始编辑新节点
            treeGuides.EditModel(mNewGuide);
        }

        private void AppendNewGuide()
        {
            mNewGuide = new NewGuideWrapper();
            Guide selectedGuide = treeGuides.SelectedObject as Guide;
            if (selectedGuide == null || selectedGuide.parent == null)
            {//没有选择任何节点或选择了根节点，在根节点的子节点列表最后添加。                
                mNewGuide.parentObject = mRoot;
            }
            else
            {//在选中节点的后面添加，父节点就是选中节点的父节点。
                mNewGuide.parentObject = treeGuides.GetParent(selectedGuide) as Guide;
                //如果选中节点不是最后一个（索引小于最后一个的索引），那么记录兄弟节点。否则就转化为在父节点的子节点列表尾部添加。
                List<Guide> children = mNewGuide.parentObject.GetChildGuides();
                if (children.IndexOf(mNewGuide.siblingObject) < children.Count - 1) mNewGuide.siblingObject = selectedGuide;
            }
            BeginEditNewGuide();
        }

        //追加新节点
        private void menuNewGuide_Click(object sender, EventArgs e)
        {
            AppendNewGuide();    
        }

        //插入新节点
        private void menuInsertGuide_Click(object sender, EventArgs e)
        {
            mNewGuide = new NewGuideWrapper();
            Guide selectedGuide = treeGuides.SelectedObject as Guide;
            if (selectedGuide == null || selectedGuide.parent == null)
            {//没有选择任何节点或选择了根节点，在根节点的子节点列表最后添加。                
                mNewGuide.parentObject = mRoot;
            }
            else
            {//在选中节点的前面添加，挤占被选中节点的位置
                mNewGuide.parentObject = treeGuides.GetParent(selectedGuide) as Guide;
                mNewGuide.siblingObject = selectedGuide;
                mNewGuide.isBeforeSibling = true;                
            }
            BeginEditNewGuide();
        }

        private void AppendNewChildGuide()
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
            BeginEditNewGuide();
        }

        //插入子节点
        private void menuNewChildGuide_Click(object sender, EventArgs e)
        {
            AppendNewChildGuide(); 
        }

        //删除选中节点
        private void menuDelete_Click(object sender, EventArgs e)
        {
            Guide target = treeGuides.SelectedObject as Guide;
            if (target == null) 
            {
                MessageBox.Show("请选择要删除的节点！");
                return;
            }
            else if (target.parent == null)
            {
                MessageBox.Show("根节点不能删除！");
                return;
            }
            Guide parent = treeGuides.GetParent(target) as Guide;
            parent.DeleteChild(target);
            treeGuides.RefreshObject(parent);
        }

        private NewGuideWrapper mNewGuide;
        //加载给定节点的子节点，如果正在编辑新节点，那么也加入到列表中。
        private List<Guide> LoadChildrenOfGuide(Guide guide)
        {
            List<Guide> results;
            if (guide.GetType() == typeof(NewGuideWrapper)) results = new List<Guide>();
            else results = guide.GetChildGuides();
            if (mNewGuide != null && mNewGuide.parentObject.GetHashCode() == guide.GetHashCode())
            {//正在新建节点，并且新建节点的parent在查询子节点，那么要把新建节点加入结果中，才能在tree中显示。                
                if (mNewGuide.siblingObject == null)
                {//尾部追加
                    results.Add(mNewGuide);
                }
                else
                {
                    if (mNewGuide.isBeforeSibling)
                    {//在某个节点之前插入
                        results.Insert(results.IndexOf(mNewGuide.siblingObject), mNewGuide);
                    }
                    else
                    {//在某个节点之后追加
                        results.Insert(results.IndexOf(mNewGuide.siblingObject)+1, mNewGuide);
                    }                    
                }                
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
            newGuide.type = mRoot.type;
            newGuide.text = newValue;

            if (tmpGuide.siblingObject == null)
            {//兄弟对象为空，在子对象列表尾部追加
                parent.AppendNewChild(newGuide);
            }
            else
            {
                parent.InsertNewChild(newGuide, tmpGuide.siblingObject, tmpGuide.isBeforeSibling);                
            }
            
            //刷新父对象
            treeGuides.RefreshObject(parent);
            treeGuides.SelectedObject = newGuide;
        }

        //确认修改Guide内容。
        private void ConfirmEditGuide(Guide guide, CellEditEventArgs e)
        {
            if (guide.parent == null)
            {
                e.Cancel = true;
                MessageBox.Show("根节点不能修改名称！");
                return;
            }
            guide.text = e.NewValue as string;
            guide.UpdateToDB(new SQLiteParameter(Guide.FIELD_TEXT.name){Value = guide.text});
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
            Guide target = e.TargetModel as Guide;
            switch (e.DropTargetLocation)
            {
                case DropTargetLocation.Item:                                        
                    foreach (Guide src in e.SourceModels)
                    {//每一个被拖动的项都成为item的子项
                        src.Move(treeGuides.GetParent(src) as Guide, target, null, false);
                    }
                    if (!treeGuides.IsExpanded(target)) treeGuides.Expand(target);
                    break;
                case DropTargetLocation.AboveItem:
                    Guide targetParent1 = treeGuides.GetParent(target) as Guide;                    
                    //拖动向导到新位置
                    foreach (Guide src in e.SourceModels)
                    {//每一个被拖动的项都成为item前面的项
                        src.Move(treeGuides.GetParent(src) as Guide, targetParent1, target, true);                        
                    }
                    break;
                case DropTargetLocation.BelowItem:
                    Guide targetParent2 = treeGuides.GetParent(target) as Guide;
                    Guide lastBeforeGuide2 = target;    //第一次是紧跟在target之后                             
                    foreach (Guide src in e.SourceModels)
                    {//每一个被拖动的项都成为item后面的项
                        src.Move(treeGuides.GetParent(src) as Guide, targetParent2, lastBeforeGuide2, false);
                        lastBeforeGuide2 = src;
                    }
                    break;
            }
            e.RefreshObjects();
        }


        
        private void treeGuides_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control)
            {
                if (e.KeyCode == Keys.N)
                {//在其后新增一项
                    AppendNewGuide();
                }
                else if (e.KeyCode == Keys.X)
                {//剪切
                    Guide x = treeGuides.SelectedObject as Guide;
                    if (x == null) return;
                    Clipboard.SetData(GuidesInClipboard.Format, new GuidesInClipboard(x.id));                    
                    Guide p = treeGuides.GetParent(x) as Guide;
                    p.DeleteChild(x);
                    treeGuides.RefreshObject(p);
                }
                else if (e.KeyCode == Keys.V)
                {//粘贴
                    if (Clipboard.ContainsData(GuidesInClipboard.Format))
                    {
                        GuidesInClipboard toPasteGuides = Clipboard.GetData(GuidesInClipboard.Format) as GuidesInClipboard;
                        Guide x = Guide.QueryById(toPasteGuides.toPasteId);                        
                        if (x != null)
                        {
                            Guide sibling = treeGuides.SelectedObject as Guide;
                            Guide parent;
                            if (sibling == null) parent = mRoot;
                            else parent = treeGuides.GetParent(sibling) as Guide;
                            parent.PasteChild(x, sibling);
                            treeGuides.RefreshObject(parent);
                            treeGuides.SelectedObject = x;
                        }                        
                    }
                }
                else if (e.KeyCode == Keys.Up)
                {//上移
                    Guide target = treeGuides.SelectedObject as Guide;
                    if (target == null) return;
                    target.MoveUp();
                    Guide parent = treeGuides.GetParent(target) as Guide;
                    treeGuides.SelectedObject = target;
                    treeGuides.RefreshObject(parent);
                }
                else if (e.KeyCode == Keys.Down)
                {//下移
                    Guide target = treeGuides.SelectedObject as Guide;
                    if (target == null) return;
                    target.MoveDown();
                    Guide parent = treeGuides.GetParent(target) as Guide;
                    treeGuides.SelectedObject = target;
                    treeGuides.RefreshObject(parent);
                }
            }
            else if (e.Modifiers == (Keys.Control | Keys.Shift))
            {
                if (e.KeyCode == Keys.N)
                {//在其子节点列表最后增加一项
                    AppendNewChildGuide();
                }
                else if (e.KeyCode == Keys.V)
                {//粘贴为子节点
                    if (Clipboard.ContainsData(GuidesInClipboard.Format))
                    {
                        GuidesInClipboard toPasteGuides = Clipboard.GetData(GuidesInClipboard.Format) as GuidesInClipboard;
                        Guide x = Guide.QueryById(toPasteGuides.toPasteId);
                        if (x != null)
                        {
                            Guide parent = treeGuides.SelectedObject as Guide;                                                        
                            parent.PasteChild(x, null);
                            if (treeGuides.IsExpanded(parent)) treeGuides.RefreshObject(parent);
                            else treeGuides.Expand(parent);
                            treeGuides.SelectedObject = x;
                        }
                    }
                }
            }       
        }
    }

    [Serializable]
    public class GuidesInClipboard
    {
        public const string Format = "MyAssistant-Guide";
        public GuidesInClipboard(string id) { _id = id; }
        private string _id;
        public string toPasteId { get { return _id; } set { _id = value; } }
    }

    class NewGuideWrapper : Guide
    {
        internal Guide parentObject;    //新节点的父节点，不能为null。
        internal Guide siblingObject;   //新节点的兄弟节点，为null表示在父节点尾部添加。
        internal bool isBeforeSibling;  //是否在兄弟节点的前面插入，只有siblingObject不为null时有意义。
        
        internal NewGuideWrapper()
            : base("")
        {

        }
    }
}
