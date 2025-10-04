shader_type canvas_item;

uniform vec4 base_color : source_color = vec4(0.15, 0.15, 0.18, 1.0);
uniform float noise_scale = 200.0;
uniform float noise_strength = 0.15;

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);

    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));

    vec2 u = f * f * (3.0 - 2.0 * f);

    return mix(a, b, u.x) +
           (c - a) * u.y * (1.0 - u.x) +
           (d - b) * u.x * u.y;
}

void fragment() {
    vec2 uv = VERTEX.xy / noise_scale; // use geometry instead of UV
    float n = noise(uv);

    float shade = mix(0.8, 1.2, n * noise_strength);
    COLOR = base_color * shade;
}
