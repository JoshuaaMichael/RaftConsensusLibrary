namespace RaftPrototype
{
    partial class RaftServercs
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
            this.lNodeName = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.bindingSource1 = new System.Windows.Forms.BindingSource(this.components);
            this.bSendMsg = new System.Windows.Forms.Button();
            this.bStop = new System.Windows.Forms.Button();
            this.bResume = new System.Windows.Forms.Button();
            this.lState = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).BeginInit();
            this.SuspendLayout();
            // 
            // lNodeName
            // 
            this.lNodeName.AutoSize = true;
            this.lNodeName.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lNodeName.Location = new System.Drawing.Point(9, 9);
            this.lNodeName.Name = "lNodeName";
            this.lNodeName.Size = new System.Drawing.Size(79, 17);
            this.lNodeName.TabIndex = 0;
            this.lNodeName.Text = "NodeName";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AutoGenerateColumns = false;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.DataSource = this.bindingSource1;
            this.dataGridView1.Location = new System.Drawing.Point(12, 62);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(303, 283);
            this.dataGridView1.TabIndex = 1;
            // 
            // bSendMsg
            // 
            this.bSendMsg.Location = new System.Drawing.Point(321, 264);
            this.bSendMsg.Name = "bSendMsg";
            this.bSendMsg.Size = new System.Drawing.Size(75, 23);
            this.bSendMsg.TabIndex = 2;
            this.bSendMsg.Text = "Send Message";
            this.bSendMsg.UseVisualStyleBackColor = true;
            // 
            // bStop
            // 
            this.bStop.Location = new System.Drawing.Point(321, 293);
            this.bStop.Name = "bStop";
            this.bStop.Size = new System.Drawing.Size(75, 23);
            this.bStop.TabIndex = 3;
            this.bStop.Text = "Stop";
            this.bStop.UseVisualStyleBackColor = true;
            // 
            // bResume
            // 
            this.bResume.Location = new System.Drawing.Point(321, 322);
            this.bResume.Name = "bResume";
            this.bResume.Size = new System.Drawing.Size(75, 23);
            this.bResume.TabIndex = 4;
            this.bResume.Text = "Resume";
            this.bResume.UseVisualStyleBackColor = true;
            // 
            // lState
            // 
            this.lState.AutoSize = true;
            this.lState.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lState.Location = new System.Drawing.Point(12, 42);
            this.lState.Name = "lState";
            this.lState.Size = new System.Drawing.Size(83, 17);
            this.lState.TabIndex = 5;
            this.lState.Text = "ServerState";
            // 
            // RaftServercs
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(406, 357);
            this.Controls.Add(this.lState);
            this.Controls.Add(this.bResume);
            this.Controls.Add(this.bStop);
            this.Controls.Add(this.bSendMsg);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.lNodeName);
            this.Name = "RaftServercs";
            this.Text = "RaftServercs";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lNodeName;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.BindingSource bindingSource1;
        private System.Windows.Forms.Button bSendMsg;
        private System.Windows.Forms.Button bStop;
        private System.Windows.Forms.Button bResume;
        private System.Windows.Forms.Label lState;
    }
}