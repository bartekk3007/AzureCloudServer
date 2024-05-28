using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace WCFServiceWebRole1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class Service1 : IService1
    {
        private static CloudTable GetTableFromAzure(string nameOfTable)
        {
            var konto = CloudStorageAccount.DevelopmentStorageAccount;
            CloudTableClient klient = konto.CreateCloudTableClient();
            var table = klient.GetTableReference(nameOfTable);
            table.CreateIfNotExists();
            return table;
        }

        private static CloudBlobContainer GetBlobFromAzure(string nameOfBlobContainer)
        {
            var konto = CloudStorageAccount.DevelopmentStorageAccount;
            CloudBlobClient klient = konto.CreateCloudBlobClient();
            CloudBlobContainer container = klient.GetContainerReference(nameOfBlobContainer);
            container.CreateIfNotExists();
            return container;
        }

        public bool Create(string login, string haslo)
        {
            CloudTable table = GetTableFromAzure("users");

            var checkIfExistsOperation = TableOperation.Retrieve<Uzytkownik>(login, haslo);
            var validationResult = table.Execute(checkIfExistsOperation);

            if (validationResult.Result != null)
            {
                return false;
            }

            var uzytkownik = new Uzytkownik(login, haslo)
            {
                Login = login,
                Haslo = haslo,
                IDSesji = Guid.Empty
            };

            var operacja = TableOperation.Insert(uzytkownik);
            var wynik = table.Execute(operacja);

            if (wynik.Result == null)
            {
                return false;
            }

            return true;
        }

        public Guid Login(string login, string haslo)
        {
            CloudTable table = GetTableFromAzure("users");

            var checkIfExistsOperation = TableOperation.Retrieve<Uzytkownik>(login, haslo);
            var wynik = table.Execute(checkIfExistsOperation);
            var uzytkownik = wynik.Result as Uzytkownik;

            if (uzytkownik == null)
            {
                return Guid.Empty;
            }

            var id_sesji = Guid.NewGuid();
            uzytkownik.IDSesji = id_sesji;

            var updateOperation = TableOperation.Replace(uzytkownik);
            table.Execute(updateOperation);

            return id_sesji;
        }

        public bool Logout(string login)
        {
            CloudTable table = GetTableFromAzure("users");

            TableQuery<Uzytkownik> query = new TableQuery<Uzytkownik>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, login));
            var wynik = table.ExecuteQuery(query);
            var uzytkownik = wynik.SingleOrDefault();

            if (uzytkownik == null)
            {
                return false;
            }

            uzytkownik.IDSesji = Guid.Empty;

            var updateOperation = TableOperation.Replace(uzytkownik);
            table.Execute(updateOperation);

            return true;
        }

        public bool Put(string nazwa, string tresc, Guid id_sesji)
        {
            var table = GetTableFromAzure("users");
            var blobContainer = GetBlobFromAzure("files");

            TableQuery<Uzytkownik> query = new TableQuery<Uzytkownik>()
                .Where(TableQuery.GenerateFilterConditionForGuid("SessionId", QueryComparisons.Equal, id_sesji));
            var wynik = table.ExecuteQuery(query);
            var uzytkownik = wynik.SingleOrDefault();

            if (uzytkownik == null)
            {
                return false;
            }

            var nameOfBlob = uzytkownik.Login + "_" + nazwa;
            var blob = blobContainer.GetBlockBlobReference(nameOfBlob);

            var bytes = new ASCIIEncoding().GetBytes(tresc);
            var stream = new MemoryStream(bytes);
            blob.UploadFromStream(stream);

            return true;
        }

        public string Get(string nazwa, Guid id_sesji)
        {
            var table = GetTableFromAzure("users");
            var blobContainer = GetBlobFromAzure("files");

            TableQuery<Uzytkownik> query = new TableQuery<Uzytkownik>()
                .Where(TableQuery.GenerateFilterConditionForGuid("SessionId", QueryComparisons.Equal, id_sesji));
            var wynik = table.ExecuteQuery(query);
            var uzytkownik = wynik.SingleOrDefault();

            if (uzytkownik == null)
            {
                return string.Empty;
            }

            var nameOfBlob = uzytkownik.Login + "_" + nazwa;
            var blob = blobContainer.GetBlockBlobReference(nameOfBlob);

            if (blob == null)
            {
                return string.Empty;
            }

            var stream = new MemoryStream();
            blob.DownloadToStream(stream);
            string content = Encoding.UTF8.GetString(stream.ToArray());

            return content;
        }

    }
}
