# Onion Architecture and DDD Architecture

This document presents the repository as an Onion Architecture and a DDD-inspired domain model.

## 1. Circular Onion Architecture

The dependency rule is: outer layers depend inward; inner layers do not depend on outer layers.

```mermaid
graph TB
    CLIENT["HTTP Client / Swagger"]
    
    subgraph API["API / Presentation Layer"]
        P["ProductsController"]
        U["UsersController"]
        O["OrdersController"]
        PGM["Program.cs"]
    end
    
    subgraph APP["Application Layer"]
        PS["ProductService"]
        US["UserService"]
        OS["OrderService"]
        PRICE["PricingService"]
    end
    
    subgraph DOMAIN["Domain Core"]
        USER["User"]
        PRODUCT["Product"]
        ORDER["Order"]
        ITEM["OrderLineItem"]
        TIER["UserTier"]
        IUR["IUserRepository"]
        IPR["IProductRepository"]
        IOR["IOrderRepository"]
        IDS["IDiscountStrategy"]
    end
    
    subgraph INFRA["Infrastructure"]
        IUMR["InMemoryUserRepository"]
        IPMR["InMemoryProductRepository"]
        IOMR["InMemoryOrderRepository"]
    end
    
    CLIENT --> P
    CLIENT --> U
    CLIENT --> O
    
    P --> PS
    U --> US
    U --> OS
    O --> OS
    
    PGM --> PS
    PGM --> US
    PGM --> OS
    PGM --> PRICE
    PGM --> IUMR
    PGM --> IPMR
    PGM --> IOMR
    
    PS --> IPR
    US --> IUR
    OS --> IUR
    OS --> IPR
    OS --> IOR
    OS --> PRICE
    
    PRICE --> IDS
    
    IUR --> USER
    IPR --> PRODUCT
    IOR --> ORDER
    
    USER --> TIER
    ORDER --> ITEM
    ITEM --> PRODUCT
    
    IUMR --> IUR
    IPMR --> IPR
    IOMR --> IOR
    
    style API fill:#dbeafe,stroke:#2563eb,stroke-width:3px
    style APP fill:#dcfce7,stroke:#16a34a,stroke-width:3px
    style DOMAIN fill:#fef3c7,stroke:#d97706,stroke-width:3px
    style INFRA fill:#f3e8ff,stroke:#9333ea,stroke-width:3px
    style CLIENT fill:#f3f4f6,stroke:#6b7280,stroke-width:2px
```

### Dependency direction

```text
DiscountAndOrdering.Api
    -> DiscountAndOrdering.Application
    -> DiscountAndOrdering.Domain

DiscountAndOrdering.Infrastructure
    -> DiscountAndOrdering.Domain
```

---

## 2. DDD Aggregate Boundaries

Three main aggregates define the domain:

### User Aggregate Root
- **Root**: `User`
- **Includes**: `UserTier`
- **Behavior**: `UpdateDetails()` validates user data
- **Repository**: `IUserRepository`

### Product Aggregate Root
- **Root**: `Product`
- **Behavior**: `UpdateDetails()` validates product data, `ReduceStock(qty)` prevents negative stock
- **Repository**: `IProductRepository`

### Order Aggregate Root
- **Root**: `Order`
- **Owns**: `OrderLineItem` (no independent repository)
- **Behavior**: Requires at least one line item; stores pricing snapshots (product name, unit price)
- **Repository**: `IOrderRepository`

**Important**: `OrderLineItem` references `ProductId` but owns snapshots of product name and price. It does not own the product itself.

```mermaid
graph TB
    subgraph USER_AGG["User Aggregate"]
        USER["User<br/>Id, Name, Email, Tier"]
        TIER["UserTier enum"]
        USER --> TIER
    end
    
    subgraph PRODUCT_AGG["Product Aggregate"]
        PRODUCT["Product<br/>Id, Name, Description, Price, StockQuantity"]
    end
    
    subgraph ORDER_AGG["Order Aggregate"]
        ORDER["Order<br/>Id, UserId, OrderDate<br/>Subtotal, DiscountPercentage<br/>DiscountAmount, FinalTotal"]
        ITEM["OrderLineItem<br/>ProductId, ProductName snapshot<br/>UnitPrice snapshot, Quantity"]
        ORDER o--o ITEM
    end
    
    subgraph PORTS["Domain Ports"]
        IUR["IUserRepository"]
        IPR["IProductRepository"]
        IOR["IOrderRepository"]
        IDS["IDiscountStrategy"]
    end
    
    IUR -.->|persists| USER
    IPR -.->|persists| PRODUCT
    IOR -.->|persists| ORDER
    ITEM -.->|references ProductId<br/>snapshots name & price| PRODUCT
    ORDER -.->|UserId reference| USER
    
    style USER_AGG fill:#fff7ed,stroke:#c2410c,stroke-width:2px
    style PRODUCT_AGG fill:#fff7ed,stroke:#c2410c,stroke-width:2px
    style ORDER_AGG fill:#fff7ed,stroke:#c2410c,stroke-width:2px
    style PORTS fill:#eff6ff,stroke:#2563eb,stroke-width:2px
    style USER fill:#fffbeb,stroke:#d97706,stroke-width:2px
    style PRODUCT fill:#fffbeb,stroke:#d97706,stroke-width:2px
    style ORDER fill:#fffbeb,stroke:#d97706,stroke-width:2px
    style ITEM fill:#fef3c7,stroke:#ca8a04,stroke-width:1px
    style TIER fill:#fef3c7,stroke:#ca8a04,stroke-width:1px
```

