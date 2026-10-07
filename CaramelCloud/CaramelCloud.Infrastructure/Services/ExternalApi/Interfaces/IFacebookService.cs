using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces
{
    public interface IFacebookService
    {
        Task<(bool Success, string ErrorMessage)> PublishPostAsync(string message, string? imageUrl = null);
    }
}
