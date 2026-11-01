using System.Text;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Options;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;
using ClinicHub.Services.Routes.Api;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ClinicHub.Services.Services.Implementations
{
    public class AdminPayoutService : IAdminPayoutService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;
        private readonly IDeserializerService _deserializerService;

        public AdminPayoutService(HttpClient httpClient, IOptions<Doctory> doctoryOptions, IDeserializerService deserializerService)
        {
            _httpClient = httpClient;
            _deserializerService = deserializerService;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);
        }

        public async Task<PayoutPreviewDto> GetPreviewAsync(decimal? minimumAmount = null)
        {
            var url = minimumAmount.HasValue
                ? $"{DoctoryRoutes.AdminPayouts.Preview}?minimumAmount={minimumAmount.Value}"
                : DoctoryRoutes.AdminPayouts.Preview;

            var response = await _httpClient.GetAsync(url);
            return await _deserializerService.DeserializeApiResponse<PayoutPreviewDto>(
                response, "عذراً، حدث خطأ أثناء تحميل مستحقات العيادات.");
        }

        public async Task<List<PayoutAccountAdminDto>> GetAccountsAsync(int? status = null, string? search = null)
        {
            var query = new List<string>();
            if (status.HasValue) query.Add($"status={status.Value}");
            if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search.Trim())}");

            var url = query.Count == 0
                ? DoctoryRoutes.AdminPayouts.Accounts
                : $"{DoctoryRoutes.AdminPayouts.Accounts}?{string.Join("&", query)}";

            var response = await _httpClient.GetAsync(url);
            return await _deserializerService.DeserializeApiResponse<List<PayoutAccountAdminDto>>(
                response, "عذراً، حدث خطأ أثناء تحميل حسابات العيادات البنكية.");
        }

        public async Task<PayoutAccountAdminDto> VerifyAccountAsync(Guid id)
        {
            var response = await _httpClient.PostAsync(
                DoctoryRoutes.AdminPayouts.Verify(id), null);

            return await _deserializerService.DeserializeApiResponse<PayoutAccountAdminDto>(
                response, "تعذر اعتماد الحساب البنكي.");
        }

        public async Task<PayoutAccountAdminDto> RejectAccountAsync(Guid id, string reason)
        {
            var content = new StringContent(
                JsonConvert.SerializeObject(new RejectPayoutAccountRequest { Id = id, Reason = reason }, _jsonSettings),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(DoctoryRoutes.AdminPayouts.Reject(id), content);
            return await _deserializerService.DeserializeApiResponse<PayoutAccountAdminDto>(
                response, "تعذر رفض الحساب البنكي.");
        }

        public async Task<PayoutAccountAdminDto> UpsertClinicAccountAsync(AdminUpsertPayoutAccountRequest request)
        {
            var payload = new
            {
                holderName = request.HolderName,
                nationalId = request.NationalId,
                bankCode = request.BankCode,
                accountNumber = request.AccountNumber,
                bankTransactionType = request.BankTransactionType
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(payload, _jsonSettings),
                Encoding.UTF8,
                "application/json");

            var message = new HttpRequestMessage(HttpMethod.Put, DoctoryRoutes.AdminPayouts.UpsertClinicAccount(request.ClinicId))
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(message);
            return await _deserializerService.DeserializeApiResponse<PayoutAccountAdminDto>(
                response, "تعذر حفظ بيانات الحساب البنكي للعيادة.");
        }

        public async Task<ClinicLedgerCreditDto> CreditClinicAsync(CreditClinicLedgerRequest request)
        {
            var content = new StringContent(
                JsonConvert.SerializeObject(request, _jsonSettings),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(DoctoryRoutes.AdminPayouts.CreditClinic, content);
            return await _deserializerService.DeserializeApiResponse<ClinicLedgerCreditDto>(
                response, "تعذر إضافة المستحقات للعيادة.");
        }

        public async Task<PayoutBudgetDto> GetBudgetAsync()
        {
            var response = await _httpClient.GetAsync(DoctoryRoutes.AdminPayouts.Budget);
            return await _deserializerService.DeserializeApiResponse<PayoutBudgetDto>(
                response, "عذراً، حدث خطأ أثناء رصيد التحويلات.");
        }

        public async Task<PagginatedResult<PayoutBatchDto>> GetBatchesAsync(int pageNumber = 1, int pageSize = 20, int? status = null)
        {
            var query = new List<string> { $"pageNumber={pageNumber}", $"pageSize={pageSize}" };
            if (status.HasValue) query.Add($"status={status.Value}");

            var response = await _httpClient.GetAsync($"{DoctoryRoutes.AdminPayouts.Batches}?{string.Join("&", query)}");
            return await _deserializerService.DeserializeApiResponse<PagginatedResult<PayoutBatchDto>>(
                response, "عذراً، حدث خطأ أثناء تحميل دفعات التحويل.");
        }

        public async Task<PayoutBatchDto> GetBatchAsync(Guid id)
        {
            var response = await _httpClient.GetAsync(DoctoryRoutes.AdminPayouts.Batch(id));
            return await _deserializerService.DeserializeApiResponse<PayoutBatchDto>(
                response, "عذراً، حدث خطأ أثناء تحميل تفاصيل الدفعة.");
        }

        public async Task<PayoutBatchDto> CreateBatchAsync(CreatePayoutBatchRequest request)
        {
            var content = new StringContent(
                JsonConvert.SerializeObject(request, _jsonSettings),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(DoctoryRoutes.AdminPayouts.Batches, content);
            return await _deserializerService.DeserializeApiResponse<PayoutBatchDto>(
                response, "تعذر إنشاء دفعة تحويل.");
        }

        public async Task<PayoutBatchDto> ApproveBatchAsync(Guid id) =>
            await PostBatchAsync(DoctoryRoutes.AdminPayouts.Approve(id), "تعذر اعتماد الدفعة.");

        public async Task<PayoutBatchDto> ExecuteBatchAsync(Guid id) =>
            await PostBatchAsync(DoctoryRoutes.AdminPayouts.Execute(id), "تعذر تنفيذ الدفعة.");

        public async Task<PayoutBatchDto> CancelBatchAsync(Guid id) =>
            await PostBatchAsync(DoctoryRoutes.AdminPayouts.Cancel(id), "تعذر إلغاء الدفعة.");

        public async Task<PayoutBatchDto> ReconcileBatchAsync(Guid id) =>
            await PostBatchAsync(DoctoryRoutes.AdminPayouts.Reconcile(id), "تعذر تحديث حالة الدفعة من بايمن.");

        public async Task<PayoutBatchDto> RetryItemAsync(Guid itemId)
        {
            var response = await _httpClient.PostAsync(DoctoryRoutes.AdminPayouts.RetryItem(itemId), null);
            return await _deserializerService.DeserializeApiResponse<PayoutBatchDto>(
                response, "تعذر إعادة محاولة التحويل.");
        }

        private async Task<PayoutBatchDto> PostBatchAsync(string url, string failureMessage)
        {
            var response = await _httpClient.PostAsync(url, null);
            return await _deserializerService.DeserializeApiResponse<PayoutBatchDto>(
                response, failureMessage);
        }
    }
}
