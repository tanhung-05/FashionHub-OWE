using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FashionHub.Areas.Admin.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalOrders { get; set; }
        public int NewOrders { get; set; } // Đơn chờ xác nhận
        public decimal TotalRevenue { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
    }
}