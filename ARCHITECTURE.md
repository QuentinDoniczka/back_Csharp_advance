# Architecture 4 Projets - Guide Complet

Ce guide explique comment créer et organiser une architecture .NET backend en 4 projets distincts, suivant les principes de Clean Architecture.

---

## Vue d'ensemble

```
Solution: back_projet_Csharp
├── Domain          [Class Library]
├── Application     [Class Library]
├── Infrastructure  [Class Library]
└── API             [ASP.NET Core Web API]
```

### Règle de dépendances

```
API → Application → Domain
  ↓         ↓
Infrastructure → Domain
```

**Principe clé :** Domain ne dépend de RIEN. Tout dépend de Domain.

**Pourquoi cette structure ?**
- **Domain** : Logique métier pure, indépendante de toute technologie
- **Application** : Orchestration et définition des besoins (interfaces)
- **Infrastructure** : Implémentations techniques (BDD, APIs externes)
- **API** : Point d'entrée HTTP, configuration et injection de dépendances

---

## 1. Domain (Class Library)

### Type de projet
**Class Library** - `dotnet new classlib -n back_projet_Csharp.Domain`

### Responsabilité
Cœur métier de l'application. **Logique business pure, sans dépendances techniques.**

### Structure

```
Domain/
├── Models/                    # Entités de domaine
│   ├── User.cs
│   ├── Product.cs
│   └── Order.cs
│
├── Exceptions/                # Exceptions métier
│   ├── UserNotFoundException.cs
│   ├── InvalidOrderException.cs
│   └── DomainException.cs
│
├── Enums/                     # Énumérations métier
│   ├── OrderStatus.cs
│   └── UserRole.cs
│
└── ValueObjects/              # Value Objects (DDD - optionnel)
    ├── Email.cs
    └── Money.cs
```

### À quoi servent les différents éléments ?

**Models (Entités)**
- **Utilité** : Représentent les objets métier avec leur état et comportement
- **Prototype** :
```csharp
public class User
{
    public int Id { get; private set; }
    public string Email { get; private set; }

    public static User Create(string email, string passwordHash) { }
    public void Activate() { }
    public void Deactivate() { }
}
```

**Exceptions**
- **Utilité** : Représentent les erreurs métier spécifiques au domaine
- **Prototype** :
```csharp
public class UserNotFoundException : Exception
{
    public int UserId { get; }
    public UserNotFoundException(int userId) : base($"User {userId} not found") { }
}
```

**Enums**
- **Utilité** : Définissent des valeurs fixes du domaine métier
- **Prototype** :
```csharp
public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered
}
```

**ValueObjects**
- **Utilité** : Objets immuables représentant des concepts métier (DDD avancé)
- **Prototype** :
```csharp
public class Email
{
    public string Value { get; }
    public Email(string value) { /* validation */ }
}
```

### Dépendances
**AUCUNE** - Le Domain doit rester pur, sans dépendances externes.

---

## 2. Application (Class Library)

### Type de projet
**Class Library** - `dotnet new classlib -n back_projet_Csharp.Application`

### Responsabilité
**Orchestration des cas d'usage et définition des besoins.** Coordonne le Domain et définit les interfaces (contrats).

### Structure

```
Application/
├── DTOs/                          # Data Transfer Objects
│   ├── Requests/
│   │   ├── CreateUserRequest.cs
│   │   └── UpdateUserRequest.cs
│   └── Responses/
│       ├── UserResponse.cs
│       └── UserListResponse.cs
│
├── Services/                      # Implémentations des services
│   ├── UserService.cs
│   ├── ProductService.cs
│   └── OrderService.cs
│
├── Interfaces/
│   ├── IServices/                 # Contrats services
│   │   ├── IUserService.cs
│   │   └── IProductService.cs
│   └── IRepositories/             # Contrats repositories
│       ├── IUserRepository.cs
│       └── IProductRepository.cs
│
└── Validators/                    # Validations (optionnel)
    └── CreateUserValidator.cs
```

### À quoi servent les différents éléments ?

**DTOs/Requests**
- **Utilité** : Objets reçus depuis l'API (ce que l'utilisateur envoie)
- **Prototype** :
```csharp
public class CreateUserRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}
```

**DTOs/Responses**
- **Utilité** : Objets renvoyés à l'API (ce qu'on expose à l'utilisateur, sans données sensibles)
- **Prototype** :
```csharp
public class UserResponse
{
    public int Id { get; set; }
    public string Email { get; set; }
    public bool IsActive { get; set; }
    // Pas de PasswordHash exposé !
}
```

