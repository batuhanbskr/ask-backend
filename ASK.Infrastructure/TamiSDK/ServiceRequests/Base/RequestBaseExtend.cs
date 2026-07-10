namespace Tami.Pago.Core.ServiceRequests
{
    public class RequestBaseExtend : RequestBase
    {
        /// <summary>
        /// Ödeme isteğinin PAGO-Client arasındaki iletişiminde kullanılan tekil bir iletişim bilgisidir.
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// İşlem tutarıdır. Küsurat ayracı (.) ile yapılmalıdır.
        /// </summary>
        public int Amount { get; set; }
    }
}
