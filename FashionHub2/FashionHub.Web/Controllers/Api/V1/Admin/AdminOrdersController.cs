using FashionHub.Web.Application.Admin;
using FashionHub.Web.Application.Common;
using FashionHub.Web.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FashionHub.Web.Controllers.Api.V1.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/orders")]
public sealed class AdminOrdersController : ControllerBase
{
    private readonly IAdminOrderService orderService;

    public AdminOrdersController(IAdminOrderService orderService)
    {
        this.orderService = orderService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminOrderSummaryDto>>> GetOrders(
        [FromQuery] AdminOrderQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await orderService.GetOrdersAsync(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminOrderDetailDto>> GetOrder(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await orderService.GetOrderAsync(id, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPut("{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<AdminOrderDetailDto>> UpdateStatus(
        int id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await orderService.UpdateStatusAsync(id, request, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("confirm-pending")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<int>> ConfirmAllPending(
        CancellationToken cancellationToken)
    {
        var result = await orderService.ConfirmAllPendingAsync(cancellationToken);
        return this.ToActionResult(result);
    }
}
