# Architecture 4 Projets - Guide Complet

Ce guide explique comment créer et organiser une architecture .NET backend en 4 projets distincts, suivant les principes de Clean Architecture.

---

## Vue d'ensemble

```
Solution: back_projet_Csharp
│
├── back_projet_Csharp.Domain          [Class Library]
├── back_projet_Csharp.Application     [Class Library]
├── back_projet_Csharp.Infrastructure  [Class Library]
└── back_projet_Csharp.API             [ASP.NET Core Web API]
```

### Règle de dépendances

```
API → Application → Domain
  ↓         ↓
Infrastructure → Domain
```

**Principe clé :** Domain ne dépend de RIEN. Tout dépend de Domain.

---

## 1. Domain (Class Library)

### Type de projet
**Class Library** - `dotnet new classlib`

### Responsabilité
Cœur métier de l'application. Logique business pure, sans dépendances techniques.

### Contenu

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
├── Enums/                     # Énumérations métier (optionnel)
│   ├── OrderStatus.cs
│   └── UserRole.cs
│
└── ValueObjects/              # Value Objects (optionnel, DDD)
    ├── Email.cs
    └── Money.cs
```

### Exemple de code

```csharp
// Models/User.cs
namespace back_projet_Csharp.Domain.Models;

public class User
{
    public int Id { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Constructor privé pour forcer l'utilisation de méthodes factory
    private User() { }

    public static User Create(string email, string passwordHash)
    {
        return new User
        {
            Email = email,
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Logique métier dans l'entité
    public void Activate()
    {
        if (IsActive)
            throw new InvalidOperationException("User is already active");
        IsActive = true;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new InvalidOperationException("User is already inactive");
        IsActive = false;
    }
}

// Exceptions/UserNotFoundException.cs
namespace back_projet_Csharp.Domain.Exceptions;

public class UserNotFoundException : Exception
{
    public int UserId { get; }

    public UserNotFoundException(int userId)
        : base($"User with ID {userId} was not found.")
    {
        UserId = userId;
    }
}
```

### Dépendances NuGet
**AUCUNE** - Le Domain doit rester pur, sans dépendances externes.

### Commande de création

```bash
dotnet new classlib -n back_projet_Csharp.Domain
```

---

## 2. Application (Class Library)

### Type de projet
**Class Library** - `dotnet new classlib`

### Responsabilité
Logique applicative, orchestration, cas d'usage. Coordonne le Domain et définit les interfaces.

### Contenu

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

### Exemple de code

```csharp
// DTOs/Requests/CreateUserRequest.cs
namespace back_projet_Csharp.Application.DTOs.Requests;

public class CreateUserRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

// DTOs/Responses/UserResponse.cs
namespace back_projet_Csharp.Application.DTOs.Responses;

public class UserResponse
{
    public int Id { get; set; }
    public string Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Interfaces/IServices/IUserService.cs
namespace back_projet_Csharp.Application.Interfaces.IServices;

public interface IUserService
{
    Task<UserResponse> GetByIdAsync(int id);
    Task<IEnumerable<UserResponse>> GetAllAsync();
    Task<UserResponse> CreateAsync(CreateUserRequest request);
    Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request);
    Task DeleteAsync(int id);
}

// Interfaces/IRepositories/IUserRepository.cs
namespace back_projet_Csharp.Application.Interfaces.IRepositories;

public interface IUserRepository
{
    Task<User> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(int id);
    Task<bool> ExistsByEmailAsync(string email);
}

// Services/UserService.cs
using back_projet_Csharp.Domain.Models;
using back_projet_Csharp.Domain.Exceptions;
using back_projet_Csharp.Application.DTOs.Requests;
using back_projet_Csharp.Application.DTOs.Responses;
using back_projet_Csharp.Application.Interfaces.IServices;
using back_projet_Csharp.Application.Interfaces.IRepositories;

namespace back_projet_Csharp.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserResponse> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            throw new UserNotFoundException(id);

        return MapToResponse(user);
    }

    public async Task<IEnumerable<UserResponse>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(MapToResponse);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        // Vérifier si l'email existe déjà
        if (await _userRepository.ExistsByEmailAsync(request.Email))
            throw new InvalidOperationException("Email already exists");

        // Créer l'entité via la méthode factory du Domain
        var user = User.Create(request.Email, HashPassword(request.Password));

        // Persister
        var createdUser = await _userRepository.AddAsync(user);

        return MapToResponse(createdUser);
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            throw new UserNotFoundException(id);

        await _userRepository.DeleteAsync(id);
    }

    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    private static string HashPassword(string password)
    {
        // TODO: Utiliser BCrypt ou similaire
        return password; // Placeholder
    }
}
```

### Dépendances NuGet

```bash
# Aucune dépendance obligatoire de base
# Optionnelles selon besoins :
dotnet add package AutoMapper          # Mapping DTOs
dotnet add package FluentValidation    # Validation
```

### Références de projets

```bash
dotnet add reference ../back_projet_Csharp.Domain/back_projet_Csharp.Domain.csproj
```

### Commande de création

```bash
dotnet new classlib -n back_projet_Csharp.Application
dotnet add back_projet_Csharp.Application reference back_projet_Csharp.Domain
```

---

## 3. Infrastructure (Class Library)

### Type de projet
**Class Library** - `dotnet new classlib`

### Responsabilité
Implémentation technique : accès aux données, services externes, configurations techniques.

### Contenu

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
└── Services/                      # Services techniques (optionnel)
    ├── EmailService.cs
    └── CacheService.cs
```

