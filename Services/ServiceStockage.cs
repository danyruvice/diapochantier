using System.Security.Cryptography;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;

namespace DiaspoChantier.Services
{
    public class ServiceStockage
    {
        private readonly IWebHostEnvironment _environment;
        private readonly BlobContainerClient? _containerClient;

        public ServiceStockage(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;

            var connectionString = configuration["AzureBlob:ConnectionString"];
            var containerName = configuration["AzureBlob:ContainerName"];

            if (!string.IsNullOrWhiteSpace(connectionString) && !string.IsNullOrWhiteSpace(containerName))
            {
                var serviceClient = new BlobServiceClient(connectionString);
                _containerClient = serviceClient.GetBlobContainerClient(containerName);
                _containerClient.CreateIfNotExists();
            }
        }

        public async Task<ResultatTeleversement> EnregistrerAsync(IFormFile? fichier, string dossier)
        {
            if (fichier is null || fichier.Length == 0)
            {
                throw new InvalidOperationException("Le fichier est vide ou absent.");
            }

            using var flux = fichier.OpenReadStream();
            using var memoire = new MemoryStream();
            await flux.CopyToAsync(memoire);
            var octets = memoire.ToArray();
            var hash = CalculerHashSHA256(octets);
            var nomFichier = $"{Guid.NewGuid():N}_{Path.GetFileName(fichier.FileName)}";
            var cheminStockage = $"{dossier.Trim('/')}/{nomFichier}";

            if (_containerClient is not null)
            {
                var blobClient = _containerClient.GetBlobClient(cheminStockage);
                await using var uploadStream = new MemoryStream(octets);
                await blobClient.UploadAsync(uploadStream, overwrite: true);

                return new ResultatTeleversement
                {
                    Url = blobClient.Uri.ToString(),
                    HashSHA256 = hash
                };
            }

            var dossierCible = Path.Combine(_environment.WebRootPath, "uploads", dossier);
            Directory.CreateDirectory(dossierCible);
            var chemin = Path.Combine(dossierCible, nomFichier);

            await using (var fichierSortie = new FileStream(chemin, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await fichierSortie.WriteAsync(octets);
            }

            return new ResultatTeleversement
            {
                Url = $"/uploads/{dossier}/{nomFichier}".Replace("\\", "/"),
                HashSHA256 = hash
            };
        }

        private static string CalculerHashSHA256(byte[] octets)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(octets);
            return Convert.ToHexString(hash);
        }
    }
}