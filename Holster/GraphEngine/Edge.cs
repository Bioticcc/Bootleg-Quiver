namespace GraphEngine
{
    /// <summary>
    /// An Edge!
    /// </summary>
    public class Edge
    {
        public Vertex Start { get; set; }
        public Vertex End { get; set; }

        public bool IsDirected { get; set; } = true;
        public string Label { get; set; } = "";

        public Edge(Vertex start, Vertex end)
        {
            Start = start;
            End = end;
        }
    }
}