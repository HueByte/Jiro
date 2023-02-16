namespace Jiro.Core.Base.TypeParsers
{
    public class DefaultValueParser<T> : TypeParser
    {
        public override object? Parse(string? input)
        {
            var type = typeof(T);
            if (string.IsNullOrEmpty(input))
            {
                if (type.IsValueType)
                    return Activator.CreateInstance(type);

                return null;
            }

            return Convert.ChangeType(input, type);
        }
    }
}