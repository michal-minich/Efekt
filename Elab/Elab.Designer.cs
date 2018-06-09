namespace Elab
{
    partial class MainForm
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.MainTree = new System.Windows.Forms.TreeView();
            this.MainTabs = new System.Windows.Forms.TabControl();
            this.PropertiesTab = new System.Windows.Forms.TabPage();
            this.CodeTab = new System.Windows.Forms.TabPage();
            this.RunTab = new System.Windows.Forms.TabPage();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.MainTabs.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.BackColor = System.Drawing.Color.LightGray;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.MainTree);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.MainTabs);
            this.splitContainer1.Size = new System.Drawing.Size(882, 694);
            this.splitContainer1.SplitterDistance = 400;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 1;
            // 
            // MainTree
            // 
            this.MainTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MainTree.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.MainTree.Location = new System.Drawing.Point(0, 0);
            this.MainTree.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MainTree.Name = "MainTree";
            this.MainTree.Size = new System.Drawing.Size(397, 694);
            this.MainTree.TabIndex = 1;
            // 
            // MainTabs
            // 
            this.MainTabs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MainTabs.Controls.Add(this.PropertiesTab);
            this.MainTabs.Controls.Add(this.CodeTab);
            this.MainTabs.Controls.Add(this.RunTab);
            this.MainTabs.Location = new System.Drawing.Point(0, 0);
            this.MainTabs.Margin = new System.Windows.Forms.Padding(0);
            this.MainTabs.Name = "MainTabs";
            this.MainTabs.SelectedIndex = 0;
            this.MainTabs.Size = new System.Drawing.Size(477, 694);
            this.MainTabs.TabIndex = 0;
            // 
            // PropertiesTab
            // 
            this.PropertiesTab.Location = new System.Drawing.Point(4, 29);
            this.PropertiesTab.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.PropertiesTab.Name = "PropertiesTab";
            this.PropertiesTab.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.PropertiesTab.Size = new System.Drawing.Size(469, 661);
            this.PropertiesTab.TabIndex = 0;
            this.PropertiesTab.Text = "Properties";
            this.PropertiesTab.UseVisualStyleBackColor = true;
            // 
            // CodeTab
            // 
            this.CodeTab.Location = new System.Drawing.Point(4, 29);
            this.CodeTab.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.CodeTab.Name = "CodeTab";
            this.CodeTab.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.CodeTab.Size = new System.Drawing.Size(469, 661);
            this.CodeTab.TabIndex = 1;
            this.CodeTab.Text = "Code";
            this.CodeTab.UseVisualStyleBackColor = true;
            // 
            // RunTab
            // 
            this.RunTab.Location = new System.Drawing.Point(4, 29);
            this.RunTab.Name = "RunTab";
            this.RunTab.Size = new System.Drawing.Size(469, 661);
            this.RunTab.TabIndex = 2;
            this.RunTab.Text = "Run";
            this.RunTab.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(882, 694);
            this.Controls.Add(this.splitContainer1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.Black;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "MainForm";
            this.Text = "Elab";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.MainTabs.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView MainTree;
        private System.Windows.Forms.TabControl MainTabs;
        private System.Windows.Forms.TabPage PropertiesTab;
        private System.Windows.Forms.TabPage CodeTab;
        private System.Windows.Forms.TabPage RunTab;
    }
}

