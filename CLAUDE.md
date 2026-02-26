## Project Context

This project is the **backend** for a browser-based RPG game inspired by **Shakes & Fidget**.
The game features character creation, quests, dungeons, PvP arena, guilds, shops, inventory management, and progression systems.
This backend exposes a REST API consumed by a frontend client. It handles all game logic, persistence, authentication, and real-time events server-side.

---

## Architecture — Four-Layer Clean Architecture

| Layer | Contains | Depends on |
|-------|----------|------------|
| **API** | Controllers, Middleware, Program.cs | Application only |
| **Application** | Commands, Queries, Handlers, Validators (FluentValidation), DTOs (Input/Output), service interfaces | Domain only |
| **Domain** | Entities (private setters, factory methods), Value Objects, Domain Services, Domain Events, repository interfaces | **NOTHING** — zero external dependencies |
| **Infrastructure** | Repository implementations (EF Core), external services, DbContext, configurations | Domain + Application |

**Dependency flow**: API → Application → Domain ← Infrastructure

---

## Core Rules (All Code)

- Follow **SOLID, KISS, DRY, YAGNI**. Prefer the simplest working design.
- No over-engineering, no premature abstractions. Keep files and classes small.
- Code and identifiers must be in **English**. Avoid comments unless strictly necessary.
- Prefer composition over inheritance. One class per file, file name = class name.
- Entities must have **private setters** and **factory methods**.
- **async/await** for all I/O. NEVER use `Thread`, `.Result`, or `.Wait()`. Always `await`. `ConfigureAwait(false)` in library code.
- **Dependency Injection**: constructor injection only. No service locator.
- Early returns over deep nesting. No magic numbers or strings — use constants, configuration, or enums.
- Nullable reference types: enable and use correctly.
- Modify only the minimal files required for each task. Keep changes focused and atomic.

---

## Naming Conventions

- Classes, methods, properties, enums: `PascalCase`
- Private fields: `_camelCase` with underscore prefix
- Local variables, parameters: `camelCase`
- Interfaces: `IName` (e.g., `IUserRepository`)
- DTOs: `CreateUserInput` / `UserOutput` — in DTOs/Input and DTOs/Output subfolders
- Commands/Queries: `CreateUserCommand`, `GetUserByIdQuery`
- Handlers: `CreateUserCommandHandler`
- Validators: `CreateUserCommandValidator`
- Booleans: prefix with is/has/can/should

---

## Communication

- If a request involves bad practices, poor architecture, or potential issues: speak up immediately and suggest better alternatives.
- Don't hesitate to ask for confirmation if the request seems problematic or unclear.
- Push back constructively when something doesn't align with best practices.
- When suggesting architecture, consider scalability, maintainability, and testability.

---

<!-- STRUCTURE:START -->
# Project Structure

```
back_Csharp_advance/
├── API/
│   ├── Controllers/
│   │   └── AuthController.cs
│   ├── DTOs/
│   │   ├── GoogleLoginRequestDto.cs
│   │   ├── LoginRequestDto.cs
│   │   ├── LoginResponseDto.cs
│   │   ├── LogoutRequestDto.cs
│   │   ├── RefreshTokenRequestDto.cs
│   │   ├── RefreshTokenResponseDto.cs
│   │   ├── RegisterRequestDto.cs
│   │   └── RegisterResponseDto.cs
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── appsettings.Development.json
├── Application/
│   ├── Authorization/
│   │   └── RoleHierarchy.cs
│   ├── Behaviors/
│   │   └── ValidationBehavior.cs
│   ├── Commands/
│   │   ├── GoogleLogin/
│   │   │   ├── GoogleLoginCommand.cs
│   │   │   ├── GoogleLoginCommandHandler.cs
│   │   │   └── GoogleLoginCommandValidator.cs
│   │   ├── Login/
│   │   │   ├── LoginCommand.cs
│   │   │   ├── LoginCommandHandler.cs
│   │   │   └── LoginCommandValidator.cs
│   │   ├── Logout/
│   │   │   ├── LogoutCommand.cs
│   │   │   ├── LogoutCommandHandler.cs
│   │   │   └── LogoutCommandValidator.cs
│   │   ├── RefreshToken/
│   │   │   ├── RefreshTokenCommand.cs
│   │   │   ├── RefreshTokenCommandHandler.cs
│   │   │   └── RefreshTokenCommandValidator.cs
│   │   └── Register/
│   │       ├── RegisterCommand.cs
│   │       ├── RegisterCommandHandler.cs
│   │       ├── RegisterCommandValidator.cs
│   │       └── RegisterResult.cs
│   ├── DTOs/
│   │   └── Output/
│   │       ├── AuthTokenResult.cs
│   │       ├── GoogleUserInfo.cs
│   │       ├── IdentityUserResult.cs
│   │       └── RefreshTokenInfo.cs
│   ├── Exceptions/
│   │   ├── AuthenticationException.cs
│   │   └── ValidationException.cs
│   ├── Interfaces/
│   │   ├── IGoogleTokenValidator.cs
│   │   ├── IIdentityService.cs
│   │   └── IJwtTokenService.cs
│   └── DependencyInjection.cs
├── Domain/
│   ├── Constants/
│   │   ├── AppRoles.cs
│   │   └── ExternalProviders.cs
│   ├── Entities/
│   │   └── RevokedToken.cs
│   ├── Enums/
│   │   └── RoleLevel.cs
│   └── Interfaces/
│       └── IRevokedTokenRepository.cs
├── Infrastructure/
│   ├── Authentication/
│   │   ├── ApplicationUser.cs
│   │   ├── GoogleAuthSettings.cs
│   │   ├── GoogleTokenValidator.cs
│   │   ├── IdentityService.cs
│   │   ├── JwtSettings.cs
│   │   ├── JwtTokenService.cs
│   │   └── RoleSeedingExtensions.cs
│   ├── Authorization/
│   │   ├── MinimumRoleAttribute.cs
│   │   ├── MinimumRoleHandler.cs
│   │   └── MinimumRoleRequirement.cs
│   ├── Data/
│   │   ├── Configurations/
│   │   │   └── RevokedTokenConfiguration.cs
│   │   └── AppDbContext.cs
│   ├── Migrations/ (5 files)
│   ├── Repositories/
│   │   └── RevokedTokenRepository.cs
│   └── DependencyInjection.cs
├── API.Tests/
│   └── Middleware/
│       └── ExceptionHandlingMiddlewareTests.cs
├── Application.Tests/
│   ├── Authorization/
│   │   ├── MinimumRoleHandlerTests.cs
│   │   └── RoleHierarchyTests.cs
│   └── Commands/
│       ├── GoogleLogin/
│       │   ├── GoogleLoginCommandHandlerTests.cs
│       │   └── GoogleLoginCommandValidatorTests.cs
│       ├── Login/
│       │   ├── LoginCommandHandlerTests.cs
│       │   └── LoginCommandValidatorTests.cs
│       ├── Logout/
│       │   ├── LogoutCommandHandlerTests.cs
│       │   └── LogoutCommandValidatorTests.cs
│       ├── RefreshToken/
│       │   ├── RefreshTokenCommandHandlerTests.cs
│       │   └── RefreshTokenCommandValidatorTests.cs
│       └── Register/
│           ├── RegisterCommandHandlerTests.cs
│           └── RegisterCommandValidatorTests.cs
├── Domain.Tests/
│   └── Entities/
│       └── RevokedTokenTests.cs
└── compose.yaml
```
<!-- STRUCTURE:END -->
