using System.Reflection;
using System.Text;
using DocumentationParser;
using EmitToolbox;
using EmitToolbox.Extensions;
using EmitToolbox.Symbols;
using EmitToolbox.Utilities;
using SnapshotExpert.Data;
using SnapshotExpert.Data.Schemas.Primitives;

namespace SnapshotExpert.Generator;

public partial class SerializerGenerator
{
    public static class SchemaHelper
    {
        public static string? RetrieveDocumentation(
            string entryName, IDocumentationProvider? provider)
        {
            var entry = provider?.GetEntry(entryName);
            if (entry == null)
                return null;

            var text = new StringBuilder();

            if (entry.Summary != null)
                text.Append(entry.Summary.ReplaceLineEndings(" "));

            if (entry.Remarks != null)
            {
                text.Append(" Remarks: ");
                text.Append(entry.Remarks.ReplaceLineEndings(" "));
            }

            if (entry.SeeAlsoEntryNames is { Count: > 0 })
            {
                text.Append(" See-Also: ");
                text.Append(string.Join(", ", entry.SeeAlsoEntryNames));
            }
            
            return text.ToString();
        }
    }

    private struct SchemaMethodBuilder() : ISerializerMethodBuilder
    {
        private ClassContext _context = null!;

        private FieldSymbol<IDocumentationProvider?> _fieldDocumentation = null!;

        private DynamicMethod<Action<ISymbol<SnapshotSchema>>> _method = null!;

        private VariableSymbol<OrderedDictionary<string, SnapshotSchema>> _variableRequiredProperties = null!;

        public void Initialize(ClassContext context)
        {
            _context = context;

            _method = _context.TypeContext.MethodFactory.Instance
                .OverrideFunctor<SnapshotSchema>(typeof(SnapshotSerializer).GetMethod("GenerateSchema",
                    BindingFlags.NonPublic | BindingFlags.Instance)!);

            var fieldDocumentation = context.TypeContext.FieldFactory
                .DefineInstance(
                    "Documentation", typeof(IDocumentationProvider));
            fieldDocumentation.MarkAttribute(AttributeInjectionMember);

            _fieldDocumentation = fieldDocumentation.SymbolOf<IDocumentationProvider?>(
                _method, _method.This());

            _variableRequiredProperties = _method.New<OrderedDictionary<string, SnapshotSchema>>();
        }

        public void Complete()
        {
            var variableObjectSchema = _method.New<ObjectSchema>();

            variableObjectSchema.SetPropertyValue(
                target => target.RequiredProperties!,
                _variableRequiredProperties);

            var variableDocumentation = _method
                .Invoke(() => SchemaHelper.RetrieveDocumentation(
                        Any<string>.Value, Any<IDocumentationProvider?>.Value)!,
                    [
                        _method.Value(EntryName.Of(_context.TargetType)),
                        _fieldDocumentation
                    ])
                .ToSymbol();

            using (_method.If(variableDocumentation.IsNotNull()))
            {
                variableObjectSchema.SetPropertyValue(
                    target => target.Description!,
                    variableDocumentation);
            }

            _method.Return(variableObjectSchema);
        }

        public void Generate(FieldInfo field, MemberInfo metadata)
        {
            var fieldSerializer = _context.GetSerializerField(field.FieldType)
                .SymbolOf(_method, _method.This());
            var variableMemberSchema =
                fieldSerializer
                    .GetPropertyValue<SnapshotSchema>(
                        typeof(SnapshotSerializer).GetProperty(nameof(SnapshotSerializer.Schema))!)
                    .ToSymbol();

            var variableDocumentation = _method
                .Invoke(() => SchemaHelper.RetrieveDocumentation(
                        Any<string>.Value, Any<IDocumentationProvider?>.Value)!,
                    [
                        _method.Value(EntryName.Of(metadata)),
                        _fieldDocumentation
                    ])
                .ToSymbol();

            using (_method.If(variableDocumentation.IsNotNull()))
            {
                variableMemberSchema.SetPropertyValue(
                    target => target.Description!,
                    variableDocumentation);
            }

            _variableRequiredProperties.Invoke(
                target => target.Add(Any<string>.Value, Any<SnapshotSchema>.Value),
                [
                    _method.Value(metadata.Name),
                    variableMemberSchema
                ]);
        }
    }
}