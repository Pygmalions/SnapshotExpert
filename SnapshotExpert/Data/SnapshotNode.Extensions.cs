namespace SnapshotExpert.Data;

public static class SnapshotNodeExtensions
{
    extension(SnapshotNode self)
    {
        public SnapshotNode BindValue(SnapshotValue? value)
        {
            self.Value = value;
            return self;
        }

        public SnapshotNode BindType(Type type)
        {
            self.Type = type;
            return self;
        }

        public SnapshotNode BindObject(object? value)
        {
            self.Object = value;
            return self;
        }

        public TSnapshotValue AssignValue<TSnapshotValue>(TSnapshotValue value)
            where TSnapshotValue : SnapshotValue
        {
            self.Value = value;
            return value;
        }
    }
}