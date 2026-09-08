# Enterprise Procurement & Approval Platform — Design Analysis

This is the "first 30 minutes" deliverable: business understanding and design,
produced before any application code was written.

---

## 1. Actors / Roles

| Role | Summary |
|---|---|
| **Employee** | Raises a business need, creates/edits DRAFT requests, submits them, tracks status, cancels while still cancellable. |
| **Manager** | First approval gate. Approves/rejects requests submitted by their reports, adds comments. |
| **Finance** | Validates budget, approves/rejects on financial grounds, sees payment info. |
| **Procurement Admin** | Reviews finance-approved requests, selects the vendor, triggers procurement/payment, marks completion. |

Each role maps to exactly one `Role` on a `User` in this simple version (no multi-role users, no delegation) — enough for the capstone scope, extensible later.

**What each role must NOT do** (negative authorization — as important as the positive list):
- Employee cannot approve their own request, cannot see other employees' requests, cannot skip stages.
- Manager cannot approve requests they don't manage, cannot touch Finance/Procurement actions.
- Finance cannot approve a request that hasn't cleared Manager approval.
- Procurement cannot select a vendor or pay before Finance approval.
- No role can move a request backwards except via the explicit Reject path.

---

## 2. Business Capabilities

- **Procurement Management** – lifecycle of a purchase request end to end.
- **Approval Management** – manager + finance approval gates, comments, decisions.
- **Vendor Management** – vendor record(s) selectable at the procurement stage.
- **Budget Validation** – Finance's check before financial sign-off.
- **Payment Processing** – single payment method for now, pluggable later (Strategy).
- **Notifications** – status-change notifications (stubbed/logged in this simple version).
- **Audit / Reporting** – full status-history trail (directly solves "no audit trail" pain point).

---

## 3. Main Workflow (state machine)

```
DRAFT
  │ (Employee submits)
  ▼
SUBMITTED
  │ (Manager approves)              │ (Manager rejects)
  ▼                                  ▼
MANAGER_APPROVED                  REJECTED
  │ (Finance approves)              ▲
  ▼                                  │ (Finance rejects)
FINANCE_APPROVED ─────────────────────
  │ (Procurement selects vendor)
  ▼
VENDOR_SELECTED
  │ (Procurement triggers payment)
  ▼
PAYMENT_IN_PROGRESS
  │ (Payment succeeds)      │ (Payment fails)
  ▼                          ▼
COMPLETED                 PAYMENT_FAILED
                              │ (Procurement retries)
                              ▼
                        PAYMENT_IN_PROGRESS
```

Cancellation: Employee may cancel only while status is `DRAFT` or `SUBMITTED` (before Manager acts) → `CANCELLED`.

**Valid transition table** (enforced server-side in one place — see Change Points):

| From | To | Actor | Action |
|---|---|---|---|
| DRAFT | SUBMITTED | Employee | Submit |
| DRAFT | CANCELLED | Employee | Cancel |
| SUBMITTED | CANCELLED | Employee | Cancel |
| SUBMITTED | MANAGER_APPROVED | Manager | Approve |
| SUBMITTED | REJECTED | Manager | Reject |
| MANAGER_APPROVED | FINANCE_APPROVED | Finance | Approve budget |
| MANAGER_APPROVED | REJECTED | Finance | Reject |
| FINANCE_APPROVED | VENDOR_SELECTED | Procurement | Select vendor |
| VENDOR_SELECTED | PAYMENT_IN_PROGRESS | Procurement | Trigger payment |
| PAYMENT_IN_PROGRESS | COMPLETED | System/Procurement | Payment succeeds |
| PAYMENT_IN_PROGRESS | PAYMENT_FAILED | System/Procurement | Payment fails |
| PAYMENT_FAILED | PAYMENT_IN_PROGRESS | Procurement | Retry payment |

Any transition not in this table is rejected with `409 Conflict`.

---

## 4. Core Entities

- **User** — identity, role, manager relationship.
- **PurchaseRequest** — the aggregate root; status, amounts, justification, links to requester/vendor/payment.
- **RequestApproval** — one row per approval decision (Manager/Finance), with comment.
- **RequestStatusHistory** — append-only audit trail of every state transition (who/when/from/to).
- **Vendor** — vendor master data (one seeded vendor for this phase).
- **Payment** — 1:1 with a request once it reaches procurement; one payment method for this phase.

See **DB Schema** below for full column-level detail.

---

## 5. Proposed Architecture

