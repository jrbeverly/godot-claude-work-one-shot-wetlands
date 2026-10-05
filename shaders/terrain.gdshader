shader_type spatial;
render_mode cull_back, diffuse_lambert, specular_disabled;

// Five hand-painted ground layers blended by vertex weights (COLOR.rgba = grass, forest floor, road, rock;
// CUSTOM0.a = dry meadow), snow by altitude, a baked hue tint in CUSTOM0.rgb and water depth in CUSTOM1.r.
// No normal/roughness maps on purpose.
uniform sampler2D tex_grass : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D tex_forest : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D tex_road : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D tex_rock : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D tex_snow : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D tex_meadow : source_color, filter_linear_mipmap_anisotropic, repeat_enable;

uniform float tile_grass = 7.0;   // metres per texture repeat
uniform float tile_forest = 8.0;
uniform float tile_road = 5.0;
uniform float tile_rock = 11.0;
uniform float tile_snow = 6.0;
uniform float tile_meadow = 6.5;
uniform float macro_scale = 0.0075;        // breaks tiling with a second, large-scale sample
uniform float macro_strength = 0.45;
uniform float snow_line = 115.0;
uniform float snow_blend = 22.0;
uniform float blend_sharpness = 2.4;     // >1 = crisper painted layer edges
// Per-layer tints let the zone palette steer the painted tiles without repainting them.
uniform vec3 tint_grass : source_color = vec3(1.0);
uniform vec3 tint_forest : source_color = vec3(1.0);
uniform vec3 tint_road : source_color = vec3(1.0);
uniform vec3 tint_rock : source_color = vec3(1.0);
uniform vec3 tint_snow : source_color = vec3(1.0);
uniform vec3 tint_meadow : source_color = vec3(1.0);
uniform vec3 wet_tint : source_color = vec3(0.43, 0.39, 0.32);   // submerged peat: darker and warmer

varying vec4 v_splat;
varying vec4 v_custom;
varying float v_wet;
varying vec3 v_wpos;
varying vec3 v_wnormal;

void vertex() {
	v_splat = COLOR;
	v_custom = CUSTOM0;
	v_wet = CUSTOM1.r;
	v_wpos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_wnormal = normalize(mat3(MODEL_MATRIX) * NORMAL);
}

vec3 sample_layer(sampler2D tex, vec2 uv, float tile) {
	vec3 a = texture(tex, uv / tile).rgb;
	vec3 b = texture(tex, uv * macro_scale + vec2(0.37, 0.11)).rgb;
	return mix(a, a * (b / max(vec3(0.001), vec3(dot(b, vec3(0.333))))), macro_strength);
}

vec3 sample_triplanar(sampler2D tex, vec3 p, vec3 n, float tile) {
	vec3 w = pow(abs(n), vec3(4.0));
	w /= (w.x + w.y + w.z);
	vec3 x = texture(tex, p.zy / tile).rgb;
	vec3 y = texture(tex, p.xz / tile).rgb;
	vec3 z = texture(tex, p.xy / tile).rgb;
	return x * w.x + y * w.y + z * w.z;
}

void fragment() {
	vec2 uv = v_wpos.xz;
	vec4 w = pow(max(v_splat, vec4(0.0)), vec4(blend_sharpness));
	float wm = pow(max(v_custom.a, 0.0), blend_sharpness);
	float wsum = max(w.r + w.g + w.b + w.a + wm, 1e-4);
	w /= wsum; wm /= wsum;

	vec3 grass = sample_layer(tex_grass, uv, tile_grass) * tint_grass;
	vec3 forest = sample_layer(tex_forest, uv, tile_forest) * tint_forest;
	vec3 road = sample_layer(tex_road, uv, tile_road) * tint_road;
	vec3 rock = sample_triplanar(tex_rock, v_wpos, v_wnormal, tile_rock) * tint_rock;
	vec3 meadow = sample_layer(tex_meadow, uv, tile_meadow) * tint_meadow;

	vec3 albedo = grass * w.r + forest * w.g + road * w.b + rock * w.a + meadow * wm;

	// Snow settles on flatter ground above the snow line; rock faces stay darker.
	float flatness = smoothstep(0.55, 0.9, v_wnormal.y);
	float snow_noise = (texture(tex_snow, uv * 0.01).r - 0.5) * snow_blend * 1.2;
	float snow = smoothstep(snow_line - snow_blend, snow_line + snow_blend, v_wpos.y + snow_noise) * mix(0.85, 1.0, flatness);   // steep faces pale too: this layer doubles as the high wall
	// Triplanar: the pale-wall layer is sampled on near-vertical faces, so no top-down streaking.
	vec3 snow_col = sample_triplanar(tex_snow, v_wpos, v_wnormal, tile_snow) * tint_snow;
	// The wall base sits in shade and the top catches the light (the dawn reference's cliff).
	snow_col *= mix(0.72, 1.12, smoothstep(10.0, 140.0, v_wpos.y));
	albedo = mix(albedo, snow_col, snow);

	// Baked tint (vertex hue drift) and darker, cooler ground under water.
	albedo *= v_custom.rgb;
	float wet = clamp(v_wet / 0.5, 0.0, 1.0);
	albedo = mix(albedo, albedo * wet_tint, wet);

	ALBEDO = albedo;
	ROUGHNESS = 1.0;
	SPECULAR = 0.0;
}
