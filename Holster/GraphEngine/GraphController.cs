    using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace GraphEngine
{
    /// <summary>
    /// Controller for the logic behind the graph.
    /// </summary>
    public class GraphController
    {
        // list of vertices
        public List<Vertex> Vertices { get; } = new();
        
        // list of edges
        public List<Edge> Edges { get; } = new();

        // undo stack
        private Stack<UndoAction> undoStack = new();

        // event for changing something on the graph
        public event EventHandler GraphChanged;

        // adding a vertex to the given position.
        public Vertex AddVertex(PointF position)
        {
            Vertex vertex = new Vertex(position);
            Vertices.Add(vertex);

            // Adding a lambda function to our undo stack that undos the action we just did,
            // so we dont have to have some gobbledegook named functions and helpers.
            undoStack.Push(new UndoAction("Add Vertex", () => { Vertices.Remove(vertex); }));

            NotifyGraphChanged();
            return vertex;
        }

        // adding an edge between two vertices.
        public Edge AddEdge(Vertex start, Vertex end)
        {
            Edge edge = new Edge(start, end);
            Edges.Add(edge);

            // same lambda push as above.
            undoStack.Push(new UndoAction("Add Edge", () => { Edges.Remove(edge); }));

            NotifyGraphChanged();
            return edge;
        }

        // renaming a vertex
        public void RenameVertex(Vertex vertex, string newLabel)
        {
            string oldLabel = vertex.Label;

            if (oldLabel == newLabel)
            {
                return;
            }

            vertex.Label = newLabel;

            undoStack.Push(new UndoAction("Rename Vertex", () => { vertex.Label = oldLabel; }));

            NotifyGraphChanged();
        }

        // renaming an edge
        public void RenameEdge(Edge edge, string newLabel)
        {
            string oldLabel = edge.Label;

            if (oldLabel == newLabel)
            {
                return;
            }

            edge.Label = newLabel;

            undoStack.Push(new UndoAction("Rename Edge", () => { edge.Label = oldLabel; }));

            NotifyGraphChanged();
        }

        // our Undo action
        public void Undo()
        {
            if (undoStack.Count == 0)
            {
                return;
            }

            // get the undo action
            UndoAction action = undoStack.Pop();
            
            // call its personal undo
            action.Undo();

            NotifyGraphChanged();
        }

        // Events
        private void NotifyGraphChanged()
        {
            GraphChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
