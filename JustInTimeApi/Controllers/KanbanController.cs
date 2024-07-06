using JustInTimeApi.Context;
using JustInTimeApi.Dto;
using JustInTimeApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JustInTimeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class KanbanController : ControllerBase
    {
        private readonly AppDbContext _kanbanCards;

        public KanbanController(AppDbContext kanbanCards)
        {
            _kanbanCards = kanbanCards;
        }

        private int GetAdminId()
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminId))
            {
                throw new UnauthorizedAccessException("Admin ID cannot be null or empty. Ensure the token is valid and includes the 'NameIdentifier' claim.");
            }
            return int.Parse(adminId);
        }

        [HttpGet("cards")]
        public async Task<ActionResult<List<KanbanCard>>> GetKanbanCards()
        {
            var adminId = GetAdminId();
            return await _kanbanCards.KanbanCards.Where(c => c.UserId == adminId).ToListAsync();
        }

        [HttpGet("cards/{id}")]
        public async Task<ActionResult<KanbanCard>> GetKanbanCardById(int id)
        {
            var adminId = GetAdminId();
            var card = await _kanbanCards.KanbanCards.FirstOrDefaultAsync(c => c.Id == id && c.UserId == adminId);
            if (card == null)
            {
                return NotFound();
            }
            return card;
        }

        [HttpPost("cards")]
        public async Task<ActionResult<KanbanCard>> AddKanbanCard([FromBody] KanbanCardDto cardDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var adminId = GetAdminId();
            var card = new KanbanCard
            {
                Name = cardDto.Name,
                Description = cardDto.Description,
                Status = cardDto.Status,
                UserId = adminId
            };

            _kanbanCards.KanbanCards.Add(card);
            await _kanbanCards.SaveChangesAsync();

            return CreatedAtAction(nameof(GetKanbanCardById), new { id = card.Id }, card);
        }

        [HttpPut("cards/{id}")]
        public async Task<IActionResult> UpdateKanbanCard(int id, [FromBody] KanbanCardDto cardDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var adminId = GetAdminId();
            var card = await _kanbanCards.KanbanCards.FirstOrDefaultAsync(c => c.Id == id && c.UserId == adminId);
            if (card == null)
            {
                return NotFound();
            }

            card.Name = cardDto.Name;
            card.Description = cardDto.Description;
            card.Status = cardDto.Status;

            await _kanbanCards.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("cards/{id}")]
        public async Task<IActionResult> DeleteKanbanCard(int id)
        {
            var adminId = GetAdminId();
            var card = await _kanbanCards.KanbanCards.FirstOrDefaultAsync(c => c.Id == id && c.UserId == adminId);
            if (card == null)
            {
                return NotFound();
            }

            _kanbanCards.KanbanCards.Remove(card);
            await _kanbanCards.SaveChangesAsync();

            return NoContent();
        }
    }
}
