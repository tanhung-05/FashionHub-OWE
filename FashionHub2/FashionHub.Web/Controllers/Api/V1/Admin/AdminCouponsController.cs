using FashionHub.Web.Application.Admin;
using FashionHub.Web.Application.Common;
using FashionHub.Web.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FashionHub.Web.Controllers.Api.V1.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/coupons")]
public sealed class AdminCouponsController : ControllerBase
{
    private readonly IAdminManagementService managementService;

    public AdminCouponsController(IAdminManagementService managementService)
    {
        this.managementService = managementService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminCouponDto>>> GetCoupons(
        [FromQuery] AdminManagementQuery query,
        CancellationToken cancellationToken)
    {
        var result = await managementService.GetCouponsAsync(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminCouponDto>> GetCoupon(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await managementService.GetCouponAsync(id, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<AdminCouponDto>> CreateCoupon(
        [FromBody] SaveAdminCouponRequest request,
        CancellationToken cancellationToken)
    {
        var result = await managementService.CreateCouponAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return CreatedAtAction(nameof(GetCoupon), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<AdminCouponDto>> UpdateCoupon(
        int id,
        [FromBody] SaveAdminCouponRequest request,
        CancellationToken cancellationToken)
    {
        var result = await managementService.UpdateCouponAsync(
            id,
            request,
            cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCoupon(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await managementService.DeleteCouponAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToActionResult(result).Result!;
    }

    [HttpPut("{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<AdminCouponDto>> ToggleCoupon(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await managementService.ToggleCouponAsync(id, cancellationToken);
        return this.ToActionResult(result);
    }
}
