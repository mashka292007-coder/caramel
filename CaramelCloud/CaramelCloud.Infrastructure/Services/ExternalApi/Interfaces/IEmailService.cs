using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink);
    }
}
