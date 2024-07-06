using JustInTimeApi.Context;
using JustInTimeApi.Dto;
using JustInTimeApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace JustInTimeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {

        private readonly AppDbContext _userDb;

        public UserController(AppDbContext userDb)
        {
            _userDb = userDb;
        }

        [Authorize(Roles = "Admin, Supplier")]
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _userDb.Users.FindAsync(id);
            return user;
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<User>> UpdateUser(int id, [FromBody] UserSettingsDto userSettingsDto)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userDb.Users.FindAsync(id);
            if (user == null) 
            {
                return NotFound();
            }

            user.FirstName = userSettingsDto.FirstName;
            user.LastName = userSettingsDto.LastName;
            // user.Email = userDto.Email;
            user.PhoneNumber = userSettingsDto.PhoneNumber;
            //user.Password = userSettingsDto.Password;
            user.Password = PasswordHasher.HashPassword(userSettingsDto.Password);



            await _userDb.SaveChangesAsync();

            return Ok(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<User>> DeleteUser(int id)
        {
            var user = await _userDb.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _userDb.Users.Remove(user);
            await _userDb.SaveChangesAsync();

            return NoContent();
        }
    }
}
