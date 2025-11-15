namespace SnapshotExpert;

public static class SerializerContainerExtensions
{
    extension(ISerializerContainer container)
    {
        /// <summary>
        /// Registers a serializer type for a specific target type in the container.
        /// </summary>
        /// <typeparam name="TTarget">The type of the object to be serialized.</typeparam>
        /// <typeparam name="TSerializer">The type of the serializer to be used for <typeparamref name="TTarget"/>.</typeparam>
        /// <returns>The updated <see cref="ISerializerContainer"/> instance.</returns>
        public ISerializerContainer WithSerializer<TTarget, TSerializer>()
            where TSerializer : SnapshotSerializer<TTarget>
        {
            container.AddSerializer(typeof(TTarget), typeof(TSerializer));
            return container;
        }

        /// <summary>
        /// Registers a specific serializer instance for a specific target type in the container.
        /// </summary>
        /// <typeparam name="TTarget">The type of the object to be serialized.</typeparam>
        /// <typeparam name="TSerializer">
        /// The type of the serializer to be used for <typeparamref name="TTarget"/>.
        /// </typeparam>
        /// <param name="serializer">
        /// The serializer instance to be used for <typeparamref name="TTarget"/>.
        /// </param>
        /// <returns>The updated <see cref="ISerializerContainer"/> instance.</returns>
        public ISerializerContainer WithSerializer<TTarget, TSerializer>(TSerializer serializer)
            where TSerializer : SnapshotSerializer<TTarget>
        {
            container.AddSerializer(typeof(TTarget), serializer);
            return container;
        }

        /// <summary>
        /// Adds a factory delegate to the container for creating serializers.
        /// </summary>
        /// <param name="factory">The factory delegate to be added.</param>
        /// <returns>The updated <see cref="ISerializerContainer"/> instance.</returns>
        public ISerializerContainer WithFactory(ISerializerContainer.FactoryDelegate factory)
        {
            container.Factories.Add(factory);
            return container;
        }
    }
}