```
React SPA (Vite)
   │  fetch/axios + JWT bearer token
   ▼
ASP.NET Core Web API
   Controllers        → HTTP in/out, model binding, [Authorize(Roles=...)]
   Services           → business rules, state-machine enforcement
   Repositories (EF Core DbContext) → data access
   Middleware         → global exception handler, JWT auth, request logging
   ▼
SQLite (EF Core Code-First, file DB) — swappable for SQL Server later
```

Layering mirrors the PDF's Route → Controller → Service → Repository → Database model.
No business logic in controllers; no EF/SQL in services (services depend on repository interfaces, not `DbContext` directly, for DIP).

---

## 6. External Dependencies

- **Payment provider** — abstracted behind `IPaymentGateway` (Strategy), one concrete `FakePaymentGateway` implementation for now that simulates success/failure.
- **Vendor directory** — modeled as a local `Vendor` table for now (would be an external system in a real deployment — same `IVendorProvider` seam is left open).
- **Notification channel** — `INotificationService`, logged to console/DB for this phase; email/Slack are future strategies.

---

## 7. Security Boundaries

- **Public**: `POST /api/auth/login` only.
- **Authenticated**: everything else requires a valid JWT bearer token.
- **Authorization**: enforced via ASP.NET `[Authorize(Roles = "...")]` at the controller-action level, **and** re-checked in the service layer against the specific request (e.g. a Manager can only approve requests where they are the requester's manager) — controller-level role check is necessary but not sufficient.
- Passwords hashed with `PBKDF2` (`Microsoft.AspNetCore.Identity.PasswordHasher`).
- JWT signing key read from configuration/environment (`Jwt:Key`), never hardcoded in source for real deployments (seed value used only for local dev).
- 401 = no/invalid/expired token. 403 = valid token, wrong role/ownership.

---

## 8. Failure Scenarios

| Dependency | Failure | Mitigation |
|---|---|---|
| Payment gateway | Times out / errors | Timeout + single retry; on exhausted retries → `PAYMENT_FAILED`, never silently stuck. |
| Payment gateway | Duplicate trigger (double-click) | Idempotency: only `VENDOR_SELECTED`/`PAYMENT_FAILED` states allow triggering payment; the transition itself is the idempotency guard. |
| Any request | Concurrent approval (two managers click at once) | Optimistic concurrency token (`RowVersion`) on `PurchaseRequest`; second writer gets `409`. |
| DB unavailable | API can't reach SQLite/SQL Server | Global exception middleware returns `503` with a generic message, logs detail server-side. |
| Bad state transition | Client sends stale UI action | Service validates current status against the transition table before mutating; returns `409` with the valid next actions. |

---

## 9. Candidate Patterns

**Strategy — Payment method**
- Problem: payment processing can use multiple providers/methods.
- Approach: `IPaymentGateway` interface, one implementation registered via DI for this phase.
- Why: payment behaviour varies while the procurement workflow stays the same.
- Alternative considered: `if/else` on a payment-type string in the service — rejected because adding a provider would mean editing existing tested business logic (violates OCP).

**State — Request lifecycle**
- Problem: allowed actions and behaviour depend entirely on current status.
- Approach: a single `RequestStateMachine` class owns the transition table (Section 3) and is the only place transitions are validated/applied.
- Why: keeps "what can happen next" in one testable place instead of scattered `if (status == ...)` checks across controllers/services.
- Alternative considered: status checks duplicated in every controller action — rejected, error-prone and violates DRY/SRP.

**Repository — Data access**
- Problem: services should not know about EF Core/SQL directly.
- Approach: `IPurchaseRequestRepository` etc., implemented with EF Core.
- Why: keeps persistence swappable and services unit-testable without a real DB.
- Alternative considered: inject `DbContext` straight into services — rejected, couples business logic to EF Core (violates DIP), harder to test.

**Facade — Complete procurement**
- Problem: completing procurement touches vendor selection, payment, notification, and audit history in one operation.
- Approach: `ProcurementService.CompleteProcurement(...)` coordinates the sub-steps behind one call.
- Why: callers (controller) shouldn't need to know the internal sequence.
- Alternative considered: controller calls four services directly — rejected, leaks orchestration logic into the HTTP layer.

**(Deferred, not built in this phase)** Observer for notifications, Chain of Responsibility for approvals — the approval chain is currently just two fixed sequential gates (Manager → Finance), so a full Chain of Responsibility is not yet justified; revisit if more approval stages are added.

---

## 10. Folder / Project Structure

```
ProcurementPlatform/
├─ docs/
│  └─ ANALYSIS.md                 (this file)
├─ backend/
│  └─ ProcurementApi/
│     ├─ Controllers/             AuthController, PurchaseRequestsController, VendorsController
│     ├─ Services/                PurchaseRequestService, RequestStateMachine, PaymentGateway
│     ├─ Repositories/            IPurchaseRequestRepository + EF implementation
│     ├─ Domain/                  Entities: User, PurchaseRequest, RequestApproval, RequestStatusHistory, Vendor, Payment + enums
│     ├─ DTOs/                    Request/response models (never expose entities directly)
│     ├─ Data/                    ProcurementDbContext, DbSeeder
│     ├─ Middleware/              ExceptionHandlingMiddleware
│     └─ Program.cs               DI, JWT config, pipeline
└─ frontend/
   └─ src/
      ├─ api/                     apiClient.js, authService.js, requestService.js
      ├─ context/                 AuthContext (JWT + role in memory/localStorage)
      ├─ components/               RequestList, RequestForm, StatusBadge, RoleGuard
      ├─ pages/                    Login, EmployeeDashboard, ManagerDashboard, FinanceDashboard, ProcurementDashboard, RequestDetail
      └─ App.jsx                  routing only
```

---

## DB Schema

### Entity-Relationship overview

```
Users (1)───────────(many) PurchaseRequests            (Requester_Id FK)
Users (1)───────────(many) RequestApprovals             (Approver_Id FK)
Users (1)───────────(many) RequestStatusHistory         (ChangedBy_Id FK)
PurchaseRequests (1)─(many) RequestApprovals            (PurchaseRequestId FK)
PurchaseRequests (1)─(many) RequestStatusHistory        (PurchaseRequestId FK)
PurchaseRequests (1)─(0..1) Payments                    (PurchaseRequestId FK, unique)
Vendors (1)──────────(many) PurchaseRequests            (VendorId FK, nullable)
```

### Table: Users
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier (PK) | |
| FullName | nvarchar(200) | |
| Email | nvarchar(256) | unique index |
| PasswordHash | nvarchar(max) | PBKDF2 hash |
| Role | int (enum: Employee=1, Manager=2, Finance=3, ProcurementAdmin=4) | |
| ManagerId | uniqueidentifier (FK → Users.Id, nullable) | who approves this employee's requests |
| Department | nvarchar(100) | |
| CreatedAt | datetime2 | |

### Table: PurchaseRequests
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier (PK) | |
| RequestNumber | nvarchar(20) | unique, e.g. `PR-2026-000123` |
| RequesterId | uniqueidentifier (FK → Users.Id) | |
| Title | nvarchar(200) | |
| BusinessJustification | nvarchar(2000) | |
| Department | nvarchar(100) | |
| EstimatedQuantity | int | |
| EstimatedUnitCost | decimal(18,2) | |
| EstimatedTotalCost | decimal(18,2) | computed at save time |
| Status | int (enum, see Section 3) | |
| VendorId | uniqueidentifier (FK → Vendors.Id, nullable) | set at VENDOR_SELECTED |
| RowVersion | rowversion / timestamp | optimistic concurrency |
| CreatedAt | datetime2 | |
| SubmittedAt | datetime2 (nullable) | |
| ManagerApprovedAt | datetime2 (nullable) | |
| FinanceApprovedAt | datetime2 (nullable) | |
| CompletedAt | datetime2 (nullable) | |

### Table: RequestApprovals
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier (PK) | |
| PurchaseRequestId | uniqueidentifier (FK) | |
| ApproverId | uniqueidentifier (FK → Users.Id) | |
| Stage | int (enum: ManagerApproval=1, FinanceApproval=2) | |
| Decision | int (enum: Approved=1, Rejected=2) | |
| Comment | nvarchar(1000) (nullable) | |
| DecidedAt | datetime2 | |

### Table: RequestStatusHistory
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier (PK) | |
| PurchaseRequestId | uniqueidentifier (FK) | |
| FromStatus | int (nullable) | null for the initial DRAFT creation row |
| ToStatus | int | |
| ChangedByUserId | uniqueidentifier (FK → Users.Id) | |
| ChangedAt | datetime2 | |
| Notes | nvarchar(500) (nullable) | |

### Table: Vendors
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier (PK) | |
| Name | nvarchar(200) | |
| ContactEmail | nvarchar(256) | |
| ContactPhone | nvarchar(30) | |
| IsActive | bit | |

### Table: Payments
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier (PK) | |
| PurchaseRequestId | uniqueidentifier (FK, unique) | 1:1 |
| Amount | decimal(18,2) | |
| Method | int (enum: BankTransfer=1) | single method for this phase |
| Status | int (enum: Pending=1, Success=2, Failed=3) | |
| TransactionReference | nvarchar(100) (nullable) | |
| FailureReason | nvarchar(500) (nullable) | |
| ProcessedAt | datetime2 (nullable) | |
