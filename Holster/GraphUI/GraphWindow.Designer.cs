namespace GraphUI
{
    partial class GraphWindow
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            menuStrip1 = new MenuStrip();
            graphToolStripMenuItem = new ToolStripMenuItem();
            generateGraphFromAdjacencyMatrixToolStripMenuItem = new ToolStripMenuItem();
            matrixInputPanel = new Panel();
            generateMatrixButton = new Button();
            matrixInputTextBox = new RichTextBox();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            closePanel = new Button();
            menuStrip1.SuspendLayout();
            matrixInputPanel.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(28, 28);
            menuStrip1.Items.AddRange(new ToolStripItem[] { graphToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(800, 38);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // graphToolStripMenuItem
            // 
            graphToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { generateGraphFromAdjacencyMatrixToolStripMenuItem });
            graphToolStripMenuItem.Name = "graphToolStripMenuItem";
            graphToolStripMenuItem.Size = new Size(87, 34);
            graphToolStripMenuItem.Text = "Graph";
            // 
            // generateGraphFromAdjacencyMatrixToolStripMenuItem
            // 
            generateGraphFromAdjacencyMatrixToolStripMenuItem.Name = "generateGraphFromAdjacencyMatrixToolStripMenuItem";
            generateGraphFromAdjacencyMatrixToolStripMenuItem.Size = new Size(487, 40);
            generateGraphFromAdjacencyMatrixToolStripMenuItem.Text = "Generate graph from adjacency matrix";
            generateGraphFromAdjacencyMatrixToolStripMenuItem.Click += generateGraphFromAdjacencyMatrixToolStripMenuItem_Click;
            // 
            // matrixInputPanel
            // 
            matrixInputPanel.Controls.Add(closePanel);
            matrixInputPanel.Controls.Add(generateMatrixButton);
            matrixInputPanel.Controls.Add(matrixInputTextBox);
            matrixInputPanel.Controls.Add(label4);
            matrixInputPanel.Controls.Add(label3);
            matrixInputPanel.Controls.Add(label2);
            matrixInputPanel.Controls.Add(label1);
            matrixInputPanel.Location = new Point(0, 41);
            matrixInputPanel.Name = "matrixInputPanel";
            matrixInputPanel.Size = new Size(692, 350);
            matrixInputPanel.TabIndex = 1;
            // 
            // generateMatrixButton
            // 
            generateMatrixButton.Location = new Point(12, 296);
            generateMatrixButton.Name = "generateMatrixButton";
            generateMatrixButton.Size = new Size(177, 40);
            generateMatrixButton.TabIndex = 2;
            generateMatrixButton.Text = "Generate Graph";
            generateMatrixButton.UseVisualStyleBackColor = true;
            generateMatrixButton.Click += generateMatrixButton_Click;
            // 
            // matrixInputTextBox
            // 
            matrixInputTextBox.Location = new Point(12, 136);
            matrixInputTextBox.Name = "matrixInputTextBox";
            matrixInputTextBox.ScrollBars = RichTextBoxScrollBars.Vertical;
            matrixInputTextBox.Size = new Size(658, 143);
            matrixInputTextBox.TabIndex = 4;
            matrixInputTextBox.Text = "";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 103);
            label4.Name = "label4";
            label4.Size = new Size(56, 30);
            label4.TabIndex = 3;
            label4.Text = "Etc...";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 43);
            label3.Name = "label3";
            label3.Size = new Size(335, 30);
            label3.TabIndex = 2;
            label3.Text = "For a 2x2 matrix: \"[a1, b1], [a2, b2]\"";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 73);
            label2.Name = "label2";
            label2.Size = new Size(504, 30);
            label2.TabIndex = 1;
            label2.Text = "For a 3x3 matrix: \"[a1, b1, c1], [a2, b2, c2], a3, b3, c3]\"";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 14);
            label1.Name = "label1";
            label1.Size = new Size(297, 30);
            label1.TabIndex = 0;
            label1.Text = "Enter you matrix in the format:";
            // 
            // closePanel
            // 
            closePanel.Location = new Point(539, 296);
            closePanel.Name = "closePanel";
            closePanel.Size = new Size(131, 40);
            closePanel.TabIndex = 5;
            closePanel.Text = "Close";
            closePanel.UseVisualStyleBackColor = true;
            closePanel.Click += closePanel_Click;
            // 
            // GraphWindow
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(matrixInputPanel);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "GraphWindow";
            Text = "Form1";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            matrixInputPanel.ResumeLayout(false);
            matrixInputPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem graphToolStripMenuItem;
        private ToolStripMenuItem generateGraphFromAdjacencyMatrixToolStripMenuItem;
        private Panel matrixInputPanel;
        private Label label4;
        private Label label3;
        private Label label2;
        private Label label1;
        private RichTextBox matrixInputTextBox;
        private Button generateMatrixButton;
        private Button closePanel;
    }
}
