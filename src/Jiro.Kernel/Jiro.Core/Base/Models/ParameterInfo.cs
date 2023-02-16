namespace Jiro.Core.Base.Models
{
    public class ParameterInfo
    {
        public Type ParamType { get; }

        internal ParameterInfo(Type type)
        {
            ParamType = type;
        }

        public object? Parse(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                if (ParamType.IsValueType)
                    return Activator.CreateInstance(ParamType);

                return null;
            }

            return Convert.ChangeType(input, ParamType);
        }
    }
}