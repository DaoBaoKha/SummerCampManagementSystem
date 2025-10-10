using Microsoft.EntityFrameworkCore;
using Net.payOS;
using Net.payOS.Types;
using SummerCampManagementSystem.BLL.DTOs.PayOS;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly PayOS _payOS;

        public PaymentService(IUnitOfWork unitOfWork, PayOS payOS)
        {
            _unitOfWork = unitOfWork;
            _payOS = payOS;
        }

        public async Task HandlePayOSWebhook(PayOSWebhookRequestDto webhookRequest)
        {
            try
            {
                // create a WebhookData object from the incoming request data
                var webhookDataForSdk = new WebhookData(
                    webhookRequest.data.orderCode,
                    webhookRequest.data.amount,
                    webhookRequest.data.description,
                    webhookRequest.data.accountNumber,
                    webhookRequest.data.reference,
                    webhookRequest.data.transactionDateTime,
                    webhookRequest.data.currency,
                    webhookRequest.data.paymentLinkId,
                    webhookRequest.data.code,
                    webhookRequest.data.desc,
                    webhookRequest.data.counterAccountBankId,
                    webhookRequest.data.counterAccountBankName,
                    webhookRequest.data.counterAccountName,
                    webhookRequest.data.counterAccountNumber,
                    webhookRequest.data.virtualAccountName,
                    webhookRequest.data.virtualAccountNumber
                );

                
                var webhookToVerify = new WebhookType(
                    code: webhookRequest.code,  //string code
                    desc: webhookRequest.desc,
                    data: webhookDataForSdk,
                    success: webhookRequest.success,
                    signature: webhookRequest.signature
                );

                // verify the webhook using PayOS SDK
                WebhookData verifiedData = _payOS.verifyPaymentWebhookData(webhookToVerify);

                // respond to the webhook based on the verification result
                if (verifiedData.code == "00")
                {
                    long paymentId = verifiedData.orderCode;
                    var payment = await _unitOfWork.Payments.GetByIdAsync((int)paymentId);

                    if (payment != null && payment.status == "Pending")
                    {
                        var registration = await _unitOfWork.Registrations.GetQueryable()
                            .FirstOrDefaultAsync(r => r.paymentId == paymentId);

                        if (registration != null)
                        {
                            payment.status = "Completed";
                            registration.status = "Confirmed";
                            await _unitOfWork.CommitAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Webhook processing failed: {ex.Message}");
                throw;
            }
        }
    }
}
