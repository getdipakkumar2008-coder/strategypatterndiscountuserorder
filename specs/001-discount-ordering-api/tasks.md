---

description: "Task list for Discount & Ordering API v1"
---

# Tasks: Discount & Ordering API

**Input**: Design documents from `/specs/001-discount-ordering-api/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.yaml, quickstart.md, `.specify/memory/constitution.md`

**Tests**: Not requested in spec.md as a general TDD approach. Unit test tasks are included
only where Constitution Principle V constitutionally mandates independent testability —
the discount mechanism and pricing calculation (User Story 3). Other stories are validated
via the quickstart.md scenarios instead of a generated test suite, to avoid inflating scope
beyond what was requested (Constitution Principle VI, YAGNI).

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent
implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1..US6, matching spec.md)
- File paths are exact and repository-relative

## Path Conventions

Single-solution layout per plan.md: `src/DiscountAndOrdering.{Domain,Application,Infrastructure,Api}/`, `tests/DiscountAndOrdering.UnitTests/`.

---

## Phase 1: Setup

**Purpose**: Solution and project scaffolding.

- [X] T001 Create `DiscountAndOrdering.sln` at the repository root and the four projects
      `src/DiscountAndOrdering.Domain` (classlib), `src/DiscountAndOrdering.Application`
      (classlib), `src/DiscountAndOrdering.Infrastructure` (classlib),
      `src/DiscountAndOrdering.Api` (webapi), plus `tests/DiscountAndOrdering.UnitTests`
      (xunit), all targeting .NET 8; add project references: Application→Domain,
      Infrastructure→Domain, Api→Application+Infrastructure, UnitTests→Domain+Application
      only (never Api/Infrastructure, per Constitution Principle V); add all projects to
      the solution.
- [X] T002 [P] Add `Microsoft.Extensions.Options` (or confirm it's included transitively via
      the webapi SDK) as a reference from `src/DiscountAndOrdering.Application` so
      `IOptionsSnapshot<DiscountSettings>` is available without an Api-layer dependency.
- [X] T003 [P] Create `src/DiscountAndOrdering.Api/appsettings.json` with a `DiscountSettings`
      section: `{ "DiscountSettings": { "TierPercentages": { "Normal": 0, "Premium": 20,
      "SuperPremium": 30 } } }` (per research.md / ARCHITECTURE.md §3.2).

**Checkpoint**: Solution builds (`dotnet build`) with four empty projects + test project.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, enums, and repository abstractions that every user story
depends on. No user story work can begin until this phase is complete.

- [X] T004 [P] Create `UserTier` enum in `src/DiscountAndOrdering.Domain/Enums/UserTier.cs`
      with exactly the values `Normal`, `Premium`, `SuperPremium` (data-model.md).
- [X] T005 [P] Create `User` entity in `src/DiscountAndOrdering.Domain/Entities/User.cs`:
      `Id` (Guid, immutable), `Name` (string, required non-empty — FR-016), `Email` (string,
      required non-empty — FR-016), `Tier` (UserTier, required).
- [X] T006 [P] Create `Product` entity in
      `src/DiscountAndOrdering.Domain/Entities/Product.cs`: `Id` (Guid, immutable), `Name`
      (string, required non-empty — FR-003), `Description` (string, optional), `Price`
      (decimal, must be > 0), `StockQuantity` (int, must be >= 0).
- [X] T007 [P] Create `OrderLineItem` entity in
      `src/DiscountAndOrdering.Domain/Entities/OrderLineItem.cs`: `ProductId` (Guid),
      `ProductName` (string, snapshot of Product.Name at checkout time — FR-013),
      `UnitPrice` (decimal, snapshot of Product.Price at checkout time — FR-013), `Quantity`
      (int, must be > 0), `LineTotal` (decimal, computed as UnitPrice * Quantity).
- [X] T008 [P] Create `Order` entity in `src/DiscountAndOrdering.Domain/Entities/Order.cs`:
      `Id` (Guid, immutable), `UserId` (Guid), `OrderDate` (DateTimeOffset), `LineItems`
      (IReadOnlyList<OrderLineItem>, must contain at least one item — FR-007), `Subtotal`
      (decimal, sum of line totals — FR-008), `DiscountPercentage` (decimal, snapshotted at
      checkout time), `DiscountAmount` (decimal), `FinalTotal` (decimal).
- [X] T009 [P] Define `IUserRepository` in
      `src/DiscountAndOrdering.Domain/Interfaces/IUserRepository.cs` with
      `GetByIdAsync(Guid id)`, `AddAsync(User user)`, `UpdateAsync(User user)`.
- [X] T010 [P] Define `IProductRepository` in
      `src/DiscountAndOrdering.Domain/Interfaces/IProductRepository.cs` with
      `GetByIdAsync(Guid id)`, `GetAllAsync()`, `AddAsync(Product product)`,
      `UpdateAsync(Product product)`, `DeleteAsync(Guid id)`.
- [X] T011 [P] Define `IOrderRepository` in
      `src/DiscountAndOrdering.Domain/Interfaces/IOrderRepository.cs` with
      `GetByIdAsync(Guid id)`, `GetByUserIdAsync(Guid userId)`, `AddAsync(Order order)`.
- [X] T012 [P] Implement `InMemoryUserRepository` (ConcurrentDictionary-backed) in
      `src/DiscountAndOrdering.Infrastructure/Repositories/InMemoryUserRepository.cs`
      (depends on T005, T009).
- [X] T013 [P] Implement `InMemoryProductRepository` (ConcurrentDictionary-backed) in
      `src/DiscountAndOrdering.Infrastructure/Repositories/InMemoryProductRepository.cs`
      (depends on T006, T010).
- [X] T014 [P] Implement `InMemoryOrderRepository` (ConcurrentDictionary-backed) in
      `src/DiscountAndOrdering.Infrastructure/Repositories/InMemoryOrderRepository.cs`
      (depends on T007, T008, T011).
- [X] T015 Register `IUserRepository`, `IProductRepository`, `IOrderRepository` against
      their in-memory implementations as singletons in
      `src/DiscountAndOrdering.Api/Program.cs` (depends on T012, T013, T014; single shared
      file, not parallelizable with other Program.cs-editing tasks).

**Checkpoint**: Foundation ready — entities, interfaces, and repositories exist and are
registered; user story implementation can now begin.

---

## Phase 3: User Story 1 - Browse the product catalog (Priority: P1) 🎯

**Goal**: Any caller can list all products and retrieve a single product by ID (FR-001,
FR-002).

**Independent Test**: Call `GET /api/products` and `GET /api/products/{id}` and verify the
returned data matches what exists in the repository — no order or user logic involved.

### Implementation for User Story 1

- [X] T016 [P] [US1] Create `ProductDto` (Id, Name, Description, Price, StockQuantity) in
      `src/DiscountAndOrdering.Application/Dtos/ProductDto.cs` (per contracts/api.yaml
      `Product` schema).
- [X] T017 [US1] Implement `ProductService.GetAllAsync()` and `GetByIdAsync(Guid id)` in
      `src/DiscountAndOrdering.Application/Services/ProductService.cs`, mapping `Product`
      entities to `ProductDto` (depends on T006, T010, T016).
- [X] T018 [US1] Implement `ProductsController` `GET /api/products` and
      `GET /api/products/{id}` in
      `src/DiscountAndOrdering.Api/Controllers/ProductsController.cs`, returning 404 when
      the id doesn't match any product (FR-002) (depends on T017).

**Checkpoint**: User Story 1 is independently testable (against products added directly to
the repository, or later via User Story 6).

---

## Phase 4: User Story 2 - Manage user accounts (Priority: P1) 🎯

**Goal**: An operator can create a user with a tier and update an existing user's details or
tier (FR-016, FR-017).

**Independent Test**: `POST /api/users`, then `GET /api/users/{id}` to confirm the tier was
recorded, then `PUT /api/users/{id}` and re-`GET` to confirm the change — no catalog or
order involved.

### Implementation for User Story 2

- [X] T019 [P] [US2] Create `UserDto`, `CreateUserRequest`, `UpdateUserRequest` in
      `src/DiscountAndOrdering.Application/Dtos/UserDto.cs` (per contracts/api.yaml
      `User`/`CreateUserRequest`/`UpdateUserRequest` schemas).
- [X] T020 [US2] Implement `UserService.CreateAsync(...)` and `UpdateAsync(...)` in
      `src/DiscountAndOrdering.Application/Services/UserService.cs`, rejecting a create or
      update whose `Tier` is missing or not one of the defined `UserTier` values (FR-017)
      (depends on T005, T009, T019).
- [X] T021 [US2] Implement `UsersController` `POST /api/users` and `PUT /api/users/{id}` in
      `src/DiscountAndOrdering.Api/Controllers/UsersController.cs`, returning 400 on
      invalid/missing tier and 404 on an unknown id for update (depends on T020).
- [X] T022 [US2] Implement `UsersController` `GET /api/users/{id}` in
      `src/DiscountAndOrdering.Api/Controllers/UsersController.cs` (FR-002, needed to verify
      created/updated users) (depends on T020).

**Checkpoint**: User Story 2 is independently testable.

---

## Phase 5: User Story 3 - Check out an order with tier-based discount pricing (Priority: P2) 🎯

**Goal**: A checkout produces a final total that correctly reflects the ordering user's tier
discount — 0% / 20% / 30% (FR-006, FR-008..FR-013).

**Independent Test**: Submit a checkout for a known user/tier and known products; verify
subtotal, discount percentage, discount amount, and final total are all arithmetically
correct for that tier (SC-002).

### Tests for User Story 3 (Constitution Principle V — discount/pricing MUST be independently testable)

> Write these first; they exercise `Domain`/`Application` types directly with no host and no
> concrete repository.

- [X] T023 [P] [US3] Unit test `PercentageDiscountStrategy.ApplyDiscount` for 0%, 20%, and
      30% inputs in
      `tests/DiscountAndOrdering.UnitTests/Discounts/PercentageDiscountStrategyTests.cs`.
- [X] T024 [P] [US3] Unit test `ConfigurableDiscountStrategyResolver.Resolve` returns a
      strategy using the configured percentage for each of the three tiers, and throws when
      a tier has no configured entry, in
      `tests/DiscountAndOrdering.UnitTests/Discounts/ConfigurableDiscountStrategyResolverTests.cs`
      (use an in-memory `DiscountSettings` instance, no real `appsettings.json` load
      required).
- [X] T025 [P] [US3] Unit test `PricingService.CalculatePricing` computes subtotal, discount
      amount, and final total correctly for all three tiers given a known cart, in
      `tests/DiscountAndOrdering.UnitTests/Services/PricingServiceTests.cs`.

### Implementation for User Story 3

- [X] T026 [P] [US3] Define `IDiscountStrategy` with `decimal ApplyDiscount(decimal
      subtotal)` in `src/DiscountAndOrdering.Domain/Interfaces/IDiscountStrategy.cs`.
- [X] T027 [US3] Implement `PercentageDiscountStrategy(decimal percentage)` in
      `src/DiscountAndOrdering.Application/Discounts/PercentageDiscountStrategy.cs`
      (depends on T026).
- [X] T028 [US3] Create `DiscountSettings` options class (`Dictionary<string,decimal>
      TierPercentages`) in
      `src/DiscountAndOrdering.Application/Discounts/DiscountSettings.cs`.
- [X] T029 [US3] Define `IDiscountStrategyResolver` with `IDiscountStrategy Resolve(UserTier
      tier)` in
      `src/DiscountAndOrdering.Application/Discounts/IDiscountStrategyResolver.cs` (depends
      on T004, T026).
- [X] T030 [US3] Implement `ConfigurableDiscountStrategyResolver` in
      `src/DiscountAndOrdering.Application/Discounts/ConfigurableDiscountStrategyResolver.cs`,
      reading `DiscountSettings` via `IOptionsSnapshot<DiscountSettings>` and throwing when
      a tier has no configured percentage (depends on T027, T028, T029).
- [X] T031 [US3] Bind `DiscountSettings` from configuration and register
      `IDiscountStrategyResolver` → `ConfigurableDiscountStrategyResolver` in DI in
      `src/DiscountAndOrdering.Api/Program.cs` (depends on T030, T003; same file as T015).
- [X] T032 [US3] Implement `PricingService.CalculatePricing(lineItems, userTier)` — sums
      subtotal, resolves the discount strategy via `IDiscountStrategyResolver`, returns
      subtotal/discountPercentage/discountAmount/finalTotal — in
      `src/DiscountAndOrdering.Application/Services/PricingService.cs` (depends on T030).
- [X] T033 [P] [US3] Create `CheckoutRequest` and `OrderResultDto` (including nested line
      item DTO) in `src/DiscountAndOrdering.Application/Dtos/CheckoutRequest.cs` and
      `OrderResultDto.cs` (per contracts/api.yaml `CheckoutRequest`/`Order` schemas).
- [X] T034 [US3] Implement `OrderService.CheckoutAsync(CheckoutRequest request)`: load the
      user via `IUserRepository` for their tier (FR-015), load each product via
      `IProductRepository`, sum duplicate `productId` entries into one line item (spec.md
      Assumptions), build snapshotted `OrderLineItem`s (FR-013), call `PricingService` for
      the pricing breakdown, persist the resulting `Order` via `IOrderRepository`, and
      decrement each product's `StockQuantity` by the ordered amount (FR-011) — in
      `src/DiscountAndOrdering.Application/Services/OrderService.cs` (depends on T008, T009,
      T010, T011, T032, T033).
- [X] T035 [US3] Implement `OrdersController` `POST /api/orders/checkout` in
      `src/DiscountAndOrdering.Api/Controllers/OrdersController.cs`, returning 201 with the
      itemized order (depends on T034).
- [X] T036 [US3] Implement `OrdersController` `GET /api/orders/{id}` in
      `src/DiscountAndOrdering.Api/Controllers/OrdersController.cs`, returning 404 when not
      found (FR-014) (depends on T034).

**Checkpoint**: User Story 3's happy-path checkout is independently testable and produces
mathematically correct tier-based pricing.

---

## Phase 6: User Story 4 - Reject an invalid checkout with a clear reason (Priority: P2) 🎯

**Goal**: An empty cart, an unknown product id, an over-quantity request, or a
zero/negative quantity is rejected with a specific reason, and no order or stock change
occurs (FR-007).

**Independent Test**: Submit each invalid shape and confirm rejection with an identified
reason, and that no order was created and no stock changed (SC-003).

- [X] T037 [P] [US4] Unit test `OrderService.CheckoutAsync` rejects an empty item list, an
      unknown product id, an insufficient-stock request, and a non-positive quantity —
      each identifying the specific failing item and reason — with no `Order` persisted and
      no stock mutated, in
      `tests/DiscountAndOrdering.UnitTests/Services/OrderServiceTests.cs` (depends on T034;
      use fake repositories, no host).
- [X] T038 [US4] Add validation to `OrderService.CheckoutAsync` for an empty item list, any
      unknown product id, any quantity exceeding current stock, and any non-positive
      quantity — evaluated after summing duplicate product ids — returning a structured
      failure that identifies each bad item and its reason, with no repository writes on
      failure, in `src/DiscountAndOrdering.Application/Services/OrderService.cs` (depends on
      T034; same file as T034, not parallelizable with it).
- [X] T039 [US4] Map `OrderService` validation failures to a 400 response with per-item
      failure detail (matching contracts/api.yaml `CheckoutValidationError`) in
      `OrdersController` `POST /api/orders/checkout` in
      `src/DiscountAndOrdering.Api/Controllers/OrdersController.cs` (depends on T038, T035;
      same file as T035/T036).

**Checkpoint**: User Stories 3 and 4 together form the complete, robust checkout capability.

---

## Phase 7: User Story 5 - View past orders (Priority: P3)

**Goal**: A user can retrieve their full order history and any specific past order with its
itemized breakdown (FR-014).

**Independent Test**: After a checkout (User Story 3) exists, `GET
/api/users/{id}/orders` and `GET /api/orders/{id}` both return the same pricing shown at
checkout time (SC-004).

- [X] T040 [US5] Implement `OrderService.GetByIdAsync(Guid id)` and
      `GetByUserIdAsync(Guid userId)` (mapping to `OrderResultDto`) in
      `src/DiscountAndOrdering.Application/Services/OrderService.cs` (depends on T011, T033,
      T034; same file as T034/T038, not parallelizable with them).
- [X] T041 [US5] Implement `UsersController` `GET /api/users/{id}/orders` in
      `src/DiscountAndOrdering.Api/Controllers/UsersController.cs` (depends on T040).

Note: `GET /api/orders/{id}` was already implemented in T036 (User Story 3) — it's the same
endpoint used for both "see the order I just placed" and "retrieve a past order later"; no
separate task is needed here beyond T040's `GetByIdAsync` implementation.

**Checkpoint**: User Story 5 is independently testable given User Stories 2 and 3 already
provide data to view.

---

## Phase 8: User Story 6 - Manage the product catalog (Priority: P4)

**Goal**: An operator can add, update, and remove products (FR-003, FR-004, FR-005).

**Independent Test**: `POST` a product, see it via User Story 1's list, `PUT` to update it,
`DELETE` it and confirm it no longer appears.

- [X] T042 [US6] Implement `ProductService.CreateAsync(...)`, `UpdateAsync(...)`,
      `DeleteAsync(Guid id)` in
      `src/DiscountAndOrdering.Application/Services/ProductService.cs` (depends on T006,
      T010, T016, T017; same file as T017, not parallelizable with it).
- [X] T043 [US6] Implement `ProductsController` `POST /api/products`,
      `PUT /api/products/{id}`, `DELETE /api/products/{id}` in
      `src/DiscountAndOrdering.Api/Controllers/ProductsController.cs`, returning 404 on an
      unknown id for update/delete (depends on T042, T018; same file as T018).

**Checkpoint**: Full product catalog CRUD is complete; User Story 1 now has real data
beyond manual repository seeding.

---

## Phase 9: Polish & Cross-Cutting Concerns

- [X] T044 [P] Verify `dotnet build` succeeds with no warnings across the full solution.
- [X] T045 [P] Verify `dotnet test tests/DiscountAndOrdering.UnitTests` passes — confirms
      Constitution Principle V (discount/pricing logic testable with no host, no concrete
      repository).
- [X] T046 Run all six scenarios in `specs/001-discount-ordering-api/quickstart.md` against
      the running API and confirm SC-001 through SC-007 are met, including the
      config-only discount-percentage change in scenario 6 (Constitution Principle IV).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories.
- **User Stories (Phase 3-8)**: All depend on Foundational completion.
  - US1 (browse) and US2 (manage users) have no dependency on each other or on any other
    story — both can start immediately after Foundational.
  - US3 (checkout) depends only on Foundational (T004-T015), not on US1/US2/US6 being
    "done" — but a meaningful end-to-end demo of US3 needs a real user (US2) and product
    (US6) to exist first. See Implementation Strategy below.
  - US4 (reject invalid checkout) builds directly on US3's `OrderService`/`OrdersController`
    (same files) — sequenced after US3, not parallel with it.
  - US5 (view past orders) builds on US3's `OrderService` (same file) and needs US2/US3 data
    to be meaningful — sequenced after US3.
  - US6 (manage catalog) builds on US1's `ProductService`/`ProductsController` (same files)
    — sequenced after US1, not parallel with it.
- **Polish (Phase 9)**: Depends on all desired user stories being complete.

### Within Each User Story

- Tests (where included, US3/US4) are written before the implementation they cover.
- Domain/DTO types before services; services before controllers.
- Story complete and checkpointed before moving to the next.

### Parallel Opportunities

- All `[P]`-marked Setup tasks (T002, T003) can run in parallel.
- All `[P]`-marked Foundational tasks (T004-T014) can run in parallel — they are distinct
  files with no inter-dependencies beyond earlier entity/interface tasks.
- Once Foundational is complete, **US1 and US2 can be implemented fully in parallel** (no
  shared files, no shared logic).
- US3's three test tasks (T023-T025) and its `IDiscountStrategy`/DTO tasks (T026, T033) can
  run in parallel with each other.
- US4, US5, and US6 each modify files already touched by an earlier story (OrderService,
  OrdersController, ProductService, ProductsController respectively) and so are sequenced
  after that story rather than run in parallel with it.

---

## Parallel Example: Foundational Phase

```text
# Launch all Foundational entity/interface tasks together (distinct files):
Task: "Create UserTier enum in src/DiscountAndOrdering.Domain/Enums/UserTier.cs"
Task: "Create User entity in src/DiscountAndOrdering.Domain/Entities/User.cs"
Task: "Create Product entity in src/DiscountAndOrdering.Domain/Entities/Product.cs"
Task: "Create OrderLineItem entity in src/DiscountAndOrdering.Domain/Entities/OrderLineItem.cs"
Task: "Create Order entity in src/DiscountAndOrdering.Domain/Entities/Order.cs"
Task: "Define IUserRepository in src/DiscountAndOrdering.Domain/Interfaces/IUserRepository.cs"
Task: "Define IProductRepository in src/DiscountAndOrdering.Domain/Interfaces/IProductRepository.cs"
Task: "Define IOrderRepository in src/DiscountAndOrdering.Domain/Interfaces/IOrderRepository.cs"
```

## Parallel Example: User Story 1 + User Story 2 (after Foundational)

```text
# Two independent stories, no shared files — can be built simultaneously:
Story US1: T016 → T017 → T018 (ProductDto, ProductService reads, ProductsController GET)
Story US2: T019 → T020 → T021 → T022 (UserDto, UserService writes, UsersController)
```

---

## Implementation Strategy

### MVP scope

Per spec.md priorities, User Story 1 (browse) and User Story 2 (manage users) are both P1
and independently deliverable immediately after Foundational. However, the feature's actual
core value proposition — correct tier-based discount pricing — is User Story 3 (P2). A
demonstrable MVP of the *discount* capability specifically requires: Foundational → US2
(create a user with a tier) → US6 (create a product) → US3 (checkout and see the discount
applied). Flagging this explicitly rather than following the "P1 = MVP" convention
mechanically, since P1 alone (browse + manage users) doesn't yet demonstrate the feature's
differentiating value.

### Incremental delivery

1. Setup + Foundational → solution builds, no functionality yet.
2. US1 + US2 (parallel) → catalog is browsable, users can be created. Testable, but no
   ordering yet.
3. US3 → the core discount-checkout capability works end-to-end (needs a user + product
   from US2/US6, or repository-seeded test data, to exercise).
4. US4 → checkout is robust against invalid input.
5. US5 → order history is retrievable.
6. US6 → the catalog is manageable via the API instead of only seeded data.
7. Polish → build/test verification and full quickstart validation.

### Suggested single-developer order

T001-T015 (Setup + Foundational) → T016-T018 (US1) → T019-T022 (US2) → T023-T036 (US3) →
T037-T039 (US4) → T040-T041 (US5) → T042-T043 (US6) → T044-T046 (Polish).
