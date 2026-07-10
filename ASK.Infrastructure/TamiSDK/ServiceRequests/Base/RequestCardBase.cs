namespace Tami.Pago.Core.ServiceRequests
{
    public class RequestCardBase
    {
        /// <summary>
        /// Ödemenin alınacağı kart sahibinin adı soyadı bilgisi.
        /// </summary>
        public string HolderName { get; set; }

        /// <summary>
        /// Ödemenin alınacağı kartın güvenlik kodu bilgisi.
        /// </summary>
        public string Cvv { get; set; }

        /// <summary>
        /// Ödemenin alınacağı kartın son kullanma tarihi ay bilgisi.
        /// </summary>
        public int ExpireMonth { get; set; }

        /// <summary>
        /// Ödemenin alınacağı kartın son kullanma tarihi yıl bilgisi.
        /// </summary>
        public int ExpireYear { get; set; }

        /// <summary>
        /// Ödemenin alınacağı kart numarası bilgisi.
        /// </summary>
        public string Number { get; set; }
    }
}
