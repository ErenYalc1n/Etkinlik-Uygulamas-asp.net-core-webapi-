using System;
using System.Collections.Generic;

namespace Bilet.Models;

public partial class Sehirler
{
    public int SehirId { get; set; }

    public string SehirAdi { get; set; } = null!;

    public virtual ICollection<Etkinlikler> Etkinliklers { get; set; } = new List<Etkinlikler>();
}
