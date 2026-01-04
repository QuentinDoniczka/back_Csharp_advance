# Clean Architecture 4 Couches - C# .NET

## Structure de la Solution

```
Solution/
│
├── MyApp.API/
│   ├── Controllers/
│   │   └── AuthController.cs
│   ├── DTOs/
│   │   ├── RegisterRequestDto.cs
│   │   └── RegisterResponseDto.cs
│   └── Program.cs
│
├── MyApp.Application/
│   ├── Commands/
│   │   └── Register/
│   │       ├── RegisterCommand.cs
│   │       ├── RegisterCommandHandler.cs
│   │       └── RegisterCommandValidator.cs
│   ├── Interfaces/
│   │   ├── IEmailService.cs
│   │   └── IPasswordHasher.cs
│   ├── DTOs/
│   │   └── UserDto.cs
│   ├── Behaviors/
│   │   └── ValidationBehavior.cs
│   └── Exceptions/
│       ├── UserAlreadyExistsException.cs
│       └── UserNotFoundException.cs
│
├── MyApp.Domain/
│   ├── Entities/
│   │   └── User.cs
│   ├── ValueObjects/
│   │   ├── Email.cs
│   │   └── Password.cs
│   ├── Interfaces/
│   │   └── IUserRepository.cs
│   ├── Events/
│   │   └── IDomainEvent.cs
│   │   └── UserRegisteredEvent.cs
│   └── Exceptions/
│       └── DomainException.cs
│
└── MyApp.Infrastructure/
    ├── Repositories/
    │   └── UserRepository.cs
    ├── Services/
    │   ├── PasswordHasher.cs
    │   └── EmailService.cs
    └── Data/
        └── AppDbContext.cs
```

---

## Références entre projets

```
MyApp.API           → MyApp.Application, MyApp.Infrastructure
MyApp.Application   → MyApp.Domain
MyApp.Infrastructure → MyApp.Domain, MyApp.Application
MyApp.Domain        → (aucune référence)
```

---

## Packages NuGet

### MyApp.API
```
MediatR.Extensions.Microsoft.DependencyInjection
FluentValidation.AspNetCore
Microsoft.EntityFrameworkCore.Design
```

### MyApp.Application
```
MediatR
FluentValidation
```

### MyApp.Infrastructure
```
Microsoft.EntityFrameworkCore.SqlServer
BCrypt.Net-Next
```

---

## 1. MyApp.Domain

### Exceptions/DomainException.cs
```csharp
namespace MyApp.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
```

### Events/IDomainEvent.cs
```csharp
namespace MyApp.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
```

### Events/UserRegisteredEvent.cs
```csharp
namespace MyApp.Domain.Events;

public record UserRegisteredEvent(Guid UserId, string Email) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```

### ValueObjects/Email.cs
```csharp
using MyApp.Domain.Exceptions;

namespace MyApp.Domain.ValueObjects;

public class Email : IEquatable<Email>
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("L'email ne peut pas être vide");

        if (!IsValidEmail(value))
            throw new DomainException($"'{value}' n'est pas un email valide");

        Value = value.ToLowerInvariant().Trim();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }

    public bool Equals(Email? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => Equals(obj as Email);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
```

### ValueObjects/Password.cs
```csharp
using MyApp.Domain.Exceptions;

namespace MyApp.Domain.ValueObjects;

public class Password
{
    public string Value { get; }

    public Password(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Le mot de passe ne peut pas être vide");

        if (value.Length < 8)
            throw new DomainException("Le mot de passe doit faire au moins 8 caractères");

        if (!value.Any(char.IsUpper))
            throw new DomainException("Le mot de passe doit contenir au moins une majuscule");

        if (!value.Any(char.IsDigit))
            throw new DomainException("Le mot de passe doit contenir au moins un chiffre");

        Value = value;
    }
}
```

### Entities/User.cs
```csharp
using MyApp.Domain.Events;
using MyApp.Domain.Exceptions;
using MyApp.Domain.ValueObjects;

namespace MyApp.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private User() { }

    public static User Create(Email email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Le hash du mot de passe est requis");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Email.Value));

        return user;
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("Le nouveau mot de passe est requis");

        PasswordHash = newPasswordHash;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("L'utilisateur est déjà désactivé");

        IsActive = false;
    }

    public void Activate()
    {
        if (IsActive)
            throw new DomainException("L'utilisateur est déjà actif");

        IsActive = true;
    }

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

### Interfaces/IUserRepository.cs
```csharp
using MyApp.Domain.Entities;

namespace MyApp.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task<bool> ExistsAsync(Guid id);
}
```

---

## 2. MyApp.Application

### Exceptions/UserAlreadyExistsException.cs
```csharp
namespace MyApp.Application.Exceptions;

public class UserAlreadyExistsException : Exception
{
    public string Email { get; }

    public UserAlreadyExistsException(string email)
        : base($"Un utilisateur avec l'email '{email}' existe déjà")
    {
        Email = email;
    }
}
```

### Exceptions/UserNotFoundException.cs
```csharp
namespace MyApp.Application.Exceptions;

public class UserNotFoundException : Exception
{
    public Guid UserId { get; }

    public UserNotFoundException(Guid userId)
        : base($"Utilisateur avec l'ID '{userId}' non trouvé")
    {
        UserId = userId;
    }
}
```

### Interfaces/IPasswordHasher.cs
```csharp
namespace MyApp.Application.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
```

### Interfaces/IEmailService.cs
```csharp
namespace MyApp.Application.Interfaces;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string email);
    Task SendPasswordResetEmailAsync(string email, string resetToken);
}
```

### DTOs/UserDto.cs
```csharp
namespace MyApp.Application.DTOs;

