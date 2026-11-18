namespace UI
{
    partial class WsModuelDef
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle17 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle18 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle22 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle23 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle24 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle19 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle20 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle21 = new System.Windows.Forms.DataGridViewCellStyle();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.dgv = new System.Windows.Forms.DataGridView();
            this.Num = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.X1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Y1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Z1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.X2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Y2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Z2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pc = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.textbox = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SN = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.enable = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.btn_get_pos = new System.Windows.Forms.DataGridViewButtonColumn();
            this.btn_goto_pos = new System.Windows.Forms.DataGridViewButtonColumn();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btn_next_flow = new System.Windows.Forms.Button();
            this.btn_save = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.btn_PosCopy = new System.Windows.Forms.Button();
            this.btn_cali = new System.Windows.Forms.Button();
            this.btn_array = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.rbtn_campos = new System.Windows.Forms.RadioButton();
            this.rbtn_pickpos = new System.Windows.Forms.RadioButton();
            this.nud_dy = new System.Windows.Forms.NumericUpDown();
            this.nud_dx = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.rbtn_WsQrcodePos = new System.Windows.Forms.RadioButton();
            this.nud_jig_Y = new System.Windows.Forms.NumericUpDown();
            this.nud_jig_X = new System.Windows.Forms.NumericUpDown();
            this.btn_jig_lean = new System.Windows.Forms.Button();
            this.rabt_jigpos = new System.Windows.Forms.RadioButton();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.flowLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_dy)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_dx)).BeginInit();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_jig_Y)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_jig_X)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.dgv, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.panel2, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 65.65035F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 23.4104F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10.98266F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1008, 346);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // dgv
            // 
            this.dgv.AllowDrop = true;
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.AllowUserToResizeColumns = false;
            this.dgv.AllowUserToResizeRows = false;
            dataGridViewCellStyle17.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(225)))), ((int)(((byte)(232)))));
            dataGridViewCellStyle17.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(44)))), ((int)(((byte)(55)))));
            dataGridViewCellStyle17.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(144)))), ((int)(((byte)(217)))));
            dataGridViewCellStyle17.SelectionForeColor = System.Drawing.Color.White;
            this.dgv.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle17;
            this.dgv.BackgroundColor = System.Drawing.SystemColors.ButtonFace;
            dataGridViewCellStyle18.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle18.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            dataGridViewCellStyle18.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle18.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            dataGridViewCellStyle18.SelectionBackColor = System.Drawing.Color.Transparent;
            dataGridViewCellStyle18.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(144)))), ((int)(((byte)(217)))));
            dataGridViewCellStyle18.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgv.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle18;
            this.dgv.ColumnHeadersHeight = 32;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Num,
            this.X1,
            this.Y1,
            this.Z1,
            this.X2,
            this.Y2,
            this.Z2,
            this.pc,
            this.textbox,
            this.SN,
            this.enable,
            this.btn_get_pos,
            this.btn_goto_pos});
            dataGridViewCellStyle22.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle22.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            dataGridViewCellStyle22.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle22.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            dataGridViewCellStyle22.SelectionBackColor = System.Drawing.Color.Transparent;
            dataGridViewCellStyle22.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(144)))), ((int)(((byte)(217)))));
            dataGridViewCellStyle22.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgv.DefaultCellStyle = dataGridViewCellStyle22;
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.EnableHeadersVisualStyles = false;
            this.dgv.GridColor = System.Drawing.Color.Gray;
            this.dgv.Location = new System.Drawing.Point(3, 4);
            this.dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgv.MultiSelect = false;
            this.dgv.Name = "dgv";
            this.dgv.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle23.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle23.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            dataGridViewCellStyle23.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle23.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            dataGridViewCellStyle23.SelectionBackColor = System.Drawing.Color.Transparent;
            dataGridViewCellStyle23.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(144)))), ((int)(((byte)(217)))));
            dataGridViewCellStyle23.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgv.RowHeadersDefaultCellStyle = dataGridViewCellStyle23;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dataGridViewCellStyle24.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            dataGridViewCellStyle24.Font = new System.Drawing.Font("宋体", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle24.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            dataGridViewCellStyle24.SelectionBackColor = System.Drawing.Color.Transparent;
            dataGridViewCellStyle24.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(144)))), ((int)(((byte)(217)))));
            this.dgv.RowsDefaultCellStyle = dataGridViewCellStyle24;
            this.dgv.RowTemplate.DefaultCellStyle.BackColor = System.Drawing.SystemColors.Control;
            this.dgv.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("宋体", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgv.RowTemplate.DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.dgv.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(144)))), ((int)(((byte)(217)))));
            this.dgv.RowTemplate.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;
            this.dgv.RowTemplate.Height = 32;
            this.dgv.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.dgv.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.dgv.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv.ShowEditingIcon = false;
            this.dgv.Size = new System.Drawing.Size(1002, 219);
            this.dgv.TabIndex = 3;
            this.dgv.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_CellContentClick);
            this.dgv.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_CellEndEdit);
            this.dgv.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.dgv_CellPainting);
            this.dgv.ColumnHeaderMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgv_ColumnHeaderMouseDoubleClick);
            // 
            // Num
            // 
            this.Num.HeaderText = "编号";
            this.Num.MinimumWidth = 60;
            this.Num.Name = "Num";
            this.Num.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.Num.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.Num.Width = 60;
            // 
            // X1
            // 
            this.X1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewCellStyle19.SelectionBackColor = System.Drawing.Color.Transparent;
            dataGridViewCellStyle19.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(144)))), ((int)(((byte)(217)))));
            this.X1.DefaultCellStyle = dataGridViewCellStyle19;
            this.X1.HeaderText = "X1";
            this.X1.MinimumWidth = 80;
            this.X1.Name = "X1";
            this.X1.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.X1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Y1
            // 
            this.Y1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Y1.HeaderText = "Y1";
            this.Y1.MinimumWidth = 80;
            this.Y1.Name = "Y1";
            this.Y1.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.Y1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Z1
            // 
            this.Z1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Z1.HeaderText = "Z1";
            this.Z1.MinimumWidth = 80;
            this.Z1.Name = "Z1";
            this.Z1.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.Z1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // X2
            // 
            this.X2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.X2.HeaderText = "X2";
            this.X2.MinimumWidth = 80;
            this.X2.Name = "X2";
            this.X2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Y2
            // 
            this.Y2.HeaderText = "Y2";
            this.Y2.Name = "Y2";
            this.Y2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Z2
            // 
            this.Z2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Z2.HeaderText = "Z2";
            this.Z2.MinimumWidth = 80;
            this.Z2.Name = "Z2";
            this.Z2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // pc
            // 
            this.pc.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.pc.FillWeight = 20F;
            this.pc.HeaderText = "电脑";
            this.pc.MinimumWidth = 60;
            this.pc.Name = "pc";
            this.pc.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // textbox
            // 
            this.textbox.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.textbox.FillWeight = 20F;
            this.textbox.HeaderText = "工装";
            this.textbox.MinimumWidth = 60;
            this.textbox.Name = "textbox";
            this.textbox.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // SN
            // 
            this.SN.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.SN.FillWeight = 20F;
            this.SN.HeaderText = "序号";
            this.SN.MinimumWidth = 60;
            this.SN.Name = "SN";
            this.SN.ReadOnly = true;
            this.SN.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // enable
            // 
            this.enable.HeaderText = "使用";
            this.enable.MinimumWidth = 60;
            this.enable.Name = "enable";
            this.enable.Width = 60;
            // 
            // btn_get_pos
            // 
            this.btn_get_pos.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewCellStyle20.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle20.BackColor = System.Drawing.Color.Transparent;
            dataGridViewCellStyle20.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            dataGridViewCellStyle20.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            dataGridViewCellStyle20.SelectionBackColor = System.Drawing.Color.Transparent;
            dataGridViewCellStyle20.SelectionForeColor = System.Drawing.Color.Transparent;
            this.btn_get_pos.DefaultCellStyle = dataGridViewCellStyle20;
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
            dataGridViewCellStyle21.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle21.BackColor = System.Drawing.Color.Transparent;
            dataGridViewCellStyle21.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            dataGridViewCellStyle21.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            dataGridViewCellStyle21.SelectionBackColor = System.Drawing.Color.Transparent;
            dataGridViewCellStyle21.SelectionForeColor = System.Drawing.Color.Transparent;
            this.btn_goto_pos.DefaultCellStyle = dataGridViewCellStyle21;
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
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.panel1);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 230);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(1002, 74);
            this.flowLayoutPanel1.TabIndex = 5;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.rbtn_WsQrcodePos);
            this.panel1.Controls.Add(this.nud_jig_Y);
            this.panel1.Controls.Add(this.nud_jig_X);
            this.panel1.Controls.Add(this.btn_jig_lean);
            this.panel1.Controls.Add(this.rabt_jigpos);
            this.panel1.Controls.Add(this.btn_next_flow);
            this.panel1.Controls.Add(this.btn_save);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.btn_PosCopy);
            this.panel1.Controls.Add(this.btn_cali);
            this.panel1.Controls.Add(this.btn_array);
            this.panel1.Controls.Add(this.button3);
            this.panel1.Controls.Add(this.rbtn_campos);
            this.panel1.Controls.Add(this.rbtn_pickpos);
            this.panel1.Controls.Add(this.nud_dy);
            this.panel1.Controls.Add(this.nud_dx);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(0, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(999, 71);
            this.panel1.TabIndex = 7;
            // 
            // btn_next_flow
            // 
            this.btn_next_flow.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btn_next_flow.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_next_flow.Location = new System.Drawing.Point(839, 9);
            this.btn_next_flow.Name = "btn_next_flow";
            this.btn_next_flow.Size = new System.Drawing.Size(71, 56);
            this.btn_next_flow.TabIndex = 4;
            this.btn_next_flow.Text = "吸嘴校正";
            this.btn_next_flow.UseVisualStyleBackColor = false;
            this.btn_next_flow.Click += new System.EventHandler(this.btn_next_flow_Click_1);
            // 
            // btn_save
            // 
            this.btn_save.BackColor = System.Drawing.Color.Chartreuse;
            this.btn_save.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_save.Location = new System.Drawing.Point(914, 9);
            this.btn_save.Name = "btn_save";
            this.btn_save.Size = new System.Drawing.Size(82, 56);
            this.btn_save.TabIndex = 1;
            this.btn_save.Text = "保存";
            this.btn_save.UseVisualStyleBackColor = false;
            this.btn_save.Click += new System.EventHandler(this.btn_save_Click);
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.button1.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button1.Location = new System.Drawing.Point(689, 9);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(71, 56);
            this.button1.TabIndex = 131;
            this.button1.Text = "下一位置";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click_1);
            // 
            // btn_PosCopy
            // 
            this.btn_PosCopy.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btn_PosCopy.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_PosCopy.Location = new System.Drawing.Point(539, 9);
            this.btn_PosCopy.Name = "btn_PosCopy";
            this.btn_PosCopy.Size = new System.Drawing.Size(71, 56);
            this.btn_PosCopy.TabIndex = 129;
            this.btn_PosCopy.Text = "位置复制";
            this.btn_PosCopy.UseVisualStyleBackColor = false;
            this.btn_PosCopy.Click += new System.EventHandler(this.btn_PosCopy_Click_1);
            // 
            // btn_cali
            // 
            this.btn_cali.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btn_cali.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_cali.Location = new System.Drawing.Point(764, 9);
            this.btn_cali.Name = "btn_cali";
            this.btn_cali.Size = new System.Drawing.Size(71, 56);
            this.btn_cali.TabIndex = 3;
            this.btn_cali.Text = "视觉校正";
            this.btn_cali.UseVisualStyleBackColor = false;
            this.btn_cali.Click += new System.EventHandler(this.btn_cali_Click);
            // 
            // btn_array
            // 
            this.btn_array.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btn_array.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_array.Location = new System.Drawing.Point(464, 9);
            this.btn_array.Name = "btn_array";
            this.btn_array.Size = new System.Drawing.Size(71, 56);
            this.btn_array.TabIndex = 130;
            this.btn_array.Text = "当前阵列";
            this.btn_array.UseVisualStyleBackColor = false;
            this.btn_array.Click += new System.EventHandler(this.btn_array_Click);
            // 
            // button3
            // 
            this.button3.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.button3.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button3.Location = new System.Drawing.Point(614, 9);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(71, 56);
            this.button3.TabIndex = 128;
            this.button3.Text = "启动测试";
            this.button3.UseVisualStyleBackColor = false;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // rbtn_campos
            // 
            this.rbtn_campos.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbtn_campos.Checked = true;
            this.rbtn_campos.FlatAppearance.CheckedBackColor = System.Drawing.Color.Yellow;
            this.rbtn_campos.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbtn_campos.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.rbtn_campos.ForeColor = System.Drawing.Color.Black;
            this.rbtn_campos.Location = new System.Drawing.Point(111, 9);
            this.rbtn_campos.Name = "rbtn_campos";
            this.rbtn_campos.Size = new System.Drawing.Size(48, 57);
            this.rbtn_campos.TabIndex = 125;
            this.rbtn_campos.TabStop = true;
            this.rbtn_campos.Text = "拍照位";
            this.rbtn_campos.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbtn_campos.UseVisualStyleBackColor = true;
            this.rbtn_campos.CheckedChanged += new System.EventHandler(this.rbtn_CheckedChanged);
            // 
            // rbtn_pickpos
            // 
            this.rbtn_pickpos.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbtn_pickpos.BackColor = System.Drawing.SystemColors.Control;
            this.rbtn_pickpos.FlatAppearance.CheckedBackColor = System.Drawing.Color.Yellow;
            this.rbtn_pickpos.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbtn_pickpos.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.rbtn_pickpos.ForeColor = System.Drawing.Color.Black;
            this.rbtn_pickpos.Location = new System.Drawing.Point(165, 9);
            this.rbtn_pickpos.Name = "rbtn_pickpos";
            this.rbtn_pickpos.Size = new System.Drawing.Size(48, 57);
            this.rbtn_pickpos.TabIndex = 124;
            this.rbtn_pickpos.Text = "取料位";
            this.rbtn_pickpos.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbtn_pickpos.UseVisualStyleBackColor = false;
            this.rbtn_pickpos.CheckedChanged += new System.EventHandler(this.rbtn_CheckedChanged);
            // 
            // nud_dy
            // 
            this.nud_dy.DecimalPlaces = 1;
            this.nud_dy.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.nud_dy.Location = new System.Drawing.Point(42, 41);
            this.nud_dy.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            -2147483648});
            this.nud_dy.Name = "nud_dy";
            this.nud_dy.Size = new System.Drawing.Size(63, 26);
            this.nud_dy.TabIndex = 2;
            this.nud_dy.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // nud_dx
            // 
            this.nud_dx.DecimalPlaces = 1;
            this.nud_dx.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.nud_dx.Location = new System.Drawing.Point(42, 8);
            this.nud_dx.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            -2147483648});
            this.nud_dx.Name = "nud_dx";
            this.nud_dx.Size = new System.Drawing.Size(63, 26);
            this.nud_dx.TabIndex = 0;
            this.nud_dx.Value = new decimal(new int[] {
            32,
            0,
            0,
            -2147483648});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(4, 46);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(48, 16);
            this.label2.TabIndex = 3;
            this.label2.Text = "△Y：";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(48, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "△X：";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.label3);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(3, 310);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1002, 33);
            this.panel2.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.Yellow;
            this.label3.Font = new System.Drawing.Font("宋体", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(54, 8);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(404, 27);
            this.label3.TabIndex = 0;
            this.label3.Text = "注意：当前Z值位为工站放料高度";
            // 
            // rbtn_WsQrcodePos
            // 
            this.rbtn_WsQrcodePos.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbtn_WsQrcodePos.FlatAppearance.CheckedBackColor = System.Drawing.Color.Yellow;
            this.rbtn_WsQrcodePos.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbtn_WsQrcodePos.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rbtn_WsQrcodePos.ForeColor = System.Drawing.Color.Black;
            this.rbtn_WsQrcodePos.Location = new System.Drawing.Point(273, 9);
            this.rbtn_WsQrcodePos.Name = "rbtn_WsQrcodePos";
            this.rbtn_WsQrcodePos.Size = new System.Drawing.Size(48, 57);
            this.rbtn_WsQrcodePos.TabIndex = 136;
            this.rbtn_WsQrcodePos.Text = "扫码位";
            this.rbtn_WsQrcodePos.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbtn_WsQrcodePos.UseVisualStyleBackColor = true;
            this.rbtn_WsQrcodePos.CheckedChanged += new System.EventHandler(this.rbtn_CheckedChanged);
            // 
            // nud_jig_Y
            // 
            this.nud_jig_Y.DecimalPlaces = 1;
            this.nud_jig_Y.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.nud_jig_Y.Location = new System.Drawing.Point(327, 39);
            this.nud_jig_Y.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            -2147483648});
            this.nud_jig_Y.Name = "nud_jig_Y";
            this.nud_jig_Y.Size = new System.Drawing.Size(64, 26);
            this.nud_jig_Y.TabIndex = 135;
            this.nud_jig_Y.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // nud_jig_X
            // 
            this.nud_jig_X.DecimalPlaces = 1;
            this.nud_jig_X.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.nud_jig_X.Location = new System.Drawing.Point(327, 9);
            this.nud_jig_X.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            -2147483648});
            this.nud_jig_X.Name = "nud_jig_X";
            this.nud_jig_X.Size = new System.Drawing.Size(64, 26);
            this.nud_jig_X.TabIndex = 134;
            this.nud_jig_X.Value = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            // 
            // btn_jig_lean
            // 
            this.btn_jig_lean.Location = new System.Drawing.Point(397, 9);
            this.btn_jig_lean.Name = "btn_jig_lean";
            this.btn_jig_lean.Size = new System.Drawing.Size(61, 56);
            this.btn_jig_lean.TabIndex = 133;
            this.btn_jig_lean.Text = "学习";
            this.btn_jig_lean.UseVisualStyleBackColor = true;
            this.btn_jig_lean.Click += new System.EventHandler(this.btn_jig_lean_Click);
            // 
            // rabt_jigpos
            // 
            this.rabt_jigpos.Appearance = System.Windows.Forms.Appearance.Button;
            this.rabt_jigpos.FlatAppearance.CheckedBackColor = System.Drawing.Color.Yellow;
            this.rabt_jigpos.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rabt_jigpos.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rabt_jigpos.ForeColor = System.Drawing.Color.Black;
            this.rabt_jigpos.Location = new System.Drawing.Point(219, 9);
            this.rabt_jigpos.Name = "rabt_jigpos";
            this.rabt_jigpos.Size = new System.Drawing.Size(48, 57);
            this.rabt_jigpos.TabIndex = 132;
            this.rabt_jigpos.Text = "夹具位";
            this.rabt_jigpos.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rabt_jigpos.UseVisualStyleBackColor = true;
            this.rabt_jigpos.CheckedChanged += new System.EventHandler(this.rbtn_CheckedChanged);
            // 
            // WsModuelDef
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.tableLayoutPanel1);
            this.DoubleBuffered = true;
            this.Name = "WsModuelDef";
            this.Size = new System.Drawing.Size(1008, 346);
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_dy)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_dx)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_jig_Y)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_jig_X)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button btn_cali;
        private System.Windows.Forms.Button btn_next_flow;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.NumericUpDown nud_dy;
        private System.Windows.Forms.NumericUpDown nud_dx;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btn_PosCopy;
        public System.Windows.Forms.Button btn_array;
        public System.Windows.Forms.RadioButton rbtn_campos;
        public System.Windows.Forms.RadioButton rbtn_pickpos;
        public System.Windows.Forms.Button btn_save;
        private System.Windows.Forms.DataGridViewTextBoxColumn Num;
        private System.Windows.Forms.DataGridViewTextBoxColumn X1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Y1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Z1;
        private System.Windows.Forms.DataGridViewTextBoxColumn X2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Y2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Z2;
        private System.Windows.Forms.DataGridViewTextBoxColumn pc;
        private System.Windows.Forms.DataGridViewTextBoxColumn textbox;
        private System.Windows.Forms.DataGridViewTextBoxColumn SN;
        private System.Windows.Forms.DataGridViewCheckBoxColumn enable;
        private System.Windows.Forms.DataGridViewButtonColumn btn_get_pos;
        private System.Windows.Forms.DataGridViewButtonColumn btn_goto_pos;
        public System.Windows.Forms.RadioButton rbtn_WsQrcodePos;
        private System.Windows.Forms.NumericUpDown nud_jig_Y;
        private System.Windows.Forms.NumericUpDown nud_jig_X;
        private System.Windows.Forms.Button btn_jig_lean;
        public System.Windows.Forms.RadioButton rabt_jigpos;
    }
}
