namespace Tami.Pago.Core.ServiceRequests
{
    public class RequestBasketItemBase
    {
        public string ItemId { get; set; }
        public string Name { get; set; }
        public string ItemType { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public int NumberOfProducts { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
