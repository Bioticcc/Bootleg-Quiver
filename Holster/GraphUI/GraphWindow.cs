using GraphEngine;
using System;
using System.Windows.Forms;

namespace GraphUI
{
    /// <summary>
    /// Main form for Holster
    /// </summary>
    public partial class GraphWindow : Form
    {
        private GraphController controller; // the controller
        private GraphCanvas canvas; // the canvas

        // Initializes our Form and other pieces.
        public GraphWindow()
        {
            InitializeComponent();

            this.Text = "H.olster, son of Q.uiver";
            this.Width = 1920;
            this.Height = 1080;
            this.KeyPreview = true; // for key presses

            // our controller!
            controller = new GraphController();

            // our canvas!
            canvas = new GraphCanvas(controller);
            canvas.Dock = DockStyle.Fill; // fills window

            // makes the panel appear in the window
            this.Controls.Add(canvas);

            // subscribed event for keypresses
            this.KeyDown += GraphWindow_KeyDown;

            // subscribed event for mouse actions
            this.MouseEnter += (sender, e) => this.Focus();

            // panel for adjacency matrix inputs.
            matrixInputPanel.Visible = false;
        }

        private void GraphWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (matrixInputTextBox.Focused)
            {
                return;
            }

            // ctrl + z undo! very nice
            if (e.Control && e.KeyCode == Keys.Z)
            {
                controller.Undo();
                e.SuppressKeyPress = true;
                return;
            }

            // deletes selected edges! very nice indeed
            if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
            {
                canvas.DeleteSelected();
                e.SuppressKeyPress = true;
                return;
            }
        }

        // auto generated button function for the menu strip. filled with our panel.
        private void generateGraphFromAdjacencyMatrixToolStripMenuItem_Click(object sender, EventArgs e)
        {
            matrixInputPanel.Visible = true;
            matrixInputPanel.BringToFront();

            matrixInputTextBox.Clear();
            matrixInputTextBox.Focus();
        }

        // auto generated generate graph button definition. filled within
        private void generateMatrixButton_Click(object sender, EventArgs e)
        {
            try
            {
                // call our canvas generate function
                canvas.GenerateFromAdjacencyMatrix(matrixInputTextBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Invalid Adjacency Matrix",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // closes our panel.
        private void closePanel_Click(object sender, EventArgs e)
        {
            matrixInputTextBox.Clear();
            matrixInputPanel.Visible = false;
        }
    }
}
