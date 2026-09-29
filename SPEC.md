# Functional Specification — Discount & Ordering API

## 1. Overview

An ASP.NET Core Web API that lets users browse a product catalog and place
orders online, with per-user discount pricing applied automatically at
checkout based on the user's membership tier. The system is designed to be
extended over time — **new user tiers, new/changed discount percentages,
and new persistence backends must be addable without rewriting existing
business logic** (see NFR-5 and §8).

## 2. Actors

| Actor | Description |
|---|---|
| Normal User | Standard customer, no discount. |
| Premium User | Receives 20% discount on order totals. |
| Super Premium User | Receives 30% discount on order totals. |
| Admin (implicit, v1) | Manages the product catalog via the same API (no separate role/auth in v1 — see §7). |

These three tiers are the v1 requirement. The tier list is not assumed to
be final — see NFR-5 for the extensibility requirement covering new tiers.

## 3. Discount Rules (v1)

| User Tier | Discount |
|---|---|
| Normal | 0% |
| Premium | 20% |
| Super Premium | 30% |

Discount is applied to the order subtotal at checkout. Only one discount
rule applies per order (the user's tier rule) — no stacking with coupons or
promotions in v1 (see §8 for how this extends later).

## 4. Functional Requirements

### 4.1 User Management
- FR-1: The system stores users with an assigned tier (`Normal`, `Premium`, `SuperPremium`).
- FR-2: The API can look up a user's tier by user ID.
- FR-3: A new user can be created with a name, email, and an assigned tier. Access is open to any caller in v1, consistent with product catalog management (no separate admin role/auth — see §7).
- FR-4: An existing user's details or tier can be updated.

### 4.2 Product Catalog
- FR-5: Users can list all available products.
- FR-6: Users can retrieve a single product by ID.
- FR-7: Products can be added to the catalog.
- FR-8: Products can be updated (price, name, stock, etc.).
- FR-9: Products can be removed from the catalog.

### 4.3 Ordering / Checkout
- FR-10: A user can submit an order containing one or more products and quantities.
- FR-11: The system rejects checkout if the item list is empty, if any referenced product ID does not exist, or if the requested quantity for any product exceeds available stock — with a specific error indicating which item(s) failed and why.
- FR-12: The system calculates the order subtotal from product prices and quantities.
- FR-13: The system applies the discount appropriate to the ordering user's tier to compute the final total.
- FR-14: On successful checkout, the system decrements stock quantity for each ordered product by the ordered amount.
- FR-15: The system persists the completed order with itemized pricing (unit price, quantity, discount applied, final total).
- FR-16: A user can retrieve their past orders.

## 5. API Surface (v1)

```
GET    /api/users/{id}           Get user + tier
POST   /api/users                Create a user (name, email, tier)
PUT    /api/users/{id}           Update a user's details/tier
GET    /api/users/{id}/orders    List a user's orders
GET    /api/products             List all products
GET    /api/products/{id}        Get one product
POST   /api/products             Add a product
PUT    /api/products/{id}        Update a product
DELETE /api/products/{id}        Remove a product
POST   /api/orders/checkout      Place an order (userId + cart items) -> returns priced order
GET    /api/orders/{id}          Get an order
```

### 5.1 Checkout request/response shape (illustrative)

Request:
```json
{
  "userId": "guid",
  "items": [
    { "productId": "guid", "quantity": 2 }
  ]
}
```

Response:
```json
{
  "orderId": "guid",
  "userId": "guid",
  "userTier": "Premium",
  "lineItems": [
    { "productId": "guid", "productName": "Widget", "unitPrice": 10.00, "quantity": 2, "lineTotal": 20.00 }
  ],
  "subtotal": 20.00,
  "discountPercentage": 20,
  "discountAmount": 4.00,
  "finalTotal": 16.00
}
```

## 6. Data Model (conceptual)

- **User**: Id, Name, Email, Tier (enum: Normal, Premium, SuperPremium)
- **Product**: Id, Name, Description, Price, StockQuantity
- **Order**: Id, UserId, OrderDate, LineItems, Subtotal, DiscountPercentage, DiscountAmount, FinalTotal
- **OrderLineItem**: ProductId, ProductName (snapshot), UnitPrice (snapshot), Quantity, LineTotal

Prices/names are snapshotted onto the order at checkout time so historical
orders remain accurate even if a product's price changes later.

## 7. Non-Functional Requirements

- NFR-1: No authentication/authorization in v1 — endpoints identify the acting user via an explicit `userId`. Auth can be layered in later (see Architecture doc) without changing business logic.
- NFR-2: Persistence is in-memory in v1 (data does not survive process restart), with repository abstractions in place so a real database can be substituted without touching services or controllers.
- NFR-3: All discount and pricing logic must be unit-testable in isolation from the API/web layer.
- NFR-4: Adding a new user tier or discount rule must not require modifying existing discount classes (Open/Closed Principle).
- NFR-5: The set of user tiers and their discount percentages is not fixed at three.
  - Changing a tier's discount percentage requires a configuration change only — no code change, no rebuild.
  - Adding a new tier that uses the same percentage-off discount requires a configuration entry only — no new class.
  - Adding a genuinely new discount mechanism (flat amount off, BOGO, threshold-based, etc.) requires exactly one new discount-rule implementation — no changes to `OrderService`, `PricingService`, controllers, or existing discount classes.

## 8. Future Extensibility (out of scope for v1, but designed for)

- Additional membership tiers (e.g. a "Gold" tier) — add one new strategy class + one registration.
- Discount stacking (tier discount + coupon codes + seasonal promotions).
- Real persistence (EF Core with SQLite/SQL Server).
- Authentication (JWT) replacing explicit `userId` parameters.
- Product categories, search, and pagination.

## 9. Out of Scope (v1)

- Payment processing / payment gateway integration.
- Shipping/tax calculation.
- Admin role separation / authorization.
- Inventory reservation beyond a simple stock check at checkout.
