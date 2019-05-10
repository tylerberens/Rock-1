using System;

namespace Rock.Blocks
{
    /// <summary>
    /// Identifies a method on an IRockBlockType as being allowed to be called via an API.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage( AttributeTargets.Method )]
    public class BlockActionAttribute : System.Attribute
    {
        /// <summary>
        /// Gets the name of the action.
        /// </summary>
        /// <value>
        /// The name of the action.
        /// </value>
        public string ActionName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockActionAttribute"/> class.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        public BlockActionAttribute( string actionName )
        {
            ActionName = actionName;
        }
    }
}
