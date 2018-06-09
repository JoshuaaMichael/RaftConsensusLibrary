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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RaftBootStrap));
            this.label1 = new System.Windows.Forms.Label();
            this.nNodes = new System.Windows.Forms.NumericUpDown();
            this.button1 = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.tbClusterName = new System.Windows.Forms.TextBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lClusterPasswd = new System.Windows.Forms.Label();
            this.tbClusterPasswd = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.cbDebugLevel = new System.Windows.Forms.ComboBox();
            this.lWarningNodesNumber = new System.Windows.Forms.Label();
            this.tbIPAddress = new System.Windows.Forms.TextBox();
            this.tbPort = new System.Windows.Forms.TextBox();
            this.cbUseEncryption = new System.Windows.Forms.CheckBox();
            this.btnCreate = new System.Windows.Forms.Button();
            this.nodeConfigDataView = new System.Windows.Forms.DataGridView();
            this.gbButton = new System.Windows.Forms.GroupBox();
            this.cbInstantiate = new System.Windows.Forms.CheckBox();
            this.gbClusterInfo = new System.Windows.Forms.GroupBox();
            this.gbTitle = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.gbConfigDetails = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.nNodes)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nodeConfigDataView)).BeginInit();
            this.gbButton.SuspendLayout();
            this.gbClusterInfo.SuspendLayout();
            this.gbTitle.SuspendLayout();
            this.gbConfigDetails.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 151);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(252, 17);
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
            this.nNodes.Location = new System.Drawing.Point(338, 149);
            this.nNodes.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nNodes.Name = "nNodes";
            this.nNodes.Size = new System.Drawing.Size(160, 22);
            this.nNodes.TabIndex = 1;
            this.nNodes.ValueChanged += new System.EventHandler(this.Nodes_ValueChanged);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(596, 16);
            this.button1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(100, 28);
            this.button1.TabIndex = 2;
            this.button1.Text = "Exit";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.Create_Button_Click);
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(4, 6);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(319, 17);
            this.label2.TabIndex = 8;
            this.label2.Text = "Please enter the cluster name you wish to create:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tbClusterName
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.tbClusterName, 2);
            this.tbClusterName.Dock = System.Windows.Forms.DockStyle.Top;
            this.tbClusterName.Location = new System.Drawing.Point(338, 4);
            this.tbClusterName.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tbClusterName.Name = "tbClusterName";
            this.tbClusterName.Size = new System.Drawing.Size(350, 22);
            this.tbClusterName.TabIndex = 9;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 48.38709F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25.80645F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25.80645F));
            this.tableLayoutPanel1.Controls.Add(this.tbClusterName, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.lClusterPasswd, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.tbClusterPasswd, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label6, 0, 7);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.label4, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.label7, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.cbDebugLevel, 1, 7);
            this.tableLayoutPanel1.Controls.Add(this.nNodes, 1, 6);
            this.tableLayoutPanel1.Controls.Add(this.lWarningNodesNumber, 2, 6);
            this.tableLayoutPanel1.Controls.Add(this.tbIPAddress, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.tbPort, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.cbUseEncryption, 1, 3);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(8, 23);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 8;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(692, 222);
            this.tableLayoutPanel1.TabIndex = 10;
            // 
            // lClusterPasswd
            // 
            this.lClusterPasswd.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lClusterPasswd.AutoSize = true;
            this.lClusterPasswd.Location = new System.Drawing.Point(4, 36);
            this.lClusterPasswd.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lClusterPasswd.Name = "lClusterPasswd";
            this.lClusterPasswd.Size = new System.Drawing.Size(198, 17);
            this.lClusterPasswd.TabIndex = 11;
            this.lClusterPasswd.Text = "Enter password to join cluster:";
            // 
            // tbClusterPasswd
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.tbClusterPasswd, 2);
            this.tbClusterPasswd.Dock = System.Windows.Forms.DockStyle.Top;
            this.tbClusterPasswd.Location = new System.Drawing.Point(338, 34);
            this.tbClusterPasswd.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tbClusterPasswd.Name = "tbClusterPasswd";
            this.tbClusterPasswd.Size = new System.Drawing.Size(350, 22);
            this.tbClusterPasswd.TabIndex = 12;
            // 
            // label6
            // 
            this.label6.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(4, 190);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(92, 17);
            this.label6.TabIndex = 17;
            this.label6.Text = "Debug Level:";
            // 
            // label3
            // 
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(4, 121);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(216, 17);
            this.label3.TabIndex = 13;
            this.label3.Text = "All servers will run on IP address:";
            // 
            // label4
            // 
            this.label4.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(4, 91);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(114, 17);
            this.label4.TabIndex = 16;
            this.label4.Text = "Default start port";
            // 
            // label7
            // 
            this.label7.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(4, 64);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(108, 17);
            this.label7.TabIndex = 19;
            this.label7.Text = "Use Encryption:";
            // 
            // cbDebugLevel
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.cbDebugLevel, 2);
            this.cbDebugLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDebugLevel.FormattingEnabled = true;
            this.cbDebugLevel.Location = new System.Drawing.Point(338, 179);
            this.cbDebugLevel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cbDebugLevel.Name = "cbDebugLevel";
            this.cbDebugLevel.Size = new System.Drawing.Size(160, 24);
            this.cbDebugLevel.TabIndex = 18;
            // 
            // lWarningNodesNumber
            // 
            this.lWarningNodesNumber.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lWarningNodesNumber.AutoSize = true;
            this.lWarningNodesNumber.Location = new System.Drawing.Point(524, 145);
            this.lWarningNodesNumber.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lWarningNodesNumber.Name = "lWarningNodesNumber";
            this.lWarningNodesNumber.Size = new System.Drawing.Size(155, 17);
            this.lWarningNodesNumber.TabIndex = 10;
            this.lWarningNodesNumber.Text = "lWarningNodesNumber";
            // 
            // tbIPAddress
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.tbIPAddress, 2);
            this.tbIPAddress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbIPAddress.Location = new System.Drawing.Point(338, 119);
            this.tbIPAddress.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tbIPAddress.Name = "tbIPAddress";
            this.tbIPAddress.Size = new System.Drawing.Size(350, 22);
            this.tbIPAddress.TabIndex = 14;
            // 
            // tbPort
            // 
            this.tbPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbPort.Location = new System.Drawing.Point(338, 89);
            this.tbPort.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tbPort.Name = "tbPort";
            this.tbPort.Size = new System.Drawing.Size(170, 22);
            this.tbPort.TabIndex = 15;
            this.tbPort.TextChanged += new System.EventHandler(this.TbPort_textChangedEventHandler);
            // 
            // cbUseEncryption
            // 
            this.cbUseEncryption.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cbUseEncryption.AutoSize = true;
            this.cbUseEncryption.Checked = true;
            this.cbUseEncryption.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbUseEncryption.Location = new System.Drawing.Point(338, 64);
            this.cbUseEncryption.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cbUseEncryption.Name = "cbUseEncryption";
            this.cbUseEncryption.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.cbUseEncryption.Size = new System.Drawing.Size(170, 17);
            this.cbUseEncryption.TabIndex = 20;
            this.cbUseEncryption.UseVisualStyleBackColor = true;
            // 
            // btnCreate
            // 
            this.btnCreate.Location = new System.Drawing.Point(488, 16);
            this.btnCreate.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(100, 28);
            this.btnCreate.TabIndex = 11;
            this.btnCreate.Text = "Create";
            this.btnCreate.UseVisualStyleBackColor = true;
            this.btnCreate.Click += new System.EventHandler(this.CreateRaftNodes_Click);
            // 
            // nodeConfigDataView
            // 
            this.nodeConfigDataView.AllowUserToResizeColumns = false;
            this.nodeConfigDataView.AllowUserToResizeRows = false;
            this.nodeConfigDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.nodeConfigDataView.Location = new System.Drawing.Point(8, 23);
            this.nodeConfigDataView.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nodeConfigDataView.Name = "nodeConfigDataView";
            this.nodeConfigDataView.Size = new System.Drawing.Size(688, 273);
            this.nodeConfigDataView.TabIndex = 12;
            // 
            // gbButton
            // 
            this.gbButton.Controls.Add(this.cbInstantiate);
            this.gbButton.Controls.Add(this.btnCreate);
            this.gbButton.Controls.Add(this.button1);
            this.gbButton.Location = new System.Drawing.Point(16, 662);
            this.gbButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gbButton.Name = "gbButton";
            this.gbButton.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gbButton.Size = new System.Drawing.Size(708, 55);
            this.gbButton.TabIndex = 13;
            this.gbButton.TabStop = false;
            // 
            // cbInstantiate
            // 
            this.cbInstantiate.AutoSize = true;
            this.cbInstantiate.Location = new System.Drawing.Point(16, 21);
            this.cbInstantiate.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cbInstantiate.Name = "cbInstantiate";
            this.cbInstantiate.Size = new System.Drawing.Size(140, 21);
            this.cbInstantiate.TabIndex = 17;
            this.cbInstantiate.Text = "Instantiate Nodes";
            this.cbInstantiate.UseVisualStyleBackColor = true;
            // 
            // gbClusterInfo
            // 
            this.gbClusterInfo.Controls.Add(this.tableLayoutPanel1);
            this.gbClusterInfo.Location = new System.Drawing.Point(16, 98);
            this.gbClusterInfo.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gbClusterInfo.Name = "gbClusterInfo";
            this.gbClusterInfo.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gbClusterInfo.Size = new System.Drawing.Size(708, 252);
            this.gbClusterInfo.TabIndex = 14;
            this.gbClusterInfo.TabStop = false;
            this.gbClusterInfo.Text = "Cluster Information";
            // 
            // gbTitle
            // 
            this.gbTitle.Controls.Add(this.label5);
            this.gbTitle.Location = new System.Drawing.Point(16, 15);
            this.gbTitle.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gbTitle.Name = "gbTitle";
            this.gbTitle.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gbTitle.Size = new System.Drawing.Size(708, 76);
            this.gbTitle.TabIndex = 15;
            this.gbTitle.TabStop = false;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(51, 20);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(529, 39);
            this.label5.TabIndex = 0;
            this.label5.Text = "Team Decided - RaftConsensus";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // gbConfigDetails
            // 
            this.gbConfigDetails.Controls.Add(this.nodeConfigDataView);
            this.gbConfigDetails.Location = new System.Drawing.Point(16, 358);
            this.gbConfigDetails.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gbConfigDetails.Name = "gbConfigDetails";
            this.gbConfigDetails.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gbConfigDetails.Size = new System.Drawing.Size(708, 297);
            this.gbConfigDetails.TabIndex = 16;
            this.gbConfigDetails.TabStop = false;
            this.gbConfigDetails.Text = "Configuration Details";
            // 
            // RaftBootStrap
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(745, 721);
            this.Controls.Add(this.gbConfigDetails);
            this.Controls.Add(this.gbTitle);
            this.Controls.Add(this.gbClusterInfo);
            this.Controls.Add(this.gbButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.Name = "RaftBootStrap";
            this.StartPosition = System.Windows.Forms.FormStartPosition.WindowsDefaultBounds;
            this.Text = "Raft Consensus BootStrap";
            ((System.ComponentModel.ISupportInitialize)(this.nNodes)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nodeConfigDataView)).EndInit();
            this.gbButton.ResumeLayout(false);
            this.gbButton.PerformLayout();
            this.gbClusterInfo.ResumeLayout(false);
            this.gbTitle.ResumeLayout(false);
            this.gbTitle.PerformLayout();
            this.gbConfigDetails.ResumeLayout(false);
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
        private System.Windows.Forms.GroupBox gbButton;
        private System.Windows.Forms.GroupBox gbClusterInfo;
        private System.Windows.Forms.GroupBox gbTitle;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.GroupBox gbConfigDetails;
        private System.Windows.Forms.CheckBox cbInstantiate;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cbDebugLevel;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox cbUseEncryption;
    }
}

