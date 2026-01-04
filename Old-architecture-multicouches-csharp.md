# Architecture Multi-Couches en C# : 3 vs 4 Couches

## 📊 Schéma des Dépendances

### Architecture 3 Couches
```
┌─────────────┐
│     API     │
└──────┬──────┘
       │ référence
       ▼
┌─────────────┐               ┌───────────────┐
│   Domain    │ ◄─────────────│Infrastructure │
└─────────────┘   référence   └───────────────┘
```

> **Note** : En 3 couches, l'API référence Domain pour accéder aux services métier. Infrastructure référence Domain pour implémenter les interfaces (repositories).

### Architecture 4 Couches (Clean Architecture)
```
┌─────────────┐
│     API     │ ─────────────────────────────────┐
└──────┬──────┘                                  │
       │ référence                               │ référence (pour DI)
       ▼                                         ▼
┌─────────────┐                          ┌───────────────┐
│ Application │ ◄────────────────────────│Infrastructure │
└──────┬──────┘      référence           └───────┬───────┘
       │ référence                               │
       ▼                                         │ référence
┌─────────────┐                                  │
│   Domain    │ ◄────────────────────────────────┘
└─────────────┘
```

> **Points clés** :
> - **API** référence Application (pour les Commands/Queries) ET Infrastructure (uniquement pour configurer la DI)
> - **Application** référence Domain uniquement (définit des interfaces, ne connaît PAS les implémentations)
> - **Infrastructure** référence Domain ET Application (pour implémenter les interfaces des deux couches)
> - **Domain** ne référence RIEN (couche la plus indépendante)

---

## 📁 ARBORESCENCE DES PROJETS

### 🔷 Architecture 3 Couches

```
Solution/
│
├── MyApp.API/                          # Couche Présentation
│   ├── Controllers/
│   │   └── AuthController.cs           # Endpoints HTTP
│   ├── DTOs/
│   │   ├── RegisterRequestDto.cs       # Données entrantes
│   │   └── RegisterResponseDto.cs      # Données sortantes
│   ├── Mappings/
│   │   └── UserMappingProfile.cs       # AutoMapper profiles
│   └── Program.cs
│
├── MyApp.Domain/                       # Couche Métier
│   ├── Entities/
│   │   └── User.cs                     # Entité métier
│   ├── Interfaces/
│   │   ├── IUserRepository.cs          # Contrat d'accès données
│   │   └── IPasswordHasher.cs          # Contrat de hachage
│   ├── Services/                       # ⚠️ PROBLÈME : Logique métier ici
│   │   └── UserService.cs              # ⚠️ Mélange orchestration + règles
│   └── Exceptions/
│       └── DomainException.cs
│
└── MyApp.Infrastructure/               # Couche Données
    ├── Repositories/
    │   └── UserRepository.cs           # Implémentation accès BDD
    ├── Services/
    │   └── PasswordHasher.cs           # Implémentation hachage
    └── Data/
        └── AppDbContext.cs             # Entity Framework Context
```

### 🔶 Architecture 4 Couches (Clean Architecture)

