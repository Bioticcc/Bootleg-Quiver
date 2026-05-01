using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace GraphEngine
{
    /// <summary>
    /// Controller for the logic behind the graph.
    /// </summary>
    public class GraphController
    {
        // ------------------------------------
        // Fields and properties
        // ------------------------------------

        // list of vertices
        public List<Vertex> Vertices { get; } = new();
        
        // list of edges
        public List<Edge> Edges { get; } = new();

        // undo stack
        private Stack<UndoAction> undoStack = new();

        // event for changing something on the graph
        public event EventHandler? GraphChanged;

        // ------------------------------------
        // Helpers
        // ------------------------------------

        // adding a vertex to the given position.
        public Vertex AddVertex(PointF position)
        {
            Vertex vertex = new Vertex(position);
            Vertices.Add(vertex);

            // Adding a lambda function to our undo stack that undos the action we just did,
            // so we dont have to have some gobbledegook named functions and helpers.
            undoStack.Push(new UndoAction("Add Vertex", () => 
            { 
                Vertices.Remove(vertex); 
            }));

            NotifyGraphChanged();
            return vertex;
        }

        // adding an edge between two vertices.
        public Edge AddEdge(Vertex start, Vertex end)
        {
            Edge edge = new Edge(start, end);
            Edges.Add(edge);

            // same lambda push as above.
            undoStack.Push(new UndoAction("Add Edge", () => 
            { 
                Edges.Remove(edge); 
            }));

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

            undoStack.Push(new UndoAction("Rename Vertex", () => 
            { 
                vertex.Label = oldLabel; 
            }));

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

            undoStack.Push(new UndoAction("Rename Edge", () => 
            { 
                edge.Label = oldLabel; 
            }));

            NotifyGraphChanged();
        }

        // reverse an edge, just switches what the edges start and end vertices are.
        public void ReverseEdge(Edge edge)
        {
            if (!Edges.Contains(edge))
            {
                return;
            }

            Vertex oldStart = edge.Start;
            Vertex oldEnd = edge.End;

            edge.Start = oldEnd;
            edge.End = oldStart;

            undoStack.Push(new UndoAction("Reverse Edge", () =>
            {
                edge.Start = oldStart;
                edge.End = oldEnd;
            }));

            NotifyGraphChanged();
        }

        // toggles wether or not an edge is considered directed.
        public void ToggleEdgeDirected(Edge edge)
        {
            if (!Edges.Contains(edge))
            {
                return;
            }

            bool oldValue = edge.IsDirected;

            edge.IsDirected = !edge.IsDirected;

            undoStack.Push(new UndoAction("Toggle Edge Directed", () =>
            {
                edge.IsDirected = oldValue;
            }));

            NotifyGraphChanged();
        }

        // Here we delete the selected edge/edges.
        public void DeleteEdge(Edge edge)
        {
            if (!Edges.Contains(edge))
            {
                return;
            }

            Edges.Remove(edge);

            undoStack.Push(new UndoAction("Delete Edge", () => 
            { 
                Edges.Add(edge); 
            }));

            NotifyGraphChanged();
        }

        // here we delete the selected vertex, and any connected edges.
        public void DeleteVertex(Vertex vertex)
        {
            if (!Vertices.Contains(vertex))
            {
                return;
            }

            // the list of edges that connect to our vertex that we want to delete.
            List<Edge> connectedEdges = Edges.Where(edge => edge.Start == vertex || edge.End == vertex).ToList();

            // remove from our list of vertices
            Vertices.Remove(vertex);

            // remove each connected edge
            foreach (Edge edge in connectedEdges)
            {
                Edges.Remove(edge);
            }

            // push the 
            undoStack.Push(new UndoAction("Delete Vertex", () =>
            {
                Vertices.Add(vertex);

                foreach (Edge edge in connectedEdges)
                {
                    Edges.Add(edge);
                }
            }));

            NotifyGraphChanged();
        }

        // clears the existing graph for when we load in new data.
        public void ClearGraph()
        {
            Vertices.Clear();
            Edges.Clear();
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

        // ------------------------------------
        // Parsing function
        // ------------------------------------

        // our parser for the input string for the adjacency matrix.
        public int[,] ParseAdjacencyMatrix(string input)
        {
            // removes whitespace at ends
            input = input.Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                throw new Exception("Matrix input cannot be empty.");
            }

            // split the input into rowstrings by chopping at each closing matrice bracket and comma.
            string[] rowStrings = input.Split(
                new string[] { "]," },
                StringSplitOptions.RemoveEmptyEntries);

            // our rows
            List<int[]> rows = new List<int[]>();

            foreach (string rawRow in rowStrings)
            {
                // get rid of any remaining brackets and replace with empty. trim whitespace again.
                string rowText = rawRow
                    .Replace("[", "")
                    .Replace("]", "")
                    .Trim();

                // split by comma, for each int in the row
                string[] valueStrings = rowText.Split(
                    new char[] { ',' },
                    StringSplitOptions.RemoveEmptyEntries);

                // our rowValues array of integers
                int[] rowValues = new int[valueStrings.Length];

                for (int i = 0; i < valueStrings.Length; i++)
                {
                    // cleans the whitespace again
                    string valueText = valueStrings[i].Trim();

                    // then trys to get the integer, otherwise complain
                    if (!int.TryParse(valueText, out int value))
                    {
                        throw new Exception("Matrix entries must be integers.");
                    }

                    // if its not 0 or 1 complain
                    if (value != 0 && value != 1)
                    {
                        throw new Exception("Matrix entries must be only 0 or 1.");
                    }

                    // sets the rowvalue for that index to the value.
                    rowValues[i] = value;
                }
                
                // once we get the full row parsed, add it to rows.
                rows.Add(rowValues);
            }

            // ours row and col counts
            int rowCount = rows.Count;
            int columnCount = rows[0].Length;

            // if its not a square, complain
            if (rowCount != columnCount)
            {
                throw new Exception("Adjacency matrix must be square.");
            }

            // creating our 2d matrix
            int[,] matrix = new int[rowCount, columnCount];

            for (int row = 0; row < rowCount; row++)
            {
                // if any row does not have the correct num of columns, complain.
                if (rows[row].Length != columnCount)
                {
                    throw new Exception("Every row must have the same number of entries.");
                }

                for (int col = 0; col < columnCount; col++)
                {
                    // if has loops, complain. (havent added this yet. Unsure if will have time.
                    if (row == col && rows[row][col] != 0)
                    {
                        throw new Exception("Loops are not currently supported, so diagonal entries must be 0.");
                    }

                    matrix[row, col] = rows[row][col];
                }
            }

            // return our parsed matrix!
            return matrix;
        }

        // ------------------------------------
        // Events
        // ------------------------------------
        private void NotifyGraphChanged()
        {
            GraphChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
