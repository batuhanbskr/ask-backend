namespace Tami.Pago.Core.ServiceRequests
{
    public class RequestAddressBase
    {
        /// <summary>
        /// Üye işyeri tarafındaki açık adres bilgisi.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Üye işyeri tarafındaki şehir bilgisi.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Üye işyeri tarafındaki ticari ünvan bilgisi.
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// Üye işyeri tarafındaki ülke bilgisi.
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Üye işyeri tarafındaki semt bilgisi.
        /// </summary>
        public string District { get; set; }

        /// <summary>
        /// Üye işyeri tarafındaki adres sahibi kişinin ad soyad bilgisi.
        /// </summary>
        public string ContactName { get; set; }

        /// <summary>
        /// Üye işyeri tarafındaki alıcıya ait GSM numarası.
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Üye işyeri tarafındaki posta kodu bilgisi.
        /// </summary>
        public string ZipCode { get; set; }
    }
}
