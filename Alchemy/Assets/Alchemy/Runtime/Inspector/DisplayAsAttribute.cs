using System;

namespace Alchemy.Inspector
{
    /// <summary>
    /// Attribute for overriding the name displayed on SerializableReference fields.
    /// </summary>
    public sealed class DisplayAs : Attribute
    {
        public DisplayAs(string title, string description="")
        {
            Title = title;
            Description = description;
        }
        
        public readonly string Title;
        public readonly string Description;
    }
}