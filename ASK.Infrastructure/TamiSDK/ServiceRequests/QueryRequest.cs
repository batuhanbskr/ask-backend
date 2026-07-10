namespace Tami.Pago.Core.ServiceRequests
{
    public class QueryRequest : RequestBase
    {
        /// <summary>
        /// İptal edilmek istenen satış işlemine ait sipariş numarası bilgisidir.
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// Eğer true gönderilirse siparişin başından geçen tüm işlemler paylaşılacaktır. Eğer false ya da boş gönderilirse siparişin sadece son statüsüne ait bilgiler iletilir.
        /// </summary>
        public bool IsTransactionDetail { get; set; }
    }
}
