using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;
using Microsoft.Extensions.Options;
using FoodDeliveryyy.Models.DTOs;

namespace FoodDeliveryyy.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_configuration["EmailSettings:SenderEmail"]));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart("html") { Text = body };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _configuration["EmailSettings:SmtpServer"],
                int.Parse(_configuration["EmailSettings:Port"]),
                SecureSocketOptions.StartTls
            );
            await smtp.AuthenticateAsync(
                _configuration["EmailSettings:SenderEmail"],
                _configuration["EmailSettings:SenderPassword"]
            );

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }

    public async Task SendOrderStatusUpdateEmailAsync(string to, string customerName, int orderId, string oldStatus, string newStatus)
    {
        var subject = $"Order #{orderId} Status Update - {newStatus}";

        var body = $@"
        <html>
        <body style='font-family: Arial;'>
            <h2>Hello {customerName},</h2>
            <p>Your order <strong>#{orderId}</strong> status has been updated:</p>
            
            <div style='background-color: #f0f0f0; padding: 15px; border-radius: 5px;'>
                <p><strong>Old Status:</strong> {oldStatus}</p>
                <p><strong>New Status:</strong> {newStatus}</p>
                <p><strong>Time:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
            </div>
            
            <br/>
            <p>Track your order in real-time:</p>
            <a href='http://localhost:5173/orders/{orderId}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                Track Order
            </a>
            
            <br/><br/>
            <p>Thank you for choosing FoodDeliveryyy!</p>
            <p>Best regards,<br/>FoodDeliveryyy Team</p>
        </body>
        </html>";

        await SendEmailAsync(to, subject, body);
    }

    // 🔥 METODA PËR MERCHANT (kur restoranti aprovohet)
    public async Task SendMerchantCredentialsEmailAsync(string to, string restaurantName, string username, string password)
    {
        var subject = $"🎉 Welcome to BlinkBite - {restaurantName} Has Been Created!";

        var body = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <style>
                body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
                .credentials {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #28a745; }}
                .credential-row {{ margin-bottom: 12px; padding: 8px; background: #f8f9fa; border-radius: 6px; }}
                .label {{ font-weight: bold; color: #495057; display: inline-block; width: 100px; }}
                .value {{ font-family: monospace; color: #2c3e50; }}
                .button {{ display: inline-block; background: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin-top: 20px; }}
                .warning {{ background: #fff3cd; border: 1px solid #ffecb5; padding: 15px; border-radius: 8px; margin-top: 20px; color: #856404; }}
                .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #6c757d; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>🎉 Welcome to BlinkBite!</h1>
                    <p>Your restaurant journey starts here</p>
                </div>
                <div class='content'>
                    <h2>Hello!</h2>
                    <p>Congratulations! Your restaurant <strong>“{restaurantName}”</strong> has been successfully created on the BlinkBite platform.</p>
                    
                    <div class='credentials'>
                        <h3 style='margin-top: 0; color: #28a745;'>🔐 Your Merchant Account Credentials</h3>
                        <div class='credential-row'>
                            <span class='label'>📧 Email:</span>
                            <span class='value'>{to}</span>
                        </div>
                        <div class='credential-row'>
                            <span class='label'>👤 Username:</span>
                            <span class='value'>{username}</span>
                        </div>
                        <div class='credential-row'>
                            <span class='label'>🔑 Password:</span>
                            <span class='value'>{password}</span>
                        </div>
                    </div>
                    
                    <div class='warning'>
                        <strong>⚠️ Important:</strong>
                        <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                            <li>Save these credentials in a safe place</li>
                            <li>Change your password after first login</li>
                            <li>These credentials will not be shown again</li>
                        </ul>
                    </div>
                    
                    <div style='text-align: center;'>
                        <a href='http://localhost:5173' class='button'>🚀 Go to BlinkBite Dashboard</a>
                    </div>
                    
                    <p style='margin-top: 25px;'><strong>What can you do next?</strong></p>
                    <ul>
                        <li>📝 Complete your restaurant profile</li>
                        <li>🍔 Add your menu items</li>
                        <li>📍 Set up your branch locations</li>
                        <li>💰 Start receiving orders!</li>
                    </ul>
                </div>
                <div class='footer'>
                    <p>© 2024 BlinkBite. All rights reserved.</p>
                    <p>Need help? Contact us at support@blinkbite.com</p>
                </div>
            </div>
        </body>
        </html>";

        await SendEmailAsync(to, subject, body);
    }

    // 🔥 METODA PËR BRANCH MANAGER (kur krijohet branch i ri)
    public async Task SendBranchManagerCredentialsEmailAsync(string to, string branchAddress, string restaurantName, string username, string password)
    {
        var subject = $"🏪 Welcome to BlinkBite - You are now the Branch Manager for {restaurantName}!";

        var body = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <style>
                body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
                .credentials {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #17a2b8; }}
                .credential-row {{ margin-bottom: 12px; padding: 8px; background: #f8f9fa; border-radius: 6px; }}
                .label {{ font-weight: bold; color: #495057; display: inline-block; width: 100px; }}
                .value {{ font-family: monospace; color: #2c3e50; }}
                .button {{ display: inline-block; background: #17a2b8; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin-top: 20px; }}
                .warning {{ background: #fff3cd; border: 1px solid #ffecb5; padding: 15px; border-radius: 8px; margin-top: 20px; color: #856404; }}
                .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #6c757d; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>🏪 Welcome to BlinkBite!</h1>
                    <p>You are now a Branch Manager</p>
                </div>
                <div class='content'>
                    <h2>Hello!</h2>
                    <p>Congratulations! You have been appointed as the <strong>Branch Manager</strong> for:</p>
                    <p><strong>Restaurant:</strong> {restaurantName}</p>
                    <p><strong>Branch Address:</strong> {branchAddress}</p>
                    
                    <div class='credentials'>
                        <h3 style='margin-top: 0; color: #17a2b8;'>🔐 Your Branch Manager Account Credentials</h3>
                        <div class='credential-row'>
                            <span class='label'>📧 Email:</span>
                            <span class='value'>{to}</span>
                        </div>
                        <div class='credential-row'>
                            <span class='label'>👤 Username:</span>
                            <span class='value'>{username}</span>
                        </div>
                        <div class='credential-row'>
                            <span class='label'>🔑 Password:</span>
                            <span class='value'>{password}</span>
                        </div>
                    </div>
                    
                    <div class='warning'>
                        <strong>⚠️ Important:</strong>
                        <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                            <li>Save these credentials in a safe place</li>
                            <li>Change your password after first login</li>
                            <li>These credentials will not be shown again</li>
                        </ul>
                    </div>
                    
                    <div style='text-align: center;'>
                        <a href='http://localhost:5173' class='button'>🚀 Go to BlinkBite Dashboard</a>
                    </div>
                    
                    <p style='margin-top: 25px;'><strong>What can you do as Branch Manager?</strong></p>
                    <ul>
                        <li>📝 Manage menu items for your branch</li>
                        <li>✅ Accept and process incoming orders</li>
                        <li>📊 Track branch performance</li>
                        <li>💰 Update pricing and availability</li>
                    </ul>
                </div>
                <div class='footer'>
                    <p>© 2024 BlinkBite. All rights reserved.</p>
                    <p>Need help? Contact support at support@blinkbite.com</p>
                </div>
            </div>
        </body>
        </html>";

        await SendEmailAsync(to, subject, body);
    }

    // 🔥 METODA PËR COURIER (kur aplikimi i courier aprovohet)
    public async Task SendCourierCredentialsEmailAsync(string to, string fullName, string username, string password)
    {
        var subject = $"🚚 Welcome to BlinkBite - You are now a Delivery Courier!";

        var body = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <style>
                body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
                .credentials {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #f5576c; }}
                .credential-row {{ margin-bottom: 12px; padding: 8px; background: #f8f9fa; border-radius: 6px; }}
                .label {{ font-weight: bold; color: #495057; display: inline-block; width: 100px; }}
                .value {{ font-family: monospace; color: #2c3e50; }}
                .button {{ display: inline-block; background: #f5576c; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin-top: 20px; }}
                .warning {{ background: #fff3cd; border: 1px solid #ffecb5; padding: 15px; border-radius: 8px; margin-top: 20px; color: #856404; }}
                .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #6c757d; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>🚚 Welcome to BlinkBite!</h1>
                    <p>You are now a Delivery Courier</p>
                </div>
                <div class='content'>
                    <h2>Hello {fullName},</h2>
                    <p>Congratulations! Your courier application has been approved. You are now ready to start delivering orders.</p>
                    
                    <div class='credentials'>
                        <h3 style='margin-top: 0; color: #f5576c;'>🔐 Your Courier Account Credentials</h3>
                        <div class='credential-row'>
                            <span class='label'>📧 Email:</span>
                            <span class='value'>{to}</span>
                        </div>
                        <div class='credential-row'>
                            <span class='label'>👤 Username:</span>
                            <span class='value'>{username}</span>
                        </div>
                        <div class='credential-row'>
                            <span class='label'>🔑 Password:</span>
                            <span class='value'>{password}</span>
                        </div>
                    </div>
                    
                    <div class='warning'>
                        <strong>⚠️ Important:</strong>
                        <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                            <li>Save these credentials in a safe place</li>
                            <li>Change your password after first login</li>
                            <li>These credentials will not be shown again</li>
                        </ul>
                    </div>
                    
                    <div style='text-align: center;'>
                        <a href='http://localhost:5173' class='button'>🚀 Go to BlinkBite Dashboard</a>
                    </div>
                    
                    <p style='margin-top: 25px;'><strong>What can you do as a Courier?</strong></p>
                    <ul>
                        <li>📱 View available delivery orders</li>
                        <li>✅ Accept and complete deliveries</li>
                        <li>💰 Track your earnings</li>
                        <li>📍 Navigate to customer locations</li>
                    </ul>
                </div>
                <div class='footer'>
                    <p>© 2024 BlinkBite. All rights reserved.</p>
                    <p>Need help? Contact support at support@blinkbite.com</p>
                </div>
            </div>
        </body>
        </html>";

        await SendEmailAsync(to, subject, body);
    }
}