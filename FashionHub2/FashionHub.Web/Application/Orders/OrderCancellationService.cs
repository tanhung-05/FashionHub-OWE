using FashionHub.Web.Data;
using FashionHub.Web.Domain;
using FashionHub.Web.Models.Generated;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Application.Orders;

public interface IOrderCancellationService
{
    Task<bool> ApplyAsync(
        DonHang order,
        int? actorUserId,
        string reason,
        CancellationToken cancellationToken = default);
}

public sealed class OrderCancellationService : IOrderCancellationService
{
    private readonly ApplicationDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public OrderCancellationService(
        ApplicationDbContext dbContext,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.timeProvider = timeProvider;
    }

    public async Task<bool> ApplyAsync(
        DonHang order,
        int? actorUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (order.IdtrangThai == OrderStatusIds.Cancelled)
        {
            return false;
        }

        var now = timeProvider.GetLocalNow().DateTime;
        var quantitiesByVariant = order.ChiTietDonHangs
            .Where(item => item.IdbienThe.HasValue)
            .GroupBy(item => item.IdbienThe!.Value)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.SoLuong));

        if (quantitiesByVariant.Count > 0)
        {
            var variantIds = quantitiesByVariant.Keys.ToList();
            var variants = await dbContext.BienTheSanPhams
                .Where(variant => variantIds.Contains(variant.IdbienThe))
                .ToListAsync(cancellationToken);

            foreach (var variant in variants)
            {
                var quantity = quantitiesByVariant[variant.IdbienThe];
                var previousStock = variant.SoLuongTon;
                variant.SoLuongTon += quantity;
                variant.TongDaBan = Math.Max(0, variant.TongDaBan - quantity);
                variant.NgayCapNhat = now;

                dbContext.LichSuTonKhos.Add(new LichSuTonKho
                {
                    IdbienThe = variant.IdbienThe,
                    IdnguoiThucHien = actorUserId,
                    IddonHang = order.IddonHang,
                    LoaiThayDoi = InventoryChangeTypes.OrderCancelled,
                    SoLuongThayDoi = quantity,
                    TonTruoc = previousStock,
                    TonSau = variant.SoLuongTon,
                    GhiChu = reason,
                    NgayTao = now
                });
            }
        }

        if (order.IdmaGiamGia.HasValue)
        {
            var coupon = await dbContext.MaGiamGia.FindAsync(
                [order.IdmaGiamGia.Value],
                cancellationToken);
            if (coupon != null)
            {
                coupon.DaSuDung = Math.Max(0, coupon.DaSuDung - 1);
            }
        }

        var previousStatus = order.IdtrangThai;
        order.IdtrangThai = OrderStatusIds.Cancelled;
        order.NgayCapNhat = now;
        dbContext.LichSuDonHangs.Add(new LichSuDonHang
        {
            IddonHang = order.IddonHang,
            IdtrangThaiCu = previousStatus,
            IdtrangThaiMoi = OrderStatusIds.Cancelled,
            IdnguoiThucHien = actorUserId,
            GhiChu = reason,
            NgayTao = now
        });

        return true;
    }
}
