namespace Tami.Pago.Core
{
    public class ValidationResult
    {
        private static readonly ValidationResult _success = new(true);

        /// <summary>
        ///     Hata mesajlarını alan başarısızlık yapıcısı
        /// </summary>
        /// <param name="errors"></param>
        public ValidationResult(params string[] errors) : this((IEnumerable<string>)errors)
        {
        }

        /// <summary>
        ///     Hata mesajlarını alan başarısızlık yapıcısı
        /// </summary>
        /// <param name="errors"></param>
        public ValidationResult(IEnumerable<string> errors)
        {
            if (errors == null)
            {
                errors = new[] { "Bir hata ile karşılaşıldı." };
            }
            Succeeded = false;
            Errors = errors;
        }

        /// <summary>
        /// Sonucun başarılı olup olmadığını alan yapıcı
        /// </summary>
        /// <param name="success"></param>
        protected ValidationResult(bool success)
        {
            Succeeded = success;
            Errors = new string[0];
        }

        /// <summary>
        ///     İşlem başarılıysa doğrudur
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        ///     Hata listesi
        /// </summary>
        public IEnumerable<string> Errors { get; private set; }

        /// <summary>
        ///     Statik başarı sonucu
        /// </summary>
        /// <returns></returns>
        public static ValidationResult Success
        {
            get { return _success; }
        }

        /// <summary>
        ///     Başarısız yardımcı yöntem
        /// </summary>
        /// <param name="errors"></param>
        /// <returns></returns>
        public static ValidationResult Failed(params string[] errors)
        {
            return new ValidationResult(errors);
        }
    }
}
