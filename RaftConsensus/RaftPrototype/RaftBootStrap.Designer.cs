namespace RaftPrototype
{
    partial class RaftBootStrap
    {
        //private System.Drawing.Font _font = new System.Drawing.Font("Microsoft Sans Serif", 10.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        
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
            this.label1 = new System.Windows.Forms.Label();
            this.nNodes = new System.Windows.Forms.NumericUpDown();
            this.button1 = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.tbClusterName = new System.Windows.Forms.TextBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lClusterPasswd = new System.Windows.Forms.Label();
            this.tbClusterPasswd = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tbIPAddress = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tbPort = new System.Windows.Forms.TextBox();
            this.lWarningNodesNumber = new System.Windows.Forms.Label();
            this.btnCreate = new System.Windows.Forms.Button();
            this.nodeConfigDataView = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.nNodes)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nodeConfigDataView)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 104);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(191, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "How many nodes do you want to start?";
            // 
            // nNodes
            // 
            this.nNodes.Increment = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.nNodes.Location = new System.Drawing.Point(245, 107);
            this.nNodes.Name = "nNodes";
            this.nNodes.Size = new System.Drawing.Size(120, 20);
            this.nNodes.TabIndex = 1;
            this.nNodes.ValueChanged += new System.EventHandler(this.nNodes_ValueChanged);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(456, 395);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "Exit";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(225, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "Please enter the cluster name you wish to join:";
            // 
            // tbClusterName
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.tbClusterName, 2);
            this.tbClusterName.Dock = System.Windows.Forms.DockStyle.Top;
            this.tbClusterName.Location = new System.Drawing.Point(245, 3);
            this.tbClusterName.Name = "tbClusterName";
            this.tbClusterName.Size = new System.Drawing.Size(271, 20);
            this.tbClusterName.TabIndex = 9;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 46.66667F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 26.66667F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 26.66667F));
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tbClusterName, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.lClusterPasswd, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.tbClusterPasswd, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.tbIPAddress, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.label4, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.tbPort, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.nNodes, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.lWarningNodesNumber, 2, 5);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(12, 12);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 6;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(519, 134);
            this.tableLayoutPanel1.TabIndex = 10;
            // 
            // lClusterPasswd
            // 
            this.lClusterPasswd.AutoSize = true;
            this.lClusterPasswd.Location = new System.Drawing.Point(3, 26);
            this.lClusterPasswd.Name = "lClusterPasswd";
            this.lClusterPasswd.Size = new System.Drawing.Size(148, 13);
            this.lClusterPasswd.TabIndex = 11;
            this.lClusterPasswd.Text = "Enter password to join cluster:";
            // 
            // tbClusterPasswd
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.tbClusterPasswd, 2);
            this.tbClusterPasswd.Dock = System.Windows.Forms.DockStyle.Top;
            this.tbClusterPasswd.Location = new System.Drawing.Point(245, 29);
            this.tbClusterPasswd.Name = "tbClusterPasswd";
            this.tbClusterPasswd.PasswordChar = '*';
            this.tbClusterPasswd.Size = new System.Drawing.Size(271, 20);
            this.tbClusterPasswd.TabIndex = 12;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 78);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(161, 13);
            this.label3.TabIndex = 13;
            this.label3.Text = "All Servers will run on IP Address";
            // 
            // tbIPAddress
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.tbIPAddress, 2);
            this.tbIPAddress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbIPAddress.Location = new System.Drawing.Point(245, 81);
            this.tbIPAddress.Name = "tbIPAddress";
            this.tbIPAddress.Size = new System.Drawing.Size(271, 20);
            this.tbIPAddress.TabIndex = 14;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 52);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(85, 13);
            this.label4.TabIndex = 16;
            this.label4.Text = "Default start port";
            // 
            // tbPort
            // 
            this.tbPort.Location = new System.Drawing.Point(245, 55);
            this.tbPort.Name = "tbPort";
            this.tbPort.Size = new System.Drawing.Size(100, 20);
            this.tbPort.TabIndex = 15;
            this.tbPort.TextChanged += new System.EventHandler(this.tbPort_textChangedEventHandler);
            // 
            // lWarningNodesNumber
            // 
            this.lWarningNodesNumber.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lWarningNodesNumber.AutoSize = true;
            this.lWarningNodesNumber.Location = new System.Drawing.Point(391, 104);
            this.lWarningNodesNumber.Name = "lWarningNodesNumber";
            this.lWarningNodesNumber.Size = new System.Drawing.Size(117, 13);
            this.lWarningNodesNumber.TabIndex = 10;
            this.lWarningNodesNumber.Text = "lWarningNodesNumber";
            // 
            // btnCreate
            // 
            this.btnCreate.Location = new System.Drawing.Point(375, 395);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(75, 23);
            this.btnCreate.TabIndex = 11;
            this.btnCreate.Text = "Create";
            this.btnCreate.UseVisualStyleBackColor = true;
            this.btnCreate.Click += new System.EventHandler(this.btnCreate_Click);
            // 
            // nodeConfigDataView
            // 
            this.nodeConfigDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.nodeConfigDataView.Location = new System.Drawing.Point(18, 152);
            this.nodeConfigDataView.Name = "nodeConfigDataView";
            this.nodeConfigDataView.Size = new System.Drawing.Size(513, 237);
            this.nodeConfigDataView.TabIndex = 12;
            // 
            // RaftBootStrap
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(546, 450);
            this.Controls.Add(this.nodeConfigDataView);
            this.Controls.Add(this.btnCreate);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.button1);
            this.Name = "RaftBootStrap";
            this.Text = "Raft Consensus Prototype";
            ((System.ComponentModel.ISupportInitialize)(this.nNodes)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nodeConfigDataView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nNodes;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbClusterName;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button btnCreate;
        private System.Windows.Forms.Label lWarningNodesNumber;
        private System.Windows.Forms.Label lClusterPasswd;
        private System.Windows.Forms.TextBox tbClusterPasswd;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbIPAddress;
        private System.Windows.Forms.TextBox tbPort;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.DataGridView nodeConfigDataView;
    }
}

