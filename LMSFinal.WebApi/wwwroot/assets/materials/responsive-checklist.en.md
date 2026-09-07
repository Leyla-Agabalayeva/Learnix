# Responsive design checklist

Walk through this before calling a page finished.

## Width

- [ ] No horizontal scrolling at 320px
- [ ] No `width` in pixels where `max-width: 100%` would do
- [ ] Tables and code blocks scroll inside themselves, not the page
- [ ] Long words and URLs do not stick out — `overflow-wrap: anywhere`

## Breakpoints

- [ ] Breakpoints sit where the layout BREAKS, not at phone model sizes
- [ ] Mobile first, then `min-width` — fewer overrides that way
- [ ] Widths between breakpoints checked too: 900px breaks more often than 375px

## Touch

- [ ] Buttons and links are at least 44×44px
- [ ] Neighbouring targets have a gap — otherwise people mis-tap
- [ ] Nothing important hides behind `:hover` — touch screens have none

## Text

- [ ] Body text no smaller than 16px: below that Safari zooms the page on focus
- [ ] Line length 45–75 characters, no wider
- [ ] Line height in body copy at least 1.5

## Images

- [ ] `<img>` has `width` and `height` — otherwise the layout jumps on load
- [ ] Heavy images below the fold use `loading="lazy"`

## Finally

- [ ] Checked at 200% browser zoom
- [ ] Checked with `prefers-reduced-motion: reduce`
- [ ] Checked in landscape orientation on a phone
