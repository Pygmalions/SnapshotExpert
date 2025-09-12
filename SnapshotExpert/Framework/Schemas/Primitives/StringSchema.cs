using System.Text.RegularExpressions;
using JetBrains.Annotations;
using SnapshotExpert.Framework.Values;
using SnapshotExpert.Framework.Values.Primitives;

namespace SnapshotExpert.Framework.Schemas.Primitives;

public record StringSchema() : PrimitiveSchema(JsonValueType.String)
{
    /// <summary>
    /// A hint on the format of the string, only for documentation purposes.
    /// </summary>
    [ValueProvider("SnapshotExpert.Framework.Schema.Primitives.StringSchema.BuiltinFormats")]
    public string? Format { get; init; } = null;
    
    public int? MaxLength { get; init; } = null;
    
    public int? MinLength { get; init; } = null;

    public Regex? Pattern { get; init; } = null;
    
    public ContentEncodingType? ContentEncoding { get; init; } = null;
    
    public string? ContentMediaType { get; init; } = null;
    
    protected override void OnGenerate(ObjectValue schema)
    {
        if (Format != null)
            schema.CreateNode("format").AssignValue(Format);

        if (MinLength != null)
            schema.CreateNode("minLength").AssignValue(MinLength.Value);
        
        if (MaxLength != null)
            schema.CreateNode("maxLength").AssignValue(MaxLength.Value);

        if (Pattern != null)
            schema.CreateNode("pattern").AssignValue(Pattern.ToString());
        
        if (ContentEncoding != null)
            schema.CreateNode("contentEncoding").AssignValue(ContentEncoding switch
            {
                ContentEncodingType.QuotedPrintable => "quoted-printable",
                ContentEncodingType.Base16 => "base16",
                ContentEncodingType.Base32 => "base32",
                ContentEncodingType.Base64 => "base64",
                _ => throw new Exception($"Unsupported content encoding type: '{ContentEncoding}'.")
            });
        
        if (ContentMediaType != null)
            schema.CreateNode("contentMediaType").AssignValue(ContentMediaType);
    }

    protected override bool OnValidate(SnapshotNode node)
    {
        if (node.Value is not StringValue value)
            return false;
        if (MinLength != null && value.Value.Length < MinLength)
            return false;
        if (MaxLength != null && value.Value.Length > MaxLength)
            return false;
        if (Pattern != null && !Pattern.IsMatch(value.Value))
            return false;
        return true;
    }
    
    /// <summary>
    /// Names of the built-in formats.
    /// </summary>
    /// <seealso href="https://json-schema.org/understanding-json-schema/reference/type"/>
    public static class BuiltinFormats
    {
        public const string DateTime = "date-time";
        public const string Date = "date";
        public const string Time = "time";
        public const string Duration = "duration";
        public const string Email = "email";
        /// <summary>
        /// The internationalized form of an Internet email address, according to RFC 6531.
        /// </summary>
        /// <seealso href="https://datatracker.ietf.org/doc/html/rfc6531"/>
        public const string EmailIdn = "idn-email";
        public const string Hostname = "hostname";
        /// <summary>
        /// An internationalized Internet host name, according to RFC5890, section 2.3.2.3.
        /// </summary>
        /// <seealso href="https://datatracker.ietf.org/doc/html/rfc5890#section-2.3.2.3"/>
        public const string HostnameIdn = "idn-hostname";
        public const string Ipv4 = "ipv4";
        public const string Ipv6 = "ipv6";
        public const string Uuid = "uuid";
        public const string Uri = "uri";
        public const string UriReference = "uri-reference";
        public const string UriTemplate = "uri-template";
        public const string JsonPointer = "json-pointer";
        public const string RelativeJsonPointer = "relative-json-pointer";
        public const string Regex = "regex";
        /// <summary>
        /// The internationalized equivalent of an "uri",
        /// according to RFC3987.
        /// </summary>
        /// <seealso href="https://datatracker.ietf.org/doc/html/rfc3987"/>
        public const string Iri = "iri";
        /// <summary>
        /// The internationalized equivalent of an "uri-reference",
        /// according to RFC3987.
        /// </summary>
        /// <seealso href="https://datatracker.ietf.org/doc/html/rfc3987"/>
        public const string IriReference = "iri-reference";
    }

    public enum ContentEncodingType
    {
        QuotedPrintable,
        Base16,
        Base32,
        Base64,
    }
}