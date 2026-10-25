using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Reviews.Domain.ValueObjects;

namespace Reviews.Infrastructure.Persistence.Mappings;

public sealed class RatingSerializer : SerializerBase<Rating>
{
    public override Rating Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        var type = reader.GetCurrentBsonType();

        return type switch
        {
            BsonType.Int32 => new Rating(reader.ReadInt32()),
            BsonType.Int64 => new Rating((int)reader.ReadInt64()),
            BsonType.Double => new Rating((int)reader.ReadDouble()),
            _ => throw new FormatException($"Cannot deserialize Rating from BSON type {type}")
        };
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Rating value)
        => context.Writer.WriteInt32(value.Value);
}
