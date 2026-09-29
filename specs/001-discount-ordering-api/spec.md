# Feature Specification: Discount & Ordering API

**Feature Branch**: `001-discount-ordering-api`

**Created**: 2026-09-29

**Status**: Draft

**Input**: User description: "Discount & Ordering API v1 — browse a product catalog and place orders online, with per-user discount pricing applied automatically at checkout based on the user's membership tier (Normal 0%, Premium 20%, Super Premium 30%). Sourced from SPEC.md at the repository root."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse the product catalog (Priority: P1)

A prospective buyer views the list of available products, with names, descriptions, prices,
and stock levels, so they can decide what to order.

**Why this priority**: Nothing else in the system is usable without a way to see what's for
sale. This is the smallest complete slice that delivers standalone value.

**Independent Test**: Can be fully tested by calling the product listing and single-product
retrieval capability and verifying the returned data matches what was added to the catalog —
no order or discount logic required.

**Acceptance Scenarios**:

1. **Given** products exist in the catalog, **When** a user requests the full product list,
   **Then** every product's name, price, and stock level is returned.
2. **Given** a specific product exists, **When** a user requests it by its identifier,
   **Then** that product's details are returned.
3. **Given** a product identifier that does not exist, **When** a user requests it,
   **Then** the system indicates the product was not found.

---

### User Story 2 - Manage user accounts (Priority: P1)

An operator registers a new user with a name, email, and an assigned membership tier, and
can update an existing user's details or tier, so that real customers exist in the system
for checkout and order history to operate on.

**Why this priority**: Equally foundational as the product catalog — per the resolved
scope decision, user accounts are created through this API (not pre-seeded fixture data),
so nothing involving a real user (checkout, order history) can be exercised end-to-end
without this capability.

**Independent Test**: Can be fully tested by creating a user with a given tier via the API,
retrieving them by ID to confirm the tier was recorded, updating their tier, and confirming
the change is reflected on a subsequent lookup — no catalog or order involved.

**Acceptance Scenarios**:

1. **Given** valid user details including a membership tier, **When** a new user is
   created, **Then** they can be retrieved by their identifier with that tier.
2. **Given** an existing user, **When** their membership tier or other details are updated,
   **Then** subsequent lookups reflect the new values.
3. **Given** a request to create a user with a missing or unrecognized membership tier,
   **When** submitted, **Then** it is rejected.

---

### User Story 3 - Check out an order with tier-based discount pricing (Priority: P2)

A user with an assigned membership tier submits an order for one or more products and
receives a final price that reflects their tier's discount, applied automatically and
correctly — Normal 0%, Premium 20%, Super Premium 30%.

**Why this priority**: This is the feature's core value proposition — correct, automatic,
per-tier discount pricing at checkout. It depends on User Story 1 (a catalog must exist to
order from) but delivers the differentiating value of the whole system.

**Independent Test**: Can be fully tested by submitting a checkout request for a known user
(with a known tier) and known products, and verifying the returned subtotal, discount
percentage, discount amount, and final total are all arithmetically correct for that tier.

**Acceptance Scenarios**:

1. **Given** a Normal-tier user and a cart of valid products with sufficient stock,
   **When** they check out, **Then** the final total equals the subtotal with 0% discount.
2. **Given** a Premium-tier user and the same cart, **When** they check out, **Then** the
   final total reflects exactly a 20% discount off the subtotal.
3. **Given** a Super Premium-tier user and the same cart, **When** they check out, **Then**
   the final total reflects exactly a 30% discount off the subtotal.
4. **Given** a completed checkout, **When** the order is retrieved afterward, **Then** it
   shows the same itemized pricing (unit price, quantity, discount, final total) that was
   returned at checkout time, even if a product's catalog price changes later.
5. **Given** a successful checkout, **When** the affected products are viewed afterward,
   **Then** each product's stock level has been reduced by the ordered quantity.

---

### User Story 4 - Reject an invalid checkout with a clear reason (Priority: P2)

A user attempts to check out a cart that cannot be fulfilled — empty, referencing an unknown
product, or requesting more of a product than is in stock — and is told specifically what
was wrong, without any order being created or any stock being changed.

