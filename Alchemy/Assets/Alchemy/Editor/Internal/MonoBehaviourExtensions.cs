namespace Alchemy.Editor
{
    public static class MonoBehaviourExtensions
    {
        public static string GetDisplayName(this MonoBehaviour target) {
            Type type = target.GetType();
            AddComponentMenu attr = type.GetCustomAttribute<AddComponentMenu>();

            if (attr != null && !string.IsNullOrEmpty(attr.componentMenu)) {
                // Returns the full path (e.g., "Physics/My Script")
                return attr.componentMenu.Split("/").Last();
            }

            // Fallback on the type name
            return type.Name;
        }
    }
}