using System;
using System.Collections.Generic;

namespace Bilet.Models;

public partial class Katilimlar
{
    public int KatilimId { get; set; }

    public int KatilimciId { get; set; }

    public int EtkinlikId { get; set; }

    public virtual Etkinlikler Etkinlik { get; set; } = null!;

    public virtual Uyeler Katilimci { get; set; } = null!;
}
