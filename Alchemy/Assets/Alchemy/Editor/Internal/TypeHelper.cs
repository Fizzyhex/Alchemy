using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alchemy.Editor
{
    internal static class TypeHelper
    {
        public static object GetDefaultValue(Type type)
        {
            if (!type.IsValueType)
            {
                return null;
            }

            return Activator.CreateInstance(type);
        }

        public static object CreateDefaultInstance(Type type)
        {
            if (type == typeof(string)) return "";
            if (type.IsSubclassOf(typeof(UnityEngine.Object))) return null;
            return Activator.CreateInstance(type);
        }

        public static IEnumerable<Type> GetBaseClassesAndInterfaces(Type type, bool includeSelf = false)
        {
            if (includeSelf) yield return type;

            if (type.BaseType == typeof(object))
            {
                foreach (var interfaceType in type.GetInterfaces())
                {
                    yield return interfaceType;
                }
            }
            else
            {
                foreach (var baseType in Enumerable.Repeat(type.BaseType, 1)
                    .Concat(type.GetInterfaces())
                    .Concat(GetBaseClassesAndInterfaces(type.BaseType))
                    .Distinct())
                {
                    yield return baseType;
                }
            }
        }

        public static bool HasDefaultConstructor(Type type)
        {
            return type.GetConstructors().Any(t => t.GetParameters().Count() == 0);
        }

        /// <summary>
        /// Returns the name of a `type` as it would appear in C# code.
        /// <para></para>
        /// For example, typeof(List&lt;float&gt;).FullName would give you:
        /// System.Collections.Generic.List`1[[System.Single, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]
        /// <para></para>
        /// This method would instead return System.Collections.Generic.List&lt;float&gt; if `fullName` is true, or
        /// just List&lt;float&gt; if it is false.
        /// <para></para>
        /// Note that all returned values are stored in a dictionary to speed up repeated use.
        /// </summary>
        public static string GetNameCS(this Type type, bool fullName = true)
        {
            if (type == null)
                return "";

            var text = new StringBuilder();

            if (type.IsArray)// Array = TypeName[].
            {
                text.Append(type.GetElementType().GetNameCS(fullName));

                text.Append('[');
                var dimensions = type.GetArrayRank();
                while (dimensions-- > 1)
                    text.Append(",");
                text.Append(']');

                goto Return;
            }

            if (type.IsPointer)// Pointer = TypeName*.
            {
                text.Append(type.GetElementType().GetNameCS(fullName));
                text.Append('*');

                goto Return;
            }

            if (type.IsGenericParameter)// Generic Parameter = TypeName (for unspecified generic parameters).
            {
                text.Append(type.Name);
                goto Return;
            }

            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)// Nullable = TypeName?.
            {
                text.Append(underlyingType.GetNameCS(fullName));
                text.Append('?');

                goto Return;
            }

            // Other Type = Namespace.NestedTypes.TypeName<GenericArguments>.

            if (fullName && type.Namespace != null)// Namespace.
            {
                text.Append(type.Namespace);
                text.Append('.');
            }

            var genericArguments = 0;

            if (type.DeclaringType != null)// Account for Nested Types.
            {
                // Count the nesting level.
                var nesting = 1;
                var declaringType = type.DeclaringType;
                while (declaringType.DeclaringType != null)
                {
                    declaringType = declaringType.DeclaringType;
                    nesting++;
                }

                // Append the name of each outer type, starting from the outside.
                while (nesting-- > 0)
                {
                    // Walk out to the current nesting level.
                    // This avoids the need to make a list of types in the nest or to insert type names instead of appending them.
                    declaringType = type;
                    for (int i = nesting; i >= 0; i--)
                        declaringType = declaringType.DeclaringType;

                    // Nested Type Name.
                    genericArguments = AppendNameAndGenericArguments(text, declaringType, fullName, genericArguments);
                    text.Append('.');
                }
            }

            // Type Name.
            AppendNameAndGenericArguments(text, type, fullName, genericArguments);

            Return:// Remember and return the name.
            var name = text.ToString();
            return name;
        }
        
        /// <summary>
        /// Appends the generic arguments of `type` (after skipping the specified number).
        /// </summary>
        public static int AppendNameAndGenericArguments(StringBuilder text, Type type, bool fullName = true, int skipGenericArguments = 0)
        {
            text.Append(type.Name);

            if (type.IsGenericType)
            {
                var backQuote = type.Name.IndexOf('`');
                if (backQuote >= 0)
                {
                    text.Length -= type.Name.Length - backQuote;

                    var genericArguments = type.GetGenericArguments();
                    if (skipGenericArguments < genericArguments.Length)
                    {
                        text.Append('<');

                        var firstArgument = genericArguments[skipGenericArguments];
                        skipGenericArguments++;

                        if (firstArgument.IsGenericParameter)
                        {
                            while (skipGenericArguments < genericArguments.Length)
                            {
                                text.Append(',');
                                skipGenericArguments++;
                            }
                        }
                        else
                        {
                            text.Append(firstArgument.GetNameCS(fullName));

                            while (skipGenericArguments < genericArguments.Length)
                            {
                                text.Append(", ");
                                text.Append(genericArguments[skipGenericArguments].GetNameCS(fullName));
                                skipGenericArguments++;
                            }
                        }

                        text.Append('>');
                    }
                }
            }

            return skipGenericArguments;
        }
    }
}
