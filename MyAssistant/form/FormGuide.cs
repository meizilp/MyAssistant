using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using MyAssistant.db;

namespace MyAssistant.form
{
    public partial class FormGuide : Form
    {
        public FormGuide()
        {
            InitializeComponent();
        }

        private void FormGuide_Load(object sender, EventArgs e)
        {
            Guide root = Guide.GetRootGuide(Guide.GUIDE_TYPE_COLLECT);
            List<Guide> roots = new List<Guide>();
            roots.Add(root);
            treeGuides.Roots = roots;
            treeGuides.ExpandAll();
        }
    }
}
