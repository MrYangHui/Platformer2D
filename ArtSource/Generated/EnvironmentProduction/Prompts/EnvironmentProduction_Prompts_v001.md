# Environment Production Prompts v001

Date: 2026-07-17

Mode: built-in `image_gen`

Reference role for every prompt: `ArtSource/Generated/ArtDirection/FrozenExclusionZone_v001.png` was used only as a style, palette, material, and mood reference. It was not an edit target, and no exact layout was requested or copied.

## Background_Sky_v001

```text
Use case: stylized-concept
Asset type: 2D side-scrolling game parallax background layer — Background_Sky
Input images: Image 1 is a STYLE AND MOOD REFERENCE ONLY; do not copy its exact layout, characters, platforms, vehicles, or structures.
Primary request: create an original overcast winter sky for the first level of a frozen futuristic city exclusion zone.
Scene/backdrop: broad layers of pale steel-blue storm clouds, soft distant snow haze, faint cold atmospheric gradient toward the horizon; horizontally extensible with subtle edge-to-edge continuity.
Style/medium: production-ready non-pixel 2D game background painting; anime-influenced simplified realism; credible scale and materials; soft painterly shapes, restrained detail.
Composition/framing: very wide landscape, camera level side-view; sky-only layer with no buildings, terrain, characters, platforms, horizon landmarks, text, or foreground objects; avoid a single obvious focal cloud so repetition/cropping is easy.
Lighting/mood: cold cloudy daylight, quiet abandoned atmosphere, diffuse light, low contrast.
Color palette: low-saturation white, cool gray, blue-gray, pale cyan; no warm focal colors and no neon.
Constraints: original asset only; no logos, trademarks, text, UI, borders, watermark; no high-frequency texture; no photographic noise; no sun disk; no dramatic lightning; no vignette.
```

## Background_Far_v001

```text
Use case: stylized-concept
Asset type: 2D side-scrolling game parallax cutout — Background_Far
Input images: Image 1 is a STYLE, PALETTE, AND MOOD REFERENCE ONLY; create a new original skyline and do not copy its exact composition.
Primary request: an original distant skyline silhouette for a frozen futuristic city exclusion zone, built as a separate parallax layer.
Scene/backdrop: a perfectly flat solid #ff00ff chroma-key background for local background removal; the background must be one uniform color with no shadows, gradients, texture, reflections, floor plane, lighting variation, or magenta haze.
Subject: a continuous bottom-aligned distant city skyline spanning the full width: simplified high-rise blocks, two restrained futuristic lockdown towers, sparse antennas, snow-softened rooftops, and distant mist shapes contained inside the subject; no foreground terrain, no platforms, no characters, no vehicles.
Style/medium: production-ready non-pixel 2D game background painting; anime-influenced simplified realism; credible monumental scale; broad readable shapes and low-frequency detail.
Composition/framing: very wide landscape, strict side-view; all skyline geometry anchored to the bottom edge, generous empty chroma-key area above, no objects touching the top; left and right ends should be low and visually compatible for overlapping/repeating.
Lighting/mood: overcast cold daylight, heavily atmospheric and desaturated; far-layer contrast lower than gameplay foreground.
Color palette: pale blue-gray, cool gray, muted icy white, very restrained cyan accents; do not use #ff00ff anywhere in the subject.
Constraints: crisp silhouette, no cast/contact shadow outside the subject, no transparent-looking glass edges, no cyan bloom spill, no text, logos, trademarks, UI, borders, or watermark; avoid tiny windows, dense linework, neon, warm focal colors, and high-frequency decoration.
```

## Background_Mid_v001

