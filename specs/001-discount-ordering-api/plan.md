# Implementation Plan: Discount & Ordering API

**Branch**: `001-discount-ordering-api` | **Date**: 2026-09-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-discount-ordering-api/spec.md`

## Summary

An ASP.NET Core Web API that lets any caller browse a product catalog, manage users
(create/update with an assigned membership tier), and check out orders that receive an
automatic, tier-based discount (Normal 0%, Premium 20%, Super Premium 30%). The technical
approach — already decided during architecture discussion and recorded in `ARCHITECTURE.md`
— is a four-layer Clean/Onion solution (`Domain -> Application -> Infrastructure -> Api`)
using the Strategy pattern for the discount *mechanism* (percentage-off today, extensible to
other mechanisms later) with tier percentages resolved from configuration rather than
hardcoded per-tier classes, and the Repository pattern for persistence (in-memory in v1,
swappable for EF Core later without touching business logic).

## Technical Context

**Language/Version**: C# 12 / .NET 8 (LTS)

**Primary Dependencies**: ASP.NET Core Web API (Microsoft.AspNetCore.App shared framework);
built-in DI container with the Options pattern (`Microsoft.Extensions.Options`) for
config-driven discount percentages. No ORM in v1 (in-memory repositories); no additional
third-party NuGet packages required for the core feature.

**Storage**: In-memory, process-lifetime only (`ConcurrentDictionary`-backed repository
implementations), per NFR-2/SPEC.md §7. Not a database — data does not survive a restart.
Repository interfaces are the seam for a future EF Core (SQLite/SQL Server) implementation.

**Testing**: xUnit for unit tests. Discount/pricing logic (`PercentageDiscountStrategy`,
`ConfigurableDiscountStrategyResolver`, `PricingService`) MUST be testable with no ASP.NET
Core host and no concrete repository, per Constitution Principle V.

**Target Platform**: Cross-platform ASP.NET Core Web API (developed on Windows in this repo;
deployable to Linux/Windows containers or bare hosts — nothing in the design is
platform-specific).

**Project Type**: Single backend web service (no frontend/mobile client in scope for v1).

**Performance Goals**: Not specified in SPEC.md/spec.md — no explicit throughput or latency
SLA for v1. Standard ASP.NET Core Web API responsiveness for a small, in-memory dataset is
assumed; no performance work is in scope beyond avoiding obviously wasteful code.

**Constraints**: No authentication (NFR-1/FR context: acting user identified via an explicit
`userId`); no external network calls; no persistence durability requirement in v1.

**Scale/Scope**: Single API service instance; in-memory data bounded by process memory; no
defined concurrent-user target for v1. The layering in `ARCHITECTURE.md` is explicitly chosen
so this can scale to a real database and auth later without a rewrite, but that scaling work
itself is out of scope for v1 (see spec.md Assumptions / SPEC.md §8-9).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Compliance |
|---|---|
| I. SOLID as Structural Law | PASS — each service/strategy/repository has one responsibility; extension is via new `IDiscountStrategy`/repository implementations, never by editing existing ones; `Application`/`Api` depend only on `Domain` interfaces. |
| II. Layered Architecture | PASS — four-project solution (`Domain -> Application -> Infrastructure -> Api`), dependencies point inward only, matching `ARCHITECTURE.md` §1. |
| III. Deliberate Pattern Selection | PASS — Strategy used for discount *mechanism* (one `PercentageDiscountStrategy`, not one class per tier); Repository used for persistence abstraction. No speculative patterns (no Decorator/Chain of Responsibility) added for discount stacking, since that's out of scope for v1. |
| IV. Configuration-Driven Discount Extensibility | PASS — `DiscountSettings.TierPercentages` bound via `IOptionsSnapshot`; new percentage-off tier = enum member + config entry; `UserTier` stays a compile-time enum per the explicit decision already confirmed with the user. |
| V. Testability | PASS — `PricingService` and `IDiscountStrategy` implementations take plain values/interfaces, constructible in xUnit tests with no host or concrete repository. |
| VI. Simplicity / YAGNI | PASS — no discount-stacking pipeline, no admin-role plumbing, no EF Core built in v1; user management reuses the exact Repository+Service pattern already justified for products rather than inventing a new mechanism. |
| VII. Scoped v1 Boundaries | PASS — no auth, in-memory persistence, no discount stacking, matching spec.md's Assumptions/Out of Scope and SPEC.md §8-9. |

No violations. Complexity Tracking table is not needed.

**Post-Phase-1 re-check**: `data-model.md`, `contracts/api.yaml`, and `quickstart.md` were
reviewed against the same table above after design. No new dependencies, patterns, or
projects were introduced beyond what's listed (in particular, `UserService`/`UsersController`
reuse the existing Repository+Service pattern per research.md rather than adding a new one).
Gate remains PASS with no violations.

## Project Structure

### Documentation (this feature)

```text
specs/001-discount-ordering-api/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md         # Phase 1 output (/speckit-plan command)
├── contracts/            # Phase 1 output (/speckit-plan command)
├── checklists/
│   └── requirements.md
└── tasks.md              # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
  DiscountAndOrdering.Domain/
    Entities/
      User.cs
      Product.cs
      Order.cs
      OrderLineItem.cs
    Enums/
      UserTier.cs
    Interfaces/
      IDiscountStrategy.cs
      IProductRepository.cs
      IOrderRepository.cs
      IUserRepository.cs

  DiscountAndOrdering.Application/
    Discounts/
      PercentageDiscountStrategy.cs
      DiscountSettings.cs
      IDiscountStrategyResolver.cs
      ConfigurableDiscountStrategyResolver.cs
    Services/
      ProductService.cs
      UserService.cs
      OrderService.cs
      PricingService.cs
    Dtos/
      ProductDto.cs
      UserDto.cs
      CheckoutRequest.cs
      OrderResultDto.cs

  DiscountAndOrdering.Infrastructure/
    Repositories/
      InMemoryProductRepository.cs
      InMemoryOrderRepository.cs
      InMemoryUserRepository.cs

  DiscountAndOrdering.Api/
    Controllers/
      ProductsController.cs
      UsersController.cs
      OrdersController.cs
    Program.cs
    appsettings.json

tests/
  DiscountAndOrdering.UnitTests/
    Discounts/
      PercentageDiscountStrategyTests.cs
      ConfigurableDiscountStrategyResolverTests.cs
    Services/
      PricingServiceTests.cs
      OrderServiceTests.cs
      ProductServiceTests.cs
      UserServiceTests.cs
```

**Structure Decision**: Single ASP.NET Core Web API solution (`DiscountAndOrdering.sln`)
split into four projects following the Clean/Onion layering already decided in
`ARCHITECTURE.md` — `Domain`, `Application`, `Infrastructure`, `Api` — plus one xUnit test
project referencing `Domain` and `Application` only (never `Api` or `Infrastructure`
directly for business-rule tests, satisfying Constitution Principle V). `UsersController`
and `UserService`/`InMemoryUserRepository` are new relative to `ARCHITECTURE.md` (which
predates the resolved clarification that user management is an API capability in v1) but
follow the exact same Repository+Service pattern already established for products —
no new pattern is introduced, per Constitution Principle VI.

## Complexity Tracking

*No violations — table omitted.*
