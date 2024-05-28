using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WCFServiceWebRole1
{
    public class Uzytkownik : TableEntity
    {
        public Guid IDSesji { get; set; }
        public string Login { get; set; }
        public string Haslo { get; set; }
        
        public Uzytkownik()
        {
        }

        public Uzytkownik(string kluczPartycji, string kluczGlowny) : base(kluczPartycji, kluczGlowny)
        {
            this.PartitionKey = kluczPartycji; // ustawiamy klucz partycji
            this.RowKey = kluczGlowny; // ustawiamy klucz główny 
        }
    }
}