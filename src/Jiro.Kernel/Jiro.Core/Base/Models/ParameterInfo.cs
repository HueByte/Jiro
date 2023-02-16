using Jiro.Core.Base.TypeParsers;

namespace Jiro.Core.Base.Models
{
    public class ParameterInfo
    {
        public Type ParamType { get; }
        public TypeParser? Parser { get; }

        internal ParameterInfo(Type type, TypeParser parser)
        {
            ParamType = type;
            Parser = parser;
        }

        // public object? Parse(string? input)
        // {
        //     if (string.IsNullOrEmpty(input))
        //     {
        //         if (ParamType.IsValueType)
        //             return Activator.CreateInstance(ParamType);

        //         return null;
        //     }

        //     return Convert.ChangeType(input, ParamType);
        // }
    }
}