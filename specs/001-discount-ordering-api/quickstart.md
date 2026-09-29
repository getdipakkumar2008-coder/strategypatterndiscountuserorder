# Quickstart: Discount & Ordering API

Validates the feature end-to-end against the user stories in [spec.md](./spec.md), using the
endpoints defined in [contracts/api.yaml](./contracts/api.yaml) and the entities in
[data-model.md](./data-model.md). Run these after `/speckit-implement` has produced the
solution described in [plan.md](./plan.md).

## Prerequisites

- .NET 8 SDK installed (`dotnet --version` reports an 8.x SDK)
- Repository root: `E:\EdriveCode\discountanduser`

## Setup

```powershell
dotnet restore
dotnet build
```

## Run

```powershell
dotnet run --project src/DiscountAndOrdering.Api
```

Note the base URL printed on startup (e.g. `http://localhost:5xxx`). All commands below use
`$base` for that URL.

## Unit tests (Constitution Principle V — discount/pricing logic testable in isolation)

```powershell
dotnet test tests/DiscountAndOrdering.UnitTests
```

Expected: all tests pass, including discount-percentage-correctness tests for all three
tiers with no ASP.NET Core host involved.

## Validation scenarios

### 1. Manage user accounts (User Story 2)

```powershell
$user = Invoke-RestMethod -Method Post -Uri "$base/api/users" -ContentType 'application/json' -Body '{"name":"Ada","email":"ada@example.com","tier":"Premium"}'
Invoke-RestMethod -Uri "$base/api/users/$($user.id)"
```

Expected: the created user is returned with `tier: "Premium"` on both calls.

### 2. Manage the product catalog (User Story 6) + Browse (User Story 1)

```powershell
$product = Invoke-RestMethod -Method Post -Uri "$base/api/products" -ContentType 'application/json' -Body '{"name":"Widget","description":"A widget","price":10.00,"stockQuantity":5}'
Invoke-RestMethod -Uri "$base/api/products"
```

Expected: the product list includes "Widget" at price 10.00 with stock 5.

### 3. Checkout with tier-based discount (User Story 3) — the core scenario

```powershell
$checkout = @{ userId = $user.id; items = @(@{ productId = $product.id; quantity = 2 }) } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "$base/api/orders/checkout" -ContentType 'application/json' -Body $checkout
```

Expected (per SC-002): for the Premium user above, subtotal = 20.00, discountPercentage =
20, discountAmount = 4.00, finalTotal = 16.00. Repeat with a Normal-tier and a
Super-Premium-tier user to confirm 0% and 30% respectively.

Then re-check the product's stock:

```powershell
Invoke-RestMethod -Uri "$base/api/products/$($product.id)"
```

Expected (FR-011): `stockQuantity` reduced from 5 to 3.

### 4. Reject an invalid checkout (User Story 4)

```powershell
# Empty cart
Invoke-RestMethod -Method Post -Uri "$base/api/orders/checkout" -ContentType 'application/json' -Body (@{ userId = $user.id; items = @() } | ConvertTo-Json) -SkipHttpErrorCheck

# Over-quantity
$badCheckout = @{ userId = $user.id; items = @(@{ productId = $product.id; quantity = 999 }) } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "$base/api/orders/checkout" -ContentType 'application/json' -Body $badCheckout -SkipHttpErrorCheck
```

Expected (FR-007, SC-003): both return 400 with a response body identifying the failing
item and reason (see `CheckoutValidationError` in contracts/api.yaml); no order is created
and the product's stock is unchanged.

### 5. View past orders (User Story 5)

```powershell
Invoke-RestMethod -Uri "$base/api/users/$($user.id)/orders"
```

Expected (SC-004): the successful checkout from step 3 appears, with pricing identical to
what was returned at checkout time.

### 6. Config-driven discount change (SC-005, Constitution Principle IV)

Edit `src/DiscountAndOrdering.Api/appsettings.json`, change `DiscountSettings.TierPercentages.Premium`
from `20` to `25`, then re-run:

```powershell
dotnet run --project src/DiscountAndOrdering.Api
```

Repeat scenario 3 for a Premium-tier user. Expected: discountPercentage is now 25, with
**no source file changed** — confirms tier percentages are configuration-driven, not
compiled literals.

## Success

All six scenarios passing confirms SC-001 through SC-007 in spec.md are met.
