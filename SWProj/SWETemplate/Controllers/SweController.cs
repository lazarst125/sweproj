using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWETemplate.Models;

namespace SWETemplate.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SweController : ControllerBase
{
    public SweContext Context { get; set; }

    public SweController(SweContext context)
    {
        Context = context;
    }

    // GET: api/swe/donors
    [HttpGet("donors")]
    public async Task<IActionResult> GetDonors()
    {
        var donors = await Context.Donors.ToListAsync();
        return Ok(donors);
    }

    // GET: api/swe/events
    [HttpGet("events")]
    public async Task<IActionResult> GetEvents()
    {
        var events = await Context.BloodDonationEvents.ToListAsync();
        return Ok(events);
    }

    // GET: api/swe/inventory
    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventory()
    {
        var inventory = await Context.BloodInventories.ToListAsync();
        return Ok(inventory);
    }
}