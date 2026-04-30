using GraphEngine;
using System.Drawing;
using System.Windows.Forms;

namespace GraphUI
{
    /// <summary>
    /// Graph canvas, inherits from winforms panel.
    /// </summary>
    internal class GraphCanvas : Panel
    {
        // our controller object
        private readonly GraphController controller;

        private float zoom = 1.0f;
        private PointF cameraOffset = new PointF(0, 0);
        private int gridSize = 50;

        // for editing the labels of vertices
        private TextBox? editBox = null;
        private Vertex? editingVertex = null;

        // for drawing our edges
        private Vertex? edgeStartVertex = null;
        private Point currentMousePixel;
        private bool isDrawingEdge = false;

        // for editing edges
        private Edge? editingEdge = null;


        // constructor with given controller.
        public GraphCanvas(GraphController controller)
        {
            this.controller = controller;

            this.DoubleBuffered = true; // this prevents flickering everytime we make a change
            this.BackColor = Color.White;
            this.TabStop = true; // just lets us use arrows when in focus

            // subscribing to events
            this.controller.GraphChanged += Controller_GraphChanged;
            this.MouseEnter += (sender, e) => this.Focus();
            this.MouseDoubleClick += GraphCanvas_MouseDoubleClick;
            this.MouseDown += GraphCanvas_MouseDown;
            this.MouseMove += GraphCanvas_MouseMove;
            this.MouseUp += GraphCanvas_MouseUp;
        }

        // event call for GraphChanged
        private void Controller_GraphChanged(object? sender, System.EventArgs e)
        {
            // default function for winforms Panel, marks a "section" as needing to be redrawn/changed.
            this.Invalidate();
        }

        // event call for MouseDoubleCLick, does various things. see within

        private void GraphCanvas_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            // the vertex we clicked, if at all
            Vertex? clickedVertex = GetVertexAtPixel(e.Location);

            if (clickedVertex != null)
            {
                StartEditingVertex(clickedVertex);
                return;
            }

            // the edge we clicked, if at all
            Edge? clickedEdge = GetEdgeAtPixel(e.Location);

            if (clickedEdge != null)
            {
                StartEditingEdge(clickedEdge, e.Location);
                return;
            }

            PointF coordsPoint = PixelsToCoords(e.Location);
            PointF snappedPoint = SnapToGridCellCenter(coordsPoint);

            controller.AddVertex(snappedPoint);
        }

        // event call for when we hold our mouse down, drawing an edge

        private void GraphCanvas_MouseDown(object? sender, MouseEventArgs e)
        {
            // if we are holding left mouse button down
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            // we get the vertex at the initial clicks location
            Vertex? clickedVertex = GetVertexAtPixel(e.Location);

            // if it exists, set the start vertex, current loc, and bool
            if (clickedVertex != null)
            {
                edgeStartVertex = clickedVertex;
                currentMousePixel = e.Location;
                isDrawingEdge = true;
            }
        }

        // event call for when we move the mouse while drawing an edge is true
        private void GraphCanvas_MouseMove(object? sender, MouseEventArgs e)
        {
            // if we are drawing an edge, continue
            if (!isDrawingEdge)
            {
                return;
            }

            // update the invalidate function with the location, telling th UI this section needs updates
            currentMousePixel = e.Location;
            this.Invalidate();
        }

        // event call for when we finish drawing the edge, and release the click.
        private void GraphCanvas_MouseUp(object? sender, MouseEventArgs e)
        {
            // if we are drawing an edge, an the start vertex exists continue
            if (!isDrawingEdge || edgeStartVertex == null)
            {
                return;
            }

            // get the vertex at the end of our edge
            Vertex? endVertex = GetVertexAtPixel(e.Location);

            // if it doesnt exist, make a new one at the center of the cell.
            if (endVertex == null)
            {
                PointF coordsPoint = PixelsToCoords(e.Location);
                PointF snappedPoint = SnapToGridCellCenter(coordsPoint);

                endVertex = controller.AddVertex(snappedPoint);
            }
           
            // if it does exist, add the edge to teh controller.
            if (endVertex != edgeStartVertex)
            {
                controller.AddEdge(edgeStartVertex, endVertex);
            }

            edgeStartVertex = null;
            isDrawingEdge = false;

            this.Invalidate();
        }

