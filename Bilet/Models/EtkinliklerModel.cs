namespace Bilet.Models
{
    public class EtkinliklerModel
    {       
        public string EtkinlikAdi { get; set; } = null!;

        public string? Aciklama { get; set; }

        public DateTime Tarih { get; set; }

        public DateTime BasvuruBitisTarihi { get; set; }

        public int Kontenjan { get; set; }

        public string? Adres { get; set; }

        public bool BiletliMi { get; set; }

        public int? Fiyat { get; set; }

        public int OrganizatorId { get; set; }

        public int KategoriId { get; set; }

        public int SehirId { get; set; }

    }
}
