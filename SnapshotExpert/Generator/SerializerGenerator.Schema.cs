using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using DocumentationParser;
using EmitToolbox.Extensions;
using EmitToolbox.Framework;
using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Schemas.Primitives;

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
                text.Append(entry.Summary);

            if (entry.Remarks != null)
            {
                text.Append(" | Remarks: ");
                text.Append(entry.Remarks);
            }

            if (entry.Example != null)
            {
                text.Append(" | Example: ");
                text.Append(entry.Example);
            }

            return text.ToString();
        }
    }

    private struct SchemaMethodBuilder() : ISerializerMethodBuilder
    {
        private ClassContext _context = null!;

        private InstanceDynamicField _fieldDocumentation = null!;

        private InstanceDynamicFunctor _method = null!;

        private LocalBuilder _variableRequiredProperties = null!;

        private LocalBuilder _variableMemberSchema = null!;

        public void Initialize(ClassContext context)
        {
            _context = context;

            _fieldDocumentation = context.TypeContext.FieldBuilder.DefineInstance(
                "DocumentationProvider", typeof(IDocumentationProvider));
            _fieldDocumentation.MarkAttribute(AttributeInjectionMember);

            _method = _context.TypeContext.FunctorBuilder
                .Override(typeof(SnapshotSerializer).GetMethod("GenerateSchema",
                    BindingFlags.NonPublic | BindingFlags.Instance)!);

            var code = _method.Code;

            _variableMemberSchema = code.DeclareLocal(typeof(SnapshotSchema));

            // Initialize the dictionary of required properties .
            _variableRequiredProperties = code.DeclareLocal(typeof(ObjectSchema));
            code.Emit(OpCodes.Newobj,
                typeof(OrderedDictionary<string, SnapshotSchema>).GetConstructor(Type.EmptyTypes)!);
            code.StoreLocal(_variableRequiredProperties);
        }

        public void Complete()
        {
            var code = _method.Code;

            var variableResult = code.DeclareLocal(typeof(ObjectSchema));
            code.NewObject(typeof(ObjectSchema).GetConstructor(Type.EmptyTypes)!);
            code.StoreLocal(variableResult);

            // Bind required properties.
            code.LoadLocal(variableResult);
            code.LoadLocal(_variableRequiredProperties);
            code.StoreProperty(typeof(ObjectSchema).GetProperty(nameof(ObjectSchema.RequiredProperties))!);

            // Set the title to the target type name.
            code.LoadLocal(variableResult);
            code.LoadLiteral(_context.TargetType.ToString());
            code.StoreProperty(typeof(ObjectSchema).GetProperty(nameof(ObjectSchema.Title))!);

            EmitInjectDocumentation(variableResult, EntryName.Of(_context.TargetType));

            code.LoadLocal(variableResult);
            code.MethodReturn();
        }

        public void Generate(FieldInfo field, MemberInfo metadata)
        {
            var code = _method.Code;

            _context.EmitLoadSerializer(code, field.FieldType);
            code.LoadProperty(
                typeof(SnapshotSerializer).GetProperty(nameof(SnapshotSerializer.Schema))!);
            code.StoreLocal(_variableMemberSchema);

            EmitInjectDocumentation(_variableMemberSchema, EntryName.Of(metadata));

            code.LoadLocal(_variableRequiredProperties);
            code.LoadLiteral(metadata.Name);
            code.LoadLocal(_variableMemberSchema);
            code.CallVirtual(typeof(OrderedDictionary<string, SnapshotSchema>)
                .GetMethod(nameof(OrderedDictionary<,>.Add))!);

            code.LoadNull();
            code.StoreLocal(_variableMemberSchema);
        }

        private void EmitInjectDocumentation(LocalBuilder variableSchema, string entryName)
        {
            var code = _method.Code;

            // Try to retrieve documentation for this entry.
            var codeDocumentation = code.DeclareLocal(typeof(string));

            code.LoadLiteral(entryName);
            code.LoadArgument_0();
            code.LoadField(_fieldDocumentation.BuildingField);
            code.Call(typeof(SchemaHelper).GetMethod(nameof(SchemaHelper.RetrieveDocumentation))!);

            code.StoreLocal(codeDocumentation);

            var labelNoDocumentation = code.DefineLabel();
            code.LoadLocal(codeDocumentation);
            code.GotoIfFalse(labelNoDocumentation);

            // Set the description if there is documentation.
            code.LoadLocal(variableSchema);
            code.LoadLocal(codeDocumentation);
            code.StoreProperty(typeof(ObjectSchema).GetProperty(nameof(ObjectSchema.Description))!);

            code.MarkLabel(labelNoDocumentation);
        }
    }
}