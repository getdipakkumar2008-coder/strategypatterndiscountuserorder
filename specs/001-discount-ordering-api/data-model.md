# Phase 1 Data Model: Discount & Ordering API

Entities and validation rules derived from `spec.md` Key Entities and Functional
Requirements (FR-001..FR-017), and `SPEC.md` §6. All entities live in
`DiscountAndOrdering.Domain/Entities/`; `UserTier` lives in
`DiscountAndOrdering.Domain/Enums/`.

## UserTier (enum)

```csharp
public enum UserTier
{
    Normal,
    Premium,
    SuperPremium,
    Platinum
}
```

Compile-time enum per Constitution Principle IV / research.md — new tiers are added here
plus a `DiscountSettings` config entry, not by modifying any discount class.

## User

| Field | Type | Rules |
|---|---|---|
| `Id` | `Guid` | Generated on creation; immutable. |
| `Name` | `string` | Required, non-empty (FR-016). |
| `Email` | `string` | Required, non-empty (FR-016). Format validation beyond "non-empty" is out of scope for v1 (no requirement specifies it). |
| `Tier` | `UserTier` | Required on create; MUST be one of the defined enum values — a create/update specifying a missing or unrecognized tier is rejected (FR-017). |

**Relationships**: One `User` has many `Order`s (via `Order.UserId`).

**Lifecycle**: Created via `POST /api/users` (FR-016); `Name`/`Email`/`Tier` updatable via
`PUT /api/users/{id}` (FR-017). No delete operation is specified for v1 (spec.md does not
request one, unlike Product — orders reference users, so deleting a user with order history
is a data-integrity question not raised in scope; omitted rather than guessed at).

## Product

| Field | Type | Rules |
|---|---|---|
| `Id` | `Guid` | Generated on creation; immutable. |
| `Name` | `string` | Required, non-empty (FR-003). |
| `Description` | `string` | Optional free text (FR-003). |
| `Price` | `decimal` | Required, MUST be > 0 (a zero/negative price has no reasonable interpretation for a sellable product; not explicitly stated in spec.md but follows directly from FR-008's subtotal calculation being meaningless otherwise). |
| `StockQuantity` | `int` | Required, MUST be >= 0 (FR-003/FR-011). |

**Relationships**: Referenced by `OrderLineItem.ProductId`. Referenced by, but not owned by,
`Order`.

**Lifecycle**: Created via `POST /api/products` (FR-003); updated via `PUT /api/products/{id}`
(FR-004); removed via `DELETE /api/products/{id}` (FR-005). `StockQuantity` is also mutated
internally by `OrderService` on successful checkout (FR-011) — this is a system-driven
update, not a direct API write.

## Order

| Field | Type | Rules |
|---|---|---|
| `Id` | `Guid` | Generated on creation; immutable. |
| `UserId` | `Guid` | Required; MUST reference an existing `User` at checkout time (looked up for tier — FR-015). |
| `OrderDate` | `DateTimeOffset` | Set at checkout time; immutable. |
| `LineItems` | `IReadOnlyList<OrderLineItem>` | MUST contain at least one item (FR-007: empty cart is rejected before an `Order` is ever constructed). |
| `Subtotal` | `decimal` | Sum of all line items' `LineTotal` (FR-008). Computed, not independently settable. |
| `DiscountPercentage` | `decimal` | The percentage applied for the ordering user's tier at checkout time (FR-009). Snapshotted — not re-derived later if the tier's configured percentage subsequently changes. |
| `DiscountAmount` | `decimal` | `Subtotal * DiscountPercentage / 100` (FR-009). |
| `FinalTotal` | `decimal` | `Subtotal - DiscountAmount` (FR-009). |

**Relationships**: Belongs to one `User` (`UserId`). Has many `OrderLineItem`s (composition
— line items have no independent existence outside their order).

**Lifecycle**: Created atomically as part of a successful checkout (FR-010..FR-012); never
updated after creation (FR-013 — pricing is immutable once recorded); retrieved via
`GET /api/orders/{id}` and `GET /api/users/{id}/orders` (FR-014). No update/delete operation
exists for `Order` — matches spec.md (no story or FR proposes modifying/canceling a placed
order in v1).

## OrderLineItem

| Field | Type | Rules |
|---|---|---|
| `ProductId` | `Guid` | Reference to the `Product` ordered. |
| `ProductName` | `string` | **Snapshot** of `Product.Name` at checkout time (FR-013) — independent of later product renames. |
| `UnitPrice` | `decimal` | **Snapshot** of `Product.Price` at checkout time (FR-013) — independent of later price changes. |
| `Quantity` | `int` | MUST be > 0 (spec.md Edge Cases: a zero/negative quantity is rejected before checkout proceeds). If the same product appears more than once in a single checkout request, quantities are summed into one line item (spec.md Assumptions). |
| `LineTotal` | `decimal` | `UnitPrice * Quantity`. Computed, not independently settable. |

**Relationships**: Owned by exactly one `Order`. References (but does not snapshot the
live state of) a `Product`.

## Discount configuration (not a persisted entity — bound from `appsettings.json`)

```csharp
public sealed class DiscountSettings
{
    // key = UserTier name (e.g. "Normal", "Premium", "SuperPremium")
    public Dictionary<string, decimal> TierPercentages { get; set; } = new();
}
```

Not stored via any repository — this is application configuration, bound once per request
scope via `IOptionsSnapshot<DiscountSettings>` (Constitution's Technology & Persistence
Constraints; research.md). Included here because it directly determines `Order.DiscountPercentage`
at checkout time.

## Validation summary (cross-entity)

- A checkout request is rejected outright (FR-007) — before any `Order` is constructed — if:
  the item list is empty; any `ProductId` does not resolve to an existing `Product`; any
  requested quantity exceeds that product's current `StockQuantity`; or any requested
  quantity is <= 0.
- A user create/update is rejected (FR-017) if `Tier` is missing or is not one of the defined
  `UserTier` values.
- `Order` and `OrderLineItem` pricing fields are write-once at checkout time; no API path
  updates them afterward (FR-013).
