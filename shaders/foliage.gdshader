shader_type spatial;
// Leaves, grass, flowers: alpha-cut painted cards. Matte on purpose (no specular, roughness 1) so canopies
// read as flat painted masses; a touch of backlight keeps thin leaves from going black against the sky.
render_mode cull_disabled, diffuse_lambert, specular_disabled, depth_draw_opaque;

uniform sampler2D albedo_tex : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform vec4 tint : source_color = vec4(1.0);
uniform float alpha_scissor : hint_range(0.0, 1.0) = 0.4;
uniform float backlight_strength : hint_range(0.0, 1.0) = 0.22;
uniform float saturation : hint_range(0.0, 1.5) = 1.0;      // <1 mutes neon kit colours
// Recolour: 1 = ignore the texture's hue and paint its luminance (relative to recolor_ref) with the tint.
// Used for the kit's red leaf sheet so the same bush/tree meshes can be green.
uniform float recolor : hint_range(0.0, 1.0) = 0.0;
uniform float recolor_ref = 0.26;
// Wind: sine over world xz + time, scaled by how high the vertex sits above the model origin so trunks stay put.
uniform float sway_amplitude = 0.1;      // metres at sway_height and above
uniform float sway_height = 6.0;         // vertex height (model units) that gets full amplitude
uniform float sway_speed = 1.3;

void vertex() {
	vec3 wpos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	float h = clamp(VERTEX.y / max(sway_height, 0.01), 0.0, 1.0);
	float phase = wpos.x * 0.21 + wpos.z * 0.17 + TIME * sway_speed;
	float s = sin(phase) * 0.7 + sin(phase * 2.3 + 1.7) * 0.3;
	// Mostly along one world axis with a little cross-motion so stands do not all nod in lockstep.
	VERTEX.x += s * sway_amplitude * h * h;
	VERTEX.z += sin(phase * 0.6 + wpos.x * 0.05) * sway_amplitude * 0.5 * h * h;
}

void fragment() {
	vec4 c = texture(albedo_tex, UV);
	float lum = dot(c.rgb, vec3(0.3, 0.59, 0.11));
	vec3 col = mix(c.rgb, vec3(lum / max(recolor_ref, 0.01)), recolor);
	col = mix(vec3(dot(col, vec3(0.3, 0.59, 0.11))), col, saturation);
	// COLOR carries the kit's baked vertex AO (grass blades, stems); leaves are flat white.
	ALBEDO = col * tint.rgb * COLOR.rgb;
	ALPHA = c.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_scissor;
	ROUGHNESS = 1.0;
	SPECULAR = 0.0;
	METALLIC = 0.0;
	BACKLIGHT = vec3(backlight_strength);
}
