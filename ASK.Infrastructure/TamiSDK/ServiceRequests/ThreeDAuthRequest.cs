namespace Tami.Pago.Core.ServiceRequests
{
    public class ThreeDAuthRequest : AuthRequest
    {
        /// <summary>
        /// İşlem sonucunun döneceği bağlantı adresidir.
        /// </summary>
        public string CallbackUrl { get; set; }
    }
}
