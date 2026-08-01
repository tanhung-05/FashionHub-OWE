using FashionHub.Web.Application.Admin;
using FashionHub.Web.Application.Common;
using FashionHub.Web.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FashionHub.Web.Controllers.Api.V1.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/categories")]
public sealed class AdminCategoriesController : ControllerBase
{
    private readonly IAdminManagementService managementService;

    public AdminCategoriesController(IAdminManagementService managementService)
    {
        this.managementService = managementService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminCategoryDto>>> GetCategories(
        [FromQuery] AdminManagementQuery query,
        CancellationToken cancellationToken)
    {
        var result = await managementService.GetCategoriesAsync(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminCategoryDto>> GetCategory(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await managementService.GetCategoryAsync(id, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<AdminCategoryDto>> CreateCategory(
        [FromBody] SaveAdminCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await managementService.CreateCategoryAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return CreatedAtAction(nameof(GetCategory), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<AdminCategoryDto>> UpdateCategory(
        int id,
        [FromBody] SaveAdminCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await managementService.UpdateCategoryAsync(
            id,
            request,
            cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await managementService.DeleteCategoryAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToActionResult(result).Result!;
    }
}