        // used for panning the camera across the graph canvas
        public void Pan(float dx, float dy)
        {
            cameraOffset.X += dx;
            cameraOffset.Y += dy;
            this.Invalidate();
        }

        // zooms in 
        public void ZoomIn()
        {
            zoom *= 1.1f;
            this.Invalidate();
        }

        // zooms out
        public void ZoomOut()
        {
            zoom /= 1.1f;
            this.Invalidate();
        }

        // overrides the default winforms Panel OnPaint function
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            DrawGrid(g);
            DrawEdges(g);
            DrawTemporaryEdge(g); // the edge that displays when we draw the mouse
            DrawVertices(g);
        }

        // overrides the default OnMouseWheel for our panning/zooming stuff.
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                Pan(0, e.Delta > 0 ? 40 : -40);
            }
            else if ((ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                Pan(e.Delta > 0 ? 40 : -40, 0);
            }
            else
            {
                if (e.Delta > 0)
                {
                    ZoomIn();
                }
                else
                {
                    ZoomOut();
                }
            }
        }

        // Draws the grid we use for the graph
        private void DrawGrid(Graphics g)
        {
            using Pen gridPen = new Pen(Color.LightGray, 1);

            float scaledGridSize = gridSize * zoom;

            // this is just so we dont draw on a super zoomed out grid.
            if (scaledGridSize < 5)
            {
                return;
            }

            // keeps grid aligned while we pan instead of jumping to different spots
            float startX = cameraOffset.X % scaledGridSize;
            float startY = cameraOffset.Y % scaledGridSize;

            // draws the vertical lines
            for (float x = startX; x < this.Width; x += scaledGridSize)
            {
                g.DrawLine(gridPen, x, 0, x, this.Height);
            }

            // draws the horizontal lines. now we have a grid!
            for (float y = startY; y < this.Height; y += scaledGridSize)
            {
                g.DrawLine(gridPen, 0, y, this.Width, y);
            }
        }

        // Draws the vertices from the controller
        private void DrawVertices(Graphics g)
        {
            foreach (Vertex vertex in controller.Vertices)
            {
                PointF pixelPoint = CoordsToPixels(vertex.Position);

                // if no label, display as normal
                if (string.IsNullOrWhiteSpace(vertex.Label))
                {
                    float radius = 7;

                    g.FillEllipse(
                        Brushes.Black,
                        pixelPoint.X - radius,
                        pixelPoint.Y - radius,
                        radius * 2,
                        radius * 2);
                }
                else
                {
                    // other wise we display the label!
                    using Font font = new Font("Arial", 14, FontStyle.Bold);
                    SizeF textSize = g.MeasureString(vertex.Label, font);

                    g.DrawString(
                        vertex.Label,
                        font,
                        Brushes.Black,
                        pixelPoint.X - textSize.Width / 2,
                        pixelPoint.Y - textSize.Height / 2);
                }
            }
        }

        // draws the edges
        private void DrawEdges(Graphics g)
        {
            foreach (Edge edge in controller.Edges)
            {
                // coverts both vertices from graph coords to pixels (the centers of the vertice)
                PointF startCenter = CoordsToPixels(edge.Start.Position);
                PointF endCenter = CoordsToPixels(edge.End.Position);

                // previously we drew the lines from center to center, but now we have a small buffer
                // for the arrow/text
                PointF start = GetEdgePointOutsideVertex(edge.Start, endCenter);
                PointF end = GetEdgePointOutsideVertex(edge.End, startCenter);
                
                using Pen edgePen = new Pen(Color.Black, 2);

                // if directed edge, add an arrow
                if (edge.IsDirected)
                {
                    edgePen.CustomEndCap =
                        new System.Drawing.Drawing2D.AdjustableArrowCap(5, 5);
                }

                g.DrawLine(edgePen, start, end);

                if (!string.IsNullOrWhiteSpace(edge.Label))
                {
                    DrawEdgeLabel(g, edge, start, end);
                }
            }
        }

        // draws a temp edge to follow the mouse.
        private void DrawTemporaryEdge(Graphics g)
        {
            if (!isDrawingEdge || edgeStartVertex == null)
            {
                return;
            }

            using Pen tempPen = new Pen(Color.DarkGray, 2);

            PointF startPixel = CoordsToPixels(edgeStartVertex.Position);
            PointF endPixel = currentMousePixel;

            g.DrawLine(tempPen, startPixel, endPixel);
        }

        // helper function to get that buffer between the vertex and edge.
        private PointF GetEdgePointOutsideVertex(Vertex vertex, PointF towardPoint)
        {
            RectangleF bounds = GetVertexBounds(vertex);

            float centerX = bounds.X + bounds.Width / 2f;
            float centerY = bounds.Y + bounds.Height / 2f;

            float dx = towardPoint.X - centerX;
            float dy = towardPoint.Y - centerY;

            if (dx == 0 && dy == 0)
            {
                return new PointF(centerX, centerY);
            }

            float scaleX = bounds.Width / 2f / Math.Abs(dx);
            float scaleY = bounds.Height / 2f / Math.Abs(dy);

            float scale = Math.Min(scaleX, scaleY);

            return new PointF(
                centerX + dx * scale,
                centerY + dy * scale);
        }

        // graoh coords to mouse pos (pixels)
        private PointF CoordsToPixels(PointF worldPoint)
        {
            return new PointF(
                worldPoint.X * zoom + cameraOffset.X,
                worldPoint.Y * zoom + cameraOffset.Y);
        }
        
        // mouse pos (pixels) to graph coords
        private PointF PixelsToCoords(PointF screenPoint)
        {
            return new PointF(
                (screenPoint.X - cameraOffset.X) / zoom,
                (screenPoint.Y - cameraOffset.Y) / zoom);
        }

        // helper function that just makes our vertex appear in the center, of the cell, not on the lines.
        private PointF SnapToGridCellCenter(PointF coordsPoint)
        {
            float cellX = (float)Math.Floor(coordsPoint.X / gridSize);
            float cellY = (float)Math.Floor(coordsPoint.Y / gridSize);

            float snappedX = cellX * gridSize + gridSize / 2f;
            float snappedY = cellY * gridSize + gridSize / 2f;

            return new PointF(snappedX, snappedY);
        }

        // vertex hit box! its just for if we are clicking an existing  vertex or not.
        private Vertex? GetVertexAtPixel(Point pixelPoint)
        {
            foreach (Vertex vertex in controller.Vertices)
            {
                PointF vertexPixel = CoordsToPixels(vertex.Position);

                float dx = pixelPoint.X - vertexPixel.X;
                float dy = pixelPoint.Y - vertexPixel.Y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                if (distance <= 12)
                {
                    return vertex;
                }
            }

            return null;
        }

        // We started editing a vertex, so we make a text box, get its input, and set the vertex label
        private void StartEditingVertex(Vertex vertex)
        {
            // our bool for if we are editing or not
            editingVertex = vertex;

            // gets the vertex local
            PointF pixelPoint = CoordsToPixels(vertex.Position);

            // the box we actuall input our edit into
            editBox = new TextBox();
            editBox.Text = vertex.Label;
            editBox.Width = 80;
            editBox.Height = 25;
            editBox.Location = new Point(
                (int)(pixelPoint.X - editBox.Width / 2),
                (int)(pixelPoint.Y - editBox.Height / 2));

            // stops editing once we press enter
            editBox.KeyDown += EditBox_KeyDown;

            // adds the edited box.
            this.Controls.Add(editBox);
            editBox.Focus();
            editBox.SelectAll();
        }

        // event for pressing enter to stop editing the box.
        private void EditBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                FinishEditingLabel(sender, e);
            }
        }

        // we finish editing the vertex/label
        private void FinishEditingLabel(object? sender, EventArgs e)
        {
            if (editBox == null)
            {
                return;
            }

            TextBox boxToRemove = editBox;
            Vertex? vertexToRename = editingVertex;
            Edge? edgeToRename = editingEdge;

            editBox = null;
            editingVertex = null;
            editingEdge = null;

            if (vertexToRename != null)
            {
                controller.RenameVertex(vertexToRename, boxToRemove.Text);
            }

            if (edgeToRename != null)
            {
                controller.RenameEdge(edgeToRename, boxToRemove.Text);
            }

            boxToRemove.KeyDown -= EditBox_KeyDown;

            this.Controls.Remove(boxToRemove);
            boxToRemove.Dispose();

            this.Invalidate();
        }

        // edge hit detection!
        private Edge? GetEdgeAtPixel(Point pixelPoint)
        {
            foreach (Edge edge in controller.Edges)
            {
                PointF startCenter = CoordsToPixels(edge.Start.Position);
                PointF endCenter = CoordsToPixels(edge.End.Position);
                
                PointF start = GetEdgePointOutsideVertex(edge.Start, endCenter);
                PointF end = GetEdgePointOutsideVertex(edge.End, startCenter);
                
                float distance = DistanceFromPointToLineSegment(pixelPoint, start, end);

                if (distance <= 8)
                {
                    return edge;
                }
            }

            return null;
        }


        private float DistanceFromPointToLineSegment(PointF point, PointF lineStart, PointF lineEnd)
        {
            float dx = lineEnd.X - lineStart.X;
            float dy = lineEnd.Y - lineStart.Y;

            if (dx == 0 && dy == 0)
            {
                float px = point.X - lineStart.X;
                float py = point.Y - lineStart.Y;
                return (float)Math.Sqrt(px * px + py * py);
            }

            float t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));

            float closestX = lineStart.X + t * dx;
            float closestY = lineStart.Y + t * dy;

            float distX = point.X - closestX;
            float distY = point.Y - closestY;

            return (float)Math.Sqrt(distX * distX + distY * distY);
        }
        
        // just like with the vertices, we start editing an edge
        private void StartEditingEdge(Edge edge, Point pixelPoint)
        {
            editingEdge = edge;
            editingVertex = null;

            editBox = new TextBox();
            editBox.Text = edge.Label;
            editBox.Width = 80;
            editBox.Height = 25;
            editBox.Location = new Point(
                pixelPoint.X - editBox.Width / 2,
                pixelPoint.Y - editBox.Height / 2);

            editBox.KeyDown += EditBox_KeyDown;

            this.Controls.Add(editBox);
            editBox.Focus();
            editBox.SelectAll();
        }

        // draws the label of the edge.
        private void DrawEdgeLabel(Graphics g, Edge edge, PointF start, PointF end)
        {
            using Font font = new Font("Arial", 12, FontStyle.Bold);

            float midX = (start.X + end.X) / 2f;
            float midY = (start.Y + end.Y) / 2f;

            SizeF textSize = g.MeasureString(edge.Label, font);

            RectangleF background = new RectangleF(
                midX - textSize.Width / 2f - 4,
                midY - textSize.Height / 2f - 2,
                textSize.Width + 8,
                textSize.Height + 4);

            using Brush backgroundBrush = new SolidBrush(Color.White);
            g.FillRectangle(backgroundBrush, background);

            g.DrawString(
                edge.Label,
                font,
                Brushes.Black,
                midX - textSize.Width / 2f,
                midY - textSize.Height / 2f);

        }

        // helper that lets us not need createGraphics
        private RectangleF GetVertexBounds(Vertex vertex)
        {
            PointF center = CoordsToPixels(vertex.Position);

            if (string.IsNullOrWhiteSpace(vertex.Label))
            {
                return new RectangleF(center.X - 10, center.Y - 10, 20, 20);
            }

            using Font font = new Font("Arial", 14, FontStyle.Bold);
            Size textSize = TextRenderer.MeasureText(vertex.Label, font);

            return new RectangleF(
                center.X - textSize.Width / 2f - 4,
                center.Y - textSize.Height / 2f - 2,
                textSize.Width + 8,
                textSize.Height + 4);
        }
    }
}
