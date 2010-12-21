namespace WindowsFormsApplication1
{
	partial class Form1
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
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.todoTab = new System.Windows.Forms.TabPage();
			this.somedayTab = new System.Windows.Forms.TabPage();
			this.todoList = new System.Windows.Forms.CheckedListBox();
			this.tabControl1.SuspendLayout();
			this.todoTab.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabControl1
			// 
			this.tabControl1.Controls.Add(this.todoTab);
			this.tabControl1.Controls.Add(this.somedayTab);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(284, 262);
			this.tabControl1.TabIndex = 0;
			// 
			// todoTab
			// 
			this.todoTab.Controls.Add(this.todoList);
			this.todoTab.Location = new System.Drawing.Point(4, 22);
			this.todoTab.Name = "todoTab";
			this.todoTab.Padding = new System.Windows.Forms.Padding(3);
			this.todoTab.Size = new System.Drawing.Size(276, 236);
			this.todoTab.TabIndex = 0;
			this.todoTab.Text = "ToDo";
			this.todoTab.UseVisualStyleBackColor = true;
			// 
			// somedayTab
			// 
			this.somedayTab.Location = new System.Drawing.Point(4, 22);
			this.somedayTab.Name = "somedayTab";
			this.somedayTab.Padding = new System.Windows.Forms.Padding(3);
			this.somedayTab.Size = new System.Drawing.Size(276, 236);
			this.somedayTab.TabIndex = 1;
			this.somedayTab.Text = "Someday";
			this.somedayTab.UseVisualStyleBackColor = true;
			// 
			// todoList
			// 
			this.todoList.Dock = System.Windows.Forms.DockStyle.Fill;
			this.todoList.FormattingEnabled = true;
			this.todoList.Location = new System.Drawing.Point(3, 3);
			this.todoList.Name = "todoList";
			this.todoList.Size = new System.Drawing.Size(270, 229);
			this.todoList.TabIndex = 0;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 262);
			this.Controls.Add(this.tabControl1);
			this.Name = "Form1";
			this.Text = "ActionList";
			this.tabControl1.ResumeLayout(false);
			this.todoTab.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage todoTab;
		private System.Windows.Forms.TabPage somedayTab;
		private System.Windows.Forms.CheckedListBox todoList;
	}
}

