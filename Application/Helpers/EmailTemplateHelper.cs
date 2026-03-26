using System;
using System.Linq;
using System.Text;
using Core.Application.Models.Email;

namespace Core.Application.Helpers
{
    public static class EmailTemplateHelper
    {
        public static string GenerateBuyerOrderConfirmationEmail(BuyerOrderConfirmationEmailModel model)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; margin: 0; padding: 0; background-color: #f5f5f5; }
        .container { max-width: 600px; margin: 0 auto; background-color: #ffffff; }
        .header { padding: 30px 40px; text-align: center; }
        .logo { width: 80px; margin-bottom: 24px; }
        .content { padding: 0 40px 40px; }
        .title { font-size: 24px; font-weight: bold; margin-bottom: 20px; color: #1a1a1a; }
        .greeting { margin-bottom: 15px; color: #333; }
        .message { line-height: 1.6; color: #555; margin-bottom: 20px; }
        .button { display: inline-block; padding: 14px 32px; background-color: #6B46C1; color: #ffffff; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; }
        .button:hover { background-color: #5a3aa8; }
        .order-details { background-color: #FEF3C7; padding: 25px; margin: 25px 0; border-radius: 8px; }
        .detail-row { display: flex; justify-content: space-between; margin-bottom: 10px; }
        .detail-label { font-weight: 600; color: #333; }
        .detail-value { color: #555; }
        .divider { border-top: 1px solid #e5e5e5; margin: 20px 0; }
        .next-steps { background-color: #FEF3C7; padding: 20px; border-radius: 8px; margin: 20px 0; }
        .next-steps h3 { margin-top: 0; color: #333; }
        .next-steps ul { margin: 10px 0; padding-left: 20px; }
        .next-steps li { margin-bottom: 8px; color: #555; }
        .support { margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e5e5; }
        .footer { background-color: #f9f9f9; padding: 30px 40px; text-align: center; color: #888; font-size: 12px; }
        .footer-note { margin-bottom: 10px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <img src=""cid:pulr-logo-id@pulr.co"" alt=""PULR Logo"" class=""logo"" width=""80"" height=""27"" style=""width: 80px; height: 27px; margin-bottom: 24px; display: block; border: 0;"" />
        </div>
        
        <div class=""content"">
            <h1 class=""title"">Your order is confirmed!</h1>
            
            <p class=""greeting"">Hi " + model.RecipientName + @",</p>
            
            <p class=""message"">
                <strong>Thank you for your purchase on PULR.</strong><br><br>
                Your order, <strong>" + model.OrderNumber + @"</strong>, has been successfully placed and confirmed. 
                We'll email you once it's shipped by the seller and is on its way.
            </p>
            
            <p class=""message"">
                For delivery updates or to manage your order, head to your Order Summary.
            </p>
            
            <p class=""message"">
                Thank you,<br>
                <strong>Team PULR</strong>
            </p>
            
            <center>
                <a href=""" + model.OrderSummaryUrl + @""" class=""button"" style=""color:#ffffff !important;text-decoration:none;color:white !important;"">View Order Summary</a>
            </center>
            
            <div class=""order-details"">
                <h3 style=""margin-top: 0; color: #333;"">Order Details</h3>
                <div class=""detail-row"">
                    <span class=""detail-label"">Order Number:</span>
                    <span class=""detail-value"">" + model.OrderNumber + @"</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Order Date:</span>
                    <span class=""detail-value"">" + model.OrderDate + @"</span>
                </div>
                <div class=""divider""></div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Total (VAT included):</span>
                    <span class=""detail-value"">" + model.Currency + " " + model.TotalAmount.ToString("F2") + @"</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Estimated VAT:</span>
                    <span class=""detail-value"">" + model.Currency + " " + model.EstimatedVAT.ToString("F2") + @"</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Shipping fee:</span>
                    <span class=""detail-value"">" + model.Currency + " " + model.ShippingFee.ToString("F2") + @"</span>
                </div>
                <div class=""divider""></div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Payment Method:</span>
                    <span class=""detail-value"">" + model.PaymentMethod + @"</span>
                </div>
            </div>
            
            <div class=""order-details"">
                <h3 style=""margin-top: 0; color: #333;"">Delivery Address</h3>
                <p style=""margin: 0; color: #555;"">" + model.DeliveryAddress + @"</p>
                <p style=""margin: 5px 0 0 0; color: #555;"">Phone Number: " + model.PhoneNumber + @"</p>
            </div>
            
            <div class=""next-steps"">
                <h3>What Happens Next?</h3>
                <ul>
                    <li>The seller is preparing your order.</li>
                    <li>You'll receive tracking details once the order is shipped.</li>
                    <li>You can view order status anytime in the PULR app under <strong>Wallet → Orders</strong>.</li>
                </ul>
            </div>
            
            <div class=""support"">
                <h3 style=""color: #333;"">Support</h3>
                <p style=""color: #555;"">Need help with your order?<br>
                Contact us at <a href=""mailto:support@pulr.co"" style=""color: #6B46C1;"">support@pulr.co</a></p>
            </div>
            
            <p style=""margin-top: 30px; color: #555;"">
                Thanks for shopping on <strong>PULR 💜</strong><br>
                <em>Discover, tag, and shop — all in one place.</em>
            </p>
        </div>
        
        <div class=""footer"">
            <p class=""footer-note"">To ensure you receive these emails in your inbox, please add support@pulr.co to your address book.</p>
            <p>© " + DateTime.UtcNow.Year + @" PULR. All rights reserved.</p>
        </div>
    </div>
</body>
</html>");

            return sb.ToString();
        }

        public static string GenerateSellerOrderNotificationEmail(SellerOrderNotificationEmailModel model)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; margin: 0; padding: 0; background-color: #f5f5f5; }
        .container { max-width: 600px; margin: 0 auto; background-color: #ffffff; }
        .header { padding: 30px 40px; text-align: center; }
        .logo { width: 80px; margin-bottom: 24px; }
        .content { padding: 0 40px 40px; }
        .title { font-size: 24px; font-weight: bold; margin-bottom: 20px; color: #1a1a1a; }
        .greeting { margin-bottom: 15px; color: #333; }
        .message { line-height: 1.6; color: #555; margin-bottom: 20px; }
        .button { display: inline-block; padding: 14px 32px; background-color: #6B46C1; color: #ffffff; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; }
        .button:hover { background-color: #5a3aa8; }
        .order-details { background-color: #FEF3C7; padding: 25px; margin: 25px 0; border-radius: 8px; }
        .detail-row { display: flex; justify-content: space-between; margin-bottom: 10px; }
        .detail-label { font-weight: 600; color: #333; }
        .detail-value { color: #555; }
        .divider { border-top: 1px solid #e5e5e5; margin: 20px 0; }
        .next-steps { background-color: #FEF3C7; padding: 20px; border-radius: 8px; margin: 20px 0; }
        .next-steps h3 { margin-top: 0; color: #333; }
        .next-steps ul { margin: 10px 0; padding-left: 20px; }
        .next-steps li { margin-bottom: 8px; color: #555; }
        .support { margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e5e5; }
        .footer { background-color: #f9f9f9; padding: 30px 40px; text-align: center; color: #888; font-size: 12px; }
        .footer-note { margin-bottom: 10px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <img src=""cid:pulr-logo-id@pulr.co"" alt=""PULR Logo"" class=""logo"" width=""80"" height=""27"" style=""width: 80px; height: 27px; margin-bottom: 24px; display: block; border: 0;"" />
        </div>
        
        <div class=""content"">
            <h1 class=""title"">You have a new order!</h1>
            
            <p class=""greeting"">Hi " + model.SellerName + @",</p>
            
            <p class=""message"">
                A new order, <strong>" + model.OrderNumber + @"</strong>, has been placed by a buyer on PULR. 
                You are responsible for shipping this order within the available delivery time.
            </p>
            
            <p class=""message"">
                You can view the order details and manage this order on your <a href=""" + model.OrdersAreaUrl + @""" style=""color: #6B46C1;"">Order Details page</a> under Wallet.
            </p>
            
            <p class=""message"">
                Thank you,<br>
                <strong>Team PULR</strong>
            </p>
            
            <center>
                <a href=""" + model.OrdersAreaUrl + @""" class=""button"" style=""color:#ffffff !important;text-decoration:none;"">View Order Details</a>
            </center>
            
            <div class=""order-details"">
                <h3 style=""margin-top: 0; color: #333;"">Order Details</h3>
                <div class=""detail-row"">
                    <span class=""detail-label"">Order Number:</span>
                    <span class=""detail-value"">" + model.OrderNumber + @"</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Order Date:</span>
                    <span class=""detail-value"">" + model.OrderDate + @"</span>
                </div>
                <div class=""divider""></div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Total (VAT included):</span>
                    <span class=""detail-value"">" + model.Currency + " " + model.TotalAmount.ToString("F2") + @"</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Estimated VAT:</span>
                    <span class=""detail-value"">" + model.Currency + " " + model.EstimatedVAT.ToString("F2") + @"</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Shipping fee:</span>
                    <span class=""detail-value"">" + model.Currency + " " + model.ShippingFee.ToString("F2") + @"</span>
                </div>
                <div class=""divider""></div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Payment Method:</span>
                    <span class=""detail-value"">" + model.PaymentMethod + @"</span>
                </div>
            </div>
            
            <div class=""order-details"">
                <h3 style=""margin-top: 0; color: #333;"">Delivery Address</h3>
                <p style=""margin: 0; color: #555;"">" + model.DeliveryAddress + @"</p>
                <p style=""margin: 5px 0 0 0; color: #555;"">Phone Number: " + model.PhoneNumber + @"</p>
            </div>
            
            <div class=""next-steps"">
                <h3>What Happens Next?</h3>
                <ul>
                    <li>Please prepare and ship the order promptly.</li>
                    <li>Update the tracking details once the order is shipped.</li>
                    <li>You can view the order status in the PULR app under <strong>Wallet → Orders</strong>.</li>
                </ul>
            </div>
            
            <div class=""support"">
                <h3 style=""color: #333;"">Support</h3>
                <p style=""color: #555;"">Need help managing this order?<br>
                Contact us at <a href=""mailto:support@pulr.co"" style=""color: #6B46C1;"">support@pulr.co</a></p>
            </div>
            
            <p style=""margin-top: 30px; color: #555;"">
                Thank you for fulfilling orders on <strong>PULR 💜</strong><br>
                <em>Discover, tag, and shop — all in one place.</em>
            </p>
        </div>
        
        <div class=""footer"">
            <p class=""footer-note"">To ensure you receive these emails in your inbox, please add support@pulr.co to your address book.</p>
            <p>© " + DateTime.UtcNow.Year + @" PULR. All rights reserved.</p>
        </div>
    </div>
</body>
</html>");

            return sb.ToString();
        }

        public static string GenerateBuyerOrderShippedEmail(BuyerOrderShippedEmailModel model)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; margin: 0; padding: 0; background-color: #f5f5f5; }
        .container { max-width: 600px; margin: 0 auto; background-color: #ffffff; }
        .header { padding: 30px 40px; text-align: center; }
        .logo { width: 80px; margin-bottom: 24px; }
        .content { padding: 0 40px 40px; }
        .title { font-size: 24px; font-weight: bold; margin-bottom: 20px; color: #1a1a1a; }
        .greeting { margin-bottom: 15px; color: #333; }
        .message { line-height: 1.6; color: #555; margin-bottom: 20px; }
        .button { display: inline-block; padding: 14px 32px; background-color: #6B46C1; color: #ffffff; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; }
        .button:hover { background-color: #5a3aa8; }
        .order-details { background-color: #FEF3C7; padding: 25px; margin: 25px 0; border-radius: 8px; }
        .detail-row { display: flex; justify-content: space-between; margin-bottom: 10px; }
        .detail-label { font-weight: 600; color: #333; }
        .detail-value { color: #555; }
        .divider { border-top: 1px solid #e5e5e5; margin: 20px 0; }
        .next-steps { background-color: #FEF3C7; padding: 20px; border-radius: 8px; margin: 20px 0; }
        .next-steps h3 { margin-top: 0; color: #333; }
        .next-steps ul { margin: 10px 0; padding-left: 20px; }
        .next-steps li { margin-bottom: 8px; color: #555; }
        .support { margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e5e5; }
        .footer { background-color: #f9f9f9; padding: 30px 40px; text-align: center; color: #888; font-size: 12px; }
        .footer-note { margin-bottom: 10px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <img src=""cid:pulr-logo-id@pulr.co"" alt=""PULR Logo"" class=""logo"" width=""80"" height=""27"" style=""width: 80px; height: 27px; margin-bottom: 24px; display: block; border: 0;"" />
        </div>
        
        <div class=""content"">
            <h1 class=""title"">Your order has shipped!</h1>
            
            <p class=""greeting"">Hi " + model.RecipientName + @",</p>
            
            <p class=""message"">
                <strong>Thank you for your purchase on PULR.</strong><br><br>
                Good news — your order <strong>" + model.OrderNumber + @"</strong> has just been shipped by the seller and is now on its way to you.
            </p>
            
            <p class=""message"">
                You can track its progress using the details below, or visit your Order Summary for updates or delivery information.
            </p>
            
            <p class=""message"">
                Thank you,<br>
                <strong>Team PULR</strong>
            </p>
            
            <center>
                <a href=""" + model.OrderSummaryUrl + @""" class=""button"" style=""color:#ffffff !important;text-decoration:none;color:white !important;"">View Order Summary</a>
            </center>
            
            <div class=""order-details"">
                <h3 style=""margin-top: 0; color: #333;"">Order Details</h3>
                <div class=""detail-row"">
                    <span class=""detail-label"">Order Number:</span>
                    <span class=""detail-value"">" + model.OrderNumber + @"</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Order Date:</span>
                    <span class=""detail-value"">" + model.OrderDate + @"</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Shipped On:</span>
                    <span class=""detail-value"">" + model.ShippedOn + @"</span>
                </div>
            </div>
            
            <div class=""order-details"">
                <h3 style=""margin-top: 0; color: #333;"">Tracking Information</h3>
                <div class=""detail-row"">
                    <span class=""detail-label"">Tracking Number:</span>
                    <span class=""detail-value"">" + model.TrackingNumber + @"</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Delivery Service:</span>
                    <span class=""detail-value"">" + model.DeliveryService + @"</span>
                </div>
            </div>
            ");

            // Add shipped products section if there are any products
            if (model.Products != null && model.Products.Any())
            {
                sb.AppendLine(@"
            <div class=""order-details"">
                <h3 style=""margin-top: 0; color: #333;"">Shipped Items</h3>");
                
                foreach (var product in model.Products)
                {
                    sb.AppendLine($@"
                <div style=""display: flex; margin-bottom: 15px; padding-bottom: 15px; border-bottom: 1px solid #e5e5e5;"">
                    <div style=""flex: 1;"">
                        <p style=""margin: 0; font-weight: 600; color: #333;"">{product.ProductName}</p>
                        {(!string.IsNullOrWhiteSpace(product.Brand) ? $"<p style=\"margin: 5px 0 0 0; color: #888; font-size: 14px;\">{product.Brand}</p>" : "")}
                        <p style=""margin: 5px 0 0 0; color: #555;"">Quantity: {product.Quantity}</p>
                    </div>
                </div>");
                }
                
                sb.AppendLine(@"
            </div>");
            }

            sb.AppendLine(@"
            
            <div class=""order-details"">
                <h3 style=""margin-top: 0; color: #333;"">Delivery Address</h3>
                <p style=""margin: 0; color: #555;"">" + model.DeliveryAddress + @"</p>
                <p style=""margin: 5px 0 0 0; color: #555;"">Phone Number: " + model.PhoneNumber + @"</p>
            </div>
            
            <div class=""next-steps"">
                <h3>What Happens Next?</h3>
                <ul>
                    <li>Your order is currently in transit.</li>
                    <li>The delivery service provider will handle final delivery to your address.</li>
                    <li>Order can be tracked via delivery service provider's website.</li>
                </ul>
            </div>
            
            <div class=""support"">
                <h3 style=""color: #333;"">Support</h3>
                <p style=""color: #555;"">Need help with your order?<br>
                Contact us at <a href=""mailto:support@pulr.co"" style=""color: #6B46C1;"">support@pulr.co</a></p>
            </div>
            
            <p style=""margin-top: 30px; color: #555;"">
                Thanks for shopping on <strong>PULR 💜</strong><br>
                <em>Discover, tag, and shop — all in one place.</em>
            </p>
        </div>
        
        <div class=""footer"">
            <p class=""footer-note"">To ensure you receive these emails in your inbox, please add support@pulr.co to your address book.</p>
            <p>© " + DateTime.UtcNow.Year + @" PULR. All rights reserved.</p>
        </div>
    </div>
</body>
</html>");

            return sb.ToString();
        }
    }
}
