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