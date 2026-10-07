using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces
{
    public interface IImageService
    {
        Task<string> UploadImageAsync(IFormFile file, string folderName);
        Task<bool> DeleteImageAsync(string imageUrl);
    }
}
