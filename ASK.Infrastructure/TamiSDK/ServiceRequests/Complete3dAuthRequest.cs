namespace Tami.Pago.Core.ServiceRequests
{
    public class Complete3dAuthRequest : RequestBaseExtend
    {
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