```
Solution/
│
├── MyApp.API/                          # Couche Présentation
│   ├── Controllers/
│   │   └── AuthController.cs           # Endpoints HTTP (très léger)
│   ├── DTOs/
│   │   ├── RegisterRequestDto.cs       # Données entrantes API
│   │   └── RegisterResponseDto.cs      # Données sortantes API
│   └── Program.cs
│
├── MyApp.Application/                  # Couche Application (NOUVEAU!)
│   ├── Commands/                       # Actions qui modifient l'état
│   │   └── Register/
│   │       ├── RegisterCommand.cs      # Données de la commande
│   │       ├── RegisterCommandHandler.cs # Orchestration de la logique
│   │       └── RegisterCommandValidator.cs # Validation des données
│   ├── Queries/                        # Actions qui lisent l'état
│   │   └── GetUser/
│   │       ├── GetUserQuery.cs
│   │       └── GetUserQueryHandler.cs
│   ├── Interfaces/                     # Contrats spécifiques application
│   │   ├── IEmailService.cs            # Services externes
│   │   └── IPasswordHasher.cs          # Service technique (hachage)
│   ├── DTOs/                           # DTOs internes application
│   │   └── UserDto.cs
│   ├── Mappings/
│   │   └── UserMappingProfile.cs
│   └── Exceptions/
│       └── ApplicationException.cs
│
├── MyApp.Domain/                       # Couche Domaine (PURE)
│   ├── Entities/
│   │   └── User.cs                     # Entité avec logique métier
│   ├── ValueObjects/                   # Objets valeur immuables
│   │   ├── Email.cs
│   │   └── Password.cs
│   ├── Interfaces/                     # Contrats du domaine uniquement
│   │   └── IUserRepository.cs          # Seul le repository reste ici
│   ├── Events/                         # Événements domaine
│   │   └── UserRegisteredEvent.cs
│   └── Exceptions/
│       └── DomainException.cs
│
└── MyApp.Infrastructure/               # Couche Infrastructure
    ├── Repositories/
    │   └── UserRepository.cs
    ├── Services/
    │   ├── PasswordHasher.cs           # Implémente IPasswordHasher (Application)
    │   └── EmailService.cs             # Implémente IEmailService (Application)
    └── Data/
        └── AppDbContext.cs
```

> **Pourquoi IPasswordHasher est dans Application et non Domain en 4 couches ?**
> - Le Domain doit rester **pur** : uniquement la logique métier
> - Le hachage de mot de passe est un **détail technique**, pas une règle métier
> - Le Domain ne doit pas savoir **comment** le mot de passe est haché, juste qu'il l'est
> - En 3 couches, cette séparation n'existe pas, donc on met tout dans Domain par défaut

---

## 🎯 CODE COMPLET : EXEMPLE REGISTER

---

## 1️⃣ CONTROLLER - Architecture 3 Couches

```csharp
// MyApp.API/Controllers/AuthController.cs

using Microsoft.AspNetCore.Mvc;
using MyApp.Domain.Services;
using MyApp.Domain.Entities;
using MyApp.API.DTOs;

namespace MyApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;  // ⚠️ PROBLÈME : Dépendance concrète, pas interface

        public AuthController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            // ⚠️ PROBLÈME : Validation dans le contrôleur
            // La validation devrait être séparée et réutilisable
            if (string.IsNullOrEmpty(request.Email))
                return BadRequest("Email requis");

            if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 8)
                return BadRequest("Mot de passe invalide");

            // ⚠️ PROBLÈME : Le contrôleur fait trop de choses
            // - Il valide
            // - Il gère les exceptions
            // - Il fait le mapping
            // - Il connaît la logique métier (vérification email unique)
            
            try
            {
                // ⚠️ PROBLÈME : Mapping manuel dans le contrôleur
                // Devrait être dans une couche dédiée
                var user = new User
                {
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName
                };

                // ⚠️ PROBLÈME : Le contrôleur passe le mot de passe en clair
                // La logique de "que faire avec le password" ne devrait pas être ici
                var createdUser = await _userService.RegisterAsync(user, request.Password);

                // ⚠️ PROBLÈME : Encore du mapping manuel
                var response = new RegisterResponseDto
                {
                    Id = createdUser.Id,
                    Email = createdUser.Email,
                    FullName = $"{createdUser.FirstName} {createdUser.LastName}"
                };

                return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, response);
            }
            catch (InvalidOperationException ex)  // ⚠️ PROBLÈME : Exception générique
            {
                // ⚠️ PROBLÈME : On ne sait pas quel type d'erreur c'est
                // Email déjà utilisé ? Erreur BDD ? Autre ?
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                // ⚠️ PROBLÈME : Gestion d'erreur basique
                return StatusCode(500, "Erreur serveur");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            // ... code similaire avec mêmes problèmes
            return Ok();
        }
    }
}
```

### ❌ Résumé des Problèmes du Controller 3 Couches :
1. **Trop de responsabilités** : validation, mapping, orchestration, gestion erreurs
2. **Couplage fort** avec le Domain (entités, services concrets)
3. **Difficile à tester** unitairement
4. **Pas réutilisable** : si on veut le même Register en CLI ou message queue, il faut tout réécrire
5. **Mapping éparpillé** : code dupliqué partout

