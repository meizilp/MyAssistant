﻿namespace MyAssistant.form
{
    partial class FormGuide
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.treeGuides = new BrightIdeasSoftware.TreeListView();
            this.olvClmText = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.cxtMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuNewGuide = new System.Windows.Forms.ToolStripMenuItem();
            this.menuNewChildGuide = new System.Windows.Forms.ToolStripMenuItem();
            this.menuDelete = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.treeGuides)).BeginInit();
            this.cxtMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // treeGuides
            // 
            this.treeGuides.AllColumns.Add(this.olvClmText);
            this.treeGuides.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.olvClmText});
            this.treeGuides.ContextMenuStrip = this.cxtMenu;
            this.treeGuides.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeGuides.FullRowSelect = true;
            this.treeGuides.HideSelection = false;
            this.treeGuides.Location = new System.Drawing.Point(0, 0);
            this.treeGuides.Name = "treeGuides";
            this.treeGuides.OwnerDraw = true;
            this.treeGuides.ShowGroups = false;
            this.treeGuides.Size = new System.Drawing.Size(683, 458);
            this.treeGuides.TabIndex = 0;
            this.treeGuides.UseCompatibleStateImageBehavior = false;
            this.treeGuides.View = System.Windows.Forms.View.Details;
            this.treeGuides.VirtualMode = true;
            // 
            // olvClmText
            // 
            this.olvClmText.AspectName = "text";
            this.olvClmText.Sortable = false;
            this.olvClmText.Text = "文本";
            this.olvClmText.Width = 667;
            // 
            // cxtMenu
            // 
            this.cxtMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuNewGuide,
            this.menuNewChildGuide,
            this.menuDelete});
            this.cxtMenu.Name = "cxtMenu";
            this.cxtMenu.Size = new System.Drawing.Size(153, 92);
            // 
            // menuNewGuide
            // 
            this.menuNewGuide.Name = "menuNewGuide";
            this.menuNewGuide.Size = new System.Drawing.Size(152, 22);
            this.menuNewGuide.Text = "新建";
            // 
            // menuNewChildGuide
            // 
            this.menuNewChildGuide.Name = "menuNewChildGuide";
            this.menuNewChildGuide.Size = new System.Drawing.Size(152, 22);
            this.menuNewChildGuide.Text = "新建子项";
            // 
            // menuDelete
            // 
            this.menuDelete.Name = "menuDelete";
            this.menuDelete.Size = new System.Drawing.Size(152, 22);
            this.menuDelete.Text = "删除";
            // 
            // FormGuide
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(683, 458);
            this.Controls.Add(this.treeGuides);
            this.Name = "FormGuide";
            this.Text = "FormGuide";
            this.Load += new System.EventHandler(this.FormGuide_Load);
            ((System.ComponentModel.ISupportInitialize)(this.treeGuides)).EndInit();
            this.cxtMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private BrightIdeasSoftware.TreeListView treeGuides;
        private BrightIdeasSoftware.OLVColumn olvClmText;
        private System.Windows.Forms.ContextMenuStrip cxtMenu;
        private System.Windows.Forms.ToolStripMenuItem menuNewGuide;
        private System.Windows.Forms.ToolStripMenuItem menuNewChildGuide;
        private System.Windows.Forms.ToolStripMenuItem menuDelete;
    }
}