### Exemple de code

```csharp
// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using back_projet_Csharp.Domain.Models;

namespace back_projet_Csharp.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Appliquer toutes les configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

// Data/Configurations/UserConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using back_projet_Csharp.Domain.Models;

namespace back_projet_Csharp.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.IsActive)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique();
    }
}

// Repositories/UserRepository.cs
using Microsoft.EntityFrameworkCore;
using back_projet_Csharp.Domain.Models;
using back_projet_Csharp.Application.Interfaces.IRepositories;
using back_projet_Csharp.Infrastructure.Data;

namespace back_projet_Csharp.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<User> AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var user = await GetByIdAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }
}
```

### Dépendances NuGet

```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer      # ou .Npgsql pour PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.Design
```

### Références de projets

```bash
dotnet add reference ../back_projet_Csharp.Domain/back_projet_Csharp.Domain.csproj
dotnet add reference ../back_projet_Csharp.Application/back_projet_Csharp.Application.csproj
```

### Commande de création

```bash
dotnet new classlib -n back_projet_Csharp.Infrastructure
dotnet add back_projet_Csharp.Infrastructure reference back_projet_Csharp.Domain
dotnet add back_projet_Csharp.Infrastructure reference back_projet_Csharp.Application
```

---

## 4. API (ASP.NET Core Web API)

### Type de projet
**ASP.NET Core Web API** - `dotnet new webapi`

### Responsabilité
Point d'entrée de l'application. Gère les requêtes HTTP, routing, middleware, configuration DI.

### Contenu

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
└── appsettings.Development.json
```

### Exemple de code

```csharp
// Controllers/UserController.cs
using Microsoft.AspNetCore.Mvc;
using back_projet_Csharp.Application.DTOs.Requests;
using back_projet_Csharp.Application.Interfaces.IServices;

namespace back_projet_Csharp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _userService.DeleteAsync(id);
        return NoContent();
    }
}

// Program.cs
using Microsoft.EntityFrameworkCore;
using back_projet_Csharp.Infrastructure.Data;
using back_projet_Csharp.Infrastructure.Repositories;
using back_projet_Csharp.Application.Interfaces.IRepositories;
using back_projet_Csharp.Application.Interfaces.IServices;
using back_projet_Csharp.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency Injection - Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Dependency Injection - Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyAppDb;Trusted_Connection=true;TrustServerCertificate=true;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Dépendances NuGet

```bash
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Swashbuckle.AspNetCore              # Swagger (déjà inclus)
```

### Références de projets

```bash
dotnet add reference ../back_projet_Csharp.Application/back_projet_Csharp.Application.csproj
dotnet add reference ../back_projet_Csharp.Infrastructure/back_projet_Csharp.Infrastructure.csproj
```

### Commande de création

```bash
dotnet new webapi -n back_projet_Csharp.API
dotnet add back_projet_Csharp.API reference back_projet_Csharp.Application
dotnet add back_projet_Csharp.API reference back_projet_Csharp.Infrastructure
```

---

## Script complet de création

Exécutez ce script dans le dossier de votre solution :

