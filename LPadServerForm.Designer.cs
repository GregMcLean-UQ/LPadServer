namespace LPadServer
   {
   partial class lPadServerForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(lPadServerForm));
            this.statusListBox = new System.Windows.Forms.ListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.fileSystemWatcher = new System.IO.FileSystemWatcher();
            this.lPadTimer = new System.Windows.Forms.Timer(this.components);
            this.eMailTestButton = new System.Windows.Forms.Button();
            this.createNcButton = new System.Windows.Forms.Button();
            this.loadAllNcbutton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher)).BeginInit();
            this.SuspendLayout();
            // 
            // statusListBox
            // 
            this.statusListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.statusListBox.FormattingEnabled = true;
            this.statusListBox.ItemHeight = 16;
            this.statusListBox.Location = new System.Drawing.Point(30, 150);
            this.statusListBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.statusListBox.Name = "statusListBox";
            this.statusListBox.Size = new System.Drawing.Size(614, 404);
            this.statusListBox.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Arial", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(37, 48);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(431, 24);
            this.label3.TabIndex = 6;
            this.label3.Text = "The LPadServer will store and graph the data";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Arial", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(37, 87);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(430, 24);
            this.label4.TabIndex = 7;
            this.label4.Text = "and check that data is coming from the LPad ";
            // 
            // fileSystemWatcher
            // 
            this.fileSystemWatcher.EnableRaisingEvents = true;
            this.fileSystemWatcher.NotifyFilter = System.IO.NotifyFilters.LastWrite;
            this.fileSystemWatcher.SynchronizingObject = this;
            this.fileSystemWatcher.Changed += new System.IO.FileSystemEventHandler(this.fileSystemWatcher_Changed);
            // 
            // lPadTimer
            // 
            this.lPadTimer.Enabled = true;
            this.lPadTimer.Interval = 300000;
            this.lPadTimer.Tick += new System.EventHandler(this.lPadTimer_Tick);
            // 
            // eMailTestButton
            // 
            this.eMailTestButton.Location = new System.Drawing.Point(723, 51);
            this.eMailTestButton.Name = "eMailTestButton";
            this.eMailTestButton.Size = new System.Drawing.Size(97, 23);
            this.eMailTestButton.TabIndex = 8;
            this.eMailTestButton.Text = "eMail Test";
            this.eMailTestButton.UseVisualStyleBackColor = true;
            this.eMailTestButton.Click += new System.EventHandler(this.eMailTestButton_Click);
            // 
            // createNcButton
            // 
            this.createNcButton.Location = new System.Drawing.Point(723, 80);
            this.createNcButton.Name = "createNcButton";
            this.createNcButton.Size = new System.Drawing.Size(97, 23);
            this.createNcButton.TabIndex = 10;
            this.createNcButton.Text = "Create NC File";
            this.createNcButton.UseVisualStyleBackColor = true;
            this.createNcButton.Click += new System.EventHandler(this.createNcButton_Click);
            // 
            // loadAllNcbutton
            // 
            this.loadAllNcbutton.Location = new System.Drawing.Point(854, 80);
            this.loadAllNcbutton.Name = "loadAllNcbutton";
            this.loadAllNcbutton.Size = new System.Drawing.Size(241, 23);
            this.loadAllNcbutton.TabIndex = 11;
            this.loadAllNcbutton.Text = "Load the whole .csv file to NetCDF";
            this.loadAllNcbutton.UseVisualStyleBackColor = true;
            this.loadAllNcbutton.Click += new System.EventHandler(this.loadAllNcbutton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(93, 16);
            this.label1.TabIndex = 12;
            this.label1.Text = "16/2/2023 1:08";
            // 
            // lPadServerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1135, 589);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.loadAllNcbutton);
            this.Controls.Add(this.createNcButton);
            this.Controls.Add(this.eMailTestButton);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.statusListBox);
            this.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "lPadServerForm";
            this.RightToLeftLayout = true;
            this.Text = "LPad Server";
            this.Load += new System.EventHandler(this.LPadServerForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

         }

      #endregion

      private System.Windows.Forms.ListBox statusListBox;
      private System.Windows.Forms.Label label3;
      private System.Windows.Forms.Label label4;
      private System.IO.FileSystemWatcher fileSystemWatcher;
      private System.Windows.Forms.Timer lPadTimer;
      private System.Windows.Forms.Button eMailTestButton;
      private System.Windows.Forms.Button createNcButton;
      private System.Windows.Forms.Button loadAllNcbutton;
        private System.Windows.Forms.Label label1;
    }
   }

