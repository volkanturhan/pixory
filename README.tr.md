# Pixory

**[English](README.md) | Türkçe**

Hafif bir Windows ekran renk seçici.

Pixory sistem tepsisinde sessizce durur. Bir kısayola basarsın, imlecini takip
eden bir büyüteçle tam istediğin pikseli hizalarsın, tıklarsın — ve renk
istediğin biçimde (HEX, RGB ya da HSL) panoya kopyalanır. Seçtiğin her renk,
yeniden açıp kopyalayabileceğin ya da sabitleyebileceğin küçük bir palette tutulur.

## Özellikler

- **Herhangi bir pikseli seç** — global kısayol (`Ctrl + Shift + C`) büyüteç ve
  anlık hex okuması olan tam ekran bir seçici açar.
- **Piksel hassasiyetinde** — masaüstünün donmuş bir anlık görüntüsünden örnekler;
  yüksek DPI ve çoklu monitör kurulumlarında bile doğru.
- **İstediğin biçimde kopyala** — HEX, RGB ya da HSL, tepsiden değiştirilebilir.
- **Palet** — seçtiğin her renk tutulur; tekrar kopyalamak için yeniden aç.
- **Favoriler** — sık kullandığın renkleri sabitle; hep üstte kalır, asla silinmez.
- **Yeniden başlatmaya dayanır** — paletin (ve sabitlerin) kaydedilip geri yüklenir.
- **Windows ile başla** — isteğe bağlı, tepsi menüsünden aç/kapa.
- **İngilizce & Türkçe** — arayüz dilini tepsiden değiştir.
- **Tasarımı gereği gizli** — her şey senin makinende kalır, hiçbir şey yüklenmez.

## Çalıştır

Pixory henüz hazır bir indirme olarak yayınlanmadı, bu yüzden şimdilik kaynaktan
çalıştırıyorsun. Windows'ta [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
(sadece runtime değil, SDK) kurulu olmalı.

```bash
git clone https://github.com/volkanturhan/Pixory.git
cd Pixory
dotnet run --project Pixory/Pixory.csproj
```

Pixory sessizce sistem tepsisinde başlar — **hiçbir pencere açılmaz**. Bu
normaldir; kullanmak için kısayola bas ya da tepsi ikonuna tıkla (aşağıdaki
**Nasıl kullanılır**'a bak).

## Nasıl kullanılır

1. Pixory'i başlat — sessizce sistem tepsisine yerleşir.
2. Tam ekran seçiciyi açmak için **`Ctrl + Shift + C`**'ye bas (ya da tepsiden
   **Renk seç**'i seç).
3. Fareyi gezdir — büyüteç imlecin altındaki pikselleri büyütür ve rengin hex
   değerini gösterir. Seçmek için **tıkla**; **Esc** ya da sağ tık iptal eder.
4. Renk panoya kopyalanır ve paletine eklenir.
5. Paleti aç (tepsi **Paleti aç** ya da bir renge çift tıkla); **Enter** ile
   tekrar kopyala, **Ctrl + P** / sağ tık ile sabitle, **Del** ile sil.

Tepsi ikonuna sağ tık: **Renk seç**, **Paleti aç**, **Kopyalama biçimi**
(HEX / RGB / HSL), **Paleti temizle**, **Windows ile başlat**, dil ve **Çıkış**.

## Verilerin nerede tutulur

Paletin yerel olarak `%APPDATA%\Pixory\palette.json` içinde saklanır ve makinenden
asla çıkmaz; tercihlerin yanındaki `settings.json` dosyasında tutulur. Temizlemek
için tepsi menüsündeki **Paleti temizle**'yi kullan (sabitlenenler korunur);
sabitlenenleri paletten tek tek kaldırabilirsin.

## Paylaşılabilir exe oluştur

SDK olmadan birine verebileceğin bağımsız bir `.exe` mi istiyorsun? Kendin
derle — çıktı repoya dahil edilmez:

```bash
# dist/ içine derler (self-contained Pixory.exe + lite sürüm)
pwsh tools/publish.ps1
```

## Teknoloji

- C# / WPF, .NET 8 (Windows)
- Üçüncü parti bağımlılık yok

## Lisans

MIT — bkz. [LICENSE](LICENSE).
