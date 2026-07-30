using FashionHub.Web.Application.Admin;
using FashionHub.Web.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FashionHub.Web.Controllers.Api.V1.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/reports")]
public sealed class AdminReportsController : ControllerBase
{
    private readonly IAdminReportService reportService;

    public AdminReportsController(IAdminReportService reportService)
    {
        this.reportService = reportService;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardReportDto>> GetDashboard(
        [FromQuery] AdminReportQuery query,
        CancellationToken cancellationToken)
    {
        var result = await reportService.GetDashboardAsync(query, cancellationToken);
        return this.ToActionResult(result);
    }
}
