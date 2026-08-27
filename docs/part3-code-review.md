# Part 3 Code Review: ReviewSubmission

Here is the method I reviewed:

```csharp
[HttpPost("submissions/{id}/review")]
public IActionResult ReviewSubmission(int id, ReviewDto dto)
{
    var submission = _context.Submissions
        .Where(s => s.Id == id).FirstOrDefault();
    submission.Status = dto.Status;
    submission.Comments = dto.Comments;
    _context.SaveChanges();

    var requirement = _context.Requirements
        .Where(r => r.Id == submission.RequirementId).FirstOrDefault();

    var sc-camel-all-submissions = _context.Submissions
        .Where(s => s.RequirementId == requirement.Id).ToList();

    foreach (var s in allSubmissions)
    {
        var vendor = _context.Vendors.Find(s.VendorId);
        Console.WriteLine(vendor.Name);
    }
}
```

I found 13 issues. I go through them in the order they show up in the code, then show a fixed version at the end.

## 1. The code does not compile

The name `sc-camel-all-submissions` is not a valid C# variable name because it has hyphens in it. Even if you fix that, the foreach loop below uses a variable called `allSubmissions`, which was never declared anywhere. On top of that, the method is supposed to return `IActionResult` but never actually returns anything. So this code cannot even build, let alone run.

## 2. FirstOrDefault is not checked for null

`FirstOrDefault()` returns null when no submission matches the given id. The next line uses `submission.Status` right away, with no check. If someone calls this with an id that does not exist, the app crashes with a `NullReferenceException`. That should be a clean `404 Not Found` instead.

## 3. SaveChanges runs too early

`SaveChanges()` is called right after setting the status and comments, before the rest of the method runs. But the code after it can still fail, for example if `requirement` turns out to be null. If that happens, the review was already saved, but the caller gets an error and has no idea the save actually went through.

## 4. No check on the status change

`submission.Status = dto.Status` just sets whatever value the caller sends, no questions asked. Nothing stops someone from reviewing a submission that was already approved, or sending some invalid status. There should be a check that the submission is actually waiting for review before changing anything.

## 5. Comments get overwritten instead of added

`submission.Comments = dto.Comments` replaces the old comments completely. Every comment from an earlier review is gone. This should add a new comment to a list instead, so there is a full history of what reviewers said.

## 6. Dead code after SaveChanges

After saving, the method loads a requirement and a list of submissions, but none of that is ever used or returned. It just gets fetched and printed to the console. This whole block does nothing useful and should be removed.

## 7. Vendor lookup inside the loop (N+1 problem)

Inside the foreach loop, `_context.Vendors.Find(s.VendorId)` runs once per submission. If there are 20 submissions, that is 20 separate calls to the database just to get vendor names. This should be one query instead of one per loop.

## 8. Console.WriteLine instead of logging

The code uses `Console.WriteLine` to print the vendor name. In a real app this should use `ILogger` instead. That way logs actually show up wherever the app sends its logs, and they can be filtered by level.

## 9. Everything runs synchronously

Every database call here, `FirstOrDefault`, `SaveChanges`, `ToList`, `Find`, is synchronous. Each one blocks the thread while it waits on the database. Under load, with many requests at once, this can slow the whole app down. These should use the async versions, like `FirstOrDefaultAsync` and `SaveChangesAsync`, and the method itself should be `async Task<IActionResult>`.

## 10. No return, and returning the entity directly would be wrong too

As mentioned in issue 1, the method never returns anything. But even after fixing that, returning the `submission` entity straight from the database is not a good idea. It would expose the entire database model in the response. It is better to return a small DTO with just the fields the caller needs.

## 11. No protection against two people reviewing at once

If two reviewers act on the same submission at the same time, one save will quietly overwrite the other, and neither person will know it happened. There is nothing here to catch that. A concurrency check would make the second save fail properly instead of silently overwriting the first.

## 12. No authorization and no record of who reviewed it

There is no check on who is allowed to call this endpoint, and nothing records who actually made the decision. In a real system you would want to know who approved or rejected something, and be able to prove it later.

## 13. No real status codes

The method returns `IActionResult`, which is the right type since it lets you return different results for different cases. But it never actually uses that. There is no `NotFound`, no `BadRequest`, no `Ok`, nothing. Every path either throws an error or does not return at all.

## Fixed version

To fix a couple of these properly, I also had to change the data model a little. `Comments` needs to be a list instead of a single string, `Submission` needs a `RowVersion` column so EF can detect if someone else changed the row first, and `ReviewDto` needs `CommentText` and `ReviewedBy` fields.

```csharp
[Authorize]
[HttpPost("submissions/{id}/review")]
public async Task<IActionResult> ReviewSubmission(int id, ReviewDto dto, CancellationToken cancellationToken)
{
    var submission = await _context.Submissions
        .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    if (submission is null)
    {
        return NotFound();
    }

    if (submission.Status != SubmissionStatus.Pending)
    {
        return Conflict($"Submission {id} has already been decided.");
    }

    if (dto.Status == SubmissionStatus.Pending)
    {
        return BadRequest("Status must be Approved, Rejected, or ResubmitRequired.");
    }

    submission.Status = dto.Status;
    submission.Comments.Add(new SubmissionComment
    {
        Text = dto.CommentText,
        ReviewedBy = dto.ReviewedBy,
        CreatedAt = DateTime.UtcNow
    });

    try
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException)
    {
        return Conflict("This submission was updated by another reviewer. Please reload and try again.");
    }

    _logger.LogInformation("Submission {SubmissionId} reviewed as {Status} by {ReviewedBy}",
        submission.Id, submission.Status, dto.ReviewedBy);

    return Ok(new SubmissionReviewResponse
    {
        Id = submission.Id,
        Status = submission.Status,
        CommentCount = submission.Comments.Count
    });
}
```

This version checks for null, checks the status before changing it, adds comments instead of replacing them, drops the dead code, uses async calls everywhere, only saves once everything else has already been checked, catches concurrency conflicts, requires authorization, and returns a proper DTO with a real status code for every case.
