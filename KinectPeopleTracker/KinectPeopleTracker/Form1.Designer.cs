namespace KinectPeopleTracker
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
            this.ToolPanel = new System.Windows.Forms.Panel();
            this.PortChooser = new System.Windows.Forms.ComboBox();
            this.ArmCheckbox = new System.Windows.Forms.CheckBox();
            this.ResetCounterButton = new System.Windows.Forms.Button();
            this.InvertSizeCheckbox = new System.Windows.Forms.CheckBox();
            this.SizeCheckbox = new System.Windows.Forms.CheckBox();
            this.InvertDirectionCheckbox = new System.Windows.Forms.CheckBox();
            this.ThresholdButton = new System.Windows.Forms.Button();
            this.DisplayPanel = new DistanceDemos.DoubleBufferedPanel();
            this.ToolPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // ToolPanel
            // 
            this.ToolPanel.Controls.Add(this.PortChooser);
            this.ToolPanel.Controls.Add(this.ArmCheckbox);
            this.ToolPanel.Controls.Add(this.ResetCounterButton);
            this.ToolPanel.Controls.Add(this.InvertSizeCheckbox);
            this.ToolPanel.Controls.Add(this.SizeCheckbox);
            this.ToolPanel.Controls.Add(this.InvertDirectionCheckbox);
            this.ToolPanel.Controls.Add(this.ThresholdButton);
            this.ToolPanel.Dock = System.Windows.Forms.DockStyle.Right;
            this.ToolPanel.Location = new System.Drawing.Point(640, 0);
            this.ToolPanel.Name = "ToolPanel";
            this.ToolPanel.Size = new System.Drawing.Size(200, 480);
            this.ToolPanel.TabIndex = 1;
            // 
            // PortChooser
            // 
            this.PortChooser.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.PortChooser.FormattingEnabled = true;
            this.PortChooser.Location = new System.Drawing.Point(11, 447);
            this.PortChooser.Name = "PortChooser";
            this.PortChooser.Size = new System.Drawing.Size(177, 21);
            this.PortChooser.TabIndex = 6;
            this.PortChooser.SelectedIndexChanged += new System.EventHandler(this.PortChooser_SelectedIndexChanged);
            // 
            // ArmCheckbox
            // 
            this.ArmCheckbox.AutoSize = true;
            this.ArmCheckbox.Location = new System.Drawing.Point(11, 424);
            this.ArmCheckbox.Name = "ArmCheckbox";
            this.ArmCheckbox.Size = new System.Drawing.Size(133, 17);
            this.ArmCheckbox.TabIndex = 5;
            this.ArmCheckbox.Text = "Enable Arm Movement";
            this.ArmCheckbox.UseVisualStyleBackColor = true;
            this.ArmCheckbox.CheckedChanged += new System.EventHandler(this.ArmCheckbox_CheckedChanged);
            // 
            // ResetCounterButton
            // 
            this.ResetCounterButton.Location = new System.Drawing.Point(11, 12);
            this.ResetCounterButton.Name = "ResetCounterButton";
            this.ResetCounterButton.Size = new System.Drawing.Size(179, 49);
            this.ResetCounterButton.TabIndex = 4;
            this.ResetCounterButton.Text = "Reset Counter";
            this.ResetCounterButton.UseVisualStyleBackColor = true;
            this.ResetCounterButton.Click += new System.EventHandler(this.ResetCounterButton_Click);
            // 
            // InvertSizeCheckbox
            // 
            this.InvertSizeCheckbox.AutoSize = true;
            this.InvertSizeCheckbox.Location = new System.Drawing.Point(11, 195);
            this.InvertSizeCheckbox.Name = "InvertSizeCheckbox";
            this.InvertSizeCheckbox.Size = new System.Drawing.Size(121, 17);
            this.InvertSizeCheckbox.TabIndex = 3;
            this.InvertSizeCheckbox.Text = "Invert Size Direction";
            this.InvertSizeCheckbox.UseVisualStyleBackColor = true;
            this.InvertSizeCheckbox.CheckedChanged += new System.EventHandler(this.InvertSizeCheckbox_CheckedChanged);
            // 
            // SizeCheckbox
            // 
            this.SizeCheckbox.AutoSize = true;
            this.SizeCheckbox.Location = new System.Drawing.Point(11, 172);
            this.SizeCheckbox.Name = "SizeCheckbox";
            this.SizeCheckbox.Size = new System.Drawing.Size(73, 17);
            this.SizeCheckbox.TabIndex = 2;
            this.SizeCheckbox.Text = "Use Sizes";
            this.SizeCheckbox.UseVisualStyleBackColor = true;
            this.SizeCheckbox.CheckedChanged += new System.EventHandler(this.SizeCheckbox_CheckedChanged);
            // 
            // InvertDirectionCheckbox
            // 
            this.InvertDirectionCheckbox.AutoSize = true;
            this.InvertDirectionCheckbox.Location = new System.Drawing.Point(11, 149);
            this.InvertDirectionCheckbox.Name = "InvertDirectionCheckbox";
            this.InvertDirectionCheckbox.Size = new System.Drawing.Size(98, 17);
            this.InvertDirectionCheckbox.TabIndex = 1;
            this.InvertDirectionCheckbox.Text = "Invert Direction";
            this.InvertDirectionCheckbox.UseVisualStyleBackColor = true;
            this.InvertDirectionCheckbox.CheckedChanged += new System.EventHandler(this.InvertDirectionCheckbox_CheckedChanged);
            // 
            // ThresholdButton
            // 
            this.ThresholdButton.Location = new System.Drawing.Point(11, 105);
            this.ThresholdButton.Name = "ThresholdButton";
            this.ThresholdButton.Size = new System.Drawing.Size(179, 23);
            this.ThresholdButton.TabIndex = 0;
            this.ThresholdButton.Text = "Position Exit";
            this.ThresholdButton.UseVisualStyleBackColor = true;
            this.ThresholdButton.Click += new System.EventHandler(this.ThresholdButton_Click);
            // 
            // DisplayPanel
            // 
            this.DisplayPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DisplayPanel.Location = new System.Drawing.Point(0, 0);
            this.DisplayPanel.Name = "DisplayPanel";
            this.DisplayPanel.Size = new System.Drawing.Size(640, 480);
            this.DisplayPanel.TabIndex = 0;
            this.DisplayPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.DisplayPanel_Paint);
            this.DisplayPanel.MouseClick += new System.Windows.Forms.MouseEventHandler(this.DisplayPanel_MouseClick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(840, 480);
            this.Controls.Add(this.DisplayPanel);
            this.Controls.Add(this.ToolPanel);
            this.KeyPreview = true;
            this.Name = "Form1";
            this.Text = "Kinect People Tracker";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            this.ToolPanel.ResumeLayout(false);
            this.ToolPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private DistanceDemos.DoubleBufferedPanel DisplayPanel;
        private System.Windows.Forms.Panel ToolPanel;
        private System.Windows.Forms.Button ThresholdButton;
        private System.Windows.Forms.CheckBox InvertDirectionCheckbox;
        private System.Windows.Forms.Button ResetCounterButton;
        private System.Windows.Forms.CheckBox InvertSizeCheckbox;
        private System.Windows.Forms.CheckBox SizeCheckbox;
        private System.Windows.Forms.CheckBox ArmCheckbox;
        private System.Windows.Forms.ComboBox PortChooser;
    }
}

