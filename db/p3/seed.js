db = db.getSiblingDB("cosmetics_reviews");

function insertIfMissing(collectionName, docs) {
  const collection = db.getCollection(collectionName);
  let inserted = 0;

  docs.forEach(doc => {
    if (collection.countDocuments({ _id: doc._id }, { limit: 1 }) === 0) {
      collection.insertOne(doc);
      inserted++;
    }
  });

  print(`${collectionName}: inserted ${inserted}, skipped ${docs.length - inserted}`);
}

const D1 = ObjectId("670a0000000000000000d001");
const D2 = ObjectId("670a0000000000000000d002");

const M1 = ObjectId("670a0000000000000000e001");
const M2 = ObjectId("670a0000000000000000e002");
const M3 = ObjectId("670a0000000000000000e003");
const M4 = ObjectId("670a0000000000000000e004");
const M5 = ObjectId("670a0000000000000000e005");

insertIfMissing("discussions", [
  {
    _id: D1,
    productId: 1,
    productName: "CeraVe Hydrating Cleanser 236 ml",
    title: "Чи підходить для чутливої шкіри?",
    createdBy: { customerId: 3, displayName: "Ірина Ш." },
    tags: ["чутлива шкіра", "очищення"],
    messagesCount: 3,
    lastMessageAt: new Date("2026-09-18T12:30:00Z"),
    recentMessages: [
      { messageId: M2, authorName: "Олена К.", text: "У мене чутлива суха шкіра, подразнень не було.", createdAt: new Date("2026-09-18T09:10:00Z") },
      { messageId: M3, authorName: "Андрій М.", text: "Підтримую, дуже м'який засіб.", createdAt: new Date("2026-09-18T12:30:00Z") }
    ],
    createdAt: new Date("2026-09-17T18:00:00Z")
  },
  {
    _id: D2,
    productId: 2,
    productName: "La Roche-Posay Effaclar Duo+ 40 ml",
    title: "Чи можна поєднувати з ретиноїдами?",
    createdBy: { customerId: 2, displayName: "Андрій М." },
    tags: ["проблемна шкіра", "поєднання засобів"],
    messagesCount: 2,
    lastMessageAt: new Date("2026-09-21T08:45:00Z"),
    recentMessages: [
      { messageId: M4, authorName: "Андрій М.", text: "Хочу додати ретинол увечері. Хтось пробував?", createdAt: new Date("2026-09-20T19:00:00Z") },
      { messageId: M5, authorName: "Олена К.", text: "Я наношу їх у різні дні, так шкіра не пересушується.", createdAt: new Date("2026-09-21T08:45:00Z") }
    ],
    createdAt: new Date("2026-09-20T19:00:00Z")
  }
]);

insertIfMissing("discussion_messages", [
  {
    _id: M1,
    discussionId: D1,
    author: { customerId: 3, displayName: "Ірина Ш." },
    text: "Маю чутливу шкіру з почервоніннями. Чи не буде печіння?",
    createdAt: new Date("2026-09-17T18:00:00Z")
  },
  {
    _id: M2,
    discussionId: D1,
    author: { customerId: 1, displayName: "Олена К." },
    text: "У мене чутлива суха шкіра, подразнень не було. Користуюсь двічі на день уже місяць.",
    replyTo: M1,
    createdAt: new Date("2026-09-18T09:10:00Z")
  },
  {
    _id: M3,
    discussionId: D1,
    author: { customerId: 2, displayName: "Андрій М." },
    text: "Підтримую, дуже м'який засіб.",
    replyTo: M1,
    createdAt: new Date("2026-09-18T12:30:00Z")
  },
  {
    _id: M4,
    discussionId: D2,
    author: { customerId: 2, displayName: "Андрій М." },
    text: "Хочу додати ретинол увечері. Хтось пробував?",
    edited: true,
    editedAt: new Date("2026-09-20T19:05:00Z"),
    createdAt: new Date("2026-09-20T19:00:00Z")
  },
  {
    _id: M5,
    discussionId: D2,
    author: { customerId: 1, displayName: "Олена К." },
    text: "Я наношу їх у різні дні, так шкіра не пересушується.",
    replyTo: M4,
    attachments: [{ type: "image", url: "/uploads/discussions/d002/routine.jpg" }],
    createdAt: new Date("2026-09-21T08:45:00Z")
  }
]);

