# SnapshotExpert

SnapshotExpert is a serialization library built on top of dynamic code (IL) generation.
It can generate snapshot serializers for any type at runtime, 
allowing for efficient serialization and deserialization of objects.

## Features

### Patching Support

Snapshot serializers can save and restore the state of an object into a node tree structure snapshot,
which is compatible with BSON and JSON formats.
Compared to tradition serialization methods, snapshot serializers can deserialize objects 'in place',
as loading the snapshots into existing objects to restore the state.
This feature allows users to 'patch' objects.

### Reference Tracking

SnapshotExpert has built-in support for automatic reference tracking.
In a snapshot, the same object reference is only serialized once, at the topmost possible position.
Further references to the same object are serialized as path strings pointing to the original object.
With this feature, the reference relationships between objects are preserved during serialization and deserialization.

In addition to (1) avoid circular references, (2) reduce the size of the snapshot,
this feature is also essential to maintain the correctness of the object graph during deserialization.
For example, consider objects A, B, and C; both A and B reference C.
Without reference tracking, after deserialization, 
A and B would reference two different instances of C, as C1 and C2.
If A and B are both reading and writing the state of C,
then after the deserialization, A and B would be out of sync.

Meanwhile, external references are also supported.
References to objects defined as external references are always serialized as identifier strings,
and they can be configured using `SnapshotWritingScope.ExternalReferences` and 
`SnapshotReadingScope.ExternalReferences`.

### Format Selection

Most serializers support two different formats for binary snapshot (BSON) and text snapshot (JSON).
Snapshot using a binary format is more compact and faster to serialize and deserialize,
while snapshot using a text format is human-readable and easier to debug.
The format preference can be specified in `SnapshotWritingScope` when saving the snapshot;
and the format is automatically detected when parsing the snapshot.
The default format is binary (BSON).

## Usage

### Basic Serialization and Deserialization

```csharp
// Create a snapshot context.
var context = new SnapshotContext();

// Get the serializer for the type to serialize and deserialize.
var serializer = context.GetSnapshotSerializer<YourType>();

// Create an empty node as the root of the snapshot tree.
var node = new SnapshotNode();

// Serialize: Save the snapshot of yourObject into the snapshot node.
serializer.SaveSnapshot(yourObject, node);

// Deserialize: Load the snapshot from the snapshot node into yourObject.
serializer.LoadSnapshot(ref yourObject, node);

// Serialize: Save the snapshot of yourObject into the snapshot node in textual format.
serializer.SaveSnapshot(target, node, new SnapshotWritingScope()
        {
            Format = SnapshotDataFormat.Textual
        });

```

## Conversion Between Snapshot and BSON/JSON

```csharp
// Serialize the snapshot node to BSON byte array.
byte[] bson = node.ToBson(); 

// Serialize the snapshot node to JSON string.
string json = node.ToJson(); 

// Deserialize the snapshot node from BSON byte array.
node.Parse(bson);

// Deserialize the snapshot node from BSON byte array.
node.Parse(bson);
```
