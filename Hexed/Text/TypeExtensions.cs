using System;
using System.Text;

namespace Hexed.Text;

public static class TypeExtensions
{
    extension(Type type)
    {
        public string TypeName()
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(short)) return "short";
            if (type == typeof(long)) return "long";
            if (type == typeof(double)) return "double";
            if (type == typeof(float)) return "float";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type == typeof(void)) return "void";

            var builder = new StringBuilder();

            if (!string.IsNullOrEmpty(type.Namespace) && !type.IsGenericParameter && !type.IsNested)
            {
                builder.Append(type.Namespace).Append('.');
            }

            if (type.IsNested && !type.IsGenericParameter)
            {
                builder.Append(type.DeclaringType!.TypeName()).Append('.');
            }

            if (type.IsGenericType)
            {
                var name = type.Name;
                var backtickIndex = name.IndexOf('`');
                if (backtickIndex > 0)
                {
                    name = name.Substring(0, backtickIndex);
                }

                builder.Append(name);
                builder.Append('<');

                var genericArguments = type.GetGenericArguments();
                for (var i = 0; i < genericArguments.Length; i++)
                {
                    builder.Append(genericArguments[i].TypeName());
                    if (i < genericArguments.Length - 1) builder.Append(", ");
                }

                builder.Append('>');
            }
            else
            {
                builder.Append(type.Name);
            }

            return builder.ToString();
        }
    }
}