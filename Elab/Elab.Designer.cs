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
            this.TypeNameLabel = new System.Windows.Forms.TextBox();
            this.ExpressionNameLabel = new System.Windows.Forms.Label();
            this.ExpressionPicture = new System.Windows.Forms.PictureBox();
            this.label2 = new System.Windows.Forms.Label();
            this.TypeLabel = new System.Windows.Forms.Label();
            this.TypePicture = new System.Windows.Forms.PictureBox();
            this.CodeTab = new System.Windows.Forms.TabPage();
            this.CodeTextBox = new System.Windows.Forms.TextBox();
            this.RunTab = new System.Windows.Forms.TabPage();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.MainTabs.SuspendLayout();
            this.PropertiesTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ExpressionPicture)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TypePicture)).BeginInit();
            this.CodeTab.SuspendLayout();
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
            this.splitContainer1.Size = new System.Drawing.Size(1182, 905);
            this.splitContainer1.SplitterDistance = 576;
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
            this.MainTree.Size = new System.Drawing.Size(573, 905);
            this.MainTree.TabIndex = 1;
            this.MainTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.MainTree_AfterSelect);
            // 
            // MainTabs
            // 
            this.MainTabs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MainTabs.Controls.Add(this.PropertiesTab);
            this.MainTabs.Controls.Add(this.CodeTab);
            this.MainTabs.Controls.Add(this.RunTab);
            this.MainTabs.Location = new System.Drawing.Point(0, -3);
            this.MainTabs.Margin = new System.Windows.Forms.Padding(0);
            this.MainTabs.Name = "MainTabs";
            this.MainTabs.Padding = new System.Drawing.Point(8, 5);
            this.MainTabs.SelectedIndex = 0;
            this.MainTabs.Size = new System.Drawing.Size(601, 910);
            this.MainTabs.TabIndex = 0;
            // 
            // PropertiesTab
            // 
            this.PropertiesTab.Controls.Add(this.TypeNameLabel);
            this.PropertiesTab.Controls.Add(this.ExpressionNameLabel);
            this.PropertiesTab.Controls.Add(this.ExpressionPicture);
            this.PropertiesTab.Controls.Add(this.label2);
            this.PropertiesTab.Controls.Add(this.TypeLabel);
            this.PropertiesTab.Controls.Add(this.TypePicture);
            this.PropertiesTab.Location = new System.Drawing.Point(4, 33);
            this.PropertiesTab.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.PropertiesTab.Name = "PropertiesTab";
            this.PropertiesTab.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.PropertiesTab.Size = new System.Drawing.Size(593, 873);
            this.PropertiesTab.TabIndex = 0;
            this.PropertiesTab.Text = "Properties";
            this.PropertiesTab.UseVisualStyleBackColor = true;
            // 
            // TypeNameLabel
            // 
            this.TypeNameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TypeNameLabel.BackColor = System.Drawing.Color.White;
            this.TypeNameLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TypeNameLabel.Location = new System.Drawing.Point(125, 42);
            this.TypeNameLabel.Margin = new System.Windows.Forms.Padding(0);
            this.TypeNameLabel.Multiline = true;
            this.TypeNameLabel.Name = "TypeNameLabel";
            this.TypeNameLabel.ReadOnly = true;
            this.TypeNameLabel.Size = new System.Drawing.Size(460, 113);
            this.TypeNameLabel.TabIndex = 6;
            this.TypeNameLabel.Text = "TypeNameLabel";
            // 
            // ExpressionNameLabel
            // 
            this.ExpressionNameLabel.AutoSize = true;
            this.ExpressionNameLabel.Location = new System.Drawing.Point(125, 9);
            this.ExpressionNameLabel.Margin = new System.Windows.Forms.Padding(0);
            this.ExpressionNameLabel.Name = "ExpressionNameLabel";
            this.ExpressionNameLabel.Size = new System.Drawing.Size(155, 20);
            this.ExpressionNameLabel.TabIndex = 5;
            this.ExpressionNameLabel.Text = "ExpressionNameLabel";
            // 
            // ExpressionPicture
            // 
            this.ExpressionPicture.Location = new System.Drawing.Point(108, 12);
            this.ExpressionPicture.Margin = new System.Windows.Forms.Padding(0);
            this.ExpressionPicture.Name = "ExpressionPicture";
            this.ExpressionPicture.Size = new System.Drawing.Size(16, 16);
            this.ExpressionPicture.TabIndex = 4;
            this.ExpressionPicture.TabStop = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.ForeColor = System.Drawing.Color.Gray;
            this.label2.Location = new System.Drawing.Point(6, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(79, 20);
            this.label2.TabIndex = 3;
            this.label2.Text = "Expression";
            // 
            // TypeLabel
            // 
            this.TypeLabel.AutoSize = true;
            this.TypeLabel.ForeColor = System.Drawing.Color.Gray;
            this.TypeLabel.Location = new System.Drawing.Point(6, 39);
            this.TypeLabel.Name = "TypeLabel";
            this.TypeLabel.Size = new System.Drawing.Size(41, 20);
            this.TypeLabel.TabIndex = 1;
            this.TypeLabel.Text = "Type";
            // 
            // TypePicture
            // 
            this.TypePicture.Location = new System.Drawing.Point(108, 43);
            this.TypePicture.Margin = new System.Windows.Forms.Padding(0);
            this.TypePicture.Name = "TypePicture";
            this.TypePicture.Size = new System.Drawing.Size(16, 16);
            this.TypePicture.TabIndex = 0;
            this.TypePicture.TabStop = false;
            // 
            // CodeTab
            // 
            this.CodeTab.Controls.Add(this.CodeTextBox);
            this.CodeTab.Location = new System.Drawing.Point(4, 33);
            this.CodeTab.Margin = new System.Windows.Forms.Padding(0);
            this.CodeTab.Name = "CodeTab";
            this.CodeTab.Size = new System.Drawing.Size(593, 873);
            this.CodeTab.TabIndex = 1;
            this.CodeTab.Text = "Code";
            this.CodeTab.UseVisualStyleBackColor = true;
            // 
            // CodeTextBox
            // 
            this.CodeTextBox.AcceptsReturn = true;
            this.CodeTextBox.AcceptsTab = true;
            this.CodeTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CodeTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.CodeTextBox.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CodeTextBox.Location = new System.Drawing.Point(0, 0);
            this.CodeTextBox.Margin = new System.Windows.Forms.Padding(0);
            this.CodeTextBox.Multiline = true;
            this.CodeTextBox.Name = "CodeTextBox";
            this.CodeTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.CodeTextBox.Size = new System.Drawing.Size(594, 868);
            this.CodeTextBox.TabIndex = 0;
            this.CodeTextBox.WordWrap = false;
            // 
            // RunTab
            // 
            this.RunTab.Location = new System.Drawing.Point(4, 33);
            this.RunTab.Name = "RunTab";
            this.RunTab.Size = new System.Drawing.Size(593, 873);
            this.RunTab.TabIndex = 2;
            this.RunTab.Text = "Run";
            this.RunTab.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1182, 905);
            this.Controls.Add(this.splitContainer1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.Black;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Elab";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.MainTabs.ResumeLayout(false);
            this.PropertiesTab.ResumeLayout(false);
            this.PropertiesTab.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ExpressionPicture)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TypePicture)).EndInit();
            this.CodeTab.ResumeLayout(false);
            this.CodeTab.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView MainTree;
        private System.Windows.Forms.TabControl MainTabs;
        private System.Windows.Forms.TabPage PropertiesTab;
        private System.Windows.Forms.TabPage CodeTab;
        private System.Windows.Forms.TabPage RunTab;
        private System.Windows.Forms.TextBox CodeTextBox;
        private System.Windows.Forms.Label TypeLabel;
        private System.Windows.Forms.PictureBox TypePicture;
        private System.Windows.Forms.Label ExpressionNameLabel;
        private System.Windows.Forms.PictureBox ExpressionPicture;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox TypeNameLabel;
    }
}

