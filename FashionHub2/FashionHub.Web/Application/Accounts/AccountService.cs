using FashionHub.Web.Application.Authentication;
using FashionHub.Web.Application.Common;
using FashionHub.Web.Data;
using FashionHub.Web.Models.Generated;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Application.Accounts;

public sealed class AccountService : IAccountService
{
    private readonly ApplicationDbContext dbContext;
    private readonly ICurrentUserService currentUser;
    private readonly IPasswordHasher passwordHasher;
    private readonly TimeProvider timeProvider;

    public AccountService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IPasswordHasher passwordHasher,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.currentUser = currentUser;
        this.passwordHasher = passwordHasher;
        this.timeProvider = timeProvider;
    }

    public async Task<ServiceResult<AccountProfileDto>> GetProfileAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (!userId.IsSuccess)
        {
            return ServiceResult<AccountProfileDto>.Failure(
                userId.Error!.Type,
                userId.Error.Code,
                userId.Error.Message);
        }

        var profile = await dbContext.NguoiDungs
            .AsNoTracking()
            .Where(user =>
                user.IdnguoiDung == userId.Value
                && user.TrangThai
                && user.DeletedAt == null)
            .Select(user => new AccountProfileDto(
                user.IdnguoiDung,
                user.HoTen,
                user.Email,
                user.SoDienThoai,
                user.IdvaiTroNavigation.TenVaiTro,
                user.NgayTao))
            .SingleOrDefaultAsync(cancellationToken);

        return profile is null
            ? AuthenticationRequired<AccountProfileDto>()
            : ServiceResult<AccountProfileDto>.Success(profile);
    }

    public async Task<ServiceResult<AccountProfileDto>> UpdateProfileAsync(
        UpdateAccountProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var userResult = await GetTrackedUserAsync(cancellationToken);
        if (!userResult.IsSuccess)
        {
            return ServiceResult<AccountProfileDto>.Failure(
                userResult.Error!.Type,
                userResult.Error.Code,
                userResult.Error.Message);
        }

        var user = userResult.Value!;
        var email = request.Email.Trim().ToLowerInvariant();
        var phone = NormalizeOptional(request.PhoneNumber);

        var emailExists = await dbContext.NguoiDungs.AnyAsync(
            item => item.IdnguoiDung != user.IdnguoiDung
                && item.Email.ToLower() == email,
            cancellationToken);
        if (emailExists)
        {
            return ServiceResult<AccountProfileDto>.Failure(
                ServiceErrorType.Conflict,
                "email-already-exists",
                "Email nay da duoc su dung.");
        }

        if (phone is not null)
        {
            var phoneExists = await dbContext.NguoiDungs.AnyAsync(
                item => item.IdnguoiDung != user.IdnguoiDung
                    && item.SoDienThoai == phone,
                cancellationToken);
            if (phoneExists)
            {
                return ServiceResult<AccountProfileDto>.Failure(
                    ServiceErrorType.Conflict,
                    "phone-already-exists",
                    "So dien thoai nay da duoc su dung.");
            }
        }

        user.HoTen = request.FullName.Trim();
        user.Email = email;
        user.SoDienThoai = phone;
        user.NgayCapNhat = Now();
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(user).Reference(item => item.IdvaiTroNavigation)
            .LoadAsync(cancellationToken);
        return ServiceResult<AccountProfileDto>.Success(MapProfile(user));
    }

    public async Task<ServiceResult<bool>> ChangePasswordAsync(
        ChangeAccountPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var userResult = await GetTrackedUserAsync(cancellationToken);
        if (!userResult.IsSuccess)
        {
            return ServiceResult<bool>.Failure(
                userResult.Error!.Type,
                userResult.Error.Code,
                userResult.Error.Message);
        }

        var user = userResult.Value!;
        if (!passwordHasher.Verify(request.CurrentPassword, user.MatKhauHash))
        {
            return ServiceResult<bool>.Failure(
                ServiceErrorType.Validation,
                "current-password-invalid",
                "Mat khau hien tai khong dung.");
        }

        if (passwordHasher.Verify(request.NewPassword, user.MatKhauHash))
        {
            return ServiceResult<bool>.Failure(
                ServiceErrorType.Validation,
                "password-unchanged",
                "Mat khau moi phai khac mat khau hien tai.");
        }

        user.MatKhauHash = passwordHasher.Hash(request.NewPassword);
        user.SecurityStamp = Guid.NewGuid();
        user.NgayCapNhat = Now();
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<IReadOnlyList<AddressDto>>> GetAddressesAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (!userId.IsSuccess)
        {
            return ServiceResult<IReadOnlyList<AddressDto>>.Failure(
                userId.Error!.Type,
                userId.Error.Code,
                userId.Error.Message);
        }

        var addresses = await dbContext.DiaChis
            .AsNoTracking()
            .Where(address => address.IdnguoiDung == userId.Value)
            .OrderByDescending(address => address.LaMacDinh)
            .ThenByDescending(address => address.IddiaChi)
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<AddressDto>>.Success(
            addresses.Select(MapAddress).ToList());
    }

    public async Task<ServiceResult<AddressDto>> GetAddressAsync(
        int addressId,
        CancellationToken cancellationToken = default)
    {
        var addressResult = await GetOwnedAddressAsync(
            addressId,
            track: false,
            cancellationToken);
        return addressResult.IsSuccess
            ? ServiceResult<AddressDto>.Success(MapAddress(addressResult.Value!))
            : ServiceResult<AddressDto>.Failure(
                addressResult.Error!.Type,
                addressResult.Error.Code,
                addressResult.Error.Message);
    }

    public async Task<ServiceResult<AddressDto>> CreateAddressAsync(
        SaveAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (!userId.IsSuccess)
        {
            return ServiceResult<AddressDto>.Failure(
                userId.Error!.Type,
                userId.Error.Code,
                userId.Error.Message);
        }

        var existingAddresses = await dbContext.DiaChis
            .Where(address => address.IdnguoiDung == userId.Value)
            .ToListAsync(cancellationToken);
        var isDefault = request.IsDefault || existingAddresses.Count == 0;
        if (isDefault)
        {
            ClearDefault(existingAddresses);
        }

        var address = new DiaChi
        {
            IdnguoiDung = userId.Value,
            TenNguoiNhan = request.RecipientName.Trim(),
            SoDienThoai = request.PhoneNumber.Trim(),
            ChiTiet = request.Street.Trim(),
            PhuongXa = request.Ward.Trim(),
            QuanHuyen = request.District.Trim(),
            TinhThanh = request.Province.Trim(),
            LaMacDinh = isDefault,
            NgayTao = Now()
        };

        dbContext.DiaChis.Add(address);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<AddressDto>.Success(MapAddress(address));
    }

    public async Task<ServiceResult<AddressDto>> UpdateAddressAsync(
        int addressId,
        SaveAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var addressResult = await GetOwnedAddressAsync(
            addressId,
            track: true,
            cancellationToken);
        if (!addressResult.IsSuccess)
        {
            return ServiceResult<AddressDto>.Failure(
                addressResult.Error!.Type,
                addressResult.Error.Code,
                addressResult.Error.Message);
        }

        var address = addressResult.Value!;
        var addresses = await dbContext.DiaChis
            .Where(item => item.IdnguoiDung == address.IdnguoiDung)
            .ToListAsync(cancellationToken);

        if (request.IsDefault)
        {
            ClearDefault(addresses);
            address.LaMacDinh = true;
        }
        else if (address.LaMacDinh)
        {
            var replacement = addresses
                .Where(item => item.IddiaChi != address.IddiaChi)
                .OrderByDescending(item => item.IddiaChi)
                .FirstOrDefault();
            address.LaMacDinh = replacement is null;
            if (replacement is not null)
            {
                replacement.LaMacDinh = true;
            }
        }

        address.TenNguoiNhan = request.RecipientName.Trim();
        address.SoDienThoai = request.PhoneNumber.Trim();
        address.ChiTiet = request.Street.Trim();
        address.PhuongXa = request.Ward.Trim();
        address.QuanHuyen = request.District.Trim();
        address.TinhThanh = request.Province.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<AddressDto>.Success(MapAddress(address));
    }

    public async Task<ServiceResult<bool>> DeleteAddressAsync(
        int addressId,
        CancellationToken cancellationToken = default)
    {
        var addressResult = await GetOwnedAddressAsync(
            addressId,
            track: true,
            cancellationToken);
        if (!addressResult.IsSuccess)
        {
            return ServiceResult<bool>.Failure(
                addressResult.Error!.Type,
                addressResult.Error.Code,
                addressResult.Error.Message);
        }

        var address = addressResult.Value!;
        if (address.LaMacDinh)
        {
            var replacement = await dbContext.DiaChis
                .Where(item =>
                    item.IdnguoiDung == address.IdnguoiDung
                    && item.IddiaChi != address.IddiaChi)
                .OrderByDescending(item => item.IddiaChi)
                .FirstOrDefaultAsync(cancellationToken);
            if (replacement is not null)
            {
                replacement.LaMacDinh = true;
            }
        }

        dbContext.DiaChis.Remove(address);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<AddressDto>> SetDefaultAddressAsync(
        int addressId,
        CancellationToken cancellationToken = default)
    {
        var addressResult = await GetOwnedAddressAsync(
            addressId,
            track: true,
            cancellationToken);
        if (!addressResult.IsSuccess)
        {
            return ServiceResult<AddressDto>.Failure(
                addressResult.Error!.Type,
                addressResult.Error.Code,
                addressResult.Error.Message);
        }

        var address = addressResult.Value!;
        var addresses = await dbContext.DiaChis
            .Where(item => item.IdnguoiDung == address.IdnguoiDung)
            .ToListAsync(cancellationToken);
        ClearDefault(addresses);
        address.LaMacDinh = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<AddressDto>.Success(MapAddress(address));
    }

    private ServiceResult<int> GetUserId() => currentUser.UserId.HasValue
        ? ServiceResult<int>.Success(currentUser.UserId.Value)
        : AuthenticationRequired<int>();

    private async Task<ServiceResult<NguoiDung>> GetTrackedUserAsync(
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!userId.IsSuccess)
        {
            return AuthenticationRequired<NguoiDung>();
        }

        var user = await dbContext.NguoiDungs
            .SingleOrDefaultAsync(item =>
                item.IdnguoiDung == userId.Value
                && item.TrangThai
                && item.DeletedAt == null,
                cancellationToken);
        return user is null
            ? AuthenticationRequired<NguoiDung>()
            : ServiceResult<NguoiDung>.Success(user);
    }

    private async Task<ServiceResult<DiaChi>> GetOwnedAddressAsync(
        int addressId,
        bool track,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!userId.IsSuccess)
        {
            return AuthenticationRequired<DiaChi>();
        }

        IQueryable<DiaChi> query = dbContext.DiaChis;
        if (!track)
        {
            query = query.AsNoTracking();
        }

        var address = await query.SingleOrDefaultAsync(item =>
            item.IddiaChi == addressId
            && item.IdnguoiDung == userId.Value,
            cancellationToken);
        return address is null
            ? ServiceResult<DiaChi>.Failure(
                ServiceErrorType.NotFound,
                "address-not-found",
                "Khong tim thay dia chi.")
            : ServiceResult<DiaChi>.Success(address);
    }

    private static ServiceResult<T> AuthenticationRequired<T>() =>
        ServiceResult<T>.Failure(
            ServiceErrorType.Unauthorized,
            "authentication-required",
            "Vui long dang nhap.");

    private static void ClearDefault(IEnumerable<DiaChi> addresses)
    {
        foreach (var address in addresses)
        {
            address.LaMacDinh = false;
        }
    }

    private static AccountProfileDto MapProfile(NguoiDung user) => new(
        user.IdnguoiDung,
        user.HoTen,
        user.Email,
        user.SoDienThoai,
        user.IdvaiTroNavigation.TenVaiTro,
        user.NgayTao);

    private static AddressDto MapAddress(DiaChi address) => new(
        address.IddiaChi,
        address.TenNguoiNhan,
        address.SoDienThoai,
        address.ChiTiet,
        address.PhuongXa,
        address.QuanHuyen,
        address.TinhThanh,
        address.LaMacDinh,
        string.Join(", ", new[]
        {
            address.ChiTiet,
            address.PhuongXa,
            address.QuanHuyen,
            address.TinhThanh
        }.Where(part => !string.IsNullOrWhiteSpace(part))),
        address.NgayTao);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private DateTime Now() => timeProvider.GetLocalNow().DateTime;
}
