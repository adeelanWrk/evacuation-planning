namespace Evacuation.Transformers
{
    public class LowerCaseParameterTransformer : IOutboundParameterTransformer
    {
        public string? TransformOutbound(object? value)
        {
            if (value == null) return null;
            var stringValue = value.ToString();
            if (stringValue == null) return null;

            var transformedValue = new System.Text.StringBuilder();
            for (int i = 0; i < stringValue.Length; i++)
            {
            char c = stringValue[i];
            if (char.IsUpper(c) && i > 0)
            {
                transformedValue.Append('-');
            }
            transformedValue.Append(char.ToLowerInvariant(c));
            }
            return transformedValue.ToString();
        }
    }
}
