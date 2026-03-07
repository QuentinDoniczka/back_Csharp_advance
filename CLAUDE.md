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

## REST API Standards

### HTTP Verbs

| Verb | Purpose | Returns |
|------|---------|---------|
| `GET` | Read resource(s). Never mutates. | `200 OK` |
| `POST` | Create a resource (returns `201` + `Location` header) or trigger an action. Auth endpoints (`login`, `logout`, `register`) are an accepted exception. | `201 Created` |
| `PUT` | Full replacement of a resource. | `200 OK` or `204 No Content` |
| `PATCH` | Partial update. | `200 OK` or `204 No Content` |
| `DELETE` | Remove a resource. | `204 No Content` |

### Status Codes

| Code | Meaning | When to use |
|------|---------|-------------|
| `200 OK` | Successful read or update with body | GET, PUT/PATCH returning updated resource |
| `201 Created` | Resource created. **MUST** include `Location` header pointing to the new resource. | POST that creates a resource |
| `204 No Content` | Successful mutation with no body | Logout, delete, change-password, etc. |
| `400 Bad Request` | Validation errors (FluentValidation) | Invalid input |
| `401 Unauthorized` | Missing or invalid authentication | No/invalid token |
| `403 Forbidden` | Authenticated but not authorized | Insufficient role/permissions |
| `404 Not Found` | Resource does not exist | Entity lookup fails |
| `409 Conflict` | Resource already exists or state conflict | Duplicate creation, concurrency |
| `500 Internal Server Error` | Unhandled exception | Global exception middleware |

### Route Conventions

- Plural nouns for resources: `api/users`, `api/quests`, `api/guilds`
- Kebab-case for multi-word segments: `api/guild-members`, `api/quest-rewards`
- No verbs in URLs -- model as resources. Exception: auth endpoints (`login`, `logout`, `register`) are industry-accepted pragmatic conventions.
- Sub-resources with nesting: `api/users/{userId}/profile`, `api/guilds/{guildId}/members`
- Max 2 levels of nesting. Beyond that, promote to top-level resource.

### Controller Standards

- `[ApiController]` + `[Route("api/[controller]")]` or explicit route
- Controllers must be `sealed`
- Constructor injection of `IMediator` only (or `ISender`)
- Thin dispatch only -- no business logic, no try/catch, no validation
- `ActionResult<T>` for actions that return a body
- `IActionResult` only for `NoContent()` responses
- `[ProducesResponseType]` required on every action -- document ALL possible status codes
- `CancellationToken` on every async action

### Error Responses

- Use ASP.NET Core built-in `ProblemDetails` (RFC 7807)
- Content-Type: `application/problem+json`
- Include `type`, `title`, `status`, `detail` fields
- Validation errors use `ProblemDetails` with `errors` extension

### DTO Conventions (API Layer)

- Request DTOs: `{Action}{Resource}RequestDto` (e.g., `CreateQuestRequestDto`)
- Response DTOs: `{Resource}ResponseDto` (e.g., `QuestResponseDto`)
- API DTOs live in `API/DTOs/` -- they map to/from Application DTOs
- Never expose Application or Domain types directly in API responses

### Pagination (for collection endpoints)

- Query parameters: `?page=1&pageSize=20`
- Default page size: 20, max: 100
- Response envelope: `{ items: [...], page: int, pageSize: int, totalCount: int, hasNextPage: bool }`

### ProducesResponseType Examples

