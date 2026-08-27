# Vendor Document Tracking Service

This is a backend service for tracking documents that vendors submit against purchase orders. A purchase order has a list of document requirements, vendors submit files against each requirement, a reviewer approves or rejects each submission, and an AI review service checks each submission in the background.

## What is in this repo

There are two web APIs and one test project.

`Cortekz.VendorDocTracking.Api` is the main service. It has the five endpoints, talks to Postgres and MongoDB, and runs a background worker that polls the AI review service.

`Cortekz.MockAiReviewService` is a small stub that pretends to be an external AI review service. It is only here so the whole flow can be tested locally.

`Cortekz.VendorDocTracking.Tests` is currently empty. Automated tests were skipped for this pass, see the trade offs section below.

## How to run it

You need Docker and the .NET 8 SDK.

Step 1, start Postgres, MongoDB, and the mock AI service:

```
docker compose up -d
```

Step 2, run the main API:

```
cd src/Cortekz.VendorDocTracking.Api
dotnet run
```

The terminal will print the URL it is listening on, something like `http://localhost:5207`. Open `/swagger` on that URL to try the endpoints. On first run, the API applies EF Core migrations and seeds a couple of vendors, one purchase order, and a few requirements, so Swagger has something to work with right away.

The mock AI service runs inside Docker and is reachable at `http://localhost:5090`. You do not need to run it separately.

## Endpoints

There are five endpoints.

`POST /api/purchase-orders` creates a purchase order together with its document requirements in one request. Returns 201 on success, 400 if the request is invalid, 404 if the vendor does not exist, 409 if the PO number is already used.

`GET /api/purchase-orders/{id}/requirements` lists the requirements for a purchase order. Supports filtering by status, document type, due date, and overdue, plus paging. Returns 200, or 404 if the purchase order does not exist.

`POST /api/requirements/{requirementId}/submissions` lets a vendor submit a document against a requirement. Returns 201, 400 on invalid input, 404 if the requirement does not exist, 409 if the requirement cannot accept a new submission right now, 502 if the submission could not be saved.

`POST /api/submissions/{submissionId}/review` records a reviewer decision, approved, rejected, or resubmit required, with a comment. Returns 200, 404 if the submission does not exist, 409 if it was already decided.

`GET /api/requirements/{requirementId}/submissions` returns the full submission history for a requirement, newest first, including all review comments and the AI review result.

## Database schema

### Postgres

Postgres holds `vendors`, `purchase_orders`, `document_requirements`, and `ai_review_jobs`. These are the core, well structured records with real foreign keys and constraints, for example a purchase order must belong to a real vendor, and a PO number must be unique.

### MongoDB

MongoDB has one collection, `document_submissions`. Each document holds one submission, its files, review comments, and the AI review result. A submission's shape changes depending on the document type and on whatever the AI service returns, so it is easier to store this as a flexible document than to force it into several relational tables.

The only link between the two databases is `requirementId`. Postgres owns the requirement's identity and status. MongoDB owns the actual submission content.

## AI review integration

When a document is submitted, the API does not wait for the AI review. It writes a row to the `ai_review_jobs` table and returns right away. A background worker, `AiReviewJobWorker`, checks that table every few seconds and does the actual work of talking to the AI service.

The worker first calls `POST /ai/review-jobs` on the mock service to start a job, then polls `GET /ai/review-jobs/{jobId}` until it comes back completed or failed. Once it has a result, it saves it to both the `ai_review_jobs` row and the matching MongoDB submission.

This is safe to run more than once on the same job. The database update only applies if the job is not already finished, and the MongoDB update only applies if the AI review is not already finished. So if the worker gets interrupted and picks the same job up again later, nothing gets overwritten twice.

If the AI service is slow or down, the worker retries with a short delay that grows each time, up to a limit. After a few failed attempts it gives up and marks that job as abandoned. Even then, the submission itself is not stuck. It just shows the AI review as failed, and a human reviewer can still approve or reject it normally.

## Trade offs and known gaps

No authentication or authorization anywhere.

No protection against two people editing the same requirement at the same time.

Creating a submission writes to Postgres first, then MongoDB. If the MongoDB write fails after the Postgres write already succeeded, there is no rollback. The requirement is left marked as submitted even though nothing was stored, so the vendor cannot send it again until a reviewer moves it.

No automated tests in this pass. This was a deliberate choice to spend the available time on the API and the AI integration instead.

The mock AI service is intentionally simple. It keeps jobs in memory only, with no persistence, and its verdicts are random rather than based on any real analysis.

## What I would do differently with more time

Write tests. This is the first thing I would go back and do. The services take everything through the constructor and return a result enum instead of throwing, so they are easy to test without spinning up a database.

Fix the case where the Postgres write succeeds and the Mongo write fails. Right now the requirement is already marked as submitted, so the vendor cannot send it again. I would roll the requirement back to its old status before returning the error.

Add auth. Every endpoint is open right now, and nothing records who actually made a review decision beyond a name in the request body.

Make the background worker safe to run on more than one instance. Two copies of the API would both pick up the same job and start two AI reviews. Postgres has `FOR UPDATE SKIP LOCKED` for exactly this, I just did not get to it.

Add a proper health endpoint that actually checks Postgres and Mongo instead of always returning healthy.

## Time spent

Around 11 hours in total. That is over the 4 to 6 hours in the brief, and the reason is that I was learning C# and .NET while building this. Roughly the first hour went on reading the brief and planning the schema and the endpoints, about five hours on Parts 1 and 2, and the rest on the mock AI service, the code review write up, and this README.

## A note on how this was built

I started learning C# and .NET a few hours before I started this. My background is backend work in Node.js and TypeScript, so most of the concepts carried over but the framework did not.

A good part of this codebase was written with AI in the loop. I used it as a pair programmer. I decided the things that matter, the schema, where the Postgres and Mongo split should sit, how the worker should retry and back off, what each endpoint should return. Then I used AI to turn that into working C# faster than I could type it myself right now, and to explain the parts of the framework I had not seen before.

I could have hand written more of it. Not in the time I had though, and not while picking up the language at the same time. So I put the time into getting the design right and understanding what the code actually does, instead of into typing it out.

Things I picked up along the way: how dependency injection and the options pattern work in ASP.NET Core, EF Core entity configurations and migrations, the MongoDB C# driver with its filter and update builders, and how BackgroundService fits into the application lifecycle.

The design decisions and trade offs in this README are mine.
