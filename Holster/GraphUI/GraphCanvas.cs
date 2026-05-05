using GraphEngine;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace GraphUI
{
    /// <summary>
    /// Graph canvas, inherits from winforms panel.
    /// </summary>
    internal class GraphCanvas : Panel
    {
        // ------------------------------------
        // Fields, properties and our Constructor
        // ------------------------------------
        // our controller object
        private readonly GraphController controller;

        private float zoom = 1.0f;
        private PointF cameraOffset = new PointF(0, 0);
        private int gridSize = 50;
        private const float MinZoom = 0.25f;
        private const float MaxZoom = 3.0f;

        // scaling for vertices/edges/buffers
        private float VertexRadius => 4.5f * zoom;
        private float EdgeThickness => Math.Max(1.0f, 1.5f * zoom);
        private float ArrowSize => 2.5f * zoom;
        private float HitDistance => 10 * zoom;

        // for editing the labels of vertices
        private TextBox? editBox = null;
        private Vertex? editingVertex = null;

        // for drawing our edges
        private Vertex? edgeStartVertex = null;
        private Point currentMousePixel;
        private bool isDrawingEdge = false;

        // for editing edges
        private Edge? editingEdge = null;

        // for selecting edges
        private Vertex? selectedVertex = null;
        private Edge? selectedEdge = null;

        // for the edge editing popup
        private Panel? edgeOptionsPanel = null;

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

        // ------------------------------------
        // Main Event Handlers
        // ------------------------------------

        // event call for GraphChanged
        private void Controller_GraphChanged(object? sender, System.EventArgs e)
        {
            // default function for winforms Panel, marks a "section" as needing to be redrawn/changed.
            this.Invalidate();
        }

        // overrides the default winforms Panel OnPaint function
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics; // makes a "drawing area" for this specific OnPaint event. 
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; // makes edges of shapes not pixelated, but a line instead.

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
                Pan(0, e.Delta > 0 ? 40 : -40); // e.Delta is how we track how far the mouse wheel scrolled and what direction
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

        // event call for when we hold our mouse down, drawing an edge
        private void GraphCanvas_MouseDown(object? sender, MouseEventArgs e)
        {

            // if an edit box is open and click is outside it, finish editing
            if (editBox != null && (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right))
            {
                // Check if click is outside the editBox bounds
                if (!editBox.Bounds.Contains(e.Location))
                {
                    FinishEditingLabel(editBox, EventArgs.Empty);
                    return;
                }
            }

            // if we are not holding left or right mouse button down
            if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right)
            {
                return;
            }

            // new selection stuff! we get the selected vertex first,
            selectedVertex = GetVertexAtPixel(e.Location);

            // set the selected edge to null
            selectedEdge = null;

            // if the vertex is null, get the edge at that pixel. otherwise we proceed with the vertex!
            if (selectedVertex == null)
            {
                selectedEdge = GetEdgeAtPixel(e.Location);
            }

            // if the edge is not null and we right-clicked it, show the panel!
            if (selectedEdge != null && e.Button == MouseButtons.Right)
            {
                ShowEdgeOptionsPanel(e.Location);
            }

            // otherwise hide it
            else
            {
                HideEdgeOptionsPanel();
            }

            this.Invalidate();

            if (e.Button == MouseButtons.Left)
            {
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
            this.Invalidate(); // this is where we are drawing our temporary line
        }

        // event call for when we finish drawing the edge, and release the click.
        private void GraphCanvas_MouseUp(object? sender, MouseEventArgs e)
        {
            // if we are drawing an edge, an the start vertex exists continue
            if (!isDrawingEdge || edgeStartVertex == null)
            {
                return;
            }

            // get the snapped position at the end of our edge
            PointF coordsPoint = PixelsToCoords(e.Location);
            PointF snappedPoint = SnapToGridCellCenter(coordsPoint);

            // try to find an existing vertex at the snapped position
            Vertex? endVertex = GetVertexAtSnappedPosition(snappedPoint);

            if (endVertex == null)
            {
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

        // event for pressing enter to stop editing the box.
        private void EditBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                FinishEditingLabel(sender, e);
            }
        }

        // ------------------------------------
        // General canvas/navigation helpers
        // ------------------------------------

        // used for panning the camera across the graph canvas
        public void Pan(float dx, float dy)
        {
            cameraOffset.X += dx;
            cameraOffset.Y += dy;
            this.Invalidate();
        }

        // zoom in
        public void ZoomIn()
        {
            ZoomAtScreenPoint(
                new PointF(this.Width / 2f, this.Height / 2f),
                1.1f);
        }

        // zoom out
        public void ZoomOut()
        {
            ZoomAtScreenPoint(
                new PointF(this.Width / 2f, this.Height / 2f),
                1f / 1.1f);
        }

        // helper for zoom. Previously our zoom was moving the camera around, so we need to make the camera offset change with the zoom.
        private void ZoomAtScreenPoint(PointF screenPoint, float zoomFactor)
        {
            // get the point where we are zooming
            PointF coordsBeforeZoom = PixelsToCoords(screenPoint);

            zoom *= zoomFactor;

            // guards for max min zoom
            if (zoom > MaxZoom)
            {
                zoom = MaxZoom;
            }

            if (zoom < MinZoom)
            {
                zoom = MinZoom;
            }

            // get the pixels after the zoom
            PointF pixelAfterZoom = CoordsToPixels(coordsBeforeZoom);

            // adjust the camera offset!
            cameraOffset.X += screenPoint.X - pixelAfterZoom.X;
            cameraOffset.Y += screenPoint.Y - pixelAfterZoom.Y;

            this.Invalidate();
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
            // gives us the index of the cell by div then rounding down.
            float cellX = (float)Math.Floor(coordsPoint.X / gridSize);
            float cellY = (float)Math.Floor(coordsPoint.Y / gridSize);

            // Then we take the grid index and mult with the gridsize to get the top left corer of the cell
            // After getting top left, we add half the gridsize, moving to midway through the cell, giving us the center
            float snappedX = cellX * gridSize + gridSize / 2f;
            float snappedY = cellY * gridSize + gridSize / 2f;

            return new PointF(snappedX, snappedY);
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

        // ------------------------------------
        // Selection/delete helpers
        // ------------------------------------

        // using our graphcontroller functions, deletes the selected objects
        public void DeleteSelected()
        {
            if (selectedEdge != null)
            {
                controller.DeleteEdge(selectedEdge);
                selectedEdge = null;
                selectedVertex = null;
                HideEdgeOptionsPanel();
                this.Invalidate();
                return;
            }

            if (selectedVertex != null)
            {
                controller.DeleteVertex(selectedVertex);
                selectedVertex = null;
                selectedEdge = null;
                HideEdgeOptionsPanel();
                this.Invalidate();
            }
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

                if (distance <= HitDistance)
                {
                    return vertex;
                }
            }

            return null;
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

                if (distance <= 8 * zoom)
                {
                    return edge;
                }
            }

            return null;
        }

        // This is an overly complicated way of giving our edges an 8 pixel hitbox that we can click on, instead of the edge itself.
        // I had to search up how to do this bit, so if that makes it not count, just gotta click the lines exactly as they appear.
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

        // ------------------------------------
        // Vertex drawing/editing helpers
        // ------------------------------------

        // Draws the vertices from the controller
        private void DrawVertices(Graphics g)
        {
            foreach (Vertex vertex in controller.Vertices)
            {
                PointF pixelPoint = CoordsToPixels(vertex.Position);

                // new selection stuff, first we check if the vertex is equal to a selected vertex.
                if (vertex == selectedVertex)
                {
                    // get the radius of the highlight
                    float highlightRadius = VertexRadius + 1 * zoom;

                    // if it doesnt have a label, just highlight the usual vertex orb (changed from being an outline, now fills!
                    if (string.IsNullOrWhiteSpace(vertex.Label))
                    {
                        g.FillEllipse(
                            Brushes.Blue,
                            pixelPoint.X - highlightRadius,
                            pixelPoint.Y - highlightRadius,
                            highlightRadius * 2,
                            highlightRadius * 2);
                    }
                    // if it does have a label, change the font to blue and a slight font size increase
                    else
                    {
                        using Font font = new Font("Arial", 16, FontStyle.Bold);
                        SizeF textSize = g.MeasureString(vertex.Label, font);

                        g.DrawString(
                            vertex.Label,
                            font,
                            Brushes.Blue,
                            pixelPoint.X - textSize.Width / 2,
                            pixelPoint.Y - textSize.Height / 2);
                    }
                }
                else
                {
                    // if no label, display as normal
                    if (string.IsNullOrWhiteSpace(vertex.Label))
                    {
                        // changed radius to be scaled with zoom instead of set
                        float radius = VertexRadius;

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

        // helper that lets us not need createGraphics
        private RectangleF GetVertexBounds(Vertex vertex)
        {
            PointF center = CoordsToPixels(vertex.Position);

            if (string.IsNullOrWhiteSpace(vertex.Label))
            {
                float radius = VertexRadius + 2 * zoom;

                return new RectangleF(
                    center.X - radius,
                    center.Y - radius,
                    radius * 2,
                    radius * 2);
            }

            using Font font = new Font("Arial", 14, FontStyle.Bold);
            Size textSize = TextRenderer.MeasureText(vertex.Label, font);

            return new RectangleF(
                center.X - textSize.Width / 2f - 4,
                center.Y - textSize.Height / 2f - 2,
                textSize.Width + 8,
                textSize.Height + 4);
        }

        // helper for the stacked vertex bug. 
        // just checks if there is already a vertex at the position we just tried to "snap" an edge or new vertex too.
        private Vertex? GetVertexAtSnappedPosition(PointF snappedPoint, float tolerance = 0.01f)
        {
            foreach (Vertex vertex in controller.Vertices)
            {
                // tolerance was added so that we would have issues with floats giving long decimals that resulted in 
                // technically being a nonexistent vertex, making this function return that there wasnt a vertex,
                if (Math.Abs(vertex.Position.X - snappedPoint.X) < tolerance &&
                    Math.Abs(vertex.Position.Y - snappedPoint.Y) < tolerance)
                {
                    return vertex;
                }
            }
            return null;
        }

        // ------------------------------------
        // Edge drawing/editing helpers
        // ------------------------------------

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

                // updated pen settings for selected edges being a different color.
                // instead of draw vertices longer if else for being selected, we can just do it shorter with this
                using Pen edgePen = new Pen(
                    edge == selectedEdge ? Color.Blue : Color.Black, // if it is a selected edge, use blue. otherwise, black
                    edge == selectedEdge ? EdgeThickness + 1.5f : EdgeThickness); // if it is a selected edge, make it a bit thicker. otherwise, default

                // if directed edge, add an arrow
                if (edge.IsDirected)
                {
                    edgePen.CustomEndCap =
                        new System.Drawing.Drawing2D.AdjustableArrowCap(ArrowSize, ArrowSize);
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

        // just like with the vertices, we start editing an edge.
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

        // Shows the Edge Panel. Details within
        private void ShowEdgeOptionsPanel(Point location)
        {
            // first hides any previous one
            HideEdgeOptionsPanel();

            // making a new Panel object. Edit later for prettiness
            edgeOptionsPanel = new Panel();
            edgeOptionsPanel.Width = 150;
            edgeOptionsPanel.Height = 130;
            edgeOptionsPanel.BackColor = Color.White;
            edgeOptionsPanel.BorderStyle = BorderStyle.FixedSingle;
            edgeOptionsPanel.Location = new Point(location.X + 10, location.Y + 10);

            // makes the button that calls our reverse function in controller.
            Button reverseButton = new Button();
            reverseButton.Text = "Reverse";
            reverseButton.Width = 130;
            reverseButton.Height = 50;
            reverseButton.Location = new Point(10, 8);

            // if the newly created button is clicked, we call reverse edge in the controller.
            reverseButton.Click += (sender, e) =>
            {
                if (selectedEdge != null)
                {
                    controller.ReverseEdge(selectedEdge);
                }
            };

            // adding a toggle button.
            Button toggleButton = new Button();
            toggleButton.Text = "Toggle Directed";
            toggleButton.Width = 130;
            toggleButton.Height = 50;
            toggleButton.Location = new Point(10, 60);

            // if clicked, call controllers toggle directed edge.
            toggleButton.Click += (sender, e) =>
            {
                if (selectedEdge != null)
                {
                    controller.ToggleEdgeDirected(selectedEdge);
                }
            };

            // adds both buttons to controls
            edgeOptionsPanel.Controls.Add(reverseButton);
            edgeOptionsPanel.Controls.Add(toggleButton);

            // adds the panel and brings it to the front.
            this.Controls.Add(edgeOptionsPanel);
            edgeOptionsPanel.BringToFront();
        }

        // hides the edge panel. just removes it from controls and disposes.
        private void HideEdgeOptionsPanel()
        {
            if (edgeOptionsPanel != null)
            {
                this.Controls.Remove(edgeOptionsPanel);
                edgeOptionsPanel.Dispose();
                edgeOptionsPanel = null;
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

        // ------------------------------------
        // Adjacency matrix stuff
        // ------------------------------------

        // OUR NEW STUFF! generating a graph from inputted adjacency matrix:
        public void GenerateFromAdjacencyMatrix(string input)
        {
            // first we parse our adjacency matrix into our matrix object
            int[,] matrix = controller.ParseAdjacencyMatrix(input);

            // empty whatever was previously on the graph
            controller.ClearGraph();

            // how many vertices do we have?
            int vertexCount = matrix.GetLength(0);

            // we make our graph in a circular layout, this gets the center and radius.
            PointF screenCenter = new PointF(this.Width / 2f, this.Height / 2f);
            PointF coordsCenter = PixelsToCoords(screenCenter);
            
            // makes a wider circle for our vertices depending on how big the gridsize is.
            float layoutRadius = gridSize * Math.Max(2, vertexCount / 2);

            // this is how we keep track of the vertices we have made so they can be referenced later
            List<Vertex> generatedVertices = new List<Vertex>();

            // create all of our vertexs
            for (int i = 0; i < vertexCount; i++)
            {
                //space our vertices out in a circle

                // first we divide 360 degrees by the vertex count, we see how far apart each vertex
                // if going to be spaced across the circle. So how many slices we need
                double angle = 2 * Math.PI * i / vertexCount;

                // using cos on the angel to find the x pos, and sin for the y pos, then mult by layout radius to scale with the circles size
                // we get our vertex points! (coords center is so its from center of screen, NOT (0,0)
                float x = coordsCenter.X + layoutRadius * (float)Math.Cos(angle);
                float y = coordsCenter.Y + layoutRadius * (float)Math.Sin(angle);

                // where we put the vertex
                PointF snappedPoint = SnapToGridCellCenter(new PointF(x, y));

                // create a new vertex at that point, and label it
                Vertex vertex = controller.AddVertex(snappedPoint);
                vertex.Label = "v" + (i + 1);
                
                // add it to our generated vertices
                generatedVertices.Add(vertex);
            }

            // create all of our edges
            for (int row = 0; row < vertexCount; row++)
            {
                for (int col = 0; col < vertexCount; col++)
                {
                    // if an edge exists from the vertex row to the vertex col, we create it.
                    if (matrix[row, col] == 1)
                    {
                        // create the edge
                        controller.AddEdge(generatedVertices[row], generatedVertices[col]);
                    }
                }
            }

            this.Invalidate();
        }
    }
}
