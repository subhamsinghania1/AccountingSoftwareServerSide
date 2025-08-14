using AccountingAPI.Data;
using AccountingAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace AccountingAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class VendorsController : ControllerBase
    {
        private readonly AccountingContext _context;

        public VendorsController(AccountingContext context)
        {
            _context = context;
        }

        // GET: api/vendors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Vendor>>> GetVendors()
        {
            return await _context.Vendors.AsNoTracking().ToListAsync();
        }

        // GET: api/vendors/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Vendor>> GetVendor(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null)
            {
                return NotFound();
            }
            return vendor;
        }

        // POST: api/vendors
        [HttpPost]
        public async Task<ActionResult<Vendor>> PostVendor(Vendor vendor)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }
            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetVendor), new { id = vendor.Id }, vendor);
        }

        // PUT: api/vendors/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVendor(int id, Vendor vendor)
        {
            if (id != vendor.Id)
            {
                return BadRequest(new { message = "Mismatched vendor id" });
            }
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }
            _context.Entry(vendor).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VendorExists(id)) return NotFound();
                else throw;
            }
            return NoContent();
        }

        // DELETE: api/vendors/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVendor(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null)
            {
                return NotFound();
            }
            _context.Vendors.Remove(vendor);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private bool VendorExists(int id)
        {
            return _context.Vendors.Any(e => e.Id == id);
        }
    }
}