```text
Use case: stylized-concept
Asset type: 2D side-scrolling game parallax cutout — Background_Mid
Input images: Image 1 is a STYLE, PALETTE, AND MOOD REFERENCE ONLY; create a new original layer and do not copy its exact composition.
Primary request: an original mid-distance frozen urban ruin layer for a futuristic city exclusion zone.
Scene/backdrop: a perfectly flat solid #ff00ff chroma-key background for local background removal; one uniform color only, with no shadows, gradients, texture, reflections, floor plane, lighting variation, or magenta haze.
Subject: a continuous bottom-aligned side-view strip of damaged mid-rise buildings, broken elevated-road segments, sparse quarantine barriers, one half-buried transit-bus silhouette, snowbanks and frost; credible urban scale; no gameplay platforms and no walkable top surfaces presented as foreground terrain.
Style/medium: production-ready non-pixel 2D game background painting; anime-influenced simplified realism; broad shapes, believable worn concrete and painted steel, restrained snow detail.
Composition/framing: very wide landscape, strict orthographic-like side-view; subject occupies only the lower 45 percent; generous empty chroma-key space above; all objects anchored into one coherent lower silhouette; ends kept low for overlap/repetition.
Lighting/mood: cold diffuse cloudy daylight; atmospheric mid-layer contrast, clearly softer and less saturated than playable platforms.
Color palette: muted blue-gray, cool concrete gray, icy white, pale cyan; tiny restrained amber hazard paint is allowed but must not become a focal point; do not use #ff00ff in the subject.
Constraints: crisp outer silhouette, no cast/contact shadow outside the subject, no transparent glass, no cyan bloom spill, no characters, collectibles, weapons, text, logos, trademarks, UI, borders, or watermark; avoid dense cables, tiny windows, high-frequency rubble, neon, and strong edge highlights.
```

## Background_Near_v001

```text
Use case: stylized-concept
Asset type: 2D side-scrolling game parallax cutout — Background_Near
Input images: Image 1 is a STYLE, PALETTE, AND MOOD REFERENCE ONLY; create a new original near-foreground decorative layer and do not copy its exact layout.
Primary request: an original sparse near-background industrial infrastructure strip for a frozen futuristic city exclusion zone, designed to sit behind the playable character and platforms.
Scene/backdrop: a perfectly flat solid #ff00ff chroma-key background for local background removal; one uniform color only, no shadows, gradients, texture, reflections, floor plane, lighting variation, or magenta haze.
Subject: sparse bottom-anchored clusters of dark blue-gray steel trusses, a few thick insulated pipes, occasional short safety rail sections, broken utility braces and small snow caps; large open gaps between clusters; all details clearly decorative and not shaped like continuous walkable gameplay platforms.
Style/medium: production-ready non-pixel 2D game background painting; anime-influenced simplified industrial realism; credible material wear, broad clean forms.
Composition/framing: very wide landscape, strict side-view; objects occupy mostly the bottom 30 percent, with a few narrow braces rising no higher than 50 percent; maintain extensive empty chroma-key space so the player silhouette stays readable; no central focal object; ends suitable for overlap.
Lighting/mood: cold diffuse overcast daylight; slightly darker than Mid layer but low contrast and no bright highlights.
Color palette: deep desaturated navy-gray, cool steel blue, icy white snow, tiny restrained pale-cyan indicator accents; do not use #ff00ff anywhere in the subject.
Constraints: crisp silhouette, no cast/contact shadow outside subject, no glass/translucency, no cyan glow bloom, no characters, platforms, ladders, text, logos, trademarks, UI, borders, or watermark; avoid dense wire tangles, thin noisy linework, bright warning stripes, and high-frequency decoration.
```

## Platform_Short_v001

```text
Use case: stylized-concept
Asset type: 2D side-scrolling game terrain sprite — short one-way platform skin
Input images: Image 1 is a STYLE, PALETTE, MATERIAL, AND MOOD REFERENCE ONLY; create a new original platform.
Primary request: a single isolated short modular industrial snow platform for a frozen futuristic city exclusion zone.
Scene/backdrop: perfectly flat solid #ff00ff chroma-key background for local background removal; one uniform color with no shadows, gradients, texture, reflections, floor plane, lighting variation, or magenta haze.
Subject: one complete rectangular platform in exact side elevation, approximately 3:1 width-to-height; bright clean snow cap and frost along the top edge, worn blue-gray painted-steel fascia below, dark structural underside, small restrained amber hazard stripe panel on the front; solid left and right end caps; no supports extending downward beyond the rectangle.
Style/medium: production-ready non-pixel 2D game sprite, anime-influenced simplified industrial realism, clean painted shapes, believable metal and compact snow accumulation.
Composition/framing: centered horizontally and vertically with generous padding; exact orthographic side view, no perspective, no top-face foreshortening, no rotation; platform silhouette fully separated from background.
Lighting/mood: cold diffuse daylight; top snow edge is the highest-contrast gameplay-readable feature; underside darker but not black.
Color palette: icy white, cool gray, desaturated blue-gray, tiny muted amber; do not use #ff00ff in subject; no cyan glow.
Constraints: crisp silhouette, no cast/contact shadow, no floating snow particles, no separate debris, no text, numbers, logos, trademarks, UI, border, or watermark; avoid tiny rivet noise, dense scratches, bright neon, bevel-heavy toy look; platform top must read as flat and safely landable.
```

