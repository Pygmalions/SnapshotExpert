using SnapshotExpert.Data;

namespace SnapshotExpert;

public static class SerializerProviderExtensions
{
    extension(ISerializerProvider self)
    {
        /// <summary>
        /// Get a snapshot serializer for the specified target type.
        /// </summary>
        /// <typeparam name="TTarget">Type of instances for the serializer to handle.</typeparam>
        /// <returns>Serializer for the specified type, or null if not found.</returns>
        public SnapshotSerializer<TTarget>? GetSerializer<TTarget>()
            => self.GetSerializer(typeof(TTarget)) as SnapshotSerializer<TTarget>;

        /// <summary>
        /// Get a snapshot serializer for the specified target type
        /// or throw an exception if it is not found.
        /// </summary>
        /// <param name="type">Type of instances for the serializer to handle.</param>
        /// <returns>Serializer for the specified type.</returns>
        /// <exception cref="Exception">
        /// Throw if the serializer for the specified type is not found.
        /// </exception>
        public SnapshotSerializer RequireSerializer(Type type)
            => self.GetSerializer(type)
               ?? throw new Exception($"Failed to find the required serializer for '{type}'.");

        /// <summary>
        /// Get a snapshot serializer for the specified target type
        /// or throw an exception if it is not found.
        /// </summary>
        /// <typeparam name="TTarget">Type of instances for the serializer to handle.</typeparam>
        /// <returns>Serializer for the specified type.</returns>
        /// <exception cref="Exception">
        /// Throw if the serializer for the specified type is not found.
        /// </exception>
        public SnapshotSerializer<TTarget> RequireSerializer<TTarget>()
            => self.GetSerializer(typeof(TTarget)) as SnapshotSerializer<TTarget>
               ?? throw new Exception($"Failed to find the required serializer for '{typeof(TTarget)}'.");

        /// <summary>
        /// Load the snapshot into the specified target object.
        /// </summary>
        /// <param name="target">Target to load the snapshot into.</param>
        /// <param name="snapshot">Node to load the snapshot from.</param>
        /// <typeparam name="TTarget">Type of the target instance.</typeparam>
        public void LoadSnapshot<TTarget>(ref TTarget target, SnapshotNode snapshot)
            => self.RequireSerializer<TTarget>().LoadSnapshot(ref target, snapshot);

        /// <summary>
        /// Load the snapshot into a new instance of the specified type.
        /// </summary>
        /// <param name="snapshot">Node to load the snapshot from.</param>
        /// <typeparam name="TTarget">Type of the target instance.</typeparam>
        /// <returns>Instance deserialized from the snapshot.</returns>
        public TTarget LoadSnapshot<TTarget>(SnapshotNode snapshot)
        {
            var serializer = self.RequireSerializer<TTarget>();
            serializer.NewInstance(out var target);
            serializer.LoadSnapshot(ref target, snapshot);
            return target;
        }

        /// <summary>
        /// Save the snapshot of the specified target object into the specified node.
        /// </summary>
        /// <param name="target">Target to save the snapshot of.</param>
        /// <param name="snapshot">Node to save snapshot into.</param>
        /// <typeparam name="TTarget">Type of the target instance.</typeparam>
        public void SaveSnapshot<TTarget>(in TTarget target, SnapshotNode snapshot)
            => self.RequireSerializer<TTarget>().SaveSnapshot(target, snapshot);

        /// <summary>
        /// Save the snapshot of the specified target object into a new snapshot node.
        /// </summary>
        /// <param name="target">Target to save the snapshot of.</param>
        /// <typeparam name="TTarget">Type of the target instance.</typeparam>
        /// <returns>Snapshot node containing the snapshot for the specified target.</returns>
        public SnapshotNode SaveSnapshot<TTarget>(in TTarget target)
        {
            var serializer = self.RequireSerializer<TTarget>();
            var snapshot = new SnapshotNode();
            serializer.SaveSnapshot(target, snapshot);
            return snapshot;
        }
    }
}