**Why this priority**: Equal in importance to successful checkout — an ordering system that
silently fails or partially applies invalid orders is not trustworthy. Independently testable
and independently valuable (protects data integrity even before every other story exists).

**Independent Test**: Can be fully tested by submitting checkout requests with an empty cart,
an unknown product ID, and an over-quantity request, and verifying each is rejected with a
reason identifying the specific problem, and that no stock or order data changed as a result.

**Acceptance Scenarios**:

1. **Given** a checkout request with no items, **When** submitted, **Then** it is rejected
   and no order is created.
2. **Given** a checkout request referencing a product ID that does not exist, **When**
   submitted, **Then** it is rejected, identifying which product ID was invalid.
3. **Given** a checkout request for more units of a product than are in stock, **When**
   submitted, **Then** it is rejected, identifying which product had insufficient stock, and
   that product's stock level is left unchanged.

---

### User Story 5 - View past orders (Priority: P3)

A user retrieves their own order history, including the itemized pricing and discount
applied to each past order.

**Why this priority**: Valuable but not required for the first successful checkout to have
value — a natural next increment once checkout exists.

**Independent Test**: Can be fully tested by checking out one or more orders for a user, then
retrieving that user's order history and verifying every prior order appears with correct
itemized details.

**Acceptance Scenarios**:

1. **Given** a user has completed one or more orders, **When** they request their order
   history, **Then** all of their past orders are returned with itemized pricing.
2. **Given** a specific past order, **When** it is retrieved by its identifier, **Then** its
   full itemized breakdown (line items, subtotal, discount, final total) is returned.

---

### User Story 6 - Manage the product catalog (Priority: P4)

An operator adds new products to the catalog, updates existing product details (price, name,
stock), and removes products no longer sold.

**Why this priority**: Necessary for the catalog to exist and stay current, but lowest
priority since the other stories can be exercised against a pre-seeded catalog.

**Independent Test**: Can be fully tested by adding a product, verifying it appears in the
catalog (User Story 1), updating it and verifying the change is reflected, then removing it
and verifying it no longer appears.

**Acceptance Scenarios**:

1. **Given** valid product details, **When** a new product is added, **Then** it appears in
   the catalog with those details.
2. **Given** an existing product, **When** its price or stock is updated, **Then**
   subsequent catalog views reflect the new values.
3. **Given** an existing product, **When** it is removed, **Then** it no longer appears in
   the catalog.

---

### Edge Cases

- A checkout cart lists the same product twice: quantities for the same product within one
  checkout request are summed as if submitted once (see Assumptions).
- A product's price changes after a past order was placed: the past order's recorded pricing
  is unaffected — it reflects the price at the time of that checkout.
- A checkout request references a valid product but requests a quantity of zero or a
  negative quantity: treated as an invalid request and rejected, identifying that item.
- Two checkout requests for the same product arrive at effectively the same time and
  together would exceed available stock: each request is evaluated against current stock at
  the moment it is processed; a request that would take stock negative is rejected.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow any user to retrieve the full list of products in the
  catalog, including name, price, and current stock level.
- **FR-002**: The system MUST allow any user to retrieve a single product's details by its
  identifier, and MUST indicate clearly when the identifier does not match any product.
- **FR-003**: The system MUST allow a new product to be added to the catalog with a name,
  description, price, and initial stock level.
- **FR-004**: The system MUST allow an existing product's details (name, description, price,
  stock level) to be updated.
- **FR-005**: The system MUST allow an existing product to be removed from the catalog.
- **FR-006**: The system MUST allow a user to submit a checkout request consisting of their
  identity and one or more (product, quantity) items.
- **FR-007**: The system MUST reject a checkout request that has no items, that references
  any product identifier not present in the catalog, or that requests a quantity for any
  product exceeding that product's current stock — and MUST identify, per rejected item,
  which item failed and why. No order or stock change MUST occur for a rejected request.
- **FR-008**: The system MUST calculate the order subtotal as the sum of each line item's
  unit price multiplied by its quantity.
- **FR-009**: The system MUST apply a discount to the subtotal based on the checking-out
  user's membership tier: 0% for Normal, 20% for Premium, 30% for Super Premium, producing a
  final total equal to the subtotal minus the discount amount.
