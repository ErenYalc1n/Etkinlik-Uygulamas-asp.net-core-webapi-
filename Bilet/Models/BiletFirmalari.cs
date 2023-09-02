using System;
using System.Collections.Generic;

namespace Bilet.Models;

public partial class BiletFirmalari
{
    public int FirmaId { get; set; }

    public string FirmaAdi { get; set; } = null!;

    public string WebSitesi { get; set; } = null!;

    public string Mail { get; set; } = null!;

    public string Sifre { get; set; } = null!;
}