**Interfaces/IServices**
- **Utilité** : Définit les contrats que l'API peut utiliser (cas d'usage métier)
- **Prototype** :
```csharp
public interface IUserService
{
    Task<UserResponse> GetByIdAsync(int id);
    Task<UserResponse> CreateAsync(CreateUserRequest request);
    Task DeleteAsync(int id);
}
```

**Interfaces/IRepositories**
- **Utilité** : Définit les besoins d'accès aux données (contrats pour Infrastructure)
- **CRUCIAL** : Ces interfaces restent dans Application pour éviter que Application dépende d'Infrastructure
- **Prototype** :
```csharp
public interface IUserRepository
{
    Task<User> GetByIdAsync(int id);              // Retourne entité Domain
    Task<User> AddAsync(User user);
    Task<bool> ExistsByEmailAsync(string email);
}
```

**Services**
- **Utilité** : Implémentent la logique métier et orchestrent les repositories
- **Responsabilités** : Validation, transformation DTO ↔ Entity, règles métier
- **Prototype** :
```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository) { }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        // 1. Validation métier
        // 2. Transformation Request → Entity
        // 3. Appel repository
        // 4. Transformation Entity → Response
    }
}
```

**Validators**
- **Utilité** : Valident les données entrantes (avec FluentValidation par exemple)
- **Prototype** :
```csharp
public class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8);
    }
}
```

### Dépendances

**Références de projets** :
```bash
dotnet add reference ../Domain/Domain.csproj
```

**NuGet optionnels** :
```bash
dotnet add package AutoMapper          # Mapping DTOs ↔ Entities
dotnet add package FluentValidation    # Validation
```

---

## 3. Infrastructure (Class Library)

### Type de projet
**Class Library** - `dotnet new classlib -n back_projet_Csharp.Infrastructure`

### Responsabilité
**Implémentations techniques concrètes.** Accès aux données, services externes, détails techniques.

### Structure

```
Infrastructure/
├── Data/
│   ├── AppDbContext.cs            # DbContext Entity Framework
│   ├── Configurations/            # Configurations EF
│   │   ├── UserConfiguration.cs
│   │   └── ProductConfiguration.cs
│   └── Migrations/                # Migrations EF (auto-générées)
│
├── Repositories/                  # Implémentations repositories
│   ├── UserRepository.cs
│   └── ProductRepository.cs
│
└── Adapters/                      # Services techniques (optionnel)
    ├── EmailService.cs
    └── CacheService.cs
```

### À quoi servent les différents éléments ?

**Data/AppDbContext**
- **Utilité** : Point d'accès à la base de données via Entity Framework
- **Prototype** :
```csharp
public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

**Data/Configurations**
- **Utilité** : Configuration des tables, contraintes, index, relations (EF Core)
- **Prototype** :
```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
```

**Repositories**
- **Utilité** : Implémentent les interfaces IRepositories définies dans Application
- **Responsabilités** : Requêtes SQL/LINQ, accès DbContext, opérations CRUD
- **Prototype** :
```csharp
public class UserRepository : IUserRepository  // Implémente interface d'Application
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) { }

    public async Task<User> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User> AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }
}
```

**Services techniques**
- **Utilité** : Services d'infrastructure (email, cache, file storage, APIs externes)
- **Prototype** :
```csharp
public class EmailService : IEmailService
{
    public async Task SendAsync(string to, string subject, string body)
    {
        // Implémentation SMTP ou service tiers (SendGrid, etc.)
    }
}
```

**Migrations**
- **Utilité** : Historique des changements de schéma de base de données (auto-générées par EF)
- **Commande** : `dotnet ef migrations add MigrationName`

### Dépendances

**Références de projets** :
```bash
dotnet add reference ../Domain/Domain.csproj
dotnet add reference ../Application/Application.csproj
```

**NuGet requis** :
```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer  # ou .Npgsql pour PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.Design
```

---

## 4. API (ASP.NET Core Web API)

### Type de projet
**ASP.NET Core Web API** - `dotnet new webapi -n back_projet_Csharp.API`

### Responsabilité
**Point d'entrée HTTP de l'application.** Gère les requêtes, routing, middleware, injection de dépendances.

### Structure

```
API/
├── Controllers/
│   ├── UserController.cs
│   └── ProductController.cs
│
├── Middleware/                    # Middlewares custom (optionnel)
│   └── ExceptionHandlerMiddleware.cs
│
├── Program.cs                     # Point d'entrée + configuration DI
├── appsettings.json
├── appsettings.Development.json
└── Dockerfile                     # Configuration Docker (optionnel)
```

### À quoi servent les différents éléments ?

**Controllers**
- **Utilité** : Exposent les endpoints HTTP et délèguent au Service
- **Responsabilités** : Recevoir requête HTTP, appeler service, retourner réponse HTTP
- **Prototype** :
```csharp
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService) { }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var user = await _userService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }
}
```

**Program.cs**
- **Utilité** : Configuration de l'application et injection de dépendances
- **Responsabilités** : Enregistrer services, configurer middleware, lancer l'app
- **Prototype** :
```csharp
var builder = WebApplication.CreateBuilder(args);

