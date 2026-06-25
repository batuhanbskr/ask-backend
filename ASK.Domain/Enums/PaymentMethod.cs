namespace ASK.Domain.Enums;

public enum PaymentMethod
{
    Cash        = 0,  // Nakit
    CreditCard  = 1,  // Kredi Kartı (fiziksel POS)
    VirtualPos  = 2,  // Sanal POS (online)
    BankTransfer = 3, // Havale / EFT
    Check       = 4   // Çek
}
