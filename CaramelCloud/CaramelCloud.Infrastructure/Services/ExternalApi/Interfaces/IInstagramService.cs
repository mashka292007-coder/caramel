using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces
{
    public interface IInstagramService
    {
        Task<bool> PublishPostAsync(string imageUrl, string caption);
    }
}
