namespace MountMichaelScheduling
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
            this.btnMasterSchedule = new System.Windows.Forms.Button();
            this.dtGridView = new System.Windows.Forms.DataGridView();
            this.btnStudent = new System.Windows.Forms.Button();
            this.btnDepartment = new System.Windows.Forms.Button();
            this.btnRoom = new System.Windows.Forms.Button();
            this.btnSubject = new System.Windows.Forms.Button();
            this.btnPairRela = new System.Windows.Forms.Button();
            this.btnConflict = new System.Windows.Forms.Button();
            this.btnSchedule = new System.Windows.Forms.Button();
            this.btnCredit = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnCheckerFileSelect = new System.Windows.Forms.Button();
            this.btnChecker = new System.Windows.Forms.Button();
            this.btnSelectMasterSchedule = new System.Windows.Forms.Button();
            this.btnCheckMasterSchedule = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dtGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // btnMasterSchedule
            // 
            this.btnMasterSchedule.Location = new System.Drawing.Point(313, 11);
            this.btnMasterSchedule.Margin = new System.Windows.Forms.Padding(2);
            this.btnMasterSchedule.Name = "btnMasterSchedule";
            this.btnMasterSchedule.Size = new System.Drawing.Size(116, 27);
            this.btnMasterSchedule.TabIndex = 1;
            this.btnMasterSchedule.Text = "Master Schedule";
            this.btnMasterSchedule.UseVisualStyleBackColor = true;
            this.btnMasterSchedule.Click += new System.EventHandler(this.btnMasterSchedule_Click);
            // 
            // dtGridView
            // 
            this.dtGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dtGridView.Location = new System.Drawing.Point(11, 44);
            this.dtGridView.Margin = new System.Windows.Forms.Padding(2);
            this.dtGridView.Name = "dtGridView";
            this.dtGridView.RowTemplate.Height = 24;
            this.dtGridView.Size = new System.Drawing.Size(778, 395);
            this.dtGridView.TabIndex = 2;
            // 
            // btnStudent
            // 
            this.btnStudent.Location = new System.Drawing.Point(603, 11);
            this.btnStudent.Margin = new System.Windows.Forms.Padding(2);
            this.btnStudent.Name = "btnStudent";
            this.btnStudent.Size = new System.Drawing.Size(76, 28);
            this.btnStudent.TabIndex = 4;
            this.btnStudent.Text = "Student";
            this.btnStudent.UseVisualStyleBackColor = true;
            this.btnStudent.Click += new System.EventHandler(this.btnStudent_Click);
            // 
            // btnDepartment
            // 
            this.btnDepartment.Location = new System.Drawing.Point(11, 11);
            this.btnDepartment.Margin = new System.Windows.Forms.Padding(2);
            this.btnDepartment.Name = "btnDepartment";
            this.btnDepartment.Size = new System.Drawing.Size(80, 27);
            this.btnDepartment.TabIndex = 6;
            this.btnDepartment.Text = "Department";
            this.btnDepartment.UseVisualStyleBackColor = true;
            this.btnDepartment.Click += new System.EventHandler(this.btnDepartment_Click);
            // 
            // btnRoom
            // 
            this.btnRoom.Location = new System.Drawing.Point(179, 11);
            this.btnRoom.Margin = new System.Windows.Forms.Padding(2);
            this.btnRoom.Name = "btnRoom";
            this.btnRoom.Size = new System.Drawing.Size(60, 27);
            this.btnRoom.TabIndex = 8;
            this.btnRoom.Text = "Room";
            this.btnRoom.UseVisualStyleBackColor = true;
            this.btnRoom.Click += new System.EventHandler(this.btnRoom_Click);
            // 
            // btnSubject
            // 
            this.btnSubject.Location = new System.Drawing.Point(95, 11);
            this.btnSubject.Margin = new System.Windows.Forms.Padding(2);
            this.btnSubject.Name = "btnSubject";
            this.btnSubject.Size = new System.Drawing.Size(80, 27);
            this.btnSubject.TabIndex = 10;
            this.btnSubject.Text = "Subject";
            this.btnSubject.UseVisualStyleBackColor = true;
            this.btnSubject.Click += new System.EventHandler(this.btnSubject_Click);
            // 
            // btnPairRela
            // 
            this.btnPairRela.Location = new System.Drawing.Point(433, 11);
            this.btnPairRela.Margin = new System.Windows.Forms.Padding(2);
            this.btnPairRela.Name = "btnPairRela";
            this.btnPairRela.Size = new System.Drawing.Size(87, 28);
            this.btnPairRela.TabIndex = 12;
            this.btnPairRela.Text = "Pair Relation";
            this.btnPairRela.UseVisualStyleBackColor = true;
            this.btnPairRela.Click += new System.EventHandler(this.btnPairRela_Click);
            // 
            // btnConflict
            // 
            this.btnConflict.Location = new System.Drawing.Point(524, 11);
            this.btnConflict.Margin = new System.Windows.Forms.Padding(2);
            this.btnConflict.Name = "btnConflict";
            this.btnConflict.Size = new System.Drawing.Size(75, 28);
            this.btnConflict.TabIndex = 14;
            this.btnConflict.Text = "Conflict";
            this.btnConflict.UseVisualStyleBackColor = true;
            this.btnConflict.Click += new System.EventHandler(this.btnConflict_Click);
            // 
            // btnSchedule
            // 
            this.btnSchedule.Location = new System.Drawing.Point(794, 367);
            this.btnSchedule.Name = "btnSchedule";
            this.btnSchedule.Size = new System.Drawing.Size(89, 27);
            this.btnSchedule.TabIndex = 15;
            this.btnSchedule.Text = "Schedule";
            this.btnSchedule.UseVisualStyleBackColor = true;
            this.btnSchedule.Click += new System.EventHandler(this.btnSchedule_Click);
            // 
            // btnCredit
            // 
            this.btnCredit.Location = new System.Drawing.Point(243, 11);
            this.btnCredit.Margin = new System.Windows.Forms.Padding(2);
            this.btnCredit.Name = "btnCredit";
            this.btnCredit.Size = new System.Drawing.Size(66, 27);
            this.btnCredit.TabIndex = 16;
            this.btnCredit.Text = "Credit";
            this.btnCredit.UseVisualStyleBackColor = true;
            this.btnCredit.Click += new System.EventHandler(this.btnCredit_Click);
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(794, 400);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(89, 38);
            this.btnExport.TabIndex = 17;
            this.btnExport.Text = "Schedule and Export";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // btnCheckerFileSelect
            // 
            this.btnCheckerFileSelect.Location = new System.Drawing.Point(794, 44);
            this.btnCheckerFileSelect.Name = "btnCheckerFileSelect";
            this.btnCheckerFileSelect.Size = new System.Drawing.Size(89, 38);
            this.btnCheckerFileSelect.TabIndex = 17;
            this.btnCheckerFileSelect.Text = "Select Student Files";
            this.btnCheckerFileSelect.UseVisualStyleBackColor = true;
            this.btnCheckerFileSelect.Click += new System.EventHandler(this.btnCheckerFileSelect_Click);
            // 
            // btnChecker
            // 
            this.btnChecker.Location = new System.Drawing.Point(794, 87);
            this.btnChecker.Name = "btnChecker";
            this.btnChecker.Size = new System.Drawing.Size(90, 38);
            this.btnChecker.TabIndex = 18;
            this.btnChecker.Text = "Check Student Files";
            this.btnChecker.UseVisualStyleBackColor = true;
            this.btnChecker.Click += new System.EventHandler(this.btnChecker_Click);
            // 
            // btnSelectMasterSchedule
            // 
            this.btnSelectMasterSchedule.Location = new System.Drawing.Point(794, 131);
            this.btnSelectMasterSchedule.Margin = new System.Windows.Forms.Padding(2);
            this.btnSelectMasterSchedule.Name = "btnSelectMasterSchedule";
            this.btnSelectMasterSchedule.Size = new System.Drawing.Size(89, 38);
            this.btnSelectMasterSchedule.TabIndex = 19;
            this.btnSelectMasterSchedule.Text = "Select Master Schedule";
            this.btnSelectMasterSchedule.UseVisualStyleBackColor = true;
            this.btnSelectMasterSchedule.Click += new System.EventHandler(this.btnSelectMasterSchedule_Click);
            // 
            // btnCheckMasterSchedule
            // 
            this.btnCheckMasterSchedule.Location = new System.Drawing.Point(794, 176);
            this.btnCheckMasterSchedule.Margin = new System.Windows.Forms.Padding(2);
            this.btnCheckMasterSchedule.Name = "btnCheckMasterSchedule";
            this.btnCheckMasterSchedule.Size = new System.Drawing.Size(90, 38);
            this.btnCheckMasterSchedule.TabIndex = 20;
            this.btnCheckMasterSchedule.Text = "Check Master Schedule";
            this.btnCheckMasterSchedule.UseVisualStyleBackColor = true;
            this.btnCheckMasterSchedule.Click += new System.EventHandler(this.btnCheckMasterSchedule_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(895, 450);
            this.Controls.Add(this.btnCheckMasterSchedule);
            this.Controls.Add(this.btnSelectMasterSchedule);
            this.Controls.Add(this.btnChecker);
            this.Controls.Add(this.btnCheckerFileSelect);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.btnCredit);
            this.Controls.Add(this.btnSchedule);
            this.Controls.Add(this.btnConflict);
            this.Controls.Add(this.btnPairRela);
            this.Controls.Add(this.btnSubject);
            this.Controls.Add(this.btnRoom);
            this.Controls.Add(this.btnDepartment);
            this.Controls.Add(this.btnStudent);
            this.Controls.Add(this.dtGridView);
            this.Controls.Add(this.btnMasterSchedule);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dtGridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnMasterSchedule;
        private System.Windows.Forms.DataGridView dtGridView;
        private System.Windows.Forms.Button btnStudent;
        private System.Windows.Forms.Button btnDepartment;
        private System.Windows.Forms.Button btnRoom;
        private System.Windows.Forms.Button btnSubject;
        private System.Windows.Forms.Button btnPairRela;
        private System.Windows.Forms.Button btnConflict;
        private System.Windows.Forms.Button btnSchedule;
        private System.Windows.Forms.Button btnCredit;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnCheckerFileSelect;
        private System.Windows.Forms.Button btnChecker;
        private System.Windows.Forms.Button btnSelectMasterSchedule;
        private System.Windows.Forms.Button btnCheckMasterSchedule;
    }
}

