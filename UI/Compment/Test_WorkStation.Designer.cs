namespace UI.Compment
{
    partial class Test_WorkStation
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.wsdgv = new System.Windows.Forms.DataGridView();
            this.位置 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.X = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Y = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.U = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Z1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Z2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btn_get_pos = new System.Windows.Forms.DataGridViewButtonColumn();
            this.btn_goto_pos = new System.Windows.Forms.DataGridViewButtonColumn();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.light_btn_save = new System.Windows.Forms.Button();
            this.light_btn_load = new System.Windows.Forms.Button();
            this.light_btn_add = new System.Windows.Forms.Button();
            this.light_btn_del = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.wsdgv)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.wsdgv, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Font = new System.Drawing.Font("宋体", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 49.69325F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(977, 379);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // wsdgv
            // 
            this.wsdgv.AllowDrop = true;
            this.wsdgv.AllowUserToAddRows = false;
            this.wsdgv.AllowUserToDeleteRows = false;
            this.wsdgv.AllowUserToResizeColumns = false;
            this.wsdgv.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(225)))), ((int)(((byte)(232)))));
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(44)))), ((int)(((byte)(55)))));
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(144)))), ((int)(((byte)(217)))));
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.Color.White;
            this.wsdgv.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.wsdgv.BackgroundColor = System.Drawing.SystemColors.ButtonFace;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("宋体", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.Transparent;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(144)))), ((int)(((byte)(217)))));
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.wsdgv.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.wsdgv.ColumnHeadersHeight = 32;
            this.wsdgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.wsdgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.位置,
            this.X,
            this.Y,
            this.U,
            this.Z1,
            this.Z2,
            this.btn_get_pos,
            this.btn_goto_pos});
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            dataGridViewCellStyle6.Font = new System.Drawing.Font("宋体", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle6.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.Color.Transparent;
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(144)))), ((int)(((byte)(217)))));
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.wsdgv.DefaultCellStyle = dataGridViewCellStyle6;
            this.wsdgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.wsdgv.EnableHeadersVisualStyles = false;
            this.wsdgv.GridColor = System.Drawing.Color.Gray;
            this.wsdgv.Location = new System.Drawing.Point(3, 4);
            this.wsdgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.wsdgv.MultiSelect = false;
            this.wsdgv.Name = "wsdgv";
            this.wsdgv.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            dataGridViewCellStyle7.Font = new System.Drawing.Font("宋体", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle7.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.Color.Transparent;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(144)))), ((int)(((byte)(217)))));
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.wsdgv.RowHeadersDefaultCellStyle = dataGridViewCellStyle7;
            this.wsdgv.RowHeadersVisible = false;
            this.wsdgv.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dataGridViewCellStyle8.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            dataGridViewCellStyle8.Font = new System.Drawing.Font("宋体", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle8.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            dataGridViewCellStyle8.SelectionBackColor = System.Drawing.Color.Transparent;
            dataGridViewCellStyle8.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(144)))), ((int)(((byte)(217)))));
            this.wsdgv.RowsDefaultCellStyle = dataGridViewCellStyle8;
            this.wsdgv.RowTemplate.DefaultCellStyle.BackColor = System.Drawing.SystemColors.Control;
            this.wsdgv.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("宋体", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.wsdgv.RowTemplate.DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.wsdgv.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(144)))), ((int)(((byte)(217)))));
            this.wsdgv.RowTemplate.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;
            this.wsdgv.RowTemplate.Height = 32;
            this.wsdgv.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.wsdgv.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.wsdgv.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.wsdgv.ShowEditingIcon = false;
            this.wsdgv.Size = new System.Drawing.Size(971, 319);
            this.wsdgv.TabIndex = 4;
            this.wsdgv.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.wsdgv_CellClick);
            this.wsdgv.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.wsdgv_CellContentClick);
            // 
            // 位置
            // 
            this.位置.HeaderText = "位置";
            this.位置.MinimumWidth = 200;
            this.位置.Name = "位置";
            this.位置.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.位置.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.位置.Width = 200;
            // 
            // X
            // 
            this.X.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.Transparent;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(144)))), ((int)(((byte)(217)))));
            this.X.DefaultCellStyle = dataGridViewCellStyle3;
            this.X.HeaderText = "X";
            this.X.MinimumWidth = 80;
            this.X.Name = "X";
            this.X.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.X.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Y
            // 
            this.Y.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Y.HeaderText = "Y";
            this.Y.MinimumWidth = 80;
            this.Y.Name = "Y";
            this.Y.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.Y.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // U
            // 
            this.U.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.U.HeaderText = "U";
            this.U.MinimumWidth = 80;
            this.U.Name = "U";
            this.U.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.U.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Z1
            // 
            this.Z1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Z1.HeaderText = "Z1";
            this.Z1.MinimumWidth = 80;
            this.Z1.Name = "Z1";
            this.Z1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Z2
            // 
            this.Z2.HeaderText = "Z2";
            this.Z2.Name = "Z2";
            this.Z2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // btn_get_pos
            // 
            this.btn_get_pos.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.Transparent;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            dataGridViewCellStyle4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.Transparent;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.Color.Transparent;
            this.btn_get_pos.DefaultCellStyle = dataGridViewCellStyle4;
            this.btn_get_pos.FillWeight = 50F;
            this.btn_get_pos.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_get_pos.HeaderText = "";
            this.btn_get_pos.MinimumWidth = 60;
            this.btn_get_pos.Name = "btn_get_pos";
            this.btn_get_pos.ReadOnly = true;
            this.btn_get_pos.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.btn_get_pos.Text = "学习";
            this.btn_get_pos.UseColumnTextForButtonValue = true;
            // 
            // btn_goto_pos
            // 
            this.btn_goto_pos.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.Transparent;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            dataGridViewCellStyle5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.Color.Transparent;
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.Color.Transparent;
            this.btn_goto_pos.DefaultCellStyle = dataGridViewCellStyle5;
            this.btn_goto_pos.FillWeight = 50F;
            this.btn_goto_pos.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_goto_pos.HeaderText = "";
            this.btn_goto_pos.MinimumWidth = 60;
            this.btn_goto_pos.Name = "btn_goto_pos";
            this.btn_goto_pos.ReadOnly = true;
            this.btn_goto_pos.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.btn_goto_pos.Text = "定位";
            this.btn_goto_pos.UseColumnTextForButtonValue = true;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 4;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.Controls.Add(this.light_btn_save, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.light_btn_load, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.light_btn_add, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.light_btn_del, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 330);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(971, 46);
            this.tableLayoutPanel2.TabIndex = 5;
            // 
            // light_btn_save
            // 
            this.light_btn_save.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.light_btn_save.Dock = System.Windows.Forms.DockStyle.Fill;
            this.light_btn_save.Location = new System.Drawing.Point(487, 3);
            this.light_btn_save.Name = "light_btn_save";
            this.light_btn_save.Size = new System.Drawing.Size(236, 40);
            this.light_btn_save.TabIndex = 5;
            this.light_btn_save.Text = "保存";
            this.light_btn_save.UseVisualStyleBackColor = false;
            this.light_btn_save.Click += new System.EventHandler(this.light_btn_save_Click);
            // 
            // light_btn_load
            // 
            this.light_btn_load.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.light_btn_load.Dock = System.Windows.Forms.DockStyle.Fill;
            this.light_btn_load.Location = new System.Drawing.Point(729, 3);
            this.light_btn_load.Name = "light_btn_load";
            this.light_btn_load.Size = new System.Drawing.Size(239, 40);
            this.light_btn_load.TabIndex = 6;
            this.light_btn_load.Text = "加载";
            this.light_btn_load.UseVisualStyleBackColor = false;
            this.light_btn_load.Click += new System.EventHandler(this.light_btn_load_Click);
            // 
            // light_btn_add
            // 
            this.light_btn_add.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.light_btn_add.Dock = System.Windows.Forms.DockStyle.Fill;
            this.light_btn_add.Location = new System.Drawing.Point(3, 3);
            this.light_btn_add.Name = "light_btn_add";
            this.light_btn_add.Size = new System.Drawing.Size(236, 40);
            this.light_btn_add.TabIndex = 7;
            this.light_btn_add.Text = "增加";
            this.light_btn_add.UseVisualStyleBackColor = false;
            this.light_btn_add.Click += new System.EventHandler(this.light_btn_add_Click);
            // 
            // light_btn_del
            // 
            this.light_btn_del.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.light_btn_del.Dock = System.Windows.Forms.DockStyle.Fill;
            this.light_btn_del.Location = new System.Drawing.Point(245, 3);
            this.light_btn_del.Name = "light_btn_del";
            this.light_btn_del.Size = new System.Drawing.Size(236, 40);
            this.light_btn_del.TabIndex = 8;
            this.light_btn_del.Text = "删除";
            this.light_btn_del.UseVisualStyleBackColor = false;
            this.light_btn_del.Click += new System.EventHandler(this.light_btn_del_Click);
            // 
            // Test_WorkStation
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Test_WorkStation";
            this.Size = new System.Drawing.Size(977, 379);
            this.Load += new System.EventHandler(this.Test_WorkStation_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.wsdgv)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.DataGridView wsdgv;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        public System.Windows.Forms.Button light_btn_save;
        public System.Windows.Forms.Button light_btn_load;
        public System.Windows.Forms.Button light_btn_add;
        public System.Windows.Forms.Button light_btn_del;
        private System.Windows.Forms.DataGridViewTextBoxColumn 位置;
        private System.Windows.Forms.DataGridViewTextBoxColumn X;
        private System.Windows.Forms.DataGridViewTextBoxColumn Y;
        private System.Windows.Forms.DataGridViewTextBoxColumn U;
        private System.Windows.Forms.DataGridViewTextBoxColumn Z1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Z2;
        private System.Windows.Forms.DataGridViewButtonColumn btn_get_pos;
        private System.Windows.Forms.DataGridViewButtonColumn btn_goto_pos;
    }
}
