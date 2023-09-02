using System.ComponentModel.DataAnnotations;

namespace Bilet.Models
{
    public class UyeSifreModel
    {
        
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8}$", ErrorMessage = "Şifre hem harf hem de sayı içermeli ve 8 karakter uzunluğunda olmalı.")]
        public string EskiSifre { get; set; } = null!;

        
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8}$", ErrorMessage = "Şifre hem harf hem de sayı içermeli ve 8 karakter uzunluğunda olmalı.")]
        public string YeniSifre { get; set; } = null!;

    }
}
