# typory

**[English](README.md) | Türkçe**

Hafif bir Windows metin genişletici.

typory sistem tepsisinde sessizce durur ve sen yazarken kısa kısaltmaları izler.
Birini yazdığın an — örneğin `;mail` — onu silip yerine tam metni yazar
(`volkanturhan@gmail.com`), hangi uygulamada olursan ol. Sürekli yazdığın
metinleri bir kez tanımla, bir daha tam halini yazma.

<p align="center">
  <img src="docs/screenshot.png" alt="typory snippet yöneticisi" width="420" />
</p>

## Özellikler

- **Her yerde genişlet** — sistem genelinde, her uygulamada, sen yazarken çalışır.
- **Kendi snippet'lerin** — kısaltma → karşılık kurallarını basit bir pencerede yönet.
- **Unicode & sembol** — karşılık her şey olabilir, örn. `;shrug` → `¯\_(ツ)_/¯`.
- **Klavye düzenine duyarlı** — tuşları senin klavye düzeninle çözer; US dışı
  düzenler (örn. Türkçe) ve AltGr karakterleri çalışır.
- **İstediğin an duraklat** — genişletmeyi tepsiden aç/kapa.
- **Yeniden başlatmaya dayanır** — snippet'lerin kaydedilip geri yüklenir.
- **Windows ile başla** — isteğe bağlı, tepsi menüsünden aç/kapa.
- **İngilizce & Türkçe** — arayüz dilini tepsiden değiştir.
- **Tasarımı gereği gizli** — her şey senin makinende kalır, hiçbir şey yüklenmez.

## Çalıştır

typory henüz hazır bir indirme olarak yayınlanmadı, bu yüzden şimdilik kaynaktan
çalıştırıyorsun. Windows'ta [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
(sadece runtime değil, SDK) kurulu olmalı.

```bash
git clone https://github.com/volkanturhan/typory.git
cd typory
dotnet run --project typory/typory.csproj
```

typory sessizce sistem tepsisinde başlar — **hiçbir pencere açılmaz**. Bu
normaldir; snippet'lerini ayarlamak için tepsi ikonuna çift tıkla (ya da
**Snippet'leri yönet**'i kullan).

## Nasıl kullanılır

1. typory'i başlat — sessizce sistem tepsisine yerleşir.
2. Yöneticiyi açmak için tepsi ikonuna çift tıkla (ya da sağ tık →
   **Snippet'leri yönet**). Birkaç örnek snippet ile başlar.
3. Satır ekle: bir **kısaltma** (örn. `;adres`) ve **karşılığı** (adresin).
   Değişiklikler otomatik kaydedilir — Kaydet düğmesi yok.
4. Artık kısaltmayı herhangi bir uygulamada yaz; typory anında değiştirir.

İpucu: kısaltmaları yanlışlıkla yazmayacağın bir karakterle başlat (`;` ya da `:`
gibi) ki yalnızca isteyince tetiklensinler.

Tepsi ikonuna sağ tık: **Snippet'leri yönet**, **Genişletme açık** (duraklat /
sürdür), **Windows ile başlat**, dil ve **Çıkış**.

## Verilerin nerede tutulur

Snippet'lerin yerel olarak `%APPDATA%\typory\snippets.json` içinde saklanır ve
makinenden asla çıkmaz; tercihlerin yanındaki `settings.json` dosyasında tutulur.

## Paylaşılabilir exe oluştur

SDK olmadan birine verebileceğin bağımsız bir `.exe` mi istiyorsun? Kendin
derle — çıktı repoya dahil edilmez:

```bash
# dist/ içine derler (self-contained typory.exe + lite sürüm)
pwsh tools/publish.ps1
```

## Teknoloji

- C# / WPF, .NET 8 (Windows)
- Üçüncü parti bağımlılık yok

## Lisans

MIT — bkz. [LICENSE](LICENSE).
