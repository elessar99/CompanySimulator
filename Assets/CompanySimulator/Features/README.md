# Features

Bu klasör oyun sistemlerini iş alanlarına göre ayırır.

## Kurallar
- Her ana sistem kendi modül klasöründe yaşar.
- Modüller olabildiğince düşük bağımlılıkla çalışmalıdır.
- Ortak ihtiyaçlar önce `Shared`, sonra gerçekten temel ise `Core` içine alınmalıdır.
- Bir modülün görsel tarafı gerekiyorsa mümkünse `Presentation` altında tutulmalıdır.

Bu yaklaşım, yeni sistem eklerken mevcut klasörleri bozmadan ilerlemeyi kolaylaştırır.
