shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;

uniform float intensity : hint_range(0.0, 1.0) = 0.86;
uniform vec2 tape_resolution = vec2(360.0, 240.0);
uniform float pixelation_strength : hint_range(0.0, 1.0) = 0.82;
uniform float scanline_strength : hint_range(0.0, 1.0) = 0.42;
uniform float noise_strength : hint_range(0.0, 1.0) = 0.55;
uniform float chromatic_aberration : hint_range(0.0, 1.0) = 0.55;
uniform float wobble_strength : hint_range(0.0, 1.0) = 0.48;
uniform float tracking_strength : hint_range(0.0, 1.0) = 0.55;
uniform float random_jitter_strength : hint_range(0.0, 1.0) = 0.28;
uniform float white_line_strength : hint_range(0.0, 1.0) = 0.64;
uniform float white_line_frequency : hint_range(0.0, 1.0) = 0.52;
uniform float damage_frequency : hint_range(0.0, 1.0) = 0.46;
uniform float dropout_strength : hint_range(0.0, 1.0) = 0.22;
uniform float bottom_tear_strength : hint_range(0.0, 1.0) = 0.38;
uniform float glare_strength : hint_range(0.0, 2.0) = 0.3;
uniform float glare_threshold : hint_range(0.2, 2.0) = 0.82;
uniform float glare_spread : hint_range(1.0, 96.0) = 42.0;
uniform float barrel_distortion : hint_range(-0.3, 0.3) = 0.025;
uniform float scene_brightness : hint_range(0.5, 1.8) = 1.08;
uniform float scene_lift : hint_range(0.0, 1.0) = 0.12;
uniform float camera_contrast : hint_range(0.5, 1.5) = 1.04;
uniform float camera_saturation : hint_range(0.0, 1.5) = 0.78;
uniform vec4 tape_tint : source_color = vec4(1.0, 0.88, 0.64, 1.0);
uniform float tint_strength : hint_range(0.0, 1.0) = 0.16;
uniform float vignette_strength : hint_range(0.0, 1.0) = 0.32;
uniform vec2 resolution = vec2(1920.0, 1080.0);

const vec3 LUMA = vec3(0.299, 0.587, 0.114);

