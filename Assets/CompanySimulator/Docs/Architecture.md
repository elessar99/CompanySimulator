# Company Simulator Mimari Taslağı

## Mimari Yaklaşım
Proje başlangıçta feature-based ve content-driven olacak şekilde ayrılmıştır. Amaç, yeni sektörler, çalışan rolleri, proje tipleri, yatırım kalemleri ve rakip firma davranışlarını mevcut sistemleri bozmadan ekleyebilmektir.

## Katmanlar

### `Core`
Framework bağımsız temel yapı taşları burada tutulur.
Örnek: sonuç tipleri, temel arayüzler, event altyapısı, ortak servis sözleşmeleri.

### `Shared`
Birden fazla feature tarafından ortak kullanılan enum, value object ve sabitler burada tutulur.

### `Features`
Her ana oyun alanı ayrı modül olarak ele alınır.
Planlanan ilk modüller:
- `Company`
- `Employees`
- `Sectors`
- `Projects`
- `Investments`
- `Finance`
- `Competitors`
- `Sabotage`
- `Office`
- `Progression`

Her modül başlangıçta kendi `Runtime` alanına sahiptir. İleride ihtiyaç olursa modül içine `Editor`, `Tests`, `Config` gibi alt klasörler eklenebilir.

#### İlk uygulanan omurga: `Finance`
`Finance` modülü oyunun ekonomi çekirdeği olarak düşünülmelidir.
İlk aşamada şu sorumlulukları taşır:
- para tipi ve ledger yönetimi
- proje gelir/gider hesabı
- çalışan, yatırım, sektör ve proje verilerini ortak formülde birleştirme
- içerik balansını `ScriptableObject` assetleri üzerinden ayarlama

### `Presentation`
UI ve dünya/ofis sunumu gibi oyuncuya görünen katman burada bulunur.
Domain mantığı ile görsel katman ayrık tutulmalıdır.

### `Content`
Koddan bağımsız içerikler burada tutulur.
Özellikle veri tanımları ileride ScriptableObject veya benzeri içerik varlıkları ile yönetilecek şekilde planlanmıştır.

### `Tools`
Sadece editör içinde çalışan üretim araçları, içerik oluşturucular ve doğrulama araçları.

### `Tests`
Edit mode ve play mode testleri ayrı tutulur.

## İçerik Genişletme Stratejisi
Yeni bir sektör eklerken ideal akış:
1. `Content/Definitions/Sectors` altına yeni sektör tanımı eklenir.
2. Gerekirse ilgili proje tipleri `Content/Definitions/ProjectTypes` altında tanımlanır.
3. Sektöre özel yatırım tanımları `Content/Definitions/Investments` altında tutulur.
4. Gerekli kurallar ilgili feature modüllerinde uygulanır.

Ekonomi balansı güncellenmek istendiğinde ideal akış:
1. `Content/Definitions/Economy` altında global balans assetleri oluşturulur.
2. Gerekirse yatırımların bütçe tepki eğrileri ayrı assetler olarak tanımlanır.
3. Runtime tarafında sadece request modelleri değiştirilir, temel hesaplayıcı mümkün olduğunca sabit tutulur.

## Temel Kural
- Kod, mümkün olduğunca genel sistemleri tanımlar.
- Oyun balansı ve genişleyebilir içerik, veri tanımları üzerinden ilerler.
- Sunum katmanı iş kurallarını barındırmaz.
