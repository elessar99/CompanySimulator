# Sectors

Bu modül sektör ekranı ve sektör verilerini yönetir.

## Ana Parçalar
- `Definitions/SectorDefinition`: tekil sektör tanımı
- `Definitions/SectorCatalogDefinition`: panelde gösterilecek sektör ve iş listesi
- `Components/SectorManager`: tamamlanan iş sayılarını takip eder

## İlk Kullanım
1. `SectorCatalogDefinition` oluştur veya örnek üretici menüsünü kullan.
2. Sahneye `SectorManager` ekle.
3. `catalog` alanına sektör kataloğunu bağla.
4. `economyManager` alanına sahnedeki ekonomi yöneticisini bağla.
5. UI için `SectorPanelUI` bileşenini ekle.
