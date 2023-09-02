using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bilet.Models;

public partial class Uyeler
{
    public int UyeId { get; set; }

    public string Adi { get; set; } = null!;

    public string Soyadi { get; set; } = null!;

    public string Eposta { get; set; } = null!;
    
    [RegularExpression(@"^(?=.*[A - Za - z])(?=.*\d)[A - Za - z\d]{8}$", ErrorMessage = "Şifre hem harf hem de sayı içermeli ve 8 karakter uzunluğunda olmalı.")]
    public string Sifre { get; set; } = null!;

    public string? Role { get; set; }

    public virtual ICollection<Etkinlikler> Etkinliklers { get; set; } = new List<Etkinlikler>();

    public virtual ICollection<Katilimlar> Katilimlars { get; set; } = new List<Katilimlar>();
}
