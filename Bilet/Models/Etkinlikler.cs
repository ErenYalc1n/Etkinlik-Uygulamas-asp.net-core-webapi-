using System;
using System.Collections.Generic;

namespace Bilet.Models;

public partial class Etkinlikler
{
    public int EtkinlikId { get; set; }

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

    public bool Onay { get; set; }

    public virtual Kategoriler Kategori { get; set; } = null!;

    public virtual ICollection<Katilimlar> Katilimlars { get; set; } = new List<Katilimlar>();

    public virtual Uyeler Organizator { get; set; } = null!;

    public virtual Sehirler Sehir { get; set; } = null!;
}
