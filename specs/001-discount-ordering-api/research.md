# Phase 0 Research: Discount & Ordering API

All technical unknowns for this feature were already resolved during prior architecture
discussion (recorded in `ARCHITECTURE.md` and `SPEC.md` at the repository root) before this
plan was generated. No `NEEDS CLARIFICATION` markers remain in the Technical Context. This
document records those decisions in the standard research format for traceability, plus one
new decision (user management pattern) made while reconciling the plan with the resolved
spec clarification.

## Decision: Discount mechanism modeled as Strategy, not one class per tier

**Rationale**: The three v1 tiers all share one algorithm — percentage off the subtotal.
Modeling each tier as its own `IDiscountStrategy` class would mean every future tier and
every percentage tweak both require a code change and redeploy, which Constitution
Principle IV explicitly rules out. A single `PercentageDiscountStrategy(decimal percentage)`
covers all percentage-off tiers; the Strategy pattern is reserved for genuinely different
discount *mechanisms* added later (flat amount, BOGO, threshold-based).

**Alternatives considered**:
- One `IDiscountStrategy` class per tier (`PremiumDiscountStrategy`, etc.) — rejected: couples
  a data value (percentage) to a compiled class, violating Principle IV.
- A `switch` on `UserTier` inside `PricingService` — rejected: violates Open/Closed
  (Principle I); every new tier would require editing `PricingService`.

## Decision: Tier -> percentage resolution via configuration, not DI keying on tier

**Rationale**: `ConfigurableDiscountStrategyResolver` reads a `DiscountSettings.TierPercentages`
dictionary bound from `appsettings.json` via `IOptionsSnapshot<DiscountSettings>`, and
constructs a `PercentageDiscountStrategy` with the configured value. This makes a percentage
change a configuration edit with no rebuild, and a new percentage-off tier a config entry
plus one `UserTier` enum member — no new class, no DI registration change.

**Alternatives considered**:
- .NET keyed DI services (`AddKeyedScoped<IDiscountStrategy>(UserTier.Premium, ...)`) — this
  was the original design during early architecture discussion, but was superseded once the
  requirement "different discount configuration" was made explicit: keyed DI still requires a
  compiled class + registration per tier, which doesn't satisfy config-only percentage changes.

## Decision: `UserTier` remains a compile-time C# enum

**Rationale**: Explicitly confirmed with the user. New tiers are a deliberate, occasional
business decision that ships with a (small) deploy — an enum member plus a config entry, no
business-logic class touched. Fully dynamic, admin-created tiers were explicitly ruled out as
out of scope.

**Alternatives considered**:
- String/code-based tier resolved from a database `Tiers` table (fully dynamic, zero-redeploy
  tier creation) — rejected by explicit user decision; adds persistence/validation complexity
  not justified by any stated v1 requirement (Constitution Principle VI).

## Decision: Repository pattern for persistence, in-memory implementation for v1

**Rationale**: `IProductRepository`, `IOrderRepository`, `IUserRepository` live in `Domain`;
`Infrastructure` provides `ConcurrentDictionary`-backed implementations. `Application` and
`Api` depend only on the interfaces, so a future EF Core implementation is a DI registration
change, not a rewrite (Constitution Principles I, II).

**Alternatives considered**:
- Direct EF Core from the start — rejected: no requirement demands real persistence in v1
  (NFR-2/SPEC.md §7); adding it now would be speculative work ahead of the actual need
  (Principle VI, YAGNI).
- One combined repository interface for all aggregates — rejected: violates Interface
  Segregation (Principle I); a caller needing only product data shouldn't depend on order/user
  operations.

## Decision: User management (create/update) via the same Repository+Service pattern as products

**Rationale**: The resolved spec clarification (spec.md User Story 2, FR-016/FR-017) adds
user creation/update as an API capability. `ARCHITECTURE.md` predates this decision and
doesn't mention a `UserService`/`UsersController` explicitly, but no new pattern is needed:
`IUserRepository` was already planned in `ARCHITECTURE.md` §2 (for lookups), so this plan
extends it with create/update methods and adds a `UserService` mirroring `ProductService`
exactly. This keeps the decision consistent with Constitution Principle VI (no new
abstraction where an existing one already fits) and Principle III (deliberate pattern reuse,
not a new pattern introduced without a new requirement demanding it).

**Alternatives considered**:
- A separate "admin" module/pattern for user management distinct from product management —
  rejected: v1 has no admin role distinction (Constitution Principle VII / SPEC.md §9); would
  be an unjustified structural split.

## Decision: xUnit for unit tests, no ASP.NET Core host or concrete repository in business-rule tests

**Rationale**: Constitution Principle V requires discount/pricing logic to be testable in
isolation. `PricingService` and `IDiscountStrategy` implementations take plain values and
interfaces as constructor/method parameters, so xUnit tests can exercise them directly with
in-memory fakes/values — no `WebApplicationFactory`, no real repository.

**Alternatives considered**:
- Integration-test-only strategy (spin up the full API per test) — rejected: slower, and
  would make it impossible to test discount math in true isolation as the constitution
  requires.
