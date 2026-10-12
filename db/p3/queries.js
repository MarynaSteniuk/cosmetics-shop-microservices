db = db.getSiblingDB("cosmetics_reviews");

print("1. Відгуки товару 1, спочатку нові");
printjson(db.reviews.find({ productId: 1 }, { productName: 1, rating: 1, title: 1, createdAt: 1 })
  .sort({ createdAt: -1 }).toArray());

print("2. Відгуки з фото (поле є лише в частини документів)");
printjson(db.reviews.find({ photos: { $exists: true } }, { productName: 1, photos: 1 }).toArray());

print("3. Повнотекстовий пошук");
printjson(db.reviews.find({ $text: { $search: "шкіра" } }, { productName: 1, title: 1 }).toArray());

print("4. Середня оцінка за типом шкіри автора");
printjson(db.reviews.aggregate([
  { $match: { "author.skinType": { $exists: true } } },
  { $group: { _id: "$author.skinType", avgRating: { $avg: "$rating" }, reviews: { $sum: 1 } } },
  { $sort: { avgRating: -1 } }
]).toArray());

print("5. Обговорення з усіма повідомленнями ($lookup за посиланням)");
printjson(db.discussions.aggregate([
  { $match: { _id: ObjectId("670a0000000000000000d001") } },
  { $lookup: {
      from: "discussion_messages",
      localField: "_id",
      foreignField: "discussionId",
      as: "messages",
      pipeline: [{ $sort: { createdAt: 1 } }, { $project: { "author.displayName": 1, text: 1, createdAt: 1 } }]
  } },
  { $project: { title: 1, productName: 1, messagesCount: 1, messages: 1 } }
]).toArray());

print("6. Топ товарів за рейтингом");
printjson(db.product_ratings.find().sort({ avgRating: -1 }).toArray());

print("7. Пагінація: друга сторінка відгуків по 2");
printjson(db.reviews.find({}, { productName: 1, rating: 1 }).sort({ createdAt: -1 }).skip(2).limit(2).toArray());
