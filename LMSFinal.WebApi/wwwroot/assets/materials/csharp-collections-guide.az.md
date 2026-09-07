# Kolleksiya seçimi — C# əsasları

// Kolleksiyanı vərdişə görə deyil, ondan ən çox nə üçün istifadə
// etdiyinizə görə seçin.

## Qısa məlumat

| Tapşırıq | Tip | Axtarış | Səbəb |
|---|---|---|---|
| Sadə sıralanmış siyahı | `List<T>` | O(n) | İndeksləmə, sıralama, minimal əlavə xərc |
| "X mövcuddurmu?" yoxlamaq | `HashSet<T>` | O(1) | Sətirləri bir-bir yoxlamaq əvəzinə hashing istifadə edir |
| Açar vasitəsilə dəyər tapmaq | `Dictionary<K,V>` | O(1) | Eyni prinsipdir, lakin əlavə dəyər (value) saxlayır |
| Məlumatı yalnız oxumaq üçün təqdim etmək | `IReadOnlyList<T>` | — | Tip özü bildirir ki, dəyişiklik edilməməlidir |
| İş növbəsi | `Queue<T>` | — | Əl ilə indeksləmə etmədən FIFO prinsipi ilə işləyir |

## Ən çox problem yaradan səhv

```csharp
// n element üzərindəki dövrdə O(n) əməliyyatı = O(n²).
// 10 000 qeyd olduqda bu, milyonlarla müqayisə deməkdir.
foreach (var id in ids)
{
    if (allLessons.Any(l => l.Id == id)) { /* ... */ }
}

// Əvvəlcə Set yaratmaq üçün bir dəfə keçid edilir,
// sonra hər yoxlama O(1) vaxt aparır.
var lessonIds = allLessons.Select(l => l.Id).ToHashSet();

foreach (var id in ids)
{
    if (lessonIds.Contains(id)) { /* ... */ }
}
Kolleksiya üzərində iterasiya zamanı onu dəyişdirmək

foreach dövrünün daxilində kolleksiyaya element əlavə etsəniz və ya
element silsəniz, InvalidOperationException yaranır.

Dəyişiklikləri əvvəlcə ayrıca kolleksiyada toplayın:

var toRemove = items.Where(i => i.IsExpired).ToList();
foreach (var item in toRemove)
{
    items.Remove(item);
}
LINQ tənbəl (lazy) işləyir

Where və Select nəticə üzərində iterasiya edilənə qədər heç bir hesablama
aparmır.

Eyni query üzərində iki foreach dövrü işlətmək mənbə üzərində iki dəfə
keçid etmək deməkdir.

Əgər nəticəyə bir neçə dəfə ehtiyacınız olacaqsa, onu əvvəlcədən
materiallaşdırın: .ToList().



**Əsas yadda saxlanmalı hissə:** `List` — siyahı, `HashSet` — sürətli `Contains`, `Dictionary` — `k