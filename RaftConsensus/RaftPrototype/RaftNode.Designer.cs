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
            this.lbNodeName = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.bSendMsg = new System.Windows.Forms.Button();
            this.bStop = new System.Windows.Forms.Button();
            this.lbState = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tbDebugLog = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lbNodeName
            // 
            this.lbNodeName.AutoSize = true;
            this.lbNodeName.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbNodeName.Location = new System.Drawing.Point(12, 11);
            this.lbNodeName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbNodeName.Name = "lbNodeName";
            this.lbNodeName.Size = new System.Drawing.Size(100, 22);
            this.lbNodeName.TabIndex = 0;
            this.lbNodeName.Text = "NodeName";
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(16, 76);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(4);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(404, 313);
            this.dataGridView1.TabIndex = 1;
            // 
            // bSendMsg
            // 
            this.bSendMsg.Location = new System.Drawing.Point(319, 527);
            this.bSendMsg.Margin = new System.Windows.Forms.Padding(4);
            this.bSendMsg.Name = "bSendMsg";
            this.bSendMsg.Size = new System.Drawing.Size(100, 28);
            this.bSendMsg.TabIndex = 2;
            this.bSendMsg.Text = "Send Message";
            this.bSendMsg.UseVisualStyleBackColor = true;
            this.bSendMsg.Click += new System.EventHandler(this.SendMsg_Click);
            // 
            // bStop
            // 
            this.bStop.Location = new System.Drawing.Point(320, 41);
            this.bStop.Margin = new System.Windows.Forms.Padding(4);
            this.bStop.Name = "bStop";
            this.bStop.Size = new System.Drawing.Size(100, 28);
            this.bStop.TabIndex = 3;
            this.bStop.Text = "Stop";
            this.bStop.UseVisualStyleBackColor = true;
            // 
            // lbState
            // 
            this.lbState.AutoSize = true;
            this.lbState.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbState.Location = new System.Drawing.Point(16, 52);
            this.lbState.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbState.Name = "lbState";
            this.lbState.Size = new System.Drawing.Size(105, 22);
            this.lbState.TabIndex = 5;
            this.lbState.Text = "ServerState";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(19, 527);
            this.textBox1.Margin = new System.Windows.Forms.Padding(4);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(291, 22);
            this.textBox1.TabIndex = 6;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tbDebugLog);
            this.groupBox1.Location = new System.Drawing.Point(16, 396);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox1.Size = new System.Drawing.Size(404, 123);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Debug Log";
            // 
            // tbDebugLog
            // 
            this.tbDebugLog.Location = new System.Drawing.Point(8, 23);
            this.tbDebugLog.Margin = new System.Windows.Forms.Padding(4);
            this.tbDebugLog.Multiline = true;
            this.tbDebugLog.Name = "tbDebugLog";
            this.tbDebugLog.Size = new System.Drawing.Size(387, 91);
            this.tbDebugLog.TabIndex = 0;
            // 
            // RaftNode
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(440, 572);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.lbState);
            this.Controls.Add(this.bStop);
            this.Controls.Add(this.bSendMsg);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.lbNodeName);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "RaftNode";
            this.Text = "Raft Prototype Node";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbNodeName;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button bSendMsg;
        private System.Windows.Forms.Button bStop;
        private System.Windows.Forms.Label lbState;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox tbDebugLog;
    }
}