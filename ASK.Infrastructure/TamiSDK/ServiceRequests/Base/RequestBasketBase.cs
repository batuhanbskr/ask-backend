namespace Tami.Pago.Core.ServiceRequests
{
    public class RequestBasketBase
    {
        public string BasketId { get; set; }
        public List<RequestBasketItemBase> BasketItems { get; set; }
    }
}
