// ViewModels/CheckoutViewModel.cs
using FashionHub.Models;
using System.Collections.Generic;
using System.Web.Mvc;

namespace FashionHub.ViewModels
{
    public class CheckoutViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; }
        public List<AddressViewModel> UserAddresses { get; set; }
        public List<PhuongThucThanhToan> PaymentMethods { get; set; }
        public IEnumerable<SelectListItem> Provinces { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; } 
        public decimal Discount { get; set; }
        public string AppliedCouponCode { get; set; }
        public decimal Total => Subtotal + ShippingFee - Discount;
    }
}