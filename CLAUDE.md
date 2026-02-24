## Project Context

This project is the **backend** for a browser-based RPG game inspired by **Shakes & Fidget**.
The game features character creation, quests, dungeons, PvP arena, guilds, shops, inventory management, and progression systems.
This backend exposes a REST API consumed by a frontend client. It handles all game logic, persistence, authentication, and real-time events server-side.

---

You are generating production-grade C# code for a .NET backend application.

Rules:
- Follow SOLID, KISS, DRY, YAGNI. Prefer the simplest working design.
- No over-engineering, no premature abstractions. Keep files and classes small.
- Use Extract Method refactorings to split overly long methods into smaller, clearly named methods.
- Code and identifiers must be in English. Avoid comments unless strictly necessary. Prefer clear naming over comments.

- Four-layer architecture (Clean Architecture):
    * API: Controllers only. Receive HTTP requests, create Commands/Queries, send via MediatR, return results. No business logic, no validation, no try/catch (use global middleware).
    * Application: Use cases orchestration. Contains Commands, Queries, Handlers, Validators (FluentValidation), DTOs (in DTOs/Input and DTOs/Output subfolders), and interfaces for external services (IEmailService, IAuditLogger, etc.).
    * Domain: Pure business logic. Entities with private setters and factory methods, Value Objects, Domain Services (zero external dependencies), Domain Events, and repository interfaces (IUserRepository, etc.). Must be testable with "new" only.
    * Infrastructure: Implementations of interfaces defined in Application and Domain. Repositories (EF Core), external services (Email, Logger), DbContext, configurations.
    * Dependencies flow: API → Application → Domain ← Infrastructure

- Prefer composition over inheritance. Small, focused classes with single responsibilities.
- When a method requires many related parameters, introduce a simple parameter object instead of a long parameter list.
- Remove dead code: delete unused code, obsolete branches, and commented-out implementations.
- Encapsulate fields: expose state via properties rather than public mutable fields. Entities must have private setters.
- Extract interfaces to isolate behavior and reduce coupling between components.
- Use the Strategy pattern to replace complex conditional logic with interchangeable behaviors when it improves clarity and flexibility.
- Functionality-first: implement end-to-end behavior and data flow (Request → Processing → Response) before strict validation. Basic guards only; hardening can come later on request.
- Prioritize efficient data handling and performance. Minimize allocations and database queries.
- Use async/await properly: async for I/O operations, avoid blocking calls, use ConfigureAwait(false) in libraries.
- Dependency Injection: use constructor injection, avoid service locator pattern. Register services with appropriate lifetimes (Transient, Scoped, Singleton).
- Modify only the minimal files required for each task. Keep changes focused and atomic.
- Avoid magic numbers or strings. Use named constants, configuration, or enums instead.
- Use consistent, descriptive naming across all systems and files.
- Prefer early returns over deep nesting to improve readability.
- Handle exceptions appropriately: don't swallow exceptions, use specific exception types, consider custom exceptions for business logic.
- Use nullable reference types correctly. Enable nullable context when appropriate.
- Jamais de thread, toujours TASK, et pas de triche avec Result ou wait, toujours await

Unit Testing:
- All unit tests must follow the Arrange-Act-Assert (AAA) pattern:
    * Arrange: Set up test data, configure dependencies, and prepare the system under test
    * Act: Execute the targeted code or method being tested
    * Assert: Validate the component's behavior and verify expected outcomes
- Keep each test focused on a single behavior or scenario
- Use descriptive test method names that clearly indicate what is being tested and the expected outcome
- Isolate dependencies using mocks or stubs when appropriate

NuGet Packages & Modules:
- When suggesting packages, specify exact package names and recommended versions.
- Common packages you may suggest:
    * Entity Framework Core (Microsoft.EntityFrameworkCore)
    * ASP.NET Core (Microsoft.AspNetCore.*)
    * Dapper for lightweight data access
    * AutoMapper for object mapping
    * Serilog or NLog for logging
    * MediatR for CQRS pattern
    * FluentValidation for validation in Application layer
    * xUnit for testing
- Always mention if a package requires additional configuration or setup.

When you implement a feature:
1) State the goal in one line.
2) Brief rationale (1-2 lines) explaining the design choice.
3) Use Read/Edit/Write tools to modify files directly.
4) Explain what was changed and why after modifications.
5) If new packages are needed, specify which ones and how to install them (dotnet add package <name>).

Communication:
- Write clear, structured explanations with proper formatting.
- Break down complex responses into digestible sections.
- If a request involves bad practices, poor architecture, or potential issues: speak up immediately, explain why, and suggest better alternatives.
- Don't hesitate to ask for confirmation if the request seems problematic or unclear.
- Push back constructively when something doesn't align with best practices.
- When suggesting architecture, consider scalability, maintainability, and testability.
- If code belongs in the wrong layer, flag it immediately and explain where it should go.

<!-- STRUCTURE:START -->
# Project Structure

```
back_Csharp_advance/
├── API/
│   ├── Controllers/
│   │   └── AuthController.cs
│   ├── DTOs/
│   │   ├── RegisterRequestDto.cs
│   │   └── RegisterResponseDto.cs
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── appsettings.Development.json
├── Application/
│   ├── Behaviors/
│   │   └── ValidationBehavior.cs
│   ├── Commands/
│   │   └── Register/
│   │       ├── RegisterCommand.cs
│   │       ├── RegisterCommandHandler.cs
│   │       ├── RegisterCommandValidator.cs
│   │       └── RegisterResult.cs
│   └── Exceptions/
│       └── ValidationException.cs
├── Domain/
│   (empty)
├── Infrastructure/
│   └── Data/
│       └── AppDbContext.cs
├── Dockerfile
└── compose.yaml
```
<!-- STRUCTURE:END -->