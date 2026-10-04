shader_type spatial;
render_mode cull_back, depth_draw_opaque, diffuse_lambert, specular_schlick_ggx;

// Painted calm water for lakes and rivers. Colour is decided by how deep the water is under the
// pixel (depth-buffer thickness): shallow is lighter and more see-through with a soft pale shoreline
// band, deep is the palette's dark tone. The surface only wobbles through the normal (two scrolling
// value-noise layers), so the silhouette stays flat and calm. No refraction, no reflections: a mild
// fresnel toward the sky colour and a small soft sun highlight are all the "shine" the look wants.

uniform vec3 color_shallow : source_color = vec3(0.30, 0.48, 0.55);
uniform vec3 color_deep : source_color = vec3(0.16, 0.30, 0.38);
uniform vec3 color_shore : source_color = vec3(0.62, 0.74, 0.74);
uniform vec3 sky_color : source_color = vec3(0.70, 0.80, 0.86);
uniform float depth_fade = 4.0;        // metres of water thickness for the full deep colour
uniform float shore_band = 2.0;       // metres: pale band width at the water's edge
uniform float alpha_base = 0.82;
uniform float alpha_shallow = 0.35;
uniform float fresnel_strength = 0.3;
uniform float roughness = 0.3;
uniform float specular = 0.35;
uniform float wobble_strength = 0.08;  // normal tilt; small on purpose
uniform float wobble_scale = 0.22;     // repeats per metre (~4-5 m ripple cells)
uniform float wobble_speed = 0.18;
uniform float ripple_tint = 0.12;      // albedo lightness swing from the wobble field
uniform float streak_strength = 0.2;  // soft pale streaks on the ripple crests: the painted "animated water" read
uniform vec2 flow = vec2(0.0, 0.0);    // UV units per second; rivers scroll along +x (downstream)
uniform sampler2D DEPTH_TEXTURE : hint_depth_texture, filter_linear;

varying vec3 v_wpos;

float hash21(vec2 p) {
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

float value_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash21(i), hash21(i + vec2(1.0, 0.0)), u.x),
	           mix(hash21(i + vec2(0.0, 1.0)), hash21(i + vec2(1.0, 1.0)), u.x), u.y);
}

// Height of the wobble field: two layers moving in different directions so it never reads as a grid.
float wobble(vec2 uv) {
	float t = TIME * wobble_speed;
	float a = value_noise(uv * wobble_scale + vec2(t * 0.9, t * 0.4));
	float b = value_noise(uv * wobble_scale * 2.3 + vec2(-t * 0.6, t * 1.1) + 31.7);
	return a * 0.65 + b * 0.35;
}

void vertex() {
	v_wpos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	// UV.x is metres along the flow for rivers, world metres for lakes; both scroll with `flow`.
	vec2 uv = UV - flow * TIME;

	// Water thickness under this pixel from the depth buffer (view-space distance difference).
	float depth_raw = texture(DEPTH_TEXTURE, SCREEN_UV).r;
	vec3 ndc = vec3(SCREEN_UV * 2.0 - 1.0, depth_raw);
	vec4 view_pos = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	view_pos.xyz /= view_pos.w;
	float scene_dist = -view_pos.z;
	float water_dist = -VERTEX.z;
	// Along-ray thickness, scaled toward vertical thickness so grazing views do not go black.
	float thickness = max(scene_dist - water_dist, 0.0) * max(abs(VIEW.y), 0.35);

	float deep = 1.0 - exp(-thickness / depth_fade);
	float shore = 1.0 - smoothstep(0.0, shore_band, thickness);
	vec3 col = mix(color_shallow, color_deep, deep);
	col = mix(col, color_shore, shore * 0.85);

	// Normal wobble by finite differences; converted into view space for lighting.
	float e = 0.35;
	float hc = wobble(uv);
	float hx = wobble(uv + vec2(e, 0.0));
	float hz = wobble(uv + vec2(0.0, e));
	vec3 wn = normalize(vec3((hc - hx) / e * wobble_strength, 1.0, (hc - hz) / e * wobble_strength));
	vec3 vn = normalize((VIEW_MATRIX * vec4(wn, 0.0)).xyz);
	NORMAL = vn;

	// Painted ripples: the same field tints the albedo a little and lays soft pale streaks on the crests,
	// so calm water still shows movement under flat light (the old animated-texture look, not a reflection).
	col *= 1.0 + (hc - 0.5) * 2.0 * ripple_tint;
	float streak = smoothstep(0.54, 0.74, hc);
	col = mix(col, mix(col, sky_color, 0.6), streak * streak_strength);

	// Mild fresnel toward the sky, never a mirror.
	float fres = pow(1.0 - clamp(dot(VIEW, vn), 0.0, 1.0), 4.0);
	col = mix(col, sky_color, fres * fresnel_strength);

	ALBEDO = col;
	ALPHA = mix(alpha_shallow, alpha_base, smoothstep(0.0, shore_band * 2.0, thickness));
	ROUGHNESS = roughness;
	SPECULAR = specular;
	METALLIC = 0.0;
}
