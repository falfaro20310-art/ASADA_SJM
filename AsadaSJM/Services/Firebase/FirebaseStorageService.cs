using Google.Cloud.Storage.V1;

namespace AsadaSJM.Services.Firebase
{
    public class FirebaseStorageService : IFirebaseStorageService
    {
        private readonly StorageClient _storageClient;
        private const string BucketName = "asada-sjm.firebasestorage.app";

        public FirebaseStorageService()
        {
            _storageClient = StorageClient.Create();
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("El archivo no es válido.");

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"images/{Guid.NewGuid()}{extension}";

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            memoryStream.Position = 0;

            var dataObject = await _storageClient.UploadObjectAsync(
                bucket: BucketName,
                objectName: fileName,
                contentType: file.ContentType,
                source: memoryStream
            );

            return $"https://storage.googleapis.com/{BucketName}/{fileName}";
        }
    }
}
