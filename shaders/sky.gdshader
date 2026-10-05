shader_type sky;

// Painted 2004-style skybox: a two-tone gradient, a warm horizon toward the sun, a big soft sun disc
// with a wide halo, and two layers of fbm cloud banks projected on a flat plane above the viewer.
// Not a scattering sim; everything here is a colour decision. Below the horizon the sky fades to the
// fog colour so fogged terrain meets the sky without a visible seam.

uniform vec3 sky_top = vec3(0.15, 0.28, 0.70);          // linear HDR, pre-compensated for the tonemapper (see Atmosphere.Paint)
uniform vec3 sky_horizon = vec3(0.78, 1.05, 1.45);
uniform vec3 fog_color = vec3(0.42, 0.62, 0.92);
uniform vec3 sun_warm = vec3(2.2, 1.4, 0.8);           // horizon tint on the sun side
uniform vec3 sun_color : source_color = vec3(1.0, 0.96, 0.86);
uniform float sky_curve = 1.0;            // >1 keeps more of the sky in the lighter horizon tone
uniform float horizon_warmth = 0.045;
uniform float sun_disc_deg = 3.0;         // angular radius of the disc
uniform float sun_disc_soft_deg = 1.6;    // edge softness beyond the radius
uniform float sun_disc_energy = 2.4;
uniform float halo_power = 24.0;          // lower = wider halo
uniform float halo_strength = 0.22;
uniform float glow_power = 140.0;         // tighter inner glow around the disc
uniform float glow_strength = 0.6;

uniform vec3 cloud_light = vec3(3.5, 2.5, 1.3);        // cream tops
uniform vec3 cloud_shadow = vec3(0.26, 0.27, 0.43);    // grey-violet undersides
uniform float cloud_plane_height = 1400.0;
uniform float cloud_fade_start = 0.03;    // EYEDIR.y below which clouds dissolve into the horizon
uniform float cloud_fade_end = 0.22;
// Layer A: big low banks. Layer B: higher, smaller, thinner puffs.
uniform float cloud_a_scale = 0.00022;
uniform float cloud_a_coverage = 0.56;    // higher = fewer clouds
uniform float cloud_a_softness = 0.14;
uniform vec2 cloud_a_drift = vec2(1.6, 0.5);  // metres per second on the cloud plane
uniform float cloud_b_scale = 0.00055;
uniform float cloud_b_coverage = 0.62;
uniform float cloud_b_softness = 0.12;
uniform vec2 cloud_b_drift = vec2(2.6, -1.0);
uniform float cloud_opacity = 0.92;
uniform float star_strength = 0.0;        // painted star field for night palettes (0 = none)
uniform int debug_ramp = 0;               // 1: output linear grey = EYEDIR.y * 3 (tonemapper calibration aid)

float hash21(vec2 p) {
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

float value_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);
	float a = hash21(i);
	float b = hash21(i + vec2(1.0, 0.0));
	float c = hash21(i + vec2(0.0, 1.0));
	float d = hash21(i + vec2(1.0, 1.0));
	return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

// Five octaves is enough for soft blobby banks; more would read as detail, which is not the look.
float fbm(vec2 p) {
	float v = 0.0;
	float amp = 0.5;
	mat2 rot = mat2(vec2(0.8, 0.6), vec2(-0.6, 0.8));
	for (int i = 0; i < 5; i++) {
		v += amp * value_noise(p);
		p = rot * p * 2.03 + vec2(17.3, 9.1);
		amp *= 0.5;
	}
	return v;
}

// Returns (alpha, lightness) for one cloud layer at plane position p.
vec2 cloud_layer(vec2 p, float scale, float coverage, float softness, vec2 sun_offset) {
	float d = fbm(p * scale);
	// Second, slightly displaced sample puts a lit rim on the sun side and shade away from it.
	float d_sun = fbm((p + sun_offset) * scale);
	float alpha = smoothstep(coverage, coverage + softness, d);
	// Thick cores go grey-violet; thin edges and the sun-facing side stay cream.
	float thickness = smoothstep(coverage + softness * 0.5, coverage + softness * 2.2, d);
	float lit = clamp(0.5 + (d_sun - d) * 6.0, 0.0, 1.0);
	float light = clamp(1.0 - thickness * 0.7 + (lit - 0.5) * 0.6, 0.0, 1.0);
	return vec2(alpha, light);
}

