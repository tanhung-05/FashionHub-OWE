namespace FashionHub.Web.Areas.Admin.ViewModels;

public class SalesReportViewModel
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Period { get; set; } = "daily";
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalShipping { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<string> ChartLabels { get; set; } = new();
    public List<decimal> ChartData { get; set; } = new();
    public List<TopProductViewModel> TopProducts { get; set; } = new();
}

public class TopProductViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class CustomerReportViewModel
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalCustomers { get; set; }
    public int NewCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public List<TopCustomerViewModel> TopCustomers { get; set; } = new();
}

public class TopCustomerViewModel
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
}

public class ProductPerformanceViewModel
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? CategoryId { get; set; }
    public Dictionary<int, string> Categories { get; set; } = new();
    public List<ProductPerformanceItemViewModel> Products { get; set; } = new();
}

public class ProductPerformanceItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}