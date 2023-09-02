using Bilet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using System.Net.Mime;

namespace Bilet.Controller
{
    [Route("api")]
    [ApiController]
    [Authorize]
    public class MainController : ControllerBase
    {
        private readonly TokenOption tokenOption;
            
        public MainController(IOptions<TokenOption> options)
        {
            tokenOption = options.Value;
        }

        [AllowAnonymous]
        [HttpPost("kayit")]
        //yeni üye kaydı
        public IActionResult Kaydol(UyelerModel uyelerModel)
        {
            string sifreDeseni = @"^(?=.*[A - Za - z])(?=.*\d)[A - Za - z\d]{8}$";          
            if (!Regex.IsMatch(uyelerModel.sifre, sifreDeseni))
            {
                return BadRequest("şifre 8 karakter olmalı ve hem harf hem rakam içermeli");
            }
            BiletDbContext context = new BiletDbContext();
            if(uyelerModel.sifre==uyelerModel.sifreTekrari)
            {
                Uyeler uye = new Uyeler();
                uye.Adi = uyelerModel.ad;
                uye.Soyadi = uyelerModel.soyad;
                uye.Eposta = uyelerModel.email;
                uye.Sifre = uyelerModel.sifre;
                context.Uyelers.Add(uye);
                context.SaveChanges();
                return Ok("kayit başarılı");
            }
            else
            {
                return BadRequest("şifre bilgileri eşleşmiyor.");
            }                                    
        }

