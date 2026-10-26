using Google.Cloud.Storage.V1;

namespace AsadaSJM.Services.Firebase
{
    public interface IFirebaseStorageService
    {
        Task<string> UploadImageAsync(IFormFile file);
    }
}
