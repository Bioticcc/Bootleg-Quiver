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
            this.Width = 1000;
            this.Height = 700;
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
        }

        private void GraphWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            // Ctrl + Z undo! very nice
            if (e.Control && e.KeyCode == Keys.Z)
            {
                controller.Undo();
                e.SuppressKeyPress = true;
            }
        }
    }
}
