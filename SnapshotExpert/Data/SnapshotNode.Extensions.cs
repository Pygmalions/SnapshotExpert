using System.Runtime.CompilerServices;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;

namespace SnapshotExpert.Data;

public static class SnapshotNodeExtensions
{
    extension(SnapshotNode node)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SnapshotNode BindObject(object? instance)
        {
            node.Object = instance;
            return node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SnapshotNode BindType(Type? type)
        {
            node.Type = type;
            return node;
        }

        /// <summary>
        /// Acquire the value of the specified snapshot node,
        /// or throw an exception if the value is not of the expected type.
        /// </summary>
        /// <typeparam name="TSnapshotValue">Required type of the snapshot value.</typeparam>
        /// <returns>Snapshot value of the required type from the specified snapshot node.</returns>
        /// <exception cref="InvalidOperationException">
        /// Throw if no value is bound to the specified snapshot node,
        /// or if the value is not of the expected type.
        /// </exception>
        public TSnapshotValue RequireValue<TSnapshotValue>()
            where TSnapshotValue : SnapshotValue
        {
            if (node.Value is null)
                throw new InvalidOperationException(
                    "Unexpected snapshot value: no value is bound to the specified snapshot node.");
            if (node.Value is not TSnapshotValue typedValue)
                throw new InvalidOperationException(
                    $"Unexpected snapshot value: snapshot value is of type {node.Value.GetType()}, " +
                    $"but {typeof(TSnapshotValue)} is expected.");
            return typedValue;
        }

        /// <summary>
        /// Acquire the value of the specified snapshot node,
        /// or throw an exception if the value is not of the expected type.
        /// </summary>
        /// <typeparam name="TNumberInterface">Required number interface of the snapshot value.</typeparam>
        /// <returns>Snapshot value of the required type from the specified snapshot node.</returns>
        /// <exception cref="InvalidOperationException">
        /// Throw if no value is bound to the specified snapshot node,
        /// or if the value is not of the expected type.
        /// </exception>
        public TNumberInterface RequireNumber<TNumberInterface>() 
            where TNumberInterface : INumberInterface
        {
            if (node.Value is null)
                throw new InvalidOperationException(
                    "Unexpected snapshot value: no value is bound to the specified snapshot node.");
            if (node.Value is not TNumberInterface typedValue)
                throw new InvalidOperationException(
                    $"Unexpected snapshot value: snapshot value is of type {node.Value.GetType()}, " +
                    $"but {typeof(TNumberInterface)} is expected.");
            return typedValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssignNull()
            => node.Value = new NullValue();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ObjectValue AssignObject(Dictionary<string, SnapshotValue>? content = null)
        {
            var value = new ObjectValue();
            node.Value = value;
            if (content == null) 
                return value;
            foreach (var (name, member) in content)
                value.CreateNode(name).Value = member;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArrayValue AssignArray(SnapshotValue[]? content = null)
        {
            var value = new ArrayValue();
            node.Value = value;
            if (content == null) 
                return value;
            foreach (var item in content)
                value.CreateNode().Value = item;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssignReference(SnapshotNode reference)
        {
            var value = new InternalReferenceValue(reference);
            node.Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssignReference(string identifier)
        {
            var value = new ExternalReferenceValue(identifier);
            node.Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssignValue(bool value)
            => node.Value = new BooleanValue(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssignValue(string? value)
            => node.Value = value != null ? new StringValue(value) : new NullValue();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssignValue(byte[] value, 
            BinaryValue.BinaryContentType content = BinaryValue.BinaryContentType.Unknown)
            => node.Value = new BinaryValue(value, content);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssignValue(int value)
            => node.Value = new Integer32Value(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssignValue(long value)
            => node.Value = new Integer64Value(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssignValue(double value)
            => node.Value = new Float64Value(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssignValue(decimal value)
            => node.Value = new DecimalValue(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssignValue(DateTimeOffset value)
            => node.Value = new DateTimeValue(value);

        public void AssignValue(Guid value)
            => node.Value = new BinaryValue(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssignValue(int? value)
        {
            if (value == null)
                node.Value = new NullValue();
            else
                node.Value = new Integer32Value(value.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssignValue(long? value)
        {
            if (value == null)
                node.Value = new NullValue();
            else
                node.Value = new Integer64Value(value.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssignValue(double? value)
        {
            if (value == null)
                node.Value = new NullValue();
            else
                node.Value = new Float64Value(value.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssignValue(decimal? value)
        {
            if (value == null)
                node.Value = new NullValue();
            else
                node.Value = new DecimalValue(value.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssignValue(DateTime? value)
        {
            if (value == null)
                node.Value = new NullValue();
            else
                node.Value = new DateTimeValue(value.Value);
        }
    }
}