```bash
# Créer les projets
dotnet new classlib -n back_projet_Csharp.Domain
dotnet new classlib -n back_projet_Csharp.Application
dotnet new classlib -n back_projet_Csharp.Infrastructure
dotnet new webapi -n back_projet_Csharp.API

# Ajouter les références entre projets
dotnet add back_projet_Csharp.Application/back_projet_Csharp.Application.csproj reference back_projet_Csharp.Domain/back_projet_Csharp.Domain.csproj

dotnet add back_projet_Csharp.Infrastructure/back_projet_Csharp.Infrastructure.csproj reference back_projet_Csharp.Domain/back_projet_Csharp.Domain.csproj
dotnet add back_projet_Csharp.Infrastructure/back_projet_Csharp.Infrastructure.csproj reference back_projet_Csharp.Application/back_projet_Csharp.Application.csproj

dotnet add back_projet_Csharp.API/back_projet_Csharp.API.csproj reference back_projet_Csharp.Application/back_projet_Csharp.Application.csproj
dotnet add back_projet_Csharp.API/back_projet_Csharp.API.csproj reference back_projet_Csharp.Infrastructure/back_projet_Csharp.Infrastructure.csproj

# Créer/mettre à jour le fichier solution
dotnet new sln -n back_projet_Csharp
dotnet sln add back_projet_Csharp.Domain/back_projet_Csharp.Domain.csproj
dotnet sln add back_projet_Csharp.Application/back_projet_Csharp.Application.csproj
dotnet sln add back_projet_Csharp.Infrastructure/back_projet_Csharp.Infrastructure.csproj
dotnet sln add back_projet_Csharp.API/back_projet_Csharp.API.csproj

# Installer les packages NuGet
dotnet add back_projet_Csharp.Infrastructure/back_projet_Csharp.Infrastructure.csproj package Microsoft.EntityFrameworkCore
dotnet add back_projet_Csharp.Infrastructure/back_projet_Csharp.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer
dotnet add back_projet_Csharp.Infrastructure/back_projet_Csharp.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Tools
dotnet add back_projet_Csharp.Infrastructure/back_projet_Csharp.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design

dotnet add back_projet_Csharp.API/back_projet_Csharp.API.csproj package Microsoft.EntityFrameworkCore.Design

# Build la solution
dotnet build
```

---

## Workflow de développement

### 1. Ajouter une nouvelle entité

```
Domain/Models/Product.cs → Créer l'entité
Infrastructure/Data/Configurations/ProductConfiguration.cs → Configuration EF
Infrastructure/Migrations → dotnet ef migrations add AddProduct
```

### 2. Ajouter un nouveau cas d'usage

```
Application/DTOs → Créer Request/Response
Application/Interfaces/IRepositories → Définir IProductRepository
Application/Interfaces/IServices → Définir IProductService
Infrastructure/Repositories → Implémenter ProductRepository
Application/Services → Implémenter ProductService
API/Controllers → Créer ProductController
API/Program.cs → Enregistrer les DI
```

### 3. Migrations Entity Framework

```bash
# Depuis le dossier de la solution
dotnet ef migrations add InitialCreate --project back_projet_Csharp.Infrastructure --startup-project back_projet_Csharp.API

dotnet ef database update --project back_projet_Csharp.Infrastructure --startup-project back_projet_Csharp.API
```

---

## Avantages de cette architecture

1. **Séparation des responsabilités** : Chaque projet a un rôle précis
2. **Testabilité** : Domain et Application testables sans BDD
3. **Maintenabilité** : Code organisé, facile à naviguer
4. **Évolutivité** : Facile d'ajouter de nouvelles fonctionnalités
5. **Flexibilité** : Changement de techno BDD sans toucher au Domain
6. **Réutilisabilité** : Domain/Application peuvent être partagés

---

## Résumé des types de projets

| Projet          | Type                | Commande                    | Dépendances                      |
|-----------------|---------------------|-----------------------------|----------------------------------|
| Domain          | Class Library       | `dotnet new classlib`       | Aucune                           |
| Application     | Class Library       | `dotnet new classlib`       | Domain                           |
| Infrastructure  | Class Library       | `dotnet new classlib`       | Domain, Application, EF Core     |
| API             | ASP.NET Core WebAPI | `dotnet new webapi`         | Application, Infrastructure      |