public class UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public bool IsActive { get; init; }
}
```

### Commands/Register/RegisterCommand.cs
```csharp
using MediatR;
using MyApp.Application.DTOs;

namespace MyApp.Application.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password
) : IRequest<UserDto>;
```

### Commands/Register/RegisterCommandValidator.cs
```csharp
using FluentValidation;

namespace MyApp.Application.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'email est requis")
            .EmailAddress().WithMessage("Format email invalide");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Le mot de passe est requis")
            .MinimumLength(8).WithMessage("Le mot de passe doit faire au moins 8 caractères")
            .Matches("[A-Z]").WithMessage("Le mot de passe doit contenir une majuscule")
            .Matches("[0-9]").WithMessage("Le mot de passe doit contenir un chiffre");
    }
}
```

### Commands/Register/RegisterCommandHandler.cs
```csharp
using MediatR;
using MyApp.Application.DTOs;
using MyApp.Application.Exceptions;
using MyApp.Application.Interfaces;
using MyApp.Domain.Entities;
using MyApp.Domain.Interfaces;
using MyApp.Domain.ValueObjects;

namespace MyApp.Application.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
    }

    public async Task<UserDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new UserAlreadyExistsException(request.Email);
        }

        var email = new Email(request.Email);
        var password = new Password(request.Password);
        var passwordHash = _passwordHasher.Hash(password.Value);

        var user = User.Create(email, passwordHash);

        await _userRepository.AddAsync(user);
        await _emailService.SendWelcomeEmailAsync(user.Email.Value);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email.Value,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive
        };
    }
}
```

### Behaviors/ValidationBehavior.cs
```csharp
using FluentValidation;
using MediatR;

namespace MyApp.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
```

---

## 3. MyApp.Infrastructure

### Data/AppDbContext.cs
```csharp
using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Entities;

namespace MyApp.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Email)
                .HasConversion(
                    e => e.Value,
                    v => new Domain.ValueObjects.Email(v))
                .HasMaxLength(256)
                .IsRequired();

            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.PasswordHash)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(u => u.CreatedAt).IsRequired();
            entity.Property(u => u.IsActive).IsRequired();

            entity.Ignore(u => u.DomainEvents);
        });
    }
}
```

### Repositories/UserRepository.cs
```csharp
using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Entities;
using MyApp.Domain.Interfaces;
using MyApp.Infrastructure.Data;

namespace MyApp.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == email.ToLowerInvariant());
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Users.AnyAsync(u => u.Id == id);
    }
}
```

### Services/PasswordHasher.cs
```csharp
using MyApp.Application.Interfaces;

namespace MyApp.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
```

### Services/EmailService.cs
```csharp
using MyApp.Application.Interfaces;

namespace MyApp.Infrastructure.Services;

public class EmailService : IEmailService
{
    public async Task SendWelcomeEmailAsync(string email)
    {
        // TODO: Implémenter l'envoi d'email
        await Task.CompletedTask;
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken)
    {
        // TODO: Implémenter l'envoi d'email
        await Task.CompletedTask;
    }
}
```

---

## 4. MyApp.API

### DTOs/RegisterRequestDto.cs
```csharp
namespace MyApp.API.DTOs;

public class RegisterRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

### DTOs/RegisterResponseDto.cs
```csharp
namespace MyApp.API.DTOs;

public class RegisterResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

### Controllers/AuthController.cs
```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyApp.API.DTOs;
using MyApp.Application.Commands.Register;

namespace MyApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var command = new RegisterCommand(
            Email: request.Email,
            Password: request.Password
        );

        var result = await _mediator.Send(command);

        var response = new RegisterResponseDto
        {
            Id = result.Id,
            Email = result.Email,
            CreatedAt = result.CreatedAt
        };

        return Created($"/api/users/{result.Id}", response);
    }
}
```

### Program.cs
```csharp
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyApp.Application.Behaviors;
using MyApp.Application.Commands.Register;
using MyApp.Application.Interfaces;
using MyApp.Domain.Interfaces;
using MyApp.Infrastructure.Data;
using MyApp.Infrastructure.Repositories;
using MyApp.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(RegisterCommand).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(RegisterCommandValidator).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

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

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyAppDb;Trusted_Connection=True;TrustServerCertificate=True;"
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

---

## Commandes pour créer la solution

```bash
# Créer la solution
dotnet new sln -n MyApp

# Créer les projets
dotnet new classlib -n MyApp.Domain
dotnet new classlib -n MyApp.Application
dotnet new classlib -n MyApp.Infrastructure
dotnet new webapi -n MyApp.API

# Ajouter les projets à la solution
dotnet sln add MyApp.Domain
dotnet sln add MyApp.Application
dotnet sln add MyApp.Infrastructure
dotnet sln add MyApp.API

# Ajouter les références entre projets
dotnet add MyApp.Application reference MyApp.Domain
dotnet add MyApp.Infrastructure reference MyApp.Domain
dotnet add MyApp.Infrastructure reference MyApp.Application
dotnet add MyApp.API reference MyApp.Application
dotnet add MyApp.API reference MyApp.Infrastructure

# Installer les packages NuGet
cd MyApp.Application
dotnet add package MediatR
dotnet add package FluentValidation

cd ../MyApp.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package BCrypt.Net-Next

cd ../MyApp.API
dotnet add package MediatR.Extensions.Microsoft.DependencyInjection
dotnet add package FluentValidation.AspNetCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Swashbuckle.AspNetCore

cd ..
```

---

## Migration Entity Framework

```bash
cd MyApp.API
dotnet ef migrations add InitialCreate --project ../MyApp.Infrastructure
dotnet ef database update --project ../MyApp.Infrastructure
```
