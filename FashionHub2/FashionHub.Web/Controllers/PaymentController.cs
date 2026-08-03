using System.Security.Claims;
using FashionHub.Web.Application.Payments;
using FashionHub.Web.ViewModels.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FashionHub.Web.Controllers;

[Route("payment")]
public sealed class PaymentController : Controller
{
    private readonly IVnPayService vnPayService;

    public PaymentController(IVnPayService vnPayService)
    {
        this.vnPayService = vnPayService;
    }

    [Authorize]
    [HttpPost("vnpay/retry")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetryVnPay(
        int orderId,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Challenge();
        }

        var result = await vnPayService.CreatePaymentUrlAsync(
            orderId,
            userId,
            GetClientIpAddress(),
            cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error!.Message;
            return RedirectToAction("OrderDetail", "Account", new { id = orderId });
        }

        return Redirect(result.Value!);
    }

    [AllowAnonymous]
    [HttpGet("vnpay-return")]
    public async Task<IActionResult> VnPayReturn(CancellationToken cancellationToken)
    {
        var result = await vnPayService.ProcessCallbackAsync(
            ReadVnPayParameters(),
            cancellationToken);

        return View("Result", new PaymentResultViewModel
        {
            IsValidSignature = result.IsValidSignature,
            IsSuccessful = result.IsSuccessful,
            OrderId = result.OrderId,
            TransactionReference = result.TransactionReference,
            ResponseCode = result.ResponseCode,
            Message = result.Message
        });
    }

    [AllowAnonymous]
    [HttpGet("vnpay-ipn")]
    public async Task<IActionResult> VnPayIpn(CancellationToken cancellationToken)
    {
        var result = await vnPayService.ProcessCallbackAsync(
            ReadVnPayParameters(),
            cancellationToken);
        return Ok(new VnPayIpnResponse(
            result.MerchantResponseCode,
            result.Message));
    }

    private Dictionary<string, string> ReadVnPayParameters() =>
        Request.Query
            .Where(item => item.Key.StartsWith("vnp_", StringComparison.Ordinal))
            .ToDictionary(
                item => item.Key,
                item => item.Value.ToString(),
                StringComparer.Ordinal);

    private string GetClientIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
}