        [AllowAnonymous]
        [HttpPost("giris")]
        //token almak için DB'de mail ve şifre kontolü yapar
        public async Task<IActionResult> Giris(UyelerModel uyelerModel)
        {           
            BiletDbContext context = new BiletDbContext();
            var email = uyelerModel.email;
            var sifre = uyelerModel.sifre;
            var kullanici = await context.Uyelers
                .Where(u => u.Eposta == email)
                .Select(u => new { u.Eposta, u.Sifre })
                .SingleOrDefaultAsync();

            if (email == kullanici.Eposta && sifre == kullanici.Sifre)
            {
                List<Claim> claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, uyelerModel.email));
                claims.Add(new Claim(ClaimTypes.Role, ""));
                JwtSecurityToken securityToken = new JwtSecurityToken(
                    issuer: tokenOption.Issuer,
                    audience: tokenOption.Audience,
                    claims:claims,
                    expires:DateTime.Now.AddDays(tokenOption.Expiration),
                    signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        tokenOption.SecretKey)), SecurityAlgorithms.HmacSha256)
                    );
                JwtSecurityTokenHandler tokenhandler = new JwtSecurityTokenHandler();
                string userToken = tokenhandler.WriteToken(securityToken);

                return Ok(userToken);
            }
            else
            {
                return NotFound("kullanıcı bilgileri hatalı");
            }
            
        }       

        [HttpGet("profil")]
        // Kullanıcının profil bilgilerini ve bildirimlerini görüntüleme
        public IActionResult KullaniciProfil()
        {
            BiletDbContext context = new BiletDbContext();
            var kullaniciEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var profilBilgileri = context.Uyelers
                .Where(u => u.Eposta == kullaniciEmail)
                .Select(u => new
                {
                    Adi = u.Adi,
                    Soyadi = u.Soyadi,
                    Eposta = u.Eposta
                })
                .FirstOrDefault();

            var kullaniciId = context.Uyelers
                .Where(u => u.Eposta == kullaniciEmail)
                .Select(u => u.UyeId)
                .FirstOrDefault();

            var bildirimler = context.Bildirimlers
                .Where(b => b.UyeId == kullaniciId)
                .Select(b => b.Bildirim)
                .ToList();
            
            var profilBilgileriListe = new List<object>
                {
                new
                    {
                    Adi = profilBilgileri.Adi,
                    Soyadi = profilBilgileri.Soyadi,
                    Eposta = profilBilgileri.Eposta
                    }
                };

            var bildirimlerListe = bildirimler.Select(b => new
            {
                Bildirim = b
            }).ToList();

            var profilVeBildirimler = new
            {
                ProfilBilgileri = profilBilgileriListe,
                Bildirimler = bildirimlerListe
            };

            return Ok(profilVeBildirimler);
        }

        [HttpPatch("profil/sifredegis")]
        //kullanıcı şifre değişikliği
        public IActionResult SifreDegistir([FromBody] UyeSifreModel sifreModel)

        {
            BiletDbContext context = new BiletDbContext();           
            var kullaniciEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var kullanici = context.Uyelers.FirstOrDefault(u => u.Eposta == kullaniciEmail);

            if (kullanici == null)
            {
                return BadRequest("Kullanıcı bulunamadı.");
            }
           
            if (sifreModel.YeniSifre == sifreModel.EskiSifre)
            {
                return BadRequest("Yeni şifre, eski şifre ile aynı olamaz.");
            }
          
            Regex sifreRegex = new Regex("^(?=.*[A-Za-z])(?=.*\\d)[A-Za-z\\d]{8}$");
            if (!sifreRegex.IsMatch(sifreModel.YeniSifre))
            {
                return BadRequest("Yeni şifre 8 karakter uzunluğunda olmalı ve hem harf hem sayı içermelidir.");
            }

            kullanici.Sifre = sifreModel.YeniSifre;
            context.SaveChanges();

            return Ok("Şifre başarıyla değiştirildi.");

        }       

        [HttpGet("etkinlikler")]
        // Kullanıcı katıldığı geçmiş etkinlikleri, katılacağı etkinlikleri, katılabileceği etkinlikleri
        // ve kendi oluşturduğu etkinlikleri listeler
        public IActionResult KullaniciEtkinlikleri()
        {
            BiletDbContext context = new BiletDbContext();
            DateTime now = DateTime.Now;
            var kullaniciEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var kullanici = context.Uyelers.FirstOrDefault(u => u.Eposta == kullaniciEmail);            
            var kullaniciId = kullanici.UyeId;

            var katilacagiEtkinlikler = context.Katilimlars
                .Where(k => k.KatilimciId == kullaniciId)
                .Select(k => k.Etkinlik)
                .Where(e => e.Onay) 
                .ToList();

            var gecmisEtkinlikler = katilacagiEtkinlikler
                .Where(e => e.Tarih < now)
                .ToList();

            var katilabilecegiEtkinlikler = katilacagiEtkinlikler
                .Where(e => e.Tarih > now)
                .ToList();

            var kendiOlusturduguEtkinlikler = context.Etkinliklers
                .Where(e => e.OrganizatorId == kullaniciId)
                .Where(e => e.Onay) 
                .ToList();

            var etkinlikBilgileri = new
            {
                KatilacagiEtkinlikler = katilacagiEtkinlikler.Select(e => new
                {
                    EtkinlikAdi = e.EtkinlikAdi,
                    Aciklama = e.Aciklama,
                    Tarih = e.Tarih,
                    BasvuruBitisTarihi = e.BasvuruBitisTarihi,
                    Kontenjan = e.Kontenjan,
                    Adres = e.Adres,
                    Fiyat = e.Fiyat,
                    KategoriAdi = context.Kategorilers.FirstOrDefault(k => k.KategoriId == e.KategoriId)?.KategoriAdi,
                    SehirAdi = context.Sehirlers.FirstOrDefault(s => s.SehirId == e.SehirId)?.SehirAdi
                }).ToList(),
                GecmisEtkinlikler = gecmisEtkinlikler.Select(e => new
                {
                    EtkinlikAdi = e.EtkinlikAdi,
                    Aciklama = e.Aciklama,
                    Tarih = e.Tarih,
                    BasvuruBitisTarihi = e.BasvuruBitisTarihi,
                    Kontenjan = e.Kontenjan,
                    Adres = e.Adres,
                    Fiyat = e.Fiyat,
                    KategoriAdi = context.Kategorilers.FirstOrDefault(k => k.KategoriId == e.KategoriId)?.KategoriAdi,
                    SehirAdi = context.Sehirlers.FirstOrDefault(s => s.SehirId == e.SehirId)?.SehirAdi
                }).ToList(),
                KatilabilecegiEtkinlikler = katilabilecegiEtkinlikler.Select(e => new
                {
                    EtkinlikAdi = e.EtkinlikAdi,
                    Aciklama = e.Aciklama,
                    Tarih = e.Tarih,
                    BasvuruBitisTarihi = e.BasvuruBitisTarihi,
                    Kontenjan = e.Kontenjan,
                    Adres = e.Adres,
                    Fiyat = e.Fiyat,
                    KategoriAdi = context.Kategorilers.FirstOrDefault(k => k.KategoriId == e.KategoriId)?.KategoriAdi,
                    SehirAdi = context.Sehirlers.FirstOrDefault(s => s.SehirId == e.SehirId)?.SehirAdi
                }).ToList(),
                KendiOlusturduguEtkinlikler = kendiOlusturduguEtkinlikler.Select(e => new
                {
                    EtkinlikAdi = e.EtkinlikAdi,
                    Aciklama = e.Aciklama,
                    Tarih = e.Tarih,
                    BasvuruBitisTarihi = e.BasvuruBitisTarihi,
                    Kontenjan = e.Kontenjan,
                    Adres = e.Adres,
                    Fiyat = e.Fiyat,
                    KategoriAdi = context.Kategorilers.FirstOrDefault(k => k.KategoriId == e.KategoriId)?.KategoriAdi,
                    SehirAdi = context.Sehirlers.FirstOrDefault(s => s.SehirId == e.SehirId)?.SehirAdi
                }).ToList()
            };

            return Ok(etkinlikBilgileri);
        }

        [HttpGet("etkinlikler/sehirara/{sehirAdi}")]
        //uye sehir adına göre filtreleme yapabilir
        public IActionResult SehirdekiEtkinlikleriAra(string sehirAdi)
        {
            BiletDbContext context = new BiletDbContext();          
            var sehir = context.Sehirlers.FirstOrDefault(s => s.SehirAdi == sehirAdi);           

            if (sehir == null)
            {
                return BadRequest("Şehir bulunamadı.");
            }

            var sehirId = sehir.SehirId;
            var etkinlikler = context.Etkinliklers
                .Where(e => e.SehirId == sehirId)
                .Select(e => new
                {
                    EtkinlikAdi = e.EtkinlikAdi,
                    Aciklama = e.Aciklama,
                    Tarih = e.Tarih,
                    BasvuruBitisTarihi = e.BasvuruBitisTarihi,
                    Kontenjan = e.Kontenjan,
                    Adres = e.Adres,
                    Fiyat = e.Fiyat,
                    KategoriAdi = context.Kategorilers.FirstOrDefault(k => k.KategoriId == e.KategoriId).KategoriAdi,
                    SehirAdi = context.Sehirlers.FirstOrDefault(s => s.SehirId == e.SehirId).SehirAdi
                })
                .ToList();

            return Ok(etkinlikler);
        }

        [HttpGet("etkinlikler/kategoriara/{kategoriAdi}")]
        //uye kategori adına göre filtreleme yapabilir
        public IActionResult KategoriyeGoreEtkinlikAra(string kategoriAdi)
        {
            BiletDbContext context = new BiletDbContext();
            var kategori = context.Kategorilers.FirstOrDefault(k => k.KategoriAdi == kategoriAdi);

            if (kategori == null)
            {
                return BadRequest("Kategori bulunamadı.");
            }

            var kategoriId = kategori.KategoriId;
            var etkinlikler = context.Etkinliklers
                .Where(e => e.KategoriId == kategoriId)
                .Select(e => new
                {
                    EtkinlikAdi = e.EtkinlikAdi,
                    Aciklama = e.Aciklama,
                    Tarih = e.Tarih,
                    BasvuruBitisTarihi = e.BasvuruBitisTarihi,
                    Kontenjan = e.Kontenjan,
                    Adres = e.Adres,
                    Fiyat = e.Fiyat,
                    KategoriAdi = context.Kategorilers.FirstOrDefault(k => k.KategoriId == e.KategoriId).KategoriAdi,
                    SehirAdi = context.Sehirlers.FirstOrDefault(s => s.SehirId == e.SehirId).SehirAdi
                })
                .ToList();

            return Ok(etkinlikler);
        }

        [HttpGet("etkinlikler/kayit/{etkinlikid}")]
        // kullanıcının etkinliğe katılma işlemi                 
        public IActionResult EtkinligeKatil(int etkinlikid)
        {
            BiletDbContext context = new BiletDbContext();
            var kullaniciEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var kullanici = context.Uyelers.FirstOrDefault(u => u.Eposta == kullaniciEmail);         
            var kullaniciId = kullanici.UyeId;            
            var biletkontrol = context.Etkinliklers.FirstOrDefault(e => e.EtkinlikId == etkinlikid);
            if (biletkontrol == null)
            {
                return BadRequest("Etkinlik bulunamadı.");
            }

            bool kayitVarMi = context.Katilimlars
                .Any(item => item.KatilimciId == kullaniciId && item.EtkinlikId == etkinlikid);

            if (kayitVarMi)
            {
                return BadRequest("Etkinliğe kayıt zaten var.");
            }
            else
            {
                if (biletkontrol.BiletliMi)
                {
                    var etkinlikKontenjan = biletkontrol.Kontenjan;
                    var kayitSayisi = context.Katilimlars
                        .Where(k => k.EtkinlikId == etkinlikid)
                        .Count();

                    if (kayitSayisi < etkinlikKontenjan)
                    {
                        var biletFirmalari = context.BiletFirmalaris
                            .Select(b => b.WebSitesi)
                            .ToList();
                        return Ok(biletFirmalari);
                    }
                    else
                    {
                        return BadRequest("Etkinlik kontenjanı dolu.");
                    }
                }
                else
                {
                    var katil = new Katilimlar
                    {
                        EtkinlikId = etkinlikid,
                        KatilimciId = kullaniciId
                    };
                    context.Katilimlars.Add(katil);
                    context.SaveChanges();

                    return Ok();
                }
            }
        }

        [HttpPost("etkinlikler/olustur")]
        // kullanıcının etkinlik oluşturma işlemi
        public IActionResult EtkinlikOlustur(EtkinliklerModel etkinliklerModel)
        {
            BiletDbContext context = new BiletDbContext();                           
            var kullaniciEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var kullanici = context.Uyelers.FirstOrDefault(u => u.Eposta == kullaniciEmail);           
            var yeniEtkinlik = new Bilet.Models.Etkinlikler
            {
                EtkinlikAdi = etkinliklerModel.EtkinlikAdi,
                Aciklama = etkinliklerModel.Aciklama,
                Tarih = etkinliklerModel.Tarih,
                BasvuruBitisTarihi = etkinliklerModel.BasvuruBitisTarihi,
                Kontenjan = etkinliklerModel.Kontenjan,
                Adres = etkinliklerModel.Adres,
                BiletliMi = etkinliklerModel.BiletliMi,
                Fiyat = etkinliklerModel.Fiyat,
                OrganizatorId = kullanici.UyeId,
                KategoriId = etkinliklerModel.KategoriId,
                SehirId = etkinliklerModel.SehirId,
            };

            context.Etkinliklers.Add(yeniEtkinlik);
            context.SaveChanges();

            return Ok("Etkinlik oluşturuldu.");
            
        }
       
        [HttpPatch("etkinlikler/duzenle/{id}")]
        // Kullanıcının oluşturduğu etkinlikleri düzenleme işlemi
        public IActionResult EtkinlikDuzenle(int id, EtkinlikGuncelleModel etkinlikGuncelleModel)
        {
            BiletDbContext context = new BiletDbContext();           
                
            var kullaniciEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var kullanici = context.Uyelers.FirstOrDefault(u => u.Eposta == kullaniciEmail);               
            var duzenlenecekEtkinlik = context.Etkinliklers.FirstOrDefault(e => e.EtkinlikId == id && e.OrganizatorId == kullanici.UyeId);

            if (duzenlenecekEtkinlik == null)
            {
               return NotFound("düzenlenecek etkinlik bulunamadı");
            }
                
            var gunFarki = (duzenlenecekEtkinlik.Tarih - DateTime.Now).TotalDays;

            if (gunFarki <= 5)
            {
                return BadRequest("etkinlik tarihine 5 günden az bir süre kaldığı için güncelleme yapılamaz");
            }
               
            duzenlenecekEtkinlik.Kontenjan = etkinlikGuncelleModel.Kontenjan;
            duzenlenecekEtkinlik.Adres = etkinlikGuncelleModel.Adres;

            context.SaveChanges();

            return Ok("etkinlik güncellendi");
            
        }

        [HttpDelete("etkinlikler/sil/{id}")]
        // Kullanıcının oluşturduğu etkinliğini silme işlemi
        public IActionResult EtkinlikSil(int id)
        {
            BiletDbContext context = new BiletDbContext();
                           
            var silinecekEtkinlik = context.Etkinliklers.FirstOrDefault(e => e.EtkinlikId == id);
            if (silinecekEtkinlik == null)
            {
                return NotFound("silinecek etkinlik bulunamadı");
            }
                
            var kullaniciEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var kullanici = context.Uyelers.FirstOrDefault(u => u.Eposta == kullaniciEmail);

            if (kullanici == null || kullanici.UyeId != silinecekEtkinlik.OrganizatorId)
            {
                return BadRequest("bu etkinliği silebilmek için yetkiniz yok");
            }
                
            var gunFarki = (silinecekEtkinlik.Tarih - DateTime.Now).TotalDays;

            if (gunFarki <= 5)
            {
                return BadRequest("etkinlik tarihine 5 günden az bir süre kaldığı için silme işlemi yapılamaz");
            }
                
            context.Etkinliklers.Remove(silinecekEtkinlik);
            context.SaveChanges();

            return Ok("etkinlik silindi");
            
        }

        [AllowAnonymous]
        [HttpPost("firma/giris")]
        //bilet firması girişinde token almak için DB'de mail ve şifre kontolü yapar
        public async Task<IActionResult> FirmaGiris(FirmaGirisModel firmaGirisModel)
        {
            BiletDbContext context = new BiletDbContext();
            var email = firmaGirisModel.Mail;
            var sifre = firmaGirisModel.Sifre;
            var firma = await context.BiletFirmalaris
                .Where(u => u.Mail == email)
                .Select(u => new { u.Mail, u.Sifre })
                .SingleOrDefaultAsync();

            if (email == firma.Mail && sifre == firma.Sifre)
            {
                List<Claim> claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, firmaGirisModel.Mail));
                claims.Add(new Claim(ClaimTypes.Role, ""));
                JwtSecurityToken securityToken = new JwtSecurityToken(
                    issuer: tokenOption.Issuer,
                    audience: tokenOption.Audience,
                    claims: claims,
                    expires: DateTime.Now.AddDays(tokenOption.Expiration),
                    signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        tokenOption.SecretKey)), SecurityAlgorithms.HmacSha256)
                    );
                JwtSecurityTokenHandler tokenhandler = new JwtSecurityTokenHandler();
                string firmaToken = tokenhandler.WriteToken(securityToken);

                return Ok(firmaToken);
            }
            else
            {
                return NotFound("bilet firması bilgileri hatalı");
            }

        }        

        [HttpGet("firma/etkinlikler/json")]
        //firmanın etkinlikleri JSON formatında görüntülemesini sağlar
        public IActionResult FirmaEtkinlikleriJSON()
        {
            BiletDbContext context = new BiletDbContext();
            var firmaEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var biletliEtkinlikler = context.Etkinliklers
                .Where(e => e.BiletliMi && e.Onay)
                .Select(e => new
                {
                    EtkinlikAdi = e.EtkinlikAdi,
                    Aciklama = e.Aciklama,
                    Tarih = e.Tarih,
                    BasvuruBitisTarihi = e.BasvuruBitisTarihi,
                    Kontenjan = e.Kontenjan,
                    Adres = e.Adres,
                    Fiyat = e.Fiyat,
                    KategoriAdi = context.Kategorilers.FirstOrDefault(k => k.KategoriId == e.KategoriId).KategoriAdi,
                    SehirAdi = context.Sehirlers.FirstOrDefault(s => s.SehirId == e.SehirId).SehirAdi
                })
                .ToList();

            return Ok(biletliEtkinlikler);
        }

        [HttpGet("firma/etkinlikler/xml")]
        [Consumes(MediaTypeNames.Application.Xml)]
        //[Produces(MediaTypeNames.Application.Xml)]
        //firmanın etkinlikleri XML formatında görüntülemesini sağlar
        public IActionResult FirmaEtkinlikleriXML()
        {
            BiletDbContext context = new BiletDbContext();
            var firmaEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;           
            var biletliEtkinlikler = context.Etkinliklers
                .Where(e => e.BiletliMi && e.Onay)
                .Select(e => new
                {
                    EtkinlikAdi = e.EtkinlikAdi,
                    Aciklama = e.Aciklama,
                    Tarih = e.Tarih,
                    BasvuruBitisTarihi = e.BasvuruBitisTarihi,
                    Kontenjan = e.Kontenjan,
                    Adres = e.Adres,
                    Fiyat = e.Fiyat,
                    KategoriAdi = context.Kategorilers.FirstOrDefault(k => k.KategoriId == e.KategoriId).KategoriAdi,
                    SehirAdi = context.Sehirlers.FirstOrDefault(s => s.SehirId == e.SehirId).SehirAdi
                })
                .ToList();

            return Ok(biletliEtkinlikler);
        }

        [AllowAnonymous]
        [HttpPost("admin/giris")]
        //admin token almak için DB'de mail ve şifre kontolü yapar
        public async Task<IActionResult> AdminGiris(UyelerModel uyelerModel)
        {
            BiletDbContext context = new BiletDbContext();
            var email = uyelerModel.email;
            var sifre = uyelerModel.sifre;
            var kullanici = await context.Uyelers
            .Where(u => u.Eposta == email)
            .Select(u => new { u.Eposta, u.Sifre })
            .SingleOrDefaultAsync();

            if (email == kullanici.Eposta && sifre == kullanici.Sifre)
            {
                List<Claim> claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, uyelerModel.email));
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                JwtSecurityToken securityToken = new JwtSecurityToken(
                    issuer: tokenOption.Issuer,
                    audience: tokenOption.Audience,
                    claims: claims,
                    expires: DateTime.Now.AddDays(tokenOption.Expiration),
                    signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        tokenOption.SecretKey)), SecurityAlgorithms.HmacSha256)
                    );
                JwtSecurityTokenHandler tokenhandler = new JwtSecurityTokenHandler();
                string userToken = tokenhandler.WriteToken(securityToken);

                return Ok(userToken);
            }
            else
            {
                return NotFound("admin bilgileri hatalı");
            }

        }

        [HttpGet("admin/onaybekleyenler")]
        [Authorize(Roles = "Admin")] 
        //admin onay bekleyen etkinlikleri görüntüler
        public async Task<IActionResult> OnayBekleyenEtkinlikler()
        {
            BiletDbContext context = new BiletDbContext();
            var onayBekleyenEtkinlikler = await context.Etkinliklers
                .Where(e => !e.Onay)
                .ToListAsync();

            if (onayBekleyenEtkinlikler == null || onayBekleyenEtkinlikler.Count == 0)
            {
               return NotFound("onay bekleyen etkinlik bulunamadı");
            }
          
            var formattedEtkinlikler = onayBekleyenEtkinlikler.Select(e => new
            {
                EtkinlikId = e.EtkinlikId,
                EtkinlikAdi = e.EtkinlikAdi,
                Aciklama = e.Aciklama,
                Tarih = e.Tarih,
                BasvuruBitisTarihi = e.BasvuruBitisTarihi,
                Kontenjan = e.Kontenjan,
                Adres = e.Adres,
                Fiyat = e.Fiyat,
                KategoriAdi = context.Kategorilers.FirstOrDefault(k => k.KategoriId == e.KategoriId).KategoriAdi,
                SehirAdi = context.Sehirlers.FirstOrDefault(s => s.SehirId == e.SehirId).SehirAdi

            });

            return Ok(formattedEtkinlikler);           
        }

        [HttpPatch("admin/onay/{id}")]
        [Authorize(Roles = "Admin")]
        //admin etkinlik onaylar
        public IActionResult OnaylaEtkinlik(int id)
        {
            BiletDbContext context = new BiletDbContext();            
            var etkinlik = context.Etkinliklers.FirstOrDefault(e => e.EtkinlikId == id);

            if (etkinlik == null)
            {
                return NotFound("etkinlik bulunamadı");
            }
               
            etkinlik.Onay = true;
            context.SaveChanges();

            return Ok("etkinlik onaylandı");
            
        }

        [HttpDelete("admin/reddet/{id}")]
        [Authorize(Roles = "Admin")]
        // Admin onay bekleyen etkinliği reddeder ve sistemden kaldırır, organizatöre bildirim yapılır.
        public IActionResult ReddetEtkinlik(int id)
        {
            BiletDbContext context = new BiletDbContext();            
            var etkinlik = context.Etkinliklers.FirstOrDefault(e => e.EtkinlikId == id);

            if (etkinlik == null)
            {
                return NotFound("Etkinlik bulunamadı.");
            }
               
            int organizatorId = etkinlik.OrganizatorId;             
            string bildirimMetni = etkinlik.EtkinlikAdi + " isimli etkinliğiniz admin tarafından reddedilmiştir.";
          
            var bildirim = new Bildirimler
            {
                UyeId = organizatorId,
                Bildirim = bildirimMetni
            };

            context.Bildirimlers.Add(bildirim);                
            context.Etkinliklers.Remove(etkinlik);
            context.SaveChanges();

            return Ok("Etkinlik reddedildi ve silindi. Organizatöre bildirim gönderildi.");
            
        }


        [HttpPost("admin/sehirekle")]
        [Authorize(Roles = "Admin")]
        // Admin şehir ekler
        public IActionResult SehirEkle(SehirlerModel sehirlerModel)
        {
            BiletDbContext context = new BiletDbContext();                           
            var varMi = context.Sehirlers.Any(s => s.SehirAdi == sehirlerModel.SehirAdi);

            if (varMi)
            {
                return BadRequest("bu isimde bir şehir zaten var");
            }

            var yeniSehir = new Sehirler
            {
                SehirAdi = sehirlerModel.SehirAdi
            };

            context.Sehirlers.Add(yeniSehir);
            context.SaveChanges();

            return Ok("şehir başarıyla eklendi");
            
        }

        [HttpDelete("admin/sehirsil/{id}")]
        [Authorize(Roles = "Admin")]
        //admin şehir siler
        public IActionResult SehirSil(int id)
        {
            BiletDbContext context = new BiletDbContext();                          
            var silinecekSehir = context.Sehirlers.FirstOrDefault(s => s.SehirId == id);

            if (silinecekSehir == null)
            {
                return NotFound("şehir bulunamadı");
            }
               
            context.Sehirlers.Remove(silinecekSehir);
            context.SaveChanges();

            return Ok("şehir başarıyla silindi");
            
        }

        [HttpPost("admin/kategoriekle")]
        [Authorize(Roles = "Admin")]
        //Admin kategori oluşturur
        public IActionResult KategoriEkle(KategorilerModel kategorilerModel)
        {
            BiletDbContext context = new BiletDbContext();                           
            var varMi = context.Kategorilers.Any(k => k.KategoriAdi == kategorilerModel.KategoriAdi);

            if (varMi)
            {
                return BadRequest("bu isimde bir kategori zatn var");
            }

            var kategori = new Kategoriler
            {
                KategoriAdi = kategorilerModel.KategoriAdi
            };

            context.Kategorilers.Add(kategori);
            context.SaveChanges();

            return Ok("kategori başarıyla eklendi");
            
        }

        [HttpDelete("admin/kategorisil/{id}")]
        [Authorize(Roles = "Admin")]
        //admin kategori siler
        public IActionResult KategoriSil(int id)
        {
            BiletDbContext context = new BiletDbContext();                           
            var kategori = context.Kategorilers.FirstOrDefault(k => k.KategoriId == id);

            if (kategori == null)
            {
                return NotFound("kategori bulunamadı");
            }
               
            context.Kategorilers.Remove(kategori);
            context.SaveChanges();

            return Ok("kategori başarıyla silindi");
            
        }

    }
}
