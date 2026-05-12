using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GrouponClone.Application.Features.Vendors;

namespace GrouponClone.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class VendorsController : ControllerBase
{
    private readonly IMediator _mediator;
    public VendorsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Register a new vendor profile.</summary>
    [HttpPost("register")]
    [Authorize]
    [ProducesResponseType(typeof(RegisterVendorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterVendorCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return StatusCode(201, result);
    }

    /// <summary>Get vendor dashboard metrics.</summary>
    [HttpGet("me/dashboard")]
    [Authorize(Roles = "Vendor")]
    [ProducesResponseType(typeof(VendorDashboardResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
        => Ok(await _mediator.Send(new GetVendorDashboardQuery(), ct));

    /// <summary>Get vendor's own deals.</summary>
    [HttpGet("me/deals")]
    [Authorize(Roles = "Vendor")]
    [ProducesResponseType(typeof(IEnumerable<VendorDealDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeals(CancellationToken ct)
        => Ok(await _mediator.Send(new GetVendorDealsQuery(), ct));

    /// <summary>Create a deal (alias — handled by DealsController).</summary>
    [HttpPost("me/deals")]
    [Authorize(Roles = "Vendor")]
    public async Task<IActionResult> CreateDeal(
        [FromBody] Application.Features.Deals.Commands.CreateDeal.CreateDealCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetDeals), result);
    }
}
