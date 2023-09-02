using System;
using System.Collections.Generic;

namespace Bilet.Models;

public partial class Kategoriler
{
    public int KategoriId { get; set; }

    public string KategoriAdi { get; set; } = null!;

    public virtual ICollection<Etkinlikler> Etkinliklers { get; set; } = new List<Etkinlikler>();
}
