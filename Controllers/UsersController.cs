using AccountingAPI.Data;
using AccountingAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace AccountingAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AccountingContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UsersController(AccountingContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.AsNoTracking().ToListAsync();
        }

        // GET: api/users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return user;
        }

        // POST: api/users
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
            {
                return Conflict(new { message = "Username already exists" });
            }
            if (!string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, user.PasswordHash);
            }
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        // PUT: api/users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User updated)
        {
            if (id != updated.Id)
            {
                return BadRequest(new { message = "Mismatched user id" });
            }
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var existing = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (existing == null)
            {
                return NotFound();
            }

            if (await _context.Users.AnyAsync(u => u.Username == updated.Username && u.Id != id))
            {
                return Conflict(new { message = "Username already exists" });
            }

            existing.Username = updated.Username;
            existing.Role = updated.Role;
            if (!string.IsNullOrWhiteSpace(updated.PasswordHash))
            {
                existing.PasswordHash = _passwordHasher.HashPassword(existing, updated.PasswordHash);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }

    }
}