insertIfMissing("reviews", [
  {
    _id: ObjectId("670a0000000000000000f001"),
    productId: 1,
    productName: "CeraVe Hydrating Cleanser 236 ml",
    author: { customerId: 1, displayName: "Олена К.", skinType: "Dry" },
    rating: 5,
    title: "Ідеальне м'яке очищення",
    text: "Шкіра після вмивання не стягнута, добре знімає залишки крему.",
    pros: ["не пересушує", "без запаху"],
    photos: ["/uploads/reviews/f001/1.jpg", "/uploads/reviews/f001/2.jpg"],
    verifiedPurchase: { orderId: 1 },
    comments: [
      { authorName: "Ірина Ш.", text: "А макіяж знімає?", createdAt: new Date("2026-09-16T10:00:00Z") },
      { authorName: "Олена К.", text: "Легкий так, водостійкий краще міцеляркою.", createdAt: new Date("2026-09-16T11:20:00Z") }
    ],
    discussionId: D1,
    helpfulVotes: 12,
    createdAt: new Date("2026-09-15T08:00:00Z")
  },
  {
    _id: ObjectId("670a0000000000000000f002"),
    productId: 3,
    productName: "The Ordinary Niacinamide 10% + Zinc 1% 30 ml",
    author: { customerId: 2, displayName: "Андрій М.", skinType: "Oily" },
    rating: 4,
    title: "Помітно менше висипань",
    text: "Пори стали менш помітні, але при нанесенні під крем іноді скочується.",
    pros: ["менше блиску", "ціна"],
    cons: ["може скочуватися"],
    usageDuration: { value: 6, unit: "weeks" },
    verifiedPurchase: { orderId: 2 },
    helpfulVotes: 7,
    createdAt: new Date("2026-09-25T17:30:00Z")
  },
  {
    _id: ObjectId("670a0000000000000000f003"),
    productId: 5,
    productName: "Neutrogena Hydro Boost Water Gel 50 ml",
    author: { customerId: 2, displayName: "Андрій М.", skinType: "Combination" },
    rating: 5,
    text: "Легкий, швидко вбирається. Куплю ще.",
    repurchase: true,
    helpfulVotes: 3,
    createdAt: new Date("2026-09-27T09:15:00Z")
  },
  {
    _id: ObjectId("670a0000000000000000f004"),
    productId: 2,
    productName: "La Roche-Posay Effaclar Duo+ 40 ml",
    author: { customerId: null, displayName: "Гість", isGuest: true },
    rating: 3,
    title: "Працює, але щипає",
    text: "Висипань стало менше, проте на подразненій шкірі відчувається печіння.",
    cons: ["печіння на подразненнях"],
    discussionId: D2,
    helpfulVotes: 2,
    createdAt: new Date("2026-09-22T14:00:00Z")
  },
  {
    _id: ObjectId("670a0000000000000000f005"),
    productId: 1,
    productName: "CeraVe Hydrating Cleanser 236 ml",
    author: { customerId: 3, displayName: "Ірина Ш.", skinType: "Sensitive" },
    rating: 4,
    title: "Підійшов чутливій шкірі",
    text: "Почервонінь не було, але флакон без дозатора незручний.",
    photos: ["/uploads/reviews/f005/1.jpg"],
    comments: [
      { authorName: "Олена К.", text: "Є версія з помпою, раджу.", createdAt: new Date("2026-09-30T13:00:00Z") }
    ],
    discussionId: D1,
    helpfulVotes: 5,
    createdAt: new Date("2026-09-29T19:40:00Z")
  },
  {
    _id: ObjectId("670a0000000000000000f006"),
    productId: 8,
    productName: "Avène Cleanance SPF50+ 50 ml",
    author: { customerId: 1, displayName: "Олена К.", skinType: "Dry" },
    rating: 5,
    title: "Не залишає білих слідів",
    text: "Матовий фініш, під макіяж лягає добре.",
    video: { url: "/uploads/reviews/f006/review.mp4", durationSec: 45 },
    skinConcerns: ["пігментація", "чутливість до сонця"],
    helpfulVotes: 9,
    createdAt: new Date("2026-10-02T07:50:00Z")
  }
]);

db.reviews.aggregate([
  {
    $group: {
      _id: "$productId",
      productName: { $first: "$productName" },
      avgRating: { $avg: "$rating" },
      reviewsCount: { $sum: 1 },
      r1: { $sum: { $cond: [{ $eq: ["$rating", 1] }, 1, 0] } },
      r2: { $sum: { $cond: [{ $eq: ["$rating", 2] }, 1, 0] } },
      r3: { $sum: { $cond: [{ $eq: ["$rating", 3] }, 1, 0] } },
      r4: { $sum: { $cond: [{ $eq: ["$rating", 4] }, 1, 0] } },
      r5: { $sum: { $cond: [{ $eq: ["$rating", 5] }, 1, 0] } }
    }
  },
  {
    $project: {
      productName: 1,
      avgRating: { $round: ["$avgRating", 2] },
      reviewsCount: 1,
      distribution: { "1": "$r1", "2": "$r2", "3": "$r3", "4": "$r4", "5": "$r5" },
      updatedAt: "$$NOW"
    }
  },
  { $merge: { into: "product_ratings", on: "_id", whenMatched: "replace", whenNotMatched: "insert" } }
]);

print(`product_ratings: ${db.product_ratings.countDocuments()} documents`);
