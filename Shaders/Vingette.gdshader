shader_type canvas_item;

uniform float outer_radius : hint_range(0.1, 5.0) = 1.0;
uniform float softness : hint_range(0.0, 2.0) = 0.5;
uniform float alpha : hint_range(0.0, 2.0) = 1.0;

void fragment() {
    // Centered UV coordinates (-1 to 1 range)
    vec2 centered_uv = (UV - vec2(0.5)) * 2.0;
    
    // Radial distance from center
    float dist = length(centered_uv);
    
    // Use smoothstep to create a soft falloff
    float vignette = smoothstep(outer_radius, outer_radius - softness, dist);
    
    // Invert so the center is bright and edges dark
    float darkness = 1.0 - vignette * alpha;

    COLOR = vec4(0.0, 0.0, 0.0, darkness);
}