---

## 2️⃣ CONTROLLER - Architecture 4 Couches

```csharp
// MyApp.API/Controllers/AuthController.cs

using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Commands.Register;
using MyApp.API.DTOs;

namespace MyApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;  // ✅ Découplage total via MediatR

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            // ✅ Le contrôleur ne fait QUE :
            // 1. Recevoir la requête HTTP
            // 2. Créer la commande
            // 3. Déléguer à MediatR
            // 4. Retourner la réponse HTTP

            var command = new RegisterCommand(
                Email: request.Email,
                Password: request.Password,
                FirstName: request.FirstName,
                LastName: request.LastName
            );

            // ✅ Toute la logique est dans le Handler
            // Le contrôleur ne sait PAS ce qui se passe
            var result = await _mediator.Send(command);

            // ✅ Mapping simple DTO API
            var response = new RegisterResponseDto
            {
                Id = result.Id,
                Email = result.Email,
                FullName = result.FullName
            };

            return CreatedAtAction(nameof(GetUser), new { id = result.Id }, response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            // Même pattern : Query -> MediatR -> Response
            return Ok();
        }
    }
}
```

### ✅ Avantages du Controller 4 Couches :
1. **Responsabilité unique** : juste HTTP in/out
2. **Aucune logique métier** : tout est délégué
3. **Facile à tester** : mock MediatR
4. **Réutilisable** : la même Command peut être appelée de partout

---

## 3️⃣ COUCHE APPLICATION (4 Couches uniquement)

### RegisterCommand.cs
```csharp
// MyApp.Application/Commands/Register/RegisterCommand.cs

using MediatR;
using MyApp.Application.DTOs;

namespace MyApp.Application.Commands.Register
{
    // ✅ Record immuable : parfait pour une commande
    // ✅ Implémente IRequest<T> pour MediatR
    public record RegisterCommand(
        string Email,
        string Password,
        string FirstName,
        string LastName
    ) : IRequest<UserDto>;  // ✅ Retourne un DTO, pas une entité
}
```

### RegisterCommandValidator.cs
```csharp
// MyApp.Application/Commands/Register/RegisterCommandValidator.cs

using FluentValidation;

namespace MyApp.Application.Commands.Register
{
    // ✅ Validation centralisée et réutilisable
    // ✅ Exécutée automatiquement via Pipeline MediatR
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

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Le prénom est requis")
                .MaximumLength(50);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Le nom est requis")
                .MaximumLength(50);
        }
    }
}
```

> **Note sur la double validation** : La validation existe à deux niveaux :
> - **FluentValidation** (ici) : validation des données d'entrée, rapide, messages utilisateur
> - **Value Objects** (Domain) : invariants métier, protection du domaine
> 
> C'est de la **défense en profondeur** : si quelqu'un appelle le Handler sans passer par le Validator, le Domain se protège lui-même.

### RegisterCommandHandler.cs
```csharp
// MyApp.Application/Commands/Register/RegisterCommandHandler.cs

using MediatR;
using MyApp.Application.DTOs;
using MyApp.Application.Interfaces;
using MyApp.Application.Exceptions;
using MyApp.Domain.Entities;
using MyApp.Domain.Interfaces;
using MyApp.Domain.ValueObjects;

namespace MyApp.Application.Commands.Register
{
    // ✅ UN Handler = UNE responsabilité = UN use case
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, UserDto>
    {
        private readonly IUserRepository _userRepository;      // ✅ Interface du Domain
        private readonly IPasswordHasher _passwordHasher;      // ✅ Interface de Application
        private readonly IEmailService _emailService;          // ✅ Interface de Application

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
            // ✅ ÉTAPE 1 : Vérification métier (email unique)
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new UserAlreadyExistsException(request.Email);
            }

            // ✅ ÉTAPE 2 : Création des Value Objects (validation dans le Domain)
            var email = new Email(request.Email);           // Validation dans le constructeur
            var password = new Password(request.Password);  // Validation dans le constructeur

            // ✅ ÉTAPE 3 : Hachage du mot de passe (service infrastructure)
            var passwordHash = _passwordHasher.Hash(password.Value);

            // ✅ ÉTAPE 4 : Création de l'entité via Factory Method
            var user = User.Create(
                email: email,
                passwordHash: passwordHash,
                firstName: request.FirstName,
                lastName: request.LastName
            );

            // ✅ ÉTAPE 5 : Persistance
            await _userRepository.AddAsync(user);

            // ✅ ÉTAPE 6 : Actions post-création (side effects)
            await _emailService.SendWelcomeEmailAsync(user.Email.Value, user.FirstName);

            // ✅ ÉTAPE 7 : Retourner un DTO, JAMAIS l'entité
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email.Value,
                FullName = user.FullName
            };
        }
    }
}
```

