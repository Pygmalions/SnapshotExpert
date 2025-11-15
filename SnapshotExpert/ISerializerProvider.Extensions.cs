namespace SnapshotExpert;

public static class SerializerProviderExtensions
{
    extension(ISerializerProvider provider)
    {
        /// <summary>
        /// Get a snapshot serializer for the specified target type.
        /// </summary>
        /// <typeparam name="TTarget">Type of instances for the serializer to handle.</typeparam>
        /// <returns>Serializer for the specified type, or null if not found.</returns>
        public SnapshotSerializer<TTarget>? GetSerializer<TTarget>()
            => provider.GetSerializer(typeof(TTarget)) as SnapshotSerializer<TTarget>;

        /// <summary>
        /// Get a snapshot serializer for the specified target type,
        /// or throw an exception if it is not found.
        /// </summary>
        /// <param name="type">Type of instances for the serializer to handle.</param>
        /// <returns>Serializer for the specified type.</returns>
        /// <exception cref="Exception">
        /// Throw if the serializer for the specified type is not found.
        /// </exception>
        public SnapshotSerializer RequireSerializer(Type type)
            => provider.GetSerializer(type)
               ?? throw new Exception($"Failed to find the required serializer for '{type}'.");

        /// <summary>
        /// Get a snapshot serializer for the specified target type,
        /// or throw an exception if it is not found.
        /// </summary>
        /// <typeparam name="TTarget">Type of instances for the serializer to handle.</typeparam>
        /// <returns>Serializer for the specified type.</returns>
        /// <exception cref="Exception">
        /// Throw if the serializer for the specified type is not found.
        /// </exception>
        public SnapshotSerializer<TTarget> RequireSerializer<TTarget>()
            => provider.GetSerializer(typeof(TTarget)) as SnapshotSerializer<TTarget>
               ?? throw new Exception($"Failed to find the required serializer for '{typeof(TTarget)}'.");
    }
}