- **FR-010**: Operators MUST be able to change a membership tier's discount percentage, and
  add support for a new membership tier that uses the same percentage-off discount, without
  requiring a new software release.
- **FR-011**: Upon a successful checkout, the system MUST reduce the stock level of each
  ordered product by the ordered quantity.
- **FR-012**: Upon a successful checkout, the system MUST persist the completed order with
  its itemized pricing (each line item's product, unit price, quantity, and line total,
  plus the order's subtotal, discount percentage, discount amount, and final total).
- **FR-013**: A completed order's recorded pricing MUST remain unchanged afterward even if
  the underlying product's price or details change later.
- **FR-014**: The system MUST allow a user to retrieve the full history of their own past
  orders, and MUST allow a single past order to be retrieved by its identifier with its full
  itemized breakdown.
- **FR-015**: The system MUST allow a user's membership tier to be looked up, so that
  checkout can apply the correct discount for that user.
- **FR-016**: The system MUST allow a new user to be created with a name, email, and an
  assigned membership tier (Normal, Premium, or Super Premium). Access is open to any
  caller in v1, consistent with the product catalog's access model (no separate
  authentication/authorization).
- **FR-017**: The system MUST allow an existing user's details or membership tier to be
  updated, and MUST reject a create or update that specifies a missing or unrecognized
  membership tier.

*Deferred to a future iteration (see Assumptions and Out of Scope): payment processing,
shipping/tax calculation, discount stacking (multiple discounts on one order), and
authentication/authorization beyond an explicitly supplied user identity.*

### Key Entities

- **User**: A person who can browse the catalog and place orders. Has an identifying name,
  contact information, and exactly one membership tier (Normal, Premium, or Super Premium)
  that determines their checkout discount.
- **Product**: An item available for purchase. Has a name, description, price, and current
  stock level.
- **Order**: A completed checkout by a user. Records who placed it, when, its line items, and
  its full pricing breakdown (subtotal, discount percentage, discount amount, final total).
- **Order Line Item**: One product-and-quantity entry within an order, recording the
  product's name and unit price *as they were at checkout time* (independent of later catalog
  changes), the quantity ordered, and the line total.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can view the full product catalog and see accurate, current prices and
  stock levels for every product.
- **SC-002**: 100% of checkouts for a given membership tier produce a final total that is
  mathematically exactly the tier's discount percentage off the subtotal — verified across
  all three tiers.
- **SC-003**: 100% of checkout attempts that violate cart validity (empty cart, unknown
  product, insufficient stock) are rejected with no order created and no stock changed.
- **SC-004**: A user can retrieve any of their own past orders and see pricing identical to
  what was shown to them at the moment they checked out, regardless of later catalog changes.
- **SC-005**: A new membership tier's discount percentage can be introduced or changed without
  any change to the compiled discount-calculation logic.
- **SC-006**: An operator can add a new product and have it appear in catalog listings
  immediately, with no separate publishing step.
- **SC-007**: An operator can register a new user with a tier and have that user able to
  check out immediately, with no separate provisioning step.

## Assumptions

- Duplicate product entries within a single checkout request are summed as one line item for
  that product, rather than rejected or treated as separate line items.
- Monetary amounts are calculated and displayed in a single, unspecified currency, rounded to
  standard two-decimal precision; multi-currency support is out of scope for v1.
- Product catalog updates (add/update/remove) and user account creation/updates are
  available to any caller in v1, consistent with the explicit v1 decision to defer
  authentication/authorization and admin role separation to a future iteration.
- There is no pagination, search, or filtering on the product catalog in v1; the full list is
  always returned.
- Discount stacking (combining a tier discount with coupons or promotions) is out of scope for
  v1; exactly one discount — the user's tier discount — applies per order.
- Payment processing and shipping/tax calculation are out of scope for v1; "checkout" produces
  a priced, persisted order, not a payment transaction.
- User identity for all v1 operations (checkout, order history) is supplied explicitly by the
  caller (a user identifier) rather than derived from an authentication session — per the
  explicit v1 decision to defer authentication.