### Interfaces Application
```csharp
// MyApp.Application/Interfaces/IEmailService.cs

namespace MyApp.Application.Interfaces
{
    // ✅ Interface définie dans Application
    // ✅ Implémentée dans Infrastructure
    public interface IEmailService
    {
        Task SendWelcomeEmailAsync(string email, string firstName);
        Task SendPasswordResetEmailAsync(string email, string resetToken);
    }
}
```

```csharp
// MyApp.Application/Interfaces/IPasswordHasher.cs

namespace MyApp.Application.Interfaces
{
    // ✅ Interface technique dans Application (pas Domain)
    // ✅ Le Domain ne doit pas connaître les détails de hachage
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string hash);
    }
}
```

### DTOs Application
```csharp
// MyApp.Application/DTOs/UserDto.cs

namespace MyApp.Application.DTOs
{
    // ✅ DTO interne à l'Application
    // ✅ Différent des DTOs de l'API (peut être plus riche)
    public class UserDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }
}
```

### Exceptions Application
```csharp
// MyApp.Application/Exceptions/ApplicationExceptions.cs

namespace MyApp.Application.Exceptions
{
    // ✅ Exceptions spécifiques et typées
    public class UserAlreadyExistsException : Exception
    {
        public string Email { get; }

        public UserAlreadyExistsException(string email) 
            : base($"Un utilisateur avec l'email '{email}' existe déjà")
        {
            Email = email;
        }
    }

    public class UserNotFoundException : Exception
    {
        public Guid UserId { get; }

        public UserNotFoundException(Guid userId) 
            : base($"Utilisateur avec l'ID '{userId}' non trouvé")
        {
            UserId = userId;
        }
    }
}
```

---

## 4️⃣ COUCHE DOMAIN - Architecture 3 Couches

```csharp
// MyApp.Domain/Entities/User.cs (3 couches)

namespace MyApp.Domain.Entities
{
    // ⚠️ PROBLÈME : Entité anémique (juste des propriétés, pas de comportement)
    public class User
    {
        public Guid Id { get; set; }           // ⚠️ PROBLÈME : set public = pas d'encapsulation
        public string Email { get; set; }       // ⚠️ PROBLÈME : string brut, pas de validation
        public string PasswordHash { get; set; } // ⚠️ PROBLÈME : Peut être modifié n'importe où
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedAt { get; set; } // ⚠️ PROBLÈME : Peut être changé après création
        public bool IsActive { get; set; }      // ⚠️ PROBLÈME : Pas de méthode Deactivate()

        // ⚠️ PROBLÈME : Pas de constructeur = objet peut être dans un état invalide
        // ⚠️ PROBLÈME : Pas de méthodes métier = logique éparpillée dans les services
    }
}
```

