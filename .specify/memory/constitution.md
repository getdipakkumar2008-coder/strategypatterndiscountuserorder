<!--
Sync Impact Report
Version change: (none) → 1.0.0
Rationale: Initial ratification. No prior constitution existed (template was unfilled).
Modified principles: n/a (initial creation)
Added sections:
  - I. SOLID as Structural Law (NON-NEGOTIABLE)
  - II. Layered Architecture (Clean/Onion)
  - III. Deliberate Pattern Selection
  - IV. Configuration-Driven Discount Extensibility
  - V. Testability
  - VI. Simplicity / YAGNI
  - VII. Scoped v1 Boundaries
  - Technology & Persistence Constraints (Section 2)
  - Development Workflow (Section 3)
  - Governance
Removed sections: n/a
Deferred / TODO items: none — all placeholders resolved from prior project discussion
  recorded in SPEC.md and ARCHITECTURE.md at the repository root.
Templates requiring follow-up: none checked yet — plan-template.md, spec-template.md,
  tasks-template.md are read at runtime by their respective commands and were not
  modified by this command per its scope guard.
-->

# Discount & Ordering API Constitution

## Core Principles

### I. SOLID as Structural Law (NON-NEGOTIABLE)
Every class MUST have exactly one reason to change. New behavior (a new user tier, a
new discount mechanism, a new persistence backend) MUST be added by introducing a new
implementation of an existing interface, never by modifying an existing class's
internals. High-level modules (services, controllers) MUST depend only on abstractions
defined in the Domain layer; they MUST NOT reference concrete Infrastructure types.
Interfaces MUST stay narrow and role-specific (one per aggregate/concern) rather than
broad, multi-purpose contracts. Any implementation of an interface MUST be fully
substitutable for any other implementation of that same interface, with no special-case
behavior the caller needs to know about.
Rationale: this is the mechanism by which "extendable in future" is actually achieved —
without it, extensibility is a claim, not a property of the code.

### II. Layered Architecture (Clean/Onion)
The codebase MUST be organized as `Domain -> Application -> Infrastructure -> Api`.
`Domain` (entities, enums, interfaces) MUST have zero outward dependencies. `Application`
(services, use-case orchestration) MUST depend only on `Domain` interfaces. `Infrastructure`
(repository implementations) MUST implement `Domain` interfaces and MUST NOT be referenced
by `Application`. `Api` (controllers, DI composition root) is the only layer permitted to
wire concrete `Infrastructure` types to `Domain` interfaces, and MUST do so exclusively in
the startup/composition root, not scattered through business logic.
Rationale: dependencies pointing inward is what makes swapping persistence, adding auth, or
changing frameworks a boundary change instead of a rewrite.

### III. Deliberate Pattern Selection
Design patterns MUST be chosen because they solve a concrete, present requirement — never
added speculatively ahead of an actual second use case. The Strategy pattern MUST be used
for discount *mechanisms* (distinct pricing algorithms: percentage-off, and later
flat-amount, BOGO, threshold-based, etc.) — NOT as one class per user tier, since tiers
sharing the same mechanism differ only by a configuration value, not by algorithm. The
Repository pattern MUST be used to abstract persistence so that swapping the backing store
(in-memory now, EF Core later) requires zero changes to `Application` services or `Api`
controllers. No additional pattern (Decorator, Chain of Responsibility, Factory, etc.) may
be introduced until the requirement it would serve is real and specified, not hypothetical.
Rationale: agreed during architecture discussion — patterns are a cost paid for a specific
extensibility need; paying it before the need exists is premature abstraction.

### IV. Configuration-Driven Discount Extensibility
Tier discount percentages MUST be configuration values, not compiled literals — changing a
percentage MUST require a configuration change only, no rebuild. Adding a new tier that
uses an existing discount mechanism (percentage-off) MUST require only a `UserTier` enum
member plus a configuration entry — no new class and no change to `PricingService`,
`OrderService`, or any controller. Adding a genuinely new discount mechanism MUST require
exactly one new `IDiscountStrategy` implementation and MUST NOT require changes to any
existing discount class. `UserTier` remains a compile-time C# enum by explicit project
decision — runtime/admin-created tiers are out of scope for this project; new tiers are a
deliberate, occasional change that ships with a deploy.
Rationale: reflects the explicit decision made during architecture discussion — percentages
change often (business/promotional decisions) and must not require a code deploy; the tier
list itself changes rarely and a small deploy for a new enum member is acceptable.

