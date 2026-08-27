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

Creating a submission writes to Postgres first, then MongoDB. If the MongoDB write fails after the Postgres write already succeeded, there is no rollback. The vendor just resubmits and gets the next revision number.

No automated tests in this pass. This was a deliberate choice to spend the available time on the API and the AI integration instead.

The mock AI service is intentionally simple. It keeps jobs in memory only, with no persistence, and its verdicts are random rather than based on any real analysis.

## A note on how this was built

I am new to .NET and C#. I used AI assistance to speed up learning the framework and help in writing the code. The design decisions and trade offs described in this README are mine.
