# Design Patterns Used in the Repo

This document summarizes the main design patterns and C# design principles followed in the `strategypatterndiscountuserorder` repository.

It is based on the repository’s architecture and specification documents, especially `ARCHITECTURE.md` and `SPEC.md`.

## 1. Strategy Pattern

The most explicit pattern in the repo is the **Strategy Pattern**.

### Where it appears
- `IDiscountStrategy`
- `PercentageDiscountStrategy`
- `ConfigurableDiscountStrategyResolver`
- `PricingService`

### How it works
The application does not create one class per user tier. Instead, it uses a single discount strategy implementation for percentage-based discounts, and resolves the correct discount value from configuration based on the user tier.

This means the business rule can vary without changing the pricing orchestration logic.

### Why this is a strategy pattern
- The discount algorithm is separated from the service that uses it.
- Different discount mechanisms can be swapped in without changing `PricingService`.
- A new rule like flat amount, BOGO, or threshold-based discount can be added as a new strategy implementation.

### Benefit
- Easier to extend
- Less duplication
- Better alignment with the Open/Closed Principle

---

## 2. Repository Pattern

The repo uses the **Repository Pattern** to abstract persistence.

### Where it appears
- `IProductRepository`
- `IOrderRepository`
- `IUserRepository`
- `InMemoryProductRepository`
- `InMemoryOrderRepository`
- `InMemoryUserRepository`

### How it works
Application services depend on repository interfaces rather than on storage details. Infrastructure classes provide the actual data access implementation.

### Why this is a repository pattern
- Business logic is isolated from data access
- Storage can be changed later without rewriting services
- The same interface can be backed by in-memory storage now and a database later

### Benefit
- Loose coupling
- Easier unit testing
- Swappable persistence layer

---

## 3. Service Layer Pattern

The business logic is organized into an application service layer.

### Where it appears
- `ProductService`
- `PricingService`
- `OrderService`

### How it works
Controllers stay thin and delegate use-case orchestration to services.

### Responsibilities
- `ProductService` handles product catalog operations
- `PricingService` calculates subtotal, discount, and final total
- `OrderService` orchestrates checkout and order creation

### Benefit
- Clear separation of concerns
- Easier maintenance
- Better testability

---

## 4. Factory / Resolver Pattern

`ConfigurableDiscountStrategyResolver` behaves like a lightweight factory.

### Where it appears
- `IDiscountStrategyResolver`
- `ConfigurableDiscountStrategyResolver`

### How it works
The resolver accepts a `UserTier` and returns the appropriate `IDiscountStrategy` instance.

### Why it is factory-like
- Object creation is centralized
- Callers do not need to know which concrete class is created
- The selection logic is hidden behind an interface

### Benefit
- Encapsulation of creation logic
- Easier future extension for different discount mechanisms

---

## 5. Dependency Injection

The repo uses ASP.NET Core’s built-in **Dependency Injection** container.

### Where it appears
- Services are injected into controllers and other services
- Repository interfaces are injected into services
- Discount resolver is injected into `PricingService`

### Why it matters
- Classes depend on abstractions, not concrete implementations
- Implementations can be swapped easily
- Testing becomes easier because mocks or stubs can replace real dependencies

### Benefit
- Low coupling
- Better composition at the application root
- Cleaner code structure

---

## 6. DTO Pattern

The application uses DTOs to separate API contracts from domain objects.

### Where it appears
- `ProductDto`
- `CheckoutRequest`
- `OrderResultDto`

### Why it is a DTO pattern
- Data transfer objects are used to move data across API boundaries
- Domain entities are not exposed directly to the client
- Request/response models remain stable even if internal models change

### Benefit
- Better encapsulation
- Safer public API design
- Reduced coupling between layers

---

## 7. Clean Architecture / Onion Architecture

The repository follows a layered architecture close to **Clean Architecture** or **Onion Architecture**.

### Layers
- **API**: Controllers, DTOs, DI wiring
- **Application**: Services and use-case orchestration
- **Domain**: Entities, enums, interfaces
- **Infrastructure**: Repository implementations

### Dependency direction
- `Api` -> `Application` -> `Domain`
- `Infrastructure` -> `Domain`

### Why it matters
- Inner layers do not depend on outer layers
- Domain logic stays protected from framework and persistence changes
- The architecture supports long-term maintainability

### Benefit
- Strong separation of concerns
- Easier evolution of the system
- Better support for testing and substitution

---

## 8. SOLID Principles Observed

The codebase is also designed around SOLID principles.

### Single Responsibility Principle (SRP)
Each class has one clear responsibility.

Examples:
- `PricingService` only handles pricing calculations
- `OrderService` handles checkout orchestration
- Repositories only manage persistence operations

### Open/Closed Principle (OCP)
The system is open for extension and closed for modification.

Examples:
- New discount mechanisms can be added with new strategy classes
- Discount percentages can be changed through configuration

### Liskov Substitution Principle (LSP)
Any implementation of a repository or discount strategy can be used wherever the interface is expected.

### Interface Segregation Principle (ISP)
The interfaces are small and focused:
- `IUserRepository`
- `IOrderRepository`
- `IProductRepository`
- `IDiscountStrategy`
- `IDiscountStrategyResolver`

### Dependency Inversion Principle (DIP)
High-level services depend on abstractions rather than concrete types.

---

## 9. Summary Table

| Pattern / Principle | Where it is used | Purpose |
|---|---|---|
| Strategy | Discount calculation | Swap discount algorithms easily |
| Repository | Data access | Hide storage details from services |
| Service Layer | Application services | Keep controllers thin and focused |
| Factory / Resolver | Discount strategy selection | Centralize object creation |
| Dependency Injection | ASP.NET Core composition | Decouple dependencies |
| DTO | API request/response models | Separate contracts from domain objects |
| Clean / Onion Architecture | Project layering | Keep domain isolated and maintainable |
| SRP | Individual services and repositories | One reason to change per class |
| OCP | Discount extension model | Add behavior without changing existing code |
| LSP | Interface-based design | Allow interchangeable implementations |
| ISP | Narrow interfaces | Avoid large, bloated contracts |
| DIP | Layer dependencies | Depend on abstractions, not concrete classes |

---

## 10. Final Note

The repository is a good example of a layered ASP.NET Core application using:
- Strategy for discounts
- Repository for persistence abstraction
- Service layer for orchestration
- Dependency Injection for composition
- DTOs for API boundaries
- SOLID and Clean Architecture principles for maintainability

If you want, I can also create a **shorter interview-style version** or a **more detailed developer-friendly version** of this document.