## Platform_Medium_v001

```text
Use case: stylized-concept
Asset type: 2D side-scrolling game terrain sprite — medium one-way platform skin
Input images: Image 1 is a STYLE, PALETTE, MATERIAL, AND MOOD REFERENCE ONLY; create a new original platform matching the same fictional environment.
Primary request: a single isolated medium modular industrial snow platform for a frozen futuristic city exclusion zone.
Scene/backdrop: perfectly flat solid #ff00ff chroma-key background for local background removal; one uniform color with no shadows, gradients, texture, reflections, floor plane, lighting variation, or magenta haze.
Subject: one complete elongated rectangular platform in exact side elevation, approximately 6:1 width-to-height; straight bright snow-capped top edge with only shallow drift variation, worn blue-gray painted-steel fascia divided into repeatable broad modules, dark structural underside, two small restrained amber hazard stripe panels, solid left and right end caps; no support legs beyond the rectangle.
Style/medium: production-ready non-pixel 2D game sprite, anime-influenced simplified industrial realism, clean painted shapes, believable metal and compact snow.
Composition/framing: platform nearly spans the canvas width with generous uniform padding; exact orthographic side view, no perspective, no visible top face, no rotation; fully separated silhouette.
Lighting/mood: cold diffuse daylight; top snow edge is the highest-contrast gameplay-readable feature; underside darker but not black.
Color palette: icy white, cool gray, desaturated blue-gray, tiny muted amber; do not use #ff00ff in subject; no cyan glow.
Constraints: coherent medium-length design, crisp silhouette, no cast/contact shadow, no floating snow, no debris, no text, numbers, logos, trademarks, UI, border, or watermark; avoid tiny rivet noise, dense scratches, neon, perspective, bevel-heavy toy look; flat safely landable top.
```

## Platform_Long_v001

```text
Use case: stylized-concept
Asset type: 2D side-scrolling game terrain sprite — long one-way platform skin
Input images: Image 1 is a STYLE, PALETTE, MATERIAL, AND MOOD REFERENCE ONLY; create a new original platform matching the same fictional environment.
Primary request: a single isolated very long modular industrial snow platform for a frozen futuristic city exclusion zone.
Scene/backdrop: perfectly flat solid #ff00ff chroma-key background for local background removal; one uniform color with no shadows, gradients, texture, reflections, floor plane, lighting variation, or magenta haze.
Subject: one complete very long low rectangular platform in exact side elevation, approximately 12:1 width-to-height; perfectly continuous flat bright snow-capped top edge with subtle shallow drift variation, worn blue-gray painted-steel fascia built from consistent repeatable broad modules, dark cross-braced underside, three small restrained amber hazard stripe panels, solid left and right end caps; no support legs beyond the rectangle.
Style/medium: production-ready non-pixel 2D game sprite, anime-influenced simplified industrial realism, clean painted shapes, believable metal and compact snow.
Composition/framing: platform spans almost the entire canvas width and remains low, with generous magenta padding on every side; exact orthographic side view, no perspective, no visible top face, no rotation; fully separated silhouette.
Lighting/mood: cold diffuse daylight; top snow edge is the strongest gameplay-readable feature; underside darker but not black.
Color palette: icy white, cool gray, desaturated blue-gray, tiny muted amber; do not use #ff00ff in subject; no cyan glow.
Constraints: coherent long design, crisp silhouette, no cast/contact shadow, no floating snow, no debris, no text, numbers, logos, trademarks, UI, border, or watermark; avoid tiny rivet noise, dense scratches, neon, perspective, bevel-heavy toy look; flat safely landable top.
```
