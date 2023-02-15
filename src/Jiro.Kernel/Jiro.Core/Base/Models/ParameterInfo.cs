namespace Jiro.Core.Base.Models
{
    public class ParameterInfo
    {
        public Type Type { get; }

        internal ParameterInfo(Type type)
        {
            Type = type;
        }

        public object? Parse(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                if (Type.IsValueType)
                    return Activator.CreateInstance(Type);

                return null;
            }

            return Convert.ChangeType(input, Type);
        }
    }
}