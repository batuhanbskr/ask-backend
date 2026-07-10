namespace Tami.Pago.Core.ServiceRequests
{
    public class ReverseRequest : RequestBaseExtend
    {
        /// <summary>
        /// İade nedeni bilgisini içermelidir.
        /// BOZUK URUN
        /// HATALI SATIN ALMA
        /// DAHA SONRA ALACAGIM
        /// DIGER
        /// </summary>
        public string Reason { get; set; }

        public override ValidationResult Validate()
        {
            if (string.IsNullOrEmpty(OrderId))
            {
                ValidationResult.Failed("OrderId alanı boş veya null olamaz.");
            }

            return ValidationResult.Success;
        }
    }
}
