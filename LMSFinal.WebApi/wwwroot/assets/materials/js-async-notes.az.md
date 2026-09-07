# Asinxron JavaScript — qeydlər

## Event loop bir abzasda

JavaScript tək axınlıdır (single-threaded). `setTimeout`, `fetch` və event
handler-lər "paralel" şəkildə işləmir — onlar bir tapşırığı növbəyə əlavə edir
və engine yalnız cari kodun icrası tamamlandıqdan sonra həmin tapşırığı icra edir.

Buna görə uzun bir sinxron dövr bütün səhifəni, animasiyaları və klikləri
dondura bilər.

## Ən çox qarışdırılan üç məsələ

**1. `forEach` daxilində `await` işlətmək gözlənilən nəticəni vermir.**

```javascript
// Heç nə gözləmir: forEach promise-ləri başa düşmür.
ids.forEach(async (id) => { await save(id); });
console.log('done');   // İLK olaraq çap olunur

// Ardıcıldır və nəticəsi proqnozlaşdırılandır.
for (const id of ids) {
    await save(id);
}

// Ardıcıllıq vacib deyilsə, paralel şəkildə icra etmək olar.
await Promise.all(ids.map((id) => save(id)));
2. fetch 404-ü xəta kimi qəbul etmir.

Promise yalnız serverə ümumiyyətlə çatmaq mümkün olmadıqda reject olur.
4xx və 5xx cavabları uğurla alınmış HTTP response hesab olunur.
Ona görə bunu özünüz yoxlamalısınız:

const response = await fetch(url);


if (!response.ok) {
    throw new Error(`HTTP ${response.status}`);
}

3. await olmadan çağırılan async funksiyadakı xəta itə bilər.

Funksiyanı await və .catch() olmadan çağırdıqda unhandled rejection
yarana bilər: console-da xəbərdarlıq görünür, UI-də isə xəta idarə
olunmadıqda səssizlik və boş ekran yarana bilər.

Məşq

Üç request-i paralel şəkildə göndərən və uğursuz olanları nəzərə almadan
ilk uğurlu nəticəni qaytaran funksiya yazın.

İpucu: Promise.any



**Qısa yadda saxla:**


- `forEach + await` ❌ → `for...of` və ya `Promise.all()` ✅
- `fetch(404)` ❌ avtomatik error deyil → `response.ok` yoxla
- `async` + `await` / `.catch()` olmadan çağırış → `unhandled rejection`
- `Promise.all()` → hamısının nəticəsini gözləyir
- `Promise.any()` → **ilk uğurlu nəticəni** qaytarır
- `Promise.allSettled()` → bütün nəticələri, uğurlu və uğursuz, gözləyir