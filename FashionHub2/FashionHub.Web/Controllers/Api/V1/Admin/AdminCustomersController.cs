using FashionHub.Web.Application.Admin;
using FashionHub.Web.Application.Common;
using FashionHub.Web.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FashionHub.Web.Controllers.Api.V1.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/customers")]
public sealed class AdminCustomersController : ControllerBase
{
    private readonly IAdminManagementService managementService;

    public AdminCustomersController(IAdminManagementService managementService)
    {
        this.managementService = managementService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminCustomerDto>>> GetCustomers(
        [FromQuery] AdminManagementQuery query,
        CancellationToken cancellationToken)
    {
        var result = await managementService.GetCustomersAsync(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminCustomerDetailDto>> GetCustomer(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await managementService.GetCustomerAsync(id, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPut("{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<AdminCustomerDto>> ToggleCustomer(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await managementService.ToggleCustomerAsync(id, cancellationToken);
        return this.ToActionResult(result);
    }
}
