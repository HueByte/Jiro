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
    }
}