// Configuration DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency Injection
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddControllers();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

**Middleware**
- **Utilité** : Traitement global des requêtes (gestion d'erreurs, logging, authentification)
- **Prototype** :
```csharp
public class ExceptionHandlerMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try { await next(context); }
        catch (Exception ex) { /* Log et retourner erreur formatée */ }
    }
}
```

**appsettings.json**
- **Utilité** : Configuration de l'application (connexion BDD, paramètres, secrets)
- **Exemple** :
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyDb;..."
  }
}
```

**Dockerfile**
- **Utilité** : Conteneurisation de l'application
- **Important** : Un seul Dockerfile dans API suffit (il inclut tous les projets lors du build)

### Dépendances

**Références de projets** :
```bash
dotnet add reference ../Application/Application.csproj
dotnet add reference ../Infrastructure/Infrastructure.csproj
```

**NuGet** :
```bash
dotnet add package Microsoft.EntityFrameworkCore.Design  # Pour les migrations
```

---

## Script complet de création

```bash
# 1. Créer les projets
dotnet new classlib -n Domain
dotnet new classlib -n Application
dotnet new classlib -n Infrastructure
dotnet new webapi -n API

# 2. Créer la solution et ajouter les projets
dotnet new sln -n back_projet_Csharp
dotnet sln add Domain/Domain.csproj
dotnet sln add Application/Application.csproj
dotnet sln add Infrastructure/Infrastructure.csproj
dotnet sln add API/API.csproj

# 3. Ajouter les références entre projets
dotnet add Application reference Domain
dotnet add Infrastructure reference Domain
dotnet add Infrastructure reference Application
dotnet add API reference Application
dotnet add API reference Infrastructure

# 4. Installer les packages NuGet
dotnet add Infrastructure package Microsoft.EntityFrameworkCore
dotnet add Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add Infrastructure package Microsoft.EntityFrameworkCore.Tools
dotnet add Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add API package Microsoft.EntityFrameworkCore.Design

# 5. Build
dotnet build
```

---

## Workflow de développement

### Ajouter une nouvelle fonctionnalité (exemple: Product)

**Ordre recommandé** :
1. **Domain** : Créer `Models/Product.cs`
2. **Application** : Créer DTOs (`ProductRequest`, `ProductResponse`)
3. **Application** : Créer interfaces (`IProductRepository`, `IProductService`)
4. **Application** : Implémenter `Services/ProductService.cs`
5. **Infrastructure** : Créer `Configurations/ProductConfiguration.cs`
6. **Infrastructure** : Implémenter `Repositories/ProductRepository.cs`
7. **Infrastructure** : Générer migration : `dotnet ef migrations add AddProduct`
8. **API** : Créer `Controllers/ProductController.cs`
9. **API** : Enregistrer DI dans `Program.cs`

### Commandes Entity Framework

```bash
# Ajouter une migration
dotnet ef migrations add MigrationName --project Infrastructure --startup-project API

# Appliquer les migrations
dotnet ef database update --project Infrastructure --startup-project API

# Supprimer la dernière migration
dotnet ef migrations remove --project Infrastructure --startup-project API
```

---

## Résumé rapide

| Projet          | Rôle                              | Dépend de              |
|-----------------|-----------------------------------|------------------------|
| **Domain**      | Logique métier pure               | Rien                   |
| **Application** | Orchestration + Contrats          | Domain                 |
| **Infrastructure** | Implémentations techniques     | Domain + Application   |
| **API**         | Point d'entrée HTTP               | Application + Infrastructure |

**Flux de données** :
```
HTTP Request → Controller (API)
              ↓
           Service (Application)
              ↓
          Repository (Infrastructure)
              ↓
           DbContext → Database
```
