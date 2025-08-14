using AccountingAPI.Data;
using AccountingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LedgerEntriesController : ControllerBase
    {
        private readonly AccountingContext _context;

        public LedgerEntriesController(AccountingContext context)
        {
            _context = context;
        }

        // GET: api/ledgerentries
        // Optional query parameters: vendorId, from, to
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LedgerEntry>>> GetLedgerEntries([FromQuery] int? vendorId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            IQueryable<LedgerEntry> query = _context.LedgerEntries.AsNoTracking().Include(e => e.Vendor);

            if (vendorId.HasValue)
            {
                query = query.Where(e => e.VendorId == vendorId.Value);
            }
            if (from.HasValue)
            {
                DateTime start = from.Value.Date;
                query = query.Where(e => e.Date >= start);
            }
            if (to.HasValue)
            {
                DateTime end = to.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(e => e.Date <= end);
            }
            return await query.ToListAsync();
        }

        // GET: api/ledgerentries/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LedgerEntry>> GetLedgerEntry(int id)
        {
            var entry = await _context.LedgerEntries.Include(e => e.Vendor).FirstOrDefaultAsync(e => e.Id == id);
            if (entry == null)
            {
                return NotFound();
            }
            return entry;
        }

        // POST: api/ledgerentries
        [HttpPost]
        public async Task<ActionResult<LedgerEntry>> PostLedgerEntry(LedgerEntry entry)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            // Validate vendor exists
            var vendorExists = await _context.Vendors.AnyAsync(v => v.Id == entry.VendorId);
            if (!vendorExists)
            {
                return BadRequest(new { message = "Vendor does not exist" });
            }

            _context.LedgerEntries.Add(entry);
            await _context.SaveChangesAsync();
            // Load Vendor navigation property
            await _context.Entry(entry).Reference(e => e.Vendor).LoadAsync();
            return CreatedAtAction(nameof(GetLedgerEntry), new { id = entry.Id }, entry);
        }

        // PUT: api/ledgerentries/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLedgerEntry(int id, LedgerEntry entry)
        {
            if (id != entry.Id)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            _context.Entry(entry).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LedgerEntryExists(id)) return NotFound();
                else throw;
            }
            return NoContent();
        }

        // DELETE: api/ledgerentries/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLedgerEntry(int id)
        {
            var entry = await _context.LedgerEntries.FindAsync(id);
            if (entry == null)
            {
                return NotFound();
            }
            _context.LedgerEntries.Remove(entry);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private bool LedgerEntryExists(int id)
        {
            return _context.LedgerEntries.Any(e => e.Id == id);
        }
    }
}