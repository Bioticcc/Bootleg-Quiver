using System.Drawing;

namespace GraphEngine
{
    /// <summary>
    /// A vertex!
    /// </summary>
    public class Vertex
    {
        public PointF Position { get; set; }
        public string Label { get; set; } = "";

        public Vertex(PointF position)
        {
            Position = position;
        }
    }
}