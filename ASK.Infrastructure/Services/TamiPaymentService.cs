using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ASK.Application.Common.Interfaces;
using ASK.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Tami.Pago.Core;
using Tami.Pago.Core.ServiceRequests;

namespace ASK.Infrastructure.Services;

public class TamiPaymentService : ITamiPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly string _endpoint;
    private readonly long _merchantId;
    private readonly long _terminalId;
    private readonly string _secretKey;
    private readonly string _kid;
    private readonly string _kValue;

    public TamiPaymentService(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;

        var section = configuration.GetSection("Tami");
        _endpoint = section["ServiceEndpoint"] ?? "https://sandbox-paymentapi.tami.com.tr";
        _merchantId = long.Parse(section["MerchantId"] ?? "77006950");
        _terminalId = long.Parse(section["TerminalId"] ?? "84006953");
        _secretKey = section["SecretKey"] ?? "0edad05a-7ea7-40f1-a80c-d600121ca51b";
        _kid = section["FixedKidValue"] ?? "be278747-680a-4636-bd46-b190cbebca82";
        _kValue = section["FixedKValue"] ?? "uIZ4yF-pkRrhuvW1YIr_CpivQNLM5OkFHwnko51OchR6KxO6NK3_HnWZ5cCayWcadzuyeev3UoavWjIoeo6-8g";
    }

    public async Task<ThreeDAuthResultDto> Initiate3DPaymentAsync(
        int userId,
        decimal amount,
        string cardHolderName,
        string cardNumber,
        int expireMonth,
        int expireYear,
        string cvv,
        string callbackUrl,
        string clientIp,
        CancellationToken ct)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, ct);
        if (user == null)
        {
            return new ThreeDAuthResultDto(false, "USER_NOT_FOUND", "Müşteri kaydı bulunamadı.", null, null);
        }

        var orderId = $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";

        // Construct Request Objects using the official SDK classes to guarantee exact property order
        var cardObj = new RequestCardBase
        {
            Number = cardNumber.Replace(" ", ""),
            HolderName = cardHolderName,
            ExpireMonth = expireMonth,
            ExpireYear = expireYear,
            Cvv = cvv
        };

        var finalIp = string.IsNullOrWhiteSpace(clientIp) || clientIp == "::1" ? "127.0.0.1" : clientIp;

        var buyerObj = new RequestBuyerBase
        {
            BuyerId = user.Id.ToString(),
            Name = user.FirstName,
            SurName = user.LastName,
            EmailAddress = user.Email,
            IpAddress = finalIp,
            City = user.City ?? "Ankara",
            Country = "Türkiye",
            ZipCode = "06000",
            PhoneNumber = user.Phone ?? "05555555555"
        };

        var addressObj = new RequestAddressBase
        {
            ContactName = $"{user.FirstName} {user.LastName}",
            Address = user.Address ?? "Ankara dükkanı",
            City = user.City ?? "Ankara",
            Country = "Türkiye",
            ZipCode = "06000",
            PhoneNumber = user.Phone ?? "05555555555"
        };

        var basketObj = new RequestBasketBase
        {
            BasketId = Guid.NewGuid().ToString("N"),
            BasketItems = new List<RequestBasketItemBase>
            {
                new RequestBasketItemBase
                {
                    ItemId = "bakiye-odeme",
                    Name = "Cari Hesap Bakiye Ödemesi",
                    Category = "Cari Ödeme",
                    SubCategory = "Cari Bakiye",
                    ItemType = "VIRTUAL",
                    NumberOfProducts = 1,
                    UnitPrice = amount,
                    TotalPrice = amount
                }
            }
        };

        var requestBody = new ThreeDAuthRequest
        {
            OrderId = orderId,
            Amount = (int)amount,
            Currency = CurrencyTypes.TRY,
            InstallmentCount = 1,
            MotoInd = false,
            PaymentGroup = PaymentGroups.PRODUCT,
            PaymentChannel = PaymentChannels.WEB,
            Card = cardObj,
            BillingAddress = addressObj,
            ShippingAddress = addressObj,
            Buyer = buyerObj,
            Basket = basketObj,
            CallbackUrl = callbackUrl
        };

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(_endpoint) };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Accept-Language", "tr");
            client.DefaultRequestHeaders.Add("PG-Api-Version", "v3");
            client.DefaultRequestHeaders.Add("PG-Auth-Token", $"{_merchantId}:{_terminalId}:{GetPagoHash(_merchantId.ToString() + _terminalId.ToString() + _secretKey)}");
            client.DefaultRequestHeaders.Add("correlationId", Guid.NewGuid().ToString("N"));

            // Use Newtonsoft.Json with CamelCase for exact matching of signature layout
            var jsonSerializerSettings = new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };

            // 1. Serialize WITHOUT securityHash (it is null initially)
            string requestObject = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody, jsonSerializerSettings);

            // 2. Sign
            var signature = GenerateJWKSignature(_kid, _kValue, requestObject);

            // 3. Populate securityHash
            requestBody.SecurityHash = signature;

            // 4. Serialize WITH securityHash included
            var finalJson = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody, jsonSerializerSettings);
            var content = new StringContent(finalJson, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("payment/auth", content, ct);
            var responseString = await response.Content.ReadAsStringAsync(ct);
            Console.WriteLine($"[TAMI RESPONSE]: {responseString}");

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            var successVal = GetSuccessValue(root);
            var isSuccess = successVal == "true";

            if (isSuccess)
            {
                var htmlContent = root.TryGetProperty("threeDSHtmlContent", out var htmlProp) ? GetSafeString(htmlProp) : "";
                return new ThreeDAuthResultDto(true, null, null, htmlContent, orderId);
            }
            else
            {
                var errorCode = root.TryGetProperty("errorCode", out var code) ? GetSafeString(code) : "UNKNOWN";
                var errorMessage = root.TryGetProperty("errorMessage", out var msg) ? GetSafeString(msg) : "Gateway hatası";
                return new ThreeDAuthResultDto(false, errorCode, errorMessage, null, orderId);
            }
        }
        catch (Exception ex)
        {
            return new ThreeDAuthResultDto(false, "EXCEPTION", ex.Message, null, orderId);
        }
    }

    public async Task<Complete3DPaymentResultDto> Complete3DPaymentAsync(string orderId, CancellationToken ct)
    {
        var requestBody = new Complete3dAuthRequest
        {
            OrderId = orderId
        };

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(_endpoint) };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Accept-Language", "tr");
            client.DefaultRequestHeaders.Add("PG-Api-Version", "v3");
            client.DefaultRequestHeaders.Add("PG-Auth-Token", $"{_merchantId}:{_terminalId}:{GetPagoHash(_merchantId.ToString() + _terminalId.ToString() + _secretKey)}");
            client.DefaultRequestHeaders.Add("correlationId", Guid.NewGuid().ToString("N"));

            var jsonSerializerSettings = new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };

            // 1. Serialize WITHOUT securityHash
            string requestObject = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody, jsonSerializerSettings);

            // 2. Sign
            var signature = GenerateJWKSignature(_kid, _kValue, requestObject);

            // 3. Populate
            requestBody.SecurityHash = signature;

            // 4. Serialize again
            var finalJson = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody, jsonSerializerSettings);
            var content = new StringContent(finalJson, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("payment/complete-3ds", content, ct);
            var responseString = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            var successVal = GetSuccessValue(root);
            var isSuccess = successVal == "true";

            if (isSuccess)
            {
                var amountVal = GetAmountValue(root);
                return new Complete3DPaymentResultDto(true, null, null, orderId, amountVal);
            }
            else
            {
                var errorCode = root.TryGetProperty("errorCode", out var code) ? GetSafeString(code) : "UNKNOWN";
                var errorMessage = root.TryGetProperty("errorMessage", out var msg) ? GetSafeString(msg) : "3D tamamlama hatası";
                return new Complete3DPaymentResultDto(false, errorCode, errorMessage, orderId, 0);
            }
        }
        catch (Exception ex)
        {
            return new Complete3DPaymentResultDto(false, "EXCEPTION", ex.Message, orderId, 0);
        }
    }

    private static string GetSafeString(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
            return element.GetString() ?? "";
        if (element.ValueKind == JsonValueKind.Number)
            return element.GetRawText();
        if (element.ValueKind == JsonValueKind.True)
            return "true";
        if (element.ValueKind == JsonValueKind.False)
            return "false";
        if (element.ValueKind == JsonValueKind.Null)
            return "";
        return element.ToString();
    }

    private static string GetSuccessValue(JsonElement root)
    {
        if (root.TryGetProperty("success", out var successProp))
        {
            return GetSafeString(successProp);
        }
        return "";
    }

    private static int GetAmountValue(JsonElement root)
    {
        if (root.TryGetProperty("amount", out var amountProp))
        {
            if (amountProp.ValueKind == JsonValueKind.Number) return amountProp.GetInt32();
            if (amountProp.ValueKind == JsonValueKind.String)
            {
                if (int.TryParse(amountProp.GetString(), out var val)) return val;
            }
        }
        return 0;
    }

    private static string GetPagoHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash);
    }

    private static string GenerateJWKSignature(string kid, string kBase64Url, string bodyJson)
    {
        var headerObj = new { alg = "HS512", typ = "JWT", kid };
        var headerB64 = Base64UrlEncoder.Encode(System.Text.Json.JsonSerializer.Serialize(headerObj));
        var payloadB64 = Base64UrlEncoder.Encode(bodyJson);

        var signingInput = $"{headerB64}.{payloadB64}";
        var key = Base64UrlEncoder.DecodeBytes(kBase64Url);
        using var hmac = new HMACSHA512(key);
        var sig = hmac.ComputeHash(Encoding.UTF8.GetBytes(signingInput));
        var signatureB64 = Base64UrlEncoder.Encode(sig);

        return $"{headerB64}.{payloadB64}.{signatureB64}";
    }
}