### V. Testability
Discount and pricing logic MUST be unit-testable in complete isolation from the `Api`/web
layer and from any concrete persistence implementation. `PricingService` and every
`IDiscountStrategy` implementation MUST be constructible and verifiable with in-memory
values alone — no HTTP pipeline, no database, no filesystem I/O required to test a discount
calculation.
Rationale: pricing correctness is the core business value of this API; it must be
verifiable fast and in isolation, not only through end-to-end tests.

### VI. Simplicity / YAGNI
Error handling, validation, configuration knobs, and abstractions MUST correspond to a real,
specified requirement — not a hypothetical future one. A bug fix MUST NOT carry unrelated
refactors. Duplicated logic MUST NOT be abstracted until a genuine third occurrence appears;
two or three similar lines are preferable to a premature shared abstraction that guesses at
future variation.
Rationale: matches the project's explicit "don't design for hypothetical future
requirements" guidance — extensibility comes from correct seams (Principles I-IV), not from
maximizing configurability everywhere.

### VII. Scoped v1 Boundaries
Authentication/authorization, real database persistence, and discount stacking (coupons,
seasonal promotions layered on tier discount) are explicitly OUT of scope for v1, per
SPEC.md §8/§9. v1 MUST identify the acting user via an explicit `userId` parameter and MUST
use in-memory repositories behind `Domain` interfaces. These exclusions are deliberate
scope decisions, not omissions — work MUST NOT silently expand to cover them without an
explicit decision to amend this constitution and SPEC.md.
Rationale: keeps v1 focused on the actual ask (discount/product/order logic) while the
layering and pattern choices (Principles I-IV) ensure these exclusions can be added later
without rearchitecting.

## Technology & Persistence Constraints

- Runtime/framework: ASP.NET Core Web API targeting .NET 8.
- Persistence (v1): in-memory repositories (e.g. `ConcurrentDictionary`-backed) implementing
  `Domain` repository interfaces (`IProductRepository`, `IOrderRepository`,
  `IUserRepository`). A future EF Core implementation MUST be substitutable via a DI
  registration change alone.
- Configuration: discount tier percentages are bound via the Options pattern
  (`IOptionsSnapshot<DiscountSettings>` or equivalent) from `appsettings.json`, not hardcoded
  in strategy classes.
- Testing: xUnit for unit tests; business-rule tests (discount, pricing) MUST NOT reference
  ASP.NET Core hosting or a concrete repository implementation.
- Data integrity: product name and unit price MUST be snapshotted onto an order at checkout
  time so historical orders remain accurate if the product later changes.

## Development Workflow

This project follows Spec-Driven Development via Spec Kit:
`/speckit-constitution` (this document) -> `/speckit-specify` -> `/speckit-plan` ->
`/speckit-tasks` -> `/speckit-implement`. `SPEC.md` and `ARCHITECTURE.md` at the repository
root are the living functional/technical references produced by prior discussion and MUST
be treated as authoritative input when running `/speckit-specify` and `/speckit-plan` —
they are not to be silently re-derived or contradicted without an explicit decision to
amend them. Every generated plan and task list MUST be checked against the Core Principles
above before `/speckit-implement` proceeds; a plan that requires violating a principle MUST
either be revised or the violation MUST be explicitly justified and recorded against the
principle it deviates from.

## Governance

This constitution supersedes ad hoc practice for this project. Amendments require an
explicit decision recorded in this document (via a Sync Impact Report) and, where an
amendment changes scope or exclusions, a corresponding update to `SPEC.md`. Versioning
follows semantic versioning: MAJOR for backward-incompatible principle removals or
redefinitions, MINOR for new principles or materially expanded guidance, PATCH for
clarifications and wording fixes. All specs, plans, and task lists produced by later
Spec Kit commands MUST be reviewed for compliance with the Core Principles above; any
necessary complexity beyond what a principle allows MUST be explicitly justified in the
relevant plan rather than silently introduced.

**Version**: 1.0.0 | **Ratified**: 2026-09-29 | **Last Amended**: 2026-09-29