```csharp
// MyApp.Domain/Services/UserService.cs (3 couches)

using MyApp.Domain.Entities;
using MyApp.Domain.Interfaces;

namespace MyApp.Domain.Services
{
    // ⚠️ PROBLÈME : Service fourre-tout avec trop de responsabilités
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<User> RegisterAsync(User user, string password)
        {
            // ⚠️ PROBLÈME : Validation ici au lieu du Domain
            if (string.IsNullOrEmpty(user.Email))
                throw new ArgumentException("Email requis");

            // ⚠️ PROBLÈME : Vérification métier mélangée avec orchestration
            var existingUser = await _userRepository.GetByEmailAsync(user.Email);
            if (existingUser != null)
                throw new InvalidOperationException("Email déjà utilisé");

            // ⚠️ PROBLÈME : L'entité est modifiée de l'extérieur
            // L'entité devrait contrôler ses propres modifications
            user.Id = Guid.NewGuid();
            user.PasswordHash = _passwordHasher.Hash(password);
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;

            // ⚠️ PROBLÈME : Pas d'événements domaine
            // Comment notifier les autres parties du système ?

            await _userRepository.AddAsync(user);

            return user;  // ⚠️ PROBLÈME : Retourne l'entité directement
        }

        // ⚠️ PROBLÈME : Méthodes utilitaires qui devraient être dans l'entité
        public string GetFullName(User user)
        {
            return $"{user.FirstName} {user.LastName}";
        }

        // ⚠️ PROBLÈME : Ce service va grossir avec chaque nouveau use case
        public async Task<User> UpdateProfileAsync(Guid userId, string firstName, string lastName) { /* ... */ return null; }
        public async Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword) { /* ... */ }
        public async Task DeactivateUserAsync(Guid userId) { /* ... */ }
        // ... 50 autres méthodes
    }
}
```

### ❌ Résumé des Problèmes du Domain 3 Couches :
1. **Entité anémique** : juste un conteneur de données
2. **Pas d'encapsulation** : tout est public et modifiable
3. **Logique métier dans les services** : l'entité n'a pas de comportement
4. **Service God Object** : devient énorme avec le temps
5. **Pas de Value Objects** : email/password sont des strings bruts
6. **Pas d'événements domaine** : difficile d'étendre le système

---

## 5️⃣ COUCHE DOMAIN - Architecture 4 Couches

### Entité User.cs
```csharp
// MyApp.Domain/Entities/User.cs (4 couches)

using MyApp.Domain.ValueObjects;
using MyApp.Domain.Events;
using MyApp.Domain.Exceptions;

namespace MyApp.Domain.Entities
{
    public class User
    {
        // ✅ Propriétés avec setters privés = encapsulation
        public Guid Id { get; private set; }
        public Email Email { get; private set; }           // ✅ Value Object, pas string
        public string PasswordHash { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public bool IsActive { get; private set; }

        // ✅ Liste d'événements domaine
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        // ✅ Propriété calculée = logique dans l'entité
        public string FullName => $"{FirstName} {LastName}";

        // ✅ Constructeur privé = on force l'utilisation de la Factory
        private User() { }

        // ✅ Factory Method = garantit un état valide
        public static User Create(Email email, string passwordHash, string firstName, string lastName)
        {
            // ✅ Validation dans le Domain
            if (string.IsNullOrWhiteSpace(firstName))
                throw new DomainException("Le prénom est requis");
            
            if (string.IsNullOrWhiteSpace(lastName))
                throw new DomainException("Le nom est requis");

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new DomainException("Le hash du mot de passe est requis");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = passwordHash,
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // ✅ Événement domaine = découplage
            user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Email.Value));

            return user;
        }

        // ✅ Méthodes métier = comportement dans l'entité
        public void UpdateProfile(string firstName, string lastName)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new DomainException("Le prénom est requis");
            
            FirstName = firstName.Trim();
            LastName = lastName.Trim();
            
            AddDomainEvent(new UserProfileUpdatedEvent(Id));
        }

        public void ChangePassword(string newPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw new DomainException("Le nouveau mot de passe est requis");
            
            PasswordHash = newPasswordHash;
            
            AddDomainEvent(new UserPasswordChangedEvent(Id));
        }

        public void Deactivate()
        {
            if (!IsActive)
                throw new DomainException("L'utilisateur est déjà désactivé");
            
            IsActive = false;
            
            AddDomainEvent(new UserDeactivatedEvent(Id));
        }

        public void Activate()
        {
            if (IsActive)
                throw new DomainException("L'utilisateur est déjà actif");
            
            IsActive = true;
        }

        // ✅ Gestion des événements
        private void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
```

