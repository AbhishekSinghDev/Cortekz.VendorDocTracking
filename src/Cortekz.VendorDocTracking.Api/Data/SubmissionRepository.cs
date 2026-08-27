using Cortekz.VendorDocTracking.Api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cortekz.VendorDocTracking.Api.Data;

public class SubmissionRepository
{
    private readonly IMongoCollection<SubmissionDocument> _collection;

    public SubmissionRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<SubmissionDocument>("document_submissions");
    }

    public async Task EnsureIndexesAsync()
    {
        var historyIndex = new CreateIndexModel<SubmissionDocument>(
            Builders<SubmissionDocument>.IndexKeys
                .Ascending(s => s.RequirementId)
                .Descending(s => s.Revision));

        var uniqueRevisionIndex = new CreateIndexModel<SubmissionDocument>(
            Builders<SubmissionDocument>.IndexKeys
                .Ascending(s => s.RequirementId)
                .Ascending(s => s.Revision),
            new CreateIndexOptions { Unique = true });

        await _collection.Indexes.CreateManyAsync(new[] { historyIndex, uniqueRevisionIndex });
    }

    public Task InsertAsync(SubmissionDocument document)
    {
        return _collection.InsertOneAsync(document);
    }

    public Task<SubmissionDocument?> GetByIdAsync(string id)
    {
        var filter = Builders<SubmissionDocument>.Filter.Eq(s => s.Id, ObjectId.Parse(id));
        return _collection.Find(filter).FirstOrDefaultAsync()!;
    }

    public Task<List<SubmissionDocument>> GetHistoryAsync(string requirementId)
    {
        var filter = Builders<SubmissionDocument>.Filter.Eq(s => s.RequirementId, requirementId);
        var sort = Builders<SubmissionDocument>.Sort.Descending(s => s.Revision);
        return _collection.Find(filter).Sort(sort).ToListAsync();
    }

    public Task UpdateReviewAsync(string id, string status, string decidedBy, ReviewComment comment)
    {
        var filter = Builders<SubmissionDocument>.Filter.Eq(s => s.Id, ObjectId.Parse(id));
        var update = Builders<SubmissionDocument>.Update
            .Set(s => s.Review.Status, status)
            .Set(s => s.Review.DecidedBy, decidedBy)
            .Set(s => s.Review.DecidedAt, DateTime.UtcNow)
            .Push(s => s.Review.Comments, comment)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        return _collection.UpdateOneAsync(filter, update);
    }

    public async Task<bool> UpdateAiReviewAsync(string id, AiReviewInfo aiReview)
    {
        var filter = Builders<SubmissionDocument>.Filter.And(
            Builders<SubmissionDocument>.Filter.Eq(s => s.Id, ObjectId.Parse(id)),
            Builders<SubmissionDocument>.Filter.Nin(s => s.AiReview.Status, new[] { "Completed", "Failed" }));

        var update = Builders<SubmissionDocument>.Update
            .Set(s => s.AiReview, aiReview)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public Task<SubmissionDocument?> FindByAiJobIdAsync(string jobId)
    {
        var filter = Builders<SubmissionDocument>.Filter.Eq(s => s.AiReview.JobId, jobId);
        return _collection.Find(filter).FirstOrDefaultAsync()!;
    }
}
