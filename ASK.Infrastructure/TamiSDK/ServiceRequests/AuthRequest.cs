using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Tami.Pago.Core.ServiceRequests
{
    public class AuthRequest : RequestBaseExtend
    {
        /// <summary>
        /// İşlemin döviz kodunu belirtir.
        /// Desteklenen döviz kodları: TRY, RUB, AED, JPY, SAR, GBP, USD, EUR
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public CurrencyTypes Currency { get; set; } = CurrencyTypes.TRY;

        /// <summary>
        /// Taksit bilgisi, tek çekim için 1 gönderilmelidir.
        /// </summary>
        public int InstallmentCount { get; set; } = 1;

        /// <summary>
        /// Standart değeri false olarak değerlendirilmektedir.
        /// </summary>
        public bool MotoInd { get; set; } = false;

        /// <summary>
        /// Ödeme grubu, varsayılan PRODUCT. 
        /// Geçerli değerler enum içinde sunulmaktadır.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public PaymentGroups PaymentGroup { get; set; } = PaymentGroups.PRODUCT;

        /// <summary>
        /// Ödeme kanalı. Geçerli değerler enum içinde sunulmaktadır.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public PaymentChannels PaymentChannel { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string SubType { get; set; }

        /// <summary>
        /// İşlem yapılan kart bilgilerinin tutulduğu objedir.
        /// </summary>
        public RequestCardBase Card { get; set; }

        /// <summary>
        /// Fatura adresi bilgilerinin tutulduğu objedir.
        /// </summary>
        public RequestAddressBase BillingAddress { get; set; }

        /// <summary>
        /// Kargo adresi bilgilerinin tutulduğu objedir.
        /// </summary>
        public RequestAddressBase ShippingAddress { get; set; }

        /// <summary>
        /// Üye işyeri tarafında işlem yapan kullanıcıya ait bilgilerin tutulduğu objedir.
        /// </summary>
        public RequestBuyerBase Buyer { get; set; }

        /// <summary>
        /// Sepetteki ürünlerin bilgilerini barındıran objedir.
        /// </summary>
        public RequestBasketBase Basket { get; set; }

        public override ValidationResult Validate()
        {
            HashSet<string> errors = new();
            if (string.IsNullOrEmpty(OrderId))
            {
                errors.Add("OrderId alanı boş veya null olamaz.");
            }

            if (Amount <= 0)
            {
                errors.Add("Amount alanı 0'dan büyük olmalıdır.");
            }

            if (Basket == null ||
                Basket.BasketItems == null ||
                Basket.BasketItems.Count <= 0 || 
                Basket.BasketItems.Sum(m => m.TotalPrice) != Amount)
            {
                errors.Add("Sepet bilgilerini kontrol ediniz.");
            }

            if (Card == null)
            {
                errors.Add("Card objesi null olmamalıdır.");
            }
            else
            {
                if (string.IsNullOrEmpty(Card.HolderName))
                {
                    errors.Add("Kart sahibinin ad soyad alanı zorunludur.");
                }

                if (Card.ExpireMonth == 0 || Card.ExpireYear == 0)
                {
                    errors.Add("Kart son kullanma tarihi alanları zorunludur.");
                }

                if (string.IsNullOrEmpty(Card.Number))
                {
                    errors.Add("Kart numarası alanı zorunludur.");
                }
            }

            if (errors != null && errors.Count > 0)
            {
                return ValidationResult.Failed(errors.ToArray());
            }

            return ValidationResult.Success;
        }
    }
}
