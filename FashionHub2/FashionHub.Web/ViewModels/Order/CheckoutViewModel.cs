namespace FashionHub.Web.ViewModels.Order;

public class CheckoutViewModel
{
    public List<Cart.CartItemViewModel> CartItems { get; set; } = new();
    
    public List<AddressViewModel> UserAddresses { get; set; } = new();
    
    public List<PaymentMethodViewModel> PaymentMethods { get; set; } = new();
    
    public decimal Subtotal { get; set; }
    
    public decimal ShippingFee { get; set; }
    
    public decimal Discount { get; set; }
    
    public string AppliedCouponCode { get; set; } = string.Empty;
    
    public decimal Total => Subtotal + ShippingFee - Discount;
}