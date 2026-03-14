# Company Simulator

Bu klasör oyunun üretim kodu ve içerik mimarisi için ana giriş noktasıdır.

## Hedefler
- Modüler yapı
- İçerik odaklı genişleme
- Kod ile içeriğin ayrılması
- Sektör, çalışan tipi ve yatırım türlerini kolay ekleyebilme

## Ana Klasörler
- `Core`: ortak altyapı, temel soyutlamalar
- `Shared`: birden fazla modülün kullandığı ortak tipler
- `Features`: oyun alanına göre ayrılmış modüller
- `Presentation`: UI ve ofis görünümü gibi sunum katmanı
- `Content`: sahne, prefab ve veri tanımları
- `Tools`: editör araçları
- `Tests`: edit mode ve play mode testleri

## İlk Ekonomi Omurgası
- `Features/Finance`: para akışı, gelir-gider hesabı ve ledger yapısı
- `Content/Definitions/Economy`: global ekonomi balans assetleri
- `Features/Projects`, `Features/Sectors`, `Features/Employees`, `Features/Investments`: ekonomiye veri sağlayan içerik tanımları

Detaylı kararlar için `Docs/Architecture.md` dosyasına bakılabilir.
