# Finance

Bu modül oyunun ekonomi omurgasını barındırır.

## Amaç
- Para akışını tek merkezde yönetmek
- Proje gelir/gider hesaplarını veri odaklı tutmak
- Sektör, çalışan ve yatırım sistemlerinin aynı hesaplama katmanını kullanmasını sağlamak

## İlk Yapı
- `Definitions`: global ekonomi ayarları, proje çalıştırma tanımları ve setup assetleri
- `Models`: hesaplama girdileri, çıktıları ve ledger kayıtları
- `Services`: saf hesaplayıcılar ve para yönetimi servisleri
- `Components`: sahnede çalışan yönetici bileşenleri

## Hedef Kullanım
1. İçerik ekipleri `ScriptableObject` tanımlarını oluşturur.
2. Runtime tarafı bu tanımları request modelleri ile hesaplayıcılara verir.
3. Sonuçlar ledger üzerinden şirkete uygulanır.

## İlk Akış
1. `EconomySetupDefinition` ile başlangıç sermayesi ve balans asseti bağlanır.
2. `ProjectExecutionDefinition` ile ilk iş paketleri tanımlanır.
3. `EconomyManager` sahnede bu setup üzerinden ekonomiyi başlatır.
4. İstenirse editör menüsünden örnek ekonomi içerikleri üretilebilir.
