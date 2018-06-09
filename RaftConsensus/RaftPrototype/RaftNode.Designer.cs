namespace RaftPrototype
{
    partial class RaftNode
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RaftNode));
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lbServerState = new System.Windows.Forms.Label();
            this.lbNodeName = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.btStop = new System.Windows.Forms.Button();
            this.btStart = new System.Windows.Forms.Button();
            this.logDataGrid = new System.Windows.Forms.DataGridView();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.gbAppendEntry = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tbKey = new System.Windows.Forms.TextBox();
            this.bSendMsg = new System.Windows.Forms.Button();
            this.tbValue = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.btStartNode = new System.Windows.Forms.Button();
            this.btStopNode = new System.Windows.Forms.Button();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.cbDebug = new System.Windows.Forms.CheckBox();
            this.tbServerStatus = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tbLog = new System.Windows.Forms.TextBox();
            this.cbDebugLevel = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.groupBox2.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.logDataGrid)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.gbAppendEntry.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.tableLayoutPanel1);
            this.groupBox2.Location = new System.Drawing.Point(6, 6);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(343, 85);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Consensus Node Detail";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.lbServerState, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.lbNodeName, 1, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(6, 19);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(331, 61);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "Node Name:";
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 39);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(121, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "User Application Server:";
            // 
            // lbServerState
            // 
            this.lbServerState.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lbServerState.AutoSize = true;
            this.lbServerState.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbServerState.Location = new System.Drawing.Point(130, 39);
            this.lbServerState.Name = "lbServerState";
            this.lbServerState.Size = new System.Drawing.Size(63, 13);
            this.lbServerState.TabIndex = 5;
            this.lbServerState.Text = "ServerState";
            // 
            // lbNodeName
            // 
            this.lbNodeName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lbNodeName.AutoSize = true;
            this.lbNodeName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbNodeName.Location = new System.Drawing.Point(130, 8);
            this.lbNodeName.Name = "lbNodeName";
            this.lbNodeName.Size = new System.Drawing.Size(61, 13);
            this.lbNodeName.TabIndex = 0;
            this.lbNodeName.Text = "NodeName";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.btStop);
            this.groupBox3.Controls.Add(this.btStart);
            this.groupBox3.Controls.Add(this.logDataGrid);
            this.groupBox3.Location = new System.Drawing.Point(6, 97);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(343, 351);
            this.groupBox3.TabIndex = 11;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Consensus Log";
            // 
            // btStop
            // 
            this.btStop.Location = new System.Drawing.Point(262, 322);
            this.btStop.Name = "btStop";
            this.btStop.Size = new System.Drawing.Size(75, 23);
            this.btStop.TabIndex = 3;
            this.btStop.Text = "Stop";
            this.btStop.UseVisualStyleBackColor = true;
            this.btStop.Click += new System.EventHandler(this.Stop_Click);
            // 
            // btStart
            // 
            this.btStart.Location = new System.Drawing.Point(181, 322);
            this.btStart.Name = "btStart";
            this.btStart.Size = new System.Drawing.Size(75, 23);
            this.btStart.TabIndex = 2;
            this.btStart.Text = "Start";
            this.btStart.UseVisualStyleBackColor = true;
            this.btStart.Click += new System.EventHandler(this.Start_Click);
            // 
            // logDataGrid
            // 
            this.logDataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.logDataGrid.Location = new System.Drawing.Point(6, 19);
            this.logDataGrid.Name = "logDataGrid";
            this.logDataGrid.Size = new System.Drawing.Size(331, 297);
            this.logDataGrid.TabIndex = 1;
            this.logDataGrid.TabStop = false;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(2, 2);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(363, 566);
            this.tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.gbAppendEntry);
            this.tabPage1.Controls.Add(this.groupBox3);
            this.tabPage1.Controls.Add(this.groupBox2);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(355, 540);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Node Detail";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // gbAppendEntry
            // 
            this.gbAppendEntry.Controls.Add(this.tableLayoutPanel2);
            this.gbAppendEntry.Enabled = false;
            this.gbAppendEntry.Location = new System.Drawing.Point(2, 454);
            this.gbAppendEntry.Name = "gbAppendEntry";
            this.gbAppendEntry.Size = new System.Drawing.Size(350, 90);
            this.gbAppendEntry.TabIndex = 12;
            this.gbAppendEntry.TabStop = false;
            this.gbAppendEntry.Text = "Append Entry";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 3;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.Controls.Add(this.tbKey, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.bSendMsg, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.tbValue, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.label3, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.label4, 0, 1);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(6, 19);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(334, 61);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // tbKey
            // 
            this.tbKey.Location = new System.Drawing.Point(46, 3);
            this.tbKey.Name = "tbKey";
            this.tbKey.Size = new System.Drawing.Size(204, 20);
            this.tbKey.TabIndex = 4;
            // 
            // bSendMsg
            // 
            this.bSendMsg.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.bSendMsg.Location = new System.Drawing.Point(256, 19);
            this.bSendMsg.Name = "bSendMsg";
            this.tableLayoutPanel2.SetRowSpan(this.bSendMsg, 2);
            this.bSendMsg.Size = new System.Drawing.Size(75, 23);
            this.bSendMsg.TabIndex = 6;
            this.bSendMsg.Text = "Append";
            this.bSendMsg.UseVisualStyleBackColor = true;
            this.bSendMsg.Click += new System.EventHandler(this.AppendMessage_Click);
            // 
            // tbValue
            // 
            this.tbValue.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.tbValue.Location = new System.Drawing.Point(46, 33);
            this.tbValue.Name = "tbValue";
            this.tbValue.Size = new System.Drawing.Size(204, 20);
            this.tbValue.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 6);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(28, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Key:";
            // 
            // label4
            // 
            this.label4.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 37);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(37, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Value:";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.cbDebug);
            this.tabPage2.Controls.Add(this.btStartNode);
            this.tabPage2.Controls.Add(this.btStopNode);
            this.tabPage2.Controls.Add(this.tableLayoutPanel3);
            this.tabPage2.Controls.Add(this.tbLog);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(355, 540);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Debug Log";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // btStartNode
            // 
            this.btStartNode.Location = new System.Drawing.Point(190, 514);
            this.btStartNode.Name = "btStartNode";
            this.btStartNode.Size = new System.Drawing.Size(75, 23);
            this.btStartNode.TabIndex = 6;
            this.btStartNode.Text = "Start Node";
            this.btStartNode.UseVisualStyleBackColor = true;
            this.btStartNode.Click += new System.EventHandler(this.Start_Click);
            // 
            // btStopNode
            // 
            this.btStopNode.Location = new System.Drawing.Point(271, 514);
            this.btStopNode.Name = "btStopNode";
            this.btStopNode.Size = new System.Drawing.Size(75, 23);
            this.btStopNode.TabIndex = 5;
            this.btStopNode.Text = "Stop Node";
            this.btStopNode.UseVisualStyleBackColor = true;
            this.btStopNode.Click += new System.EventHandler(this.Stop_Click);
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 2;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Controls.Add(this.tbServerStatus, 1, 1);
            this.tableLayoutPanel3.Controls.Add(this.label5, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.cbDebugLevel, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.label6, 0, 0);
            this.tableLayoutPanel3.Location = new System.Drawing.Point(6, 6);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.Size = new System.Drawing.Size(343, 50);
            this.tableLayoutPanel3.TabIndex = 4;
            // 
            // cbDebug
            // 
            this.cbDebug.AutoSize = true;
            this.cbDebug.Location = new System.Drawing.Point(12, 514);
            this.cbDebug.Name = "cbDebug";
            this.cbDebug.Size = new System.Drawing.Size(112, 17);
            this.cbDebug.TabIndex = 1;
            this.cbDebug.Text = "Debug Log Active";
            this.cbDebug.UseVisualStyleBackColor = true;
            // 
            // tbServerStatus
            // 
            this.tbServerStatus.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.tbServerStatus.Enabled = false;
            this.tbServerStatus.Location = new System.Drawing.Point(83, 30);
            this.tbServerStatus.Name = "tbServerStatus";
            this.tbServerStatus.Size = new System.Drawing.Size(257, 20);
            this.tbServerStatus.TabIndex = 3;
            // 
            // label5
            // 
            this.label5.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 33);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(74, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "Server Status:";
            // 
            // tbLog
            // 
            this.tbLog.Location = new System.Drawing.Point(6, 58);
            this.tbLog.Multiline = true;
            this.tbLog.Name = "tbLog";
            this.tbLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbLog.Size = new System.Drawing.Size(343, 450);
            this.tbLog.TabIndex = 0;
            this.tbLog.TabStop = false;
            this.tbLog.Click += new System.EventHandler(this.Stop_Click);
            // 
            // cbDebugLevel
            // 
            this.cbDebugLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDebugLevel.FormattingEnabled = true;
            this.cbDebugLevel.Location = new System.Drawing.Point(83, 3);
            this.cbDebugLevel.Name = "cbDebugLevel";
            this.cbDebugLevel.Size = new System.Drawing.Size(121, 21);
            this.cbDebugLevel.TabIndex = 4;
            this.cbDebugLevel.SelectedIndexChanged += new System.EventHandler(this.cbDebugLevel_SelectedIndexChanged);
            // 
            // label6
            // 
            this.label6.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 7);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(71, 13);
            this.label6.TabIndex = 5;
            this.label6.Text = "Debug Level:";
            // 
            // RaftNode
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(367, 575);
            this.Controls.Add(this.tabControl1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "RaftNode";
            this.Text = "RaftNode2";
            this.groupBox2.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.logDataGrid)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.gbAppendEntry.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lbServerState;
        private System.Windows.Forms.Label lbNodeName;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.DataGridView logDataGrid;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Button btStop;
        private System.Windows.Forms.Button btStart;
        private System.Windows.Forms.GroupBox gbAppendEntry;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TextBox tbKey;
        private System.Windows.Forms.Button bSendMsg;
        private System.Windows.Forms.TextBox tbValue;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TextBox tbLog;
        private System.Windows.Forms.CheckBox cbDebug;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.TextBox tbServerStatus;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btStartNode;
        private System.Windows.Forms.Button btStopNode;
        private System.Windows.Forms.ComboBox cbDebugLevel;
        private System.Windows.Forms.Label label6;
    }
}