float hash(vec2 value) {
	vec3 p3 = fract(vec3(value.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float value_noise(vec2 value) {
	vec2 i = floor(value);
	vec2 f = fract(value);
	f = f * f * (3.0 - 2.0 * f);

	float a = hash(i);
	float b = hash(i + vec2(1.0, 0.0));
	float c = hash(i + vec2(0.0, 1.0));
	float d = hash(i + vec2(1.0, 1.0));

	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

vec3 rgb_to_yiq(vec3 color) {
	return vec3(
		dot(color, LUMA),
		dot(color, vec3(0.596, -0.274, -0.322)),
		dot(color, vec3(0.211, -0.523, 0.312))
	);
}

vec3 yiq_to_rgb(vec3 color) {
	return vec3(
		color.x + 0.956 * color.y + 0.621 * color.z,
		color.x - 0.272 * color.y - 0.647 * color.z,
		color.x - 1.106 * color.y + 1.703 * color.z
	);
}

vec3 sample_tape(vec2 uv) {
	return texture(screen_texture, clamp(uv, vec2(0.001), vec2(0.999))).rgb;
}

vec2 apply_lens(vec2 uv) {
	vec2 centered = uv - vec2(0.5);
	float radius = dot(centered, centered);
	return centered * (1.0 + barrel_distortion * radius * 1.8) + vec2(0.5);
}

void fragment() {
	vec2 original_uv = SCREEN_UV;
	vec2 uv = apply_lens(original_uv);
	float time = TIME;
	float frame = floor(TIME * 30.0);
	vec2 tape_size = max(tape_resolution, vec2(64.0, 64.0));
	vec2 tape_pixel = 1.0 / tape_size;

	float line = floor(uv.y * tape_size.y);
	float frame_bucket = floor(time * 8.0);
	float slow_line_noise = value_noise(vec2(line * 0.022, time * 0.55));
	float fast_line_noise = hash(vec2(line * 0.16, floor(time * 18.0)));
	float tracking_center = fract(0.88 - time * 0.028 + hash(vec2(floor(time * 0.18), 3.0)) * 0.08);
	float tracking_band = exp(-pow((uv.y - tracking_center) / 0.022, 2.0));
	float bottom_tear = smoothstep(0.86, 1.0, uv.y);
	float head_noise = hash(vec2(floor(uv.x * 70.0 + time * 18.0), line));
	float glitch_band = step(1.0 - damage_frequency * 0.08, hash(vec2(floor(uv.y * 18.0), frame_bucket)));
	float frame_jitter = (hash(vec2(frame_bucket, 31.7)) - 0.5) * random_jitter_strength * 0.0035;

	float line_wobble = sin(uv.y * 25.0 + time * 1.7) * 0.0012;
	line_wobble += (slow_line_noise - 0.5) * 0.0032;
	line_wobble += (fast_line_noise - 0.5) * 0.001;
	line_wobble += tracking_band * tracking_strength * 0.006 * (hash(vec2(line, floor(time * 6.0))) * 2.0 - 1.0);
	line_wobble += bottom_tear * (head_noise - 0.5) * 0.012 * bottom_tear_strength;
	line_wobble += glitch_band * (hash(vec2(frame_bucket, line)) - 0.5) * random_jitter_strength * 0.014;
	uv.x += (line_wobble * wobble_strength * intensity) + frame_jitter;
	uv = clamp(uv, vec2(0.001), vec2(0.999));

	vec2 low_uv = (floor(uv * tape_size) + vec2(0.5)) / tape_size;
	vec2 sample_uv = mix(uv, low_uv, pixelation_strength);

	float chroma_step = mix(1.0, 6.5, chromatic_aberration * intensity) * tape_pixel.x;
	vec3 native_color = sample_tape(sample_uv);
	vec3 left_color = sample_tape(sample_uv - vec2(chroma_step * 1.7, 0.0));
	vec3 right_color = sample_tape(sample_uv + vec2(chroma_step * 1.35, 0.0));
	vec3 center_yiq = rgb_to_yiq(native_color);
	float y = center_yiq.x;
	float sharp_y = dot(left_color + right_color, LUMA) - y * 2.0;
	y -= sharp_y * 0.055;

	vec2 chroma = center_yiq.yz * 0.5;
	chroma += rgb_to_yiq(left_color).yz * 0.29;
	chroma += rgb_to_yiq(right_color).yz * 0.21;
	chroma += sin((line * 0.52) + time * 10.0 + uv.x * 18.0) * chromatic_aberration * intensity * 0.003;
	chroma *= mix(0.95, 0.72, intensity);

	vec3 color = mix(native_color, yiq_to_rgb(vec3(y, chroma)), 0.72);

	vec3 glare = vec3(0.0);
	for (int i = 1; i <= 5; i++) {
		float offset = (float(i) * glare_spread) / max(resolution.x, 1.0);
		vec3 left_glare = sample_tape(sample_uv - vec2(offset, 0.0));
		vec3 right_glare = sample_tape(sample_uv + vec2(offset, 0.0));
		glare += max(left_glare - vec3(glare_threshold), vec3(0.0));
		glare += max(right_glare - vec3(glare_threshold), vec3(0.0));
	}
	color += glare * glare_strength * vec3(1.0, 0.72, 0.38) * 0.2;

	float field = mod(floor(FRAGCOORD.y) + mod(frame, 2.0), 2.0);
	color *= mix(1.0, 0.965, field * intensity);
	color *= scene_brightness;

	float lifted_luma = clamp(dot(color, LUMA), 0.0, 1.0);
	vec3 lifted_color = sqrt(max(color, vec3(0.0))) * 0.82 + vec3(pow(lifted_luma, 0.42)) * 0.18;
	color = mix(color, lifted_color, scene_lift);
	float shadow_luma = dot(color, LUMA);
	color += vec3(smoothstep(0.28, 0.0, shadow_luma) * (0.08 + scene_lift * 0.34));

	float scanline = 1.0 - (0.055 + 0.11 * scanline_strength) * (0.5 + 0.5 * sin(FRAGCOORD.y * 3.14159265));
	color *= mix(1.0, scanline, scanline_strength);

	float grain = hash(FRAGCOORD.xy + vec2(time * 91.0, frame * 17.0)) - 0.5;
	float line_grain = hash(vec2(line, floor(time * 28.0))) - 0.5;
	float snow = step(0.9985 - noise_strength * 0.0032, hash(vec2(FRAGCOORD.x * 0.43 + frame, FRAGCOORD.y)));
	color += grain * (0.045 * noise_strength * intensity);
	color += line_grain * (0.02 * noise_strength * intensity);
	color += snow * (0.14 * noise_strength * intensity);

	float dropout_seed = hash(vec2(line, floor(time * 3.0)));
	float dropout_gate = step(0.9987 - noise_strength * 0.0018 - white_line_frequency * 0.006, dropout_seed);
	float dropout_start = hash(vec2(line, frame_bucket + 41.0)) * 0.86;
	float dropout_end = min(1.0, dropout_start + 0.035 + hash(vec2(line, frame_bucket + 73.0)) * 0.16);
	float dropout_mask = step(dropout_start, uv.x) * (1.0 - step(dropout_end, uv.x));
	float thin_line = 1.0 - smoothstep(0.18, 0.5, abs(fract(uv.y * tape_size.y) - 0.5));
	float dropout = dropout_gate * dropout_mask * thin_line;

	float scratch_seed = hash(vec2(line, floor(time * 12.0) + 113.0));
	float scratch_gate = step(1.0 - white_line_frequency * 0.015, scratch_seed);
	float scratch_start = hash(vec2(line, frame_bucket + 157.0)) * 0.82;
	float scratch_length = 0.08 + hash(vec2(line, frame_bucket + 191.0)) * 0.54;
	float scratch_end = min(1.0, scratch_start + scratch_length);
	float scratch_mask = step(scratch_start, uv.x) * (1.0 - step(scratch_end, uv.x));
	float scratch_breakup = smoothstep(0.28, 0.82, value_noise(vec2(uv.x * 64.0 + time * 7.0, line * 0.37 + frame_bucket)));
	float scratch = scratch_gate * scratch_mask * thin_line * scratch_breakup;

	float full_line_seed = hash(vec2(line, floor(time * 9.0) + 277.0));
	float full_line_gate = step(1.0 - white_line_frequency * 0.004, full_line_seed);
	float full_line_breakup = step(0.26, value_noise(vec2(uv.x * 96.0 + frame * 0.13, line + 19.0)));
	float full_line = full_line_gate * thin_line * full_line_breakup * 0.75;

	float white_lines = clamp(dropout + scratch + full_line, 0.0, 1.0);
	vec3 tape_white = vec3(0.94, 0.92, 0.84);
	color = mix(color, tape_white, white_lines * (0.22 + white_line_strength * 0.58));
	color += white_lines * white_line_strength * 0.12;

	float band_id = floor(uv.y * 42.0);
	float band_random = hash(vec2(band_id, floor(time * 8.0)));
	float band_gate = step(1.0 - (damage_frequency * 0.1), band_random);
	float band_shape = smoothstep(0.45, 0.0, abs(fract(uv.y * 42.0) - 0.5));
	float band_noise = hash(vec2(floor(uv.x * 96.0), band_id + frame));
	color += (band_noise - 0.45) * band_gate * band_shape * dropout_strength;

	float bottom_comb = step(0.58, hash(vec2(line, floor(time * 10.0)))) * bottom_tear;
	float bottom_lines = bottom_comb * (0.55 + 0.45 * value_noise(vec2(uv.x * 14.0, time * 8.0)));
	color += bottom_lines * (head_noise - 0.42) * tracking_strength * bottom_tear_strength * 0.22;
	color *= 1.0 - bottom_tear * intensity * bottom_tear_strength * 0.13;

	color = mix(color, color * tape_tint.rgb, tint_strength);
	float camera_luma = dot(color, LUMA);
	color = mix(vec3(camera_luma), color, camera_saturation);
	color = (color - 0.5) * camera_contrast + 0.5;

	float vignette = 1.0 - smoothstep(0.28, 0.92, distance(original_uv, vec2(0.5)));
	color *= mix(1.0 - 0.28 * vignette_strength, 1.0, vignette);

	float color_steps = mix(255.0, 96.0, pixelation_strength);
	color = floor(clamp(color, vec3(0.0), vec3(1.0)) * color_steps) / color_steps;

	COLOR = vec4(color, 1.0);
}
