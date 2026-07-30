using FashionHub.Web.Application.Cart;
using FashionHub.Web.Application.Common;

namespace FashionHub.Web.Services;

public interface ICartService
{
    Task<ServiceResult<CartDto>> GetCartAsync(
        CancellationToken cancellationToken = default);

    Task<ServiceResult<CartDto>> AddAsync(
        int variantId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<CartDto>> UpdateAsync(
        int variantId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<CartDto>> RemoveAsync(
        int variantId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<CartDto>> ClearAsync(
        CancellationToken cancellationToken = default);

    Task MergeGuestCartAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
