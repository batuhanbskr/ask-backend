namespace Tami.Pago.Core.ServiceRequests
{
    public abstract class RequestBase
    {
        /// <summary>
        /// Hesaplanıp iletilmesi beklenen değerdir. Eksik veya hatalı iletilirse işlem bankaya yönlendirilmez, hata verilir.
        /// </summary>
        public string SecurityHash { get; set; }

        public virtual ValidationResult Validate()
        {
            return ValidationResult.Success;
        }
    }
}
