using System;
using System.Collections.Generic;
using System.Text;

namespace GraphEngine
{
    /// <summary>
    /// An undo action, how we keep track of what happened, and what needs changing. Will make fancier later,
    /// for now barebones.
    /// </summary>
    public class UndoAction
    {
        public string Description { get; set; }
        public Action Undo { get; set; }

        public UndoAction(string description, Action undo)
        {
            Description = description;
            Undo = undo;
        }
    }
}