### Value Objects
```csharp
// MyApp.Domain/ValueObjects/Email.cs

using MyApp.Domain.Exceptions;

namespace MyApp.Domain.ValueObjects
{
    // ✅ Value Object = immuable, validé, comparable par valeur
    public class Email : IEquatable<Email>
    {
        public string Value { get; }

        public Email(string value)
        {
            // ✅ Validation à la création = toujours valide
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

        // ✅ Égalité par valeur
        public bool Equals(Email? other) => other is not null && Value == other.Value;
        public override bool Equals(object? obj) => Equals(obj as Email);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value;

        public static implicit operator string(Email email) => email.Value;
    }
}
```

```csharp
// MyApp.Domain/ValueObjects/Password.cs

using MyApp.Domain.Exceptions;

namespace MyApp.Domain.ValueObjects
{
    // ✅ Value Object pour le mot de passe en clair (avant hachage)
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
}
```

### Domain Events
```csharp
// MyApp.Domain/Events/DomainEvents.cs

namespace MyApp.Domain.Events
{
    // ✅ Interface marqueur pour les événements
    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }

    // ✅ Événement émis quand un utilisateur s'inscrit
    public record UserRegisteredEvent(Guid UserId, string Email) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

    public record UserProfileUpdatedEvent(Guid UserId) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

    public record UserPasswordChangedEvent(Guid UserId) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

    public record UserDeactivatedEvent(Guid UserId) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
```

### Interface Repository Domain
```csharp
// MyApp.Domain/Interfaces/IUserRepository.cs

using MyApp.Domain.Entities;

namespace MyApp.Domain.Interfaces
{
    // ✅ Interface dans le Domain
    // ✅ Implémentation dans Infrastructure
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task<bool> ExistsAsync(Guid id);
    }
}
```

### Domain Exception
```csharp
// MyApp.Domain/Exceptions/DomainException.cs

namespace MyApp.Domain.Exceptions
{
    // ✅ Exception spécifique au Domain
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
    }
}
```

---

## 📊 TABLEAU COMPARATIF RÉCAPITULATIF

| Aspect | 3 Couches | 4 Couches |
|--------|-----------|-----------|
| **Controller** | Gros, fait tout | Léger, délègue |
| **Validation** | Éparpillée | Centralisée (Validators) |
| **Logique métier** | Dans Services | Dans Entities + Handlers |
| **Entités** | Anémiques | Riches (comportement) |
| **Testabilité** | Difficile | Facile |
| **Réutilisabilité** | Faible | Forte |
| **Découplage** | Faible | Fort (MediatR) |
| **Évolutivité** | Difficile | Facile |
| **Interfaces techniques** | Dans Domain | Dans Application |

---

## 🔧 CONFIGURATION DEPENDENCY INJECTION

### 3 Couches
```csharp
// Program.cs (3 couches)
builder.Services.AddScoped<UserService>();  // ⚠️ Service concret
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
```

### 4 Couches
```csharp
// Program.cs (4 couches)
// ✅ MediatR pour CQRS
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(RegisterCommand).Assembly));

// ✅ FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(RegisterCommandValidator).Assembly);

// ✅ Pipeline de validation automatique
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// ✅ Repositories (interface Domain, implémentation Infrastructure)
builder.Services.AddScoped<IUserRepository, UserRepository>();

// ✅ Services techniques (interfaces Application, implémentations Infrastructure)
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IEmailService, EmailService>();
```

---

## 📚 RÉSUMÉ FINAL

**3 Couches** : Simple mais ne scale pas. OK pour petits projets/POC.

**4 Couches** : Plus complexe au départ, mais :
- ✅ Code plus maintenable
- ✅ Tests unitaires faciles
- ✅ Logique métier protégée
- ✅ Évolutions sans casser l'existant
- ✅ Équipe peut travailler en parallèle

**Règle d'or** : 
- La couche **Application** ORCHESTRE (quoi faire, dans quel ordre)
- La couche **Domain** DÉCIDE (règles métier, invariants)
- La couche **Infrastructure** EXÉCUTE (base de données, emails, etc.)
- La couche **API** EXPOSE (HTTP in/out)
