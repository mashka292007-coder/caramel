using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces
{
    public interface IThreadsService
    {
        Task<(bool Success, string ErrorMessage)> PublishPostAsync(string text, string? imageUrl = null);
    }
}