---

## 3. Complete Class and Interface Connections

```mermaid
graph LR
    subgraph API_LAYER["DiscountAndOrdering.Api"]
        ProdCtrl["ProductsController"]
        UserCtrl["UsersController"]
        OrdCtrl["OrdersController"]
        Program["Program.cs"]
    end
    
    subgraph APP_LAYER["DiscountAndOrdering.Application"]
        ProdSvc["ProductService"]
        UserSvc["UserService"]
        OrdSvc["OrderService"]
        PriceSvc["PricingService"]
        Resolver["ConfigurableDiscountStrategyResolver"]
        ResolverInt["IDiscountStrategyResolver"]
        Strategy["PercentageDiscountStrategy"]
        DiscountSettings["DiscountSettings"]
    end
    
    subgraph DOMAIN_LAYER["DiscountAndOrdering.Domain"]
        User["User"]
        Product["Product"]
        Order["Order"]
        LineItem["OrderLineItem"]
        UserTier["UserTier"]
        IUserRepo["IUserRepository"]
        IProdRepo["IProductRepository"]
        IOrdRepo["IOrderRepository"]
        IDiscountStrat["IDiscountStrategy"]
    end
    
    subgraph INFRA_LAYER["DiscountAndOrdering.Infrastructure"]
        UserRepoImpl["InMemoryUserRepository"]
        ProdRepoImpl["InMemoryProductRepository"]
        OrdRepoImpl["InMemoryOrderRepository"]
    end
    
    Program -->|wires| ProdSvc
    Program -->|wires| UserSvc
    Program -->|wires| OrdSvc
    Program -->|wires| PriceSvc
    Program -->|wires| Resolver
    Program -->|registers| UserRepoImpl
    Program -->|registers| ProdRepoImpl
    Program -->|registers| OrdRepoImpl
    
    ProdCtrl --> ProdSvc
    UserCtrl --> UserSvc
    UserCtrl --> OrdSvc
    OrdCtrl --> OrdSvc
    
    ProdSvc --> IProdRepo
    UserSvc --> IUserRepo
    OrdSvc --> IUserRepo
    OrdSvc --> IProdRepo
    OrdSvc --> IOrdRepo
    OrdSvc --> PriceSvc
    
    PriceSvc --> ResolverInt
    Resolver -->|implements| ResolverInt
    Resolver --> DiscountSettings
    Resolver --> Strategy
    Strategy -->|implements| IDiscountStrat
    
    IUserRepo --> User
    IProdRepo --> Product
    IOrdRepo --> Order
    
    User --> UserTier
    Order o--o LineItem
    LineItem -.->|snapshot| Product
    
    UserRepoImpl -->|implements| IUserRepo
    ProdRepoImpl -->|implements| IProdRepo
    OrdRepoImpl -->|implements| IOrdRepo
    
    classDef apiLayer fill:#dbeafe,stroke:#2563eb,stroke-width:2px,color:#000
    classDef appLayer fill:#dcfce7,stroke:#16a34a,stroke-width:2px,color:#000
    classDef domainLayer fill:#fef3c7,stroke:#d97706,stroke-width:2px,color:#000
    classDef infraLayer fill:#f3e8ff,stroke:#9333ea,stroke-width:2px,color:#000
    
    class API_LAYER,ProdCtrl,UserCtrl,OrdCtrl,Program apiLayer
    class APP_LAYER,ProdSvc,UserSvc,OrdSvc,PriceSvc,Resolver,ResolverInt,Strategy,DiscountSettings appLayer
    class DOMAIN_LAYER,User,Product,Order,LineItem,UserTier,IUserRepo,IProdRepo,IOrdRepo,IDiscountStrat domainLayer
    class INFRA_LAYER,UserRepoImpl,ProdRepoImpl,OrdRepoImpl infraLayer
```

---

## 4. Checkout Sequence