void sky() {
	vec3 dir = EYEDIR;
	float y = dir.y;
	float h = clamp(y, 0.0, 1.0);

	// 1. Gradient.
	vec3 col = mix(sky_horizon, sky_top, pow(h, sky_curve));

	// 2. Warm the horizon on the sun side.
	vec3 sun_dir = LIGHT0_DIRECTION;
	vec2 flat_dir = normalize(dir.xz + vec2(1e-5, 0.0));
	vec2 flat_sun = normalize(sun_dir.xz + vec2(1e-5, 0.0));
	float side = max(dot(flat_dir, flat_sun), 0.0);
	float low = pow(1.0 - h, 3.0);
	col = mix(col, sun_warm, side * side * low * horizon_warmth);

	// 3. Sun disc + halos.
	float cos_a = dot(dir, sun_dir);
	float ang = degrees(acos(clamp(cos_a, -1.0, 1.0)));
	float disc = 1.0 - smoothstep(sun_disc_deg, sun_disc_deg + sun_disc_soft_deg, ang);
	float halo = pow(max(cos_a, 0.0), halo_power) * halo_strength;
	float glow = pow(max(cos_a, 0.0), glow_power) * glow_strength;
	vec3 sun_total = sun_color * (disc * sun_disc_energy + glow + halo);

	// 4. Clouds on a flat plane above the viewer.
	float cloud_a_alpha = 0.0;
	vec3 cloud_col = vec3(0.0);
	if (y > 0.002 && cloud_opacity > 0.0) {
		vec2 plane = dir.xz / max(y, 0.002) * cloud_plane_height;
		plane += POSITION.xz * 0.6;  // slight parallax when the camera moves, keeps the sky from feeling glued on
		// Sun offset on the plane: toward the sun's azimuth, scaled so the lit rim is a few hundred metres wide.
		vec2 sun_off = flat_sun * 420.0;
		vec2 a = cloud_layer(plane + cloud_a_drift * TIME, cloud_a_scale, cloud_a_coverage, cloud_a_softness, sun_off);
		vec2 b = cloud_layer(plane * 1.0 + vec2(5000.0, -3000.0) + cloud_b_drift * TIME, cloud_b_scale, cloud_b_coverage, cloud_b_softness, sun_off * 0.6);
		float fade = smoothstep(cloud_fade_start, cloud_fade_end, y);
		// Thin high layer sits behind the low banks.
		vec3 col_b = mix(cloud_shadow, cloud_light, b.y);
		vec3 col_a = mix(cloud_shadow, cloud_light, a.y);
		float alpha_b = b.x * 0.55 * fade * cloud_opacity;
		float alpha_a = a.x * fade * cloud_opacity;
		// Composite B then A over the sky, keep a combined alpha so the sun shows through thin cloud only.
		cloud_col = mix(col_b, col_a, alpha_a / max(alpha_a + alpha_b * (1.0 - alpha_a), 1e-4));
		cloud_a_alpha = alpha_a + alpha_b * (1.0 - alpha_a);
		// Clouds near the horizon take on the sky/fog colour, like distant painted banks.
		float far_haze = 1.0 - smoothstep(0.03, 0.35, y);
		cloud_col = mix(cloud_col, mix(sky_horizon, fog_color, 0.5), far_haze * 0.55);
	}

	// Stars: a sparse hash field on the sky dome, fading toward the horizon and hidden by cloud.
	if (star_strength > 0.0 && y > 0.0) {
		vec2 sp = dir.xz / (y + 0.35) * 260.0;
		vec2 cellId = floor(sp);
		float hs = hash21(cellId);
		vec2 centre = cellId + 0.5 + (vec2(hash21(cellId + 7.1), hash21(cellId + 3.3)) - 0.5) * 0.8;
		float dist = length(sp - centre);
		float star = (1.0 - smoothstep(0.0, 0.12 + hs * 0.1, dist)) * step(0.93, hs);
		float twinkle = 0.7 + 0.3 * sin(TIME * (1.0 + hs * 3.0) + hs * 40.0);
		col += vec3(0.9, 0.95, 1.0) * star * twinkle * star_strength * smoothstep(0.02, 0.3, y) * 3.0;
	}

	// Sun disc behind clouds, halo in front (it reads as glow through the cloud).
	col = mix(col, cloud_col, cloud_a_alpha);
	col += sun_total * (1.0 - cloud_a_alpha * 0.85);

	// 5. Horizon band and below-horizon: fade into the fog colour.
	// The sky at the rim line stays a little lighter than fully fogged ground, so far ridges read as darker silhouettes.
	float horizon_blend = 1.0 - smoothstep(0.0, 0.18, y);
	col = mix(col, mix(sky_horizon, fog_color, 0.3), horizon_blend * 0.7);
	if (y < 0.0) {
		float below = smoothstep(0.0, -0.35, y);
		col = mix(fog_color, fog_color * 0.9, below);
	}

	COLOR = debug_ramp == 1 ? vec3(max(y, 0.0) * 3.0) : col;
}
