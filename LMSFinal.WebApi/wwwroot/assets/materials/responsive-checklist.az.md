# Responsive design yoxlama siyahısı

Səhifəni hazır hesab etməzdən əvvəl bu siyahını yoxlayın.

## En (Width)

- [ ] 320px genişlikdə üfüqi scroll yaranmır
- [ ] `max-width: 100%` kifayət etdiyi halda `width` üçün pixel dəyərlərindən istifadə edilmir
- [ ] Cədvəllər və kod blokları səhifənin özündə deyil, yalnız öz daxilində scroll olunur
- [ ] Uzun sözlər və URL-lər konteynerdən kənara çıxmır — `overflow-wrap: anywhere`

## Breakpoint-lər

- [ ] Breakpoint-lər telefon modellərinin ölçülərinə görə deyil,
      layout-un pozulduğu yerlərə görə təyin olunur
- [ ] Əvvəlcə Mobile First yanaşması, sonra `min-width` istifadə olunur —
      beləliklə daha az override lazım olur
- [ ] Breakpoint-lər arasındakı ölçülər də yoxlanılıb:
      məsələn, 375px-dən çox 900px-də layout daha tez pozula bilər

## Touch

- [ ] Düymələr və linklər ən azı 44×44px ölçüsündədir
- [ ] Bir-birinə yaxın elementlər arasında kifayət qədər boşluq var —
      əks halda istifadəçi səhv elementə toxuna bilər
- [ ] Vacib heç bir funksiya yalnız `:hover` arxasında gizlənmir —
      çünki touch ekranlarda `:hover` yoxdur

## Mətn (Text)

- [ ] Əsas mətn 16px-dən kiçik deyil:
      bundan kiçik ölçüdə Safari fokus zamanı səhifəni zoom edə bilər
- [ ] Sətir uzunluğu 45–75 simvol arasındadır və bundan geniş deyil
- [ ] Əsas mətn üçün line-height ən azı 1.5-dir

## Şəkillər (Images)

- [ ] `<img>` elementində `width` və `height` göstərilib —
      əks halda səhifə yüklənərkən layout sıçraya bilər
- [ ] Ekranın aşağı hissəsində yerləşən ağır şəkillər üçün
      `loading="lazy"` istifadə olunur

## Sonda

- [ ] Brauzerin 200% zoom səviyyəsində yoxlanılıb
- [ ] `prefers-reduced-motion: reduce` ilə yoxlanılıb
- [ ] Telefonda landscape (üfüqi) rejimdə yoxlanılıb