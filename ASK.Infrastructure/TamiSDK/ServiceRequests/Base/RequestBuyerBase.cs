using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Tami.Pago.Core.ServiceRequests
{
    public class RequestBuyerBase
    {
        /// <summary>
        /// İşlem yapan kullanıcının IP adresi bilgisi.
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Üye işyeri tarafındaki alıcıya ait ID bilgisi.
        /// </summary>
        public string BuyerId { get; set; }

        /// <summary>
        /// İşlem yapan kullanıcının ad bilgisi.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// İşlem yapan kullanıcının soyad bilgisi.
        /// </summary>
        public string SurName { get; set; }

        /// <summary>
        /// İşlem yapan kullanıcının kimlik(TCKN) bilgisi.
        /// </summary>
        public long IdentityNumber { get; set; }

        /// <summary>
        /// İşlem yapan kullanıcının şehir bilgisi.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// İşlem yapan kullanıcının ülke bilgisi.
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// İşlem yapan kullanıcının posta kodu bilgisi.
        /// </summary>
        public string ZipCode { get; set; }

        /// <summary>
        /// İşlem yapan kullanıcının e-posta adresi bilgisi.
        /// </summary>
        public string EmailAddress { get; set; }

        /// <summary>
        /// İşlem yapan kullanıcının GSM bilgisi.
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// İşlem yapan kullanıcının kayıt adresi bilgisi.
        /// </summary>
        public string RegistrationAddress { get; set; }

        /// <summary>
        /// İilem yapan kullanıcının üye iş yerindeki son giriş tarihi.
        /// Tarih formatı yyyy-MM-ddTHH:mm:ss.fff şeklinde olmalıdır.
        /// </summary>
        public DateTime LastLoginDate { get; set; }

        /// <summary>
        /// İilem yapan kullanıcının üye iş yerindeki kayıt tarihi.
        /// Tarih formatı yyyy-MM-ddTHH:mm:ss.fff şeklinde olmalıdır.
        /// </summary>
        public DateTime RegistrationDate { get; set; }
    }
}
