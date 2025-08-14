using AccountingAPI.Data;
using AccountingAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace AccountingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AccountingContext _context;

        public UsersController(AccountingContext context)
        {
            _context = context;
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
            // Hash the password before saving
            if (!string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                user.PasswordHash = ComputeSha256Hash(user.PasswordHash);
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
                return BadRequest();
            }

            // If password is provided, hash it. Otherwise, keep existing hash.
            var existing = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Username = updated.Username;
            existing.Role = updated.Role;
            if (!string.IsNullOrWhiteSpace(updated.PasswordHash))
            {
                existing.PasswordHash = ComputeSha256Hash(updated.PasswordHash);
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

        private static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256 hash of the input string
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                var builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}