```mermaid
sequenceDiagram
    actor Client
    participant OrdCtrl as OrdersController
    participant OrdSvc as OrderService
    participant URep as IUserRepository
    participant PRep as IProductRepository
    participant PSvc as PricingService
    participant Res as IDiscountStrategyResolver
    participant Strat as IDiscountStrategy
    participant ORep as IOrderRepository
    
    Client->>OrdCtrl: POST /api/orders/checkout
    OrdCtrl->>OrdSvc: CheckoutAsync(request)
    OrdSvc->>URep: GetByIdAsync(userId)
    URep-->>OrdSvc: User with UserTier
    
    loop Each product in cart
        OrdSvc->>PRep: GetByIdAsync(productId)
        PRep-->>OrdSvc: Product
    end
    
    OrdSvc->>OrdSvc: Validate cart, create snapshots
    OrdSvc->>PSvc: CalculatePricing(lineItems, userTier)
    PSvc->>Res: Resolve(userTier)
    Res-->>PSvc: PercentageDiscountStrategy
    PSvc->>Strat: ApplyDiscount(subtotal)
    Strat-->>PSvc: discountAmount
    PSvc-->>OrdSvc: PricingResult
    
    OrdSvc->>OrdSvc: Create Order aggregate
    OrdSvc->>ORep: AddAsync(order)
    OrdSvc->>PRep: ReduceStock, UpdateAsync(product)
    OrdSvc-->>OrdCtrl: CheckoutResult.Success
    OrdCtrl-->>Client: 201 Created
```

---

## 5. Discount Policy (Separate from Aggregates)

The discount policy is **not part of the Order aggregate**. It belongs to the Application layer pricing orchestration.

```mermaid
graph LR
    PSvc["PricingService<br/>CalculatePricing"]
    ResolverInt["IDiscountStrategyResolver<br/>Resolve UserTier to Strategy"]
    Resolver["ConfigurableDiscountStrategyResolver<br/>Bound from appsettings.json"]
    Strat["PercentageDiscountStrategy<br/>ApplyDiscount computation"]
    IStrat["IDiscountStrategy interface"]
    Settings["DiscountSettings<br/>TierPercentages config"]
    Result["PricingResult<br/>subtotal, discount, finalTotal"]
    Order["Order aggregate<br/>stores pricing snapshot"]
    
    PSvc --> ResolverInt
    ResolverInt --> IStrat
    Resolver -->|implements| ResolverInt
    Resolver --> Settings
    Resolver --> Strat
    Strat -->|implements| IStrat
    Result --> Order
    
    style PSvc fill:#dcfce7,stroke:#16a34a,stroke-width:2px
    style ResolverInt fill:#dbeafe,stroke:#2563eb,stroke-width:2px
    style Resolver fill:#dcfce7,stroke:#16a34a,stroke-width:2px
    style Strat fill:#dcfce7,stroke:#16a34a,stroke-width:2px
    style IStrat fill:#dbeafe,stroke:#2563eb,stroke-width:2px
    style Settings fill:#f3e8ff,stroke:#9333ea,stroke-width:2px
    style Result fill:#dcfce7,stroke:#16a34a,stroke-width:2px
    style Order fill:#fef3c7,stroke:#d97706,stroke-width:2px
```

---

## 6. Key Architectural Patterns

| Pattern | Implementation | Purpose |
|---|---|---|
| **Onion Architecture** | 4-layer project structure (API, Application, Domain, Infrastructure) | Dependency control: inner layers independent of outer |
| **Domain-Driven Design** | 3 aggregate roots (User, Product, Order) with repository ports | Business logic centralized in entities, persistence abstracted |
| **Strategy Pattern** | `IDiscountStrategy`, `PercentageDiscountStrategy` | Discount algorithms replaceable without service changes |
| **Repository Pattern** | `IUserRepository`, `IProductRepository`, `IOrderRepository` | Persistence details hidden from business logic |
| **Service Layer** | `OrderService`, `PricingService`, `ProductService`, `UserService` | Use-case orchestration separated from HTTP handling |
| **Dependency Injection** | `Program.cs` DI container | Runtime composition without hard-coded dependencies |
| **DTO Pattern** | `CheckoutRequest`, `OrderResultDto`, `ProductDto`, `UserDto` | API contracts separated from domain entities |

---

## 7. DDD Assessment

✅ **What is implemented**:
- Entities with embedded business rules
- Aggregate roots with clear boundaries
- Child entities owned by aggregates (OrderLineItem)
- Repository interfaces as domain ports
- Invariant enforcement (positive prices, non-negative stock)
- Pricing snapshot on Order for historical accuracy

❌ **What is not yet implemented**:
- Value objects (Money, Email, Quantity)
- Domain events
- Unit of Work pattern
- Explicit domain services
- Separate bounded contexts
- CQRS or event sourcing

The current implementation is **pragmatically DDD-inspired** rather than enterprise-grade DDD.

---

## 8. Final Summary

This repository demonstrates:

1. **Onion Architecture** through correct project layering and inward-pointing dependencies
2. **DDD tactical patterns** with aggregates, repositories, entities, and business rules
3. **SOLID principles** across all layers
4. **Separation of concerns** between HTTP handling, orchestration, business logic, and persistence

**Architecture statement**:

> The repository follows Onion Architecture in project structure and DDD-inspired modeling in aggregates and repository ports, while keeping the implementation lightweight and practical for a small-to-medium enterprise C# service.