```csharp
// GET -- read
[ProducesResponseType(typeof(QuestResponseDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status404NotFound)]

// POST -- create
[ProducesResponseType(typeof(QuestResponseDto), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status409Conflict)]

// PUT/PATCH -- update
[ProducesResponseType(typeof(QuestResponseDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status404NotFound)]

// DELETE -- remove
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
```

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
│   ├── Authorization/
│   │   ├── MinimumRoleAttribute.cs
│   │   ├── MinimumRoleHandler.cs
│   │   └── MinimumRoleRequirement.cs
│   ├── Controllers/
│   │   ├── AccountController.cs
│   │   ├── AuthController.cs
│   │   └── RoleController.cs
│   ├── DTOs/
│   │   ├── ChangePasswordRequestDto.cs
│   │   ├── ChangeUserRoleRequestDto.cs
│   │   ├── GoogleLoginRequestDto.cs
│   │   ├── GoogleLoginResponseDto.cs
│   │   ├── LoginRequestDto.cs
│   │   ├── LoginResponseDto.cs
│   │   ├── LogoutRequestDto.cs
│   │   ├── RefreshTokenRequestDto.cs
│   │   ├── RefreshTokenResponseDto.cs
│   │   ├── RegisterRequestDto.cs
│   │   ├── RegisterResponseDto.cs
│   │   ├── SetPasswordRequestDto.cs
│   │   ├── UpdateProfileRequestDto.cs
│   │   ├── UserProfileResponseDto.cs
│   │   └── UserRoleResponseDto.cs
│   ├── Extensions/
│   │   └── ClaimsPrincipalExtensions.cs
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── appsettings.Development.json
├── Application/
│   ├── Authorization/
│   │   ├── MinimumRoleRequirement.cs
│   │   └── RoleHierarchy.cs
│   ├── Behaviors/
│   │   └── ValidationBehavior.cs
│   ├── Commands/
│   │   ├── ChangePassword/
│   │   │   ├── ChangePasswordCommand.cs
│   │   │   ├── ChangePasswordCommandHandler.cs
│   │   │   └── ChangePasswordCommandValidator.cs
│   │   ├── ChangeUserRole/
│   │   │   ├── ChangeUserRoleCommand.cs
│   │   │   ├── ChangeUserRoleCommandHandler.cs
│   │   │   └── ChangeUserRoleCommandValidator.cs
│   │   ├── DeactivateAccount/
│   │   │   ├── DeactivateAccountCommand.cs
│   │   │   ├── DeactivateAccountCommandHandler.cs
│   │   │   └── DeactivateAccountCommandValidator.cs
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
│   │   ├── ReactivateAccount/
│   │   │   ├── ReactivateAccountCommand.cs
│   │   │   ├── ReactivateAccountCommandHandler.cs
│   │   │   └── ReactivateAccountCommandValidator.cs
│   │   ├── RefreshToken/
│   │   │   ├── RefreshTokenCommand.cs
│   │   │   ├── RefreshTokenCommandHandler.cs
│   │   │   └── RefreshTokenCommandValidator.cs
│   │   ├── Register/
│   │   │   ├── RegisterCommand.cs
│   │   │   ├── RegisterCommandHandler.cs
│   │   │   ├── RegisterCommandValidator.cs
│   │   │   └── RegisterResult.cs
│   │   ├── SendChatMessage/
│   │   │   ├── SendChatMessageCommand.cs
│   │   │   ├── SendChatMessageCommandHandler.cs
│   │   │   └── SendChatMessageCommandValidator.cs
│   │   ├── SetPassword/
│   │   │   ├── SetPasswordCommand.cs
│   │   │   ├── SetPasswordCommandHandler.cs
│   │   │   └── SetPasswordCommandValidator.cs
│   │   └── UpdateProfile/
│   │       ├── UpdateProfileCommand.cs
│   │       ├── UpdateProfileCommandHandler.cs
│   │       └── UpdateProfileCommandValidator.cs
│   ├── Constants/
│   │   ├── AuthErrorMessages.cs
│   │   ├── ChannelConstants.cs
│   │   ├── ChatConstants.cs
│   │   ├── ErrorMessages.cs
│   │   ├── NotificationConstants.cs
│   │   └── ProfileConstants.cs
│   ├── DTOs/
│   │   └── Output/
│   │       ├── AuthTokenResult.cs
│   │       ├── ChatMessageOutput.cs
│   │       ├── ExternalLoginResult.cs
│   │       ├── GoogleLoginResult.cs
│   │       ├── GoogleUserInfo.cs
│   │       ├── IdentityUserResult.cs
│   │       ├── NotificationOutput.cs
│   │       ├── RefreshTokenInfo.cs
│   │       ├── UserProfileOutput.cs
│   │       └── UserRoleOutput.cs
│   ├── Exceptions/
│   │   ├── AuthenticationException.cs
│   │   ├── ConflictException.cs
│   │   ├── ForbiddenException.cs
│   │   ├── NotFoundException.cs
│   │   └── ValidationException.cs
│   ├── Helpers/
│   │   └── ChannelNameBuilder.cs
│   ├── Interfaces/
│   │   ├── IChannelAuthorizationService.cs
│   │   ├── IChatNotificationService.cs
│   │   ├── IGoogleTokenValidator.cs
│   │   ├── IIdentityService.cs
│   │   ├── IJwtTokenService.cs
│   │   └── IPersonalNotificationService.cs
│   ├── Queries/
│   │   ├── GetMyProfile/
│   │   │   ├── GetMyProfileQuery.cs
│   │   │   ├── GetMyProfileQueryHandler.cs
│   │   │   └── GetMyProfileQueryValidator.cs
│   │   ├── GetUserProfile/
│   │   │   ├── GetUserProfileQuery.cs
│   │   │   ├── GetUserProfileQueryHandler.cs
│   │   │   └── GetUserProfileQueryValidator.cs
│   │   └── GetUserRole/
│   │       ├── GetUserRoleQuery.cs
│   │       ├── GetUserRoleQueryHandler.cs
│   │       └── GetUserRoleQueryValidator.cs
│   ├── Validators/
│   │   └── PasswordRules.cs
│   └── DependencyInjection.cs
├── Domain/
│   ├── Constants/
│   │   ├── AppRoles.cs
│   │   └── ExternalProviders.cs
│   ├── Entities/
│   │   ├── RevokedToken.cs
│   │   └── UserProfile.cs
│   ├── Enums/
│   │   ├── ChannelType.cs
│   │   ├── NotificationType.cs
│   │   └── RoleLevel.cs
│   └── Interfaces/
│       ├── IRevokedTokenRepository.cs
│       └── IUserProfileRepository.cs
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
│   ├── Chat/
│   │   ├── ChannelAuthorizationService.cs
│   │   ├── ChatNotificationService.cs
│   │   ├── GameHub.cs
│   │   ├── HubExceptionFilter.cs
│   │   └── PersonalNotificationService.cs
│   ├── Data/
│   │   ├── Configurations/
│   │   │   ├── RevokedTokenConfiguration.cs
│   │   │   └── UserProfileConfiguration.cs
│   │   └── AppDbContext.cs
│   ├── Migrations/ (7 files)
│   ├── Repositories/
│   │   ├── RevokedTokenRepository.cs
│   │   └── UserProfileRepository.cs
│   └── DependencyInjection.cs
├── API.Tests/
│   ├── Authorization/
│   │   └── MinimumRoleHandlerTests.cs
│   └── Middleware/
│       └── ExceptionHandlingMiddlewareTests.cs
├── Application.Tests/
│   ├── Authorization/
│   │   ├── MinimumRoleHandlerTests.cs
│   │   └── RoleHierarchyTests.cs
│   ├── Behaviors/
│   │   └── ValidationBehaviorTests.cs
│   ├── Chat/
│   │   └── ChannelAuthorizationServiceTests.cs
│   ├── Commands/
│   │   ├── ChangePassword/
│   │   │   ├── ChangePasswordCommandHandlerTests.cs
│   │   │   └── ChangePasswordCommandValidatorTests.cs
│   │   ├── ChangeUserRole/
│   │   │   ├── ChangeUserRoleCommandHandlerTests.cs
│   │   │   └── ChangeUserRoleCommandValidatorTests.cs
│   │   ├── DeactivateAccount/
│   │   │   ├── DeactivateAccountCommandHandlerTests.cs
│   │   │   └── DeactivateAccountCommandValidatorTests.cs
│   │   ├── GoogleLogin/
│   │   │   ├── GoogleLoginCommandHandlerTests.cs
│   │   │   └── GoogleLoginCommandValidatorTests.cs
│   │   ├── Login/
│   │   │   ├── LoginCommandHandlerTests.cs
│   │   │   └── LoginCommandValidatorTests.cs
│   │   ├── Logout/
│   │   │   ├── LogoutCommandHandlerTests.cs
│   │   │   └── LogoutCommandValidatorTests.cs
│   │   ├── ReactivateAccount/
│   │   │   ├── ReactivateAccountCommandHandlerTests.cs
│   │   │   └── ReactivateAccountCommandValidatorTests.cs
│   │   ├── RefreshToken/
│   │   │   ├── RefreshTokenCommandHandlerTests.cs
│   │   │   └── RefreshTokenCommandValidatorTests.cs
│   │   ├── Register/
│   │   │   ├── RegisterCommandHandlerTests.cs
│   │   │   └── RegisterCommandValidatorTests.cs
│   │   ├── SendChatMessage/
│   │   │   ├── SendChatMessageCommandHandlerTests.cs
│   │   │   └── SendChatMessageCommandValidatorTests.cs
│   │   ├── SetPassword/
│   │   │   ├── SetPasswordCommandHandlerTests.cs
│   │   │   └── SetPasswordCommandValidatorTests.cs
│   │   └── UpdateProfile/
│   │       ├── UpdateProfileCommandHandlerTests.cs
│   │       └── UpdateProfileCommandValidatorTests.cs
│   ├── Exceptions/
│   │   └── ValidationExceptionTests.cs
│   ├── Helpers/
│   │   └── ChannelNameBuilderTests.cs
│   └── Queries/
│       ├── GetMyProfile/
│       │   ├── GetMyProfileQueryHandlerTests.cs
│       │   └── GetMyProfileQueryValidatorTests.cs
│       ├── GetUserProfile/
│       │   ├── GetUserProfileQueryHandlerTests.cs
│       │   └── GetUserProfileQueryValidatorTests.cs
│       └── GetUserRole/
│           ├── GetUserRoleQueryHandlerTests.cs
│           └── GetUserRoleQueryValidatorTests.cs
├── API.IntegrationTests/
│   ├── Account/
│   │   └── AccountTests.cs
│   ├── Auth/
│   │   ├── LoginTests.cs
│   │   └── RegisterTests.cs
│   ├── Chat/
│   │   ├── ChannelScenarioTests.cs
│   │   └── GameHubTests.cs
│   ├── Fixtures/
│   │   ├── CustomWebApplicationFactory.cs
│   │   ├── IntegrationTestBase.cs
│   │   └── SignalRTestBase.cs
│   ├── Notifications/
│   │   └── PersonalNotificationTests.cs
│   └── Roles/
│       └── RoleTests.cs
├── Domain.Tests/
│   └── Entities/
│       ├── RevokedTokenTests.cs
│       └── UserProfileTests.cs
└── compose.yaml
```
<!-- STRUCTURE:END -->
