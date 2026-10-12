db = db.getSiblingDB("cosmetics_reviews");

function ensureCollection(name, validator) {
  const exists = db.getCollectionNames().includes(name);

  if (!exists) {
    if (validator) {
      db.createCollection(name, { validator: validator, validationLevel: "moderate" });
    } else {
      db.createCollection(name);
    }
    print(`created: ${name}`);
  } else if (validator) {
    db.runCommand({ collMod: name, validator: validator, validationLevel: "moderate" });
    print(`validator updated: ${name}`);
  } else {
    print(`exists: ${name}`);
  }
}

// reviews: embedding (author, comments), reference (productId -> Каталог, discussionId -> discussions),
// productName денормалізовано з Каталогу; решта полів може відрізнятися між документами
ensureCollection("reviews", {
  $jsonSchema: {
    bsonType: "object",
    required: ["productId", "productName", "author", "rating", "createdAt"],
    properties: {
      productId:    { bsonType: ["int", "long"] },
      productName:  { bsonType: "string" },
      author: {
        bsonType: "object",
        required: ["displayName"],
        properties: {
          customerId:  { bsonType: ["int", "long", "null"] },
          displayName: { bsonType: "string" },
          skinType:    { enum: ["Normal", "Dry", "Oily", "Combination", "Sensitive"] }
        }
      },
      rating:       { bsonType: "int", minimum: 1, maximum: 5 },
      title:        { bsonType: "string" },
      text:         { bsonType: "string" },
      photos:       { bsonType: "array", items: { bsonType: "string" } },
      comments: {
        bsonType: "array",
        items: {
          bsonType: "object",
          required: ["authorName", "text", "createdAt"],
          properties: {
            authorName: { bsonType: "string" },
            text:       { bsonType: "string" },
            createdAt:  { bsonType: "date" }
          }
        }
      },
      discussionId: { bsonType: "objectId" },
      helpfulVotes: { bsonType: "int", minimum: 0 },
      createdAt:    { bsonType: "date" }
    }
  }
});

// discussions: гібридний підхід — останні повідомлення вбудовані (recentMessages),
// повна історія в окремій колекції discussion_messages за посиланням
ensureCollection("discussions", {
  $jsonSchema: {
    bsonType: "object",
    required: ["productId", "productName", "title", "createdBy", "messagesCount", "createdAt"],
    properties: {
      productId:      { bsonType: ["int", "long"] },
      productName:    { bsonType: "string" },
      title:          { bsonType: "string" },
      createdBy:      { bsonType: "object", required: ["displayName"] },
      tags:           { bsonType: "array", items: { bsonType: "string" } },
      messagesCount:  { bsonType: "int", minimum: 0 },
      lastMessageAt:  { bsonType: "date" },
      recentMessages: { bsonType: "array", maxItems: 3 },
      createdAt:      { bsonType: "date" }
    }
  }
});

// discussion_messages: referencing — необмежено велика підколекція обговорення
ensureCollection("discussion_messages", {
  $jsonSchema: {
    bsonType: "object",
    required: ["discussionId", "author", "text", "createdAt"],
    properties: {
      discussionId: { bsonType: "objectId" },
      author:       { bsonType: "object", required: ["displayName"] },
      text:         { bsonType: "string" },
      replyTo:      { bsonType: "objectId" },
      createdAt:    { bsonType: "date" }
    }
  }
});

// product_ratings: зведений рейтинг товару, перераховується aggregation pipeline з reviews
ensureCollection("product_ratings", null);

// запит: відгуки товару, спочатку нові
db.reviews.createIndex({ productId: 1, createdAt: -1 }, { name: "ix_reviews_product_createdAt" });

// запит: усі відгуки конкретного покупця
db.reviews.createIndex({ "author.customerId": 1 }, { name: "ix_reviews_author_customerId" });

// запит: повнотекстовий пошук по заголовку й тексту відгуку
db.reviews.createIndex(
  { title: "text", text: "text" },
  { name: "ix_reviews_text", default_language: "none" }
);

// запит: обговорення товару, спочатку найактивніші
db.discussions.createIndex({ productId: 1, lastMessageAt: -1 }, { name: "ix_discussions_product_lastMessageAt" });

// запит: обговорення за тегом
db.discussions.createIndex({ tags: 1 }, { name: "ix_discussions_tags" });

// запит: повідомлення обговорення в хронологічному порядку
db.discussion_messages.createIndex({ discussionId: 1, createdAt: 1 }, { name: "ix_messages_discussion_createdAt" });

// запит: топ товарів за рейтингом
db.product_ratings.createIndex({ avgRating: -1 }, { name: "ix_ratings_avgRating" });

print("indexes ready");
