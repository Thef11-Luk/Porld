shader_type canvas_item;

uniform sampler2D chunk_texture: source_color, filter_nearest;
uniform vec2 chunk_world_pos;

float random(vec2 uv) {
    return fract(sin(dot(uv.xy, vec2(12.9898, 78.233))) * 43758.5453123);
}

void fragment() {
	vec4 tex = texture(chunk_texture, UV);
	int material = int(tex.r * 255.0);
	
	// sand
	if (material == 1) {
		COLOR = vec4(0.9, 0.8, 0.3, 1.0);
	} 
	// stone
	else if (material == 2){
		COLOR = vec4(0.2, 0.2, 0.2, 1.0);
	} 
	// grass
	else if (material == 3){
		COLOR = vec4(0.05, 0.5, 0.0, 1.0);
	}
	// dirt
	else if (material == 4) {
		COLOR = vec4(0.5, 0.25, 0.0, 1.0);
	}
	// water
	else if (material == 5) {
		COLOR = vec4(0.0, 0.1, 1.0, 0.6);
	}
	else {
		COLOR = vec4(0.0, 0.0, 0.0, 0.0);
	}
	
	if (COLOR.a > 0.0) {
		vec2 pixel_world_coord = floor(UV * vec2(64.0, 64.0) + chunk_world_pos);
        float noise = random(pixel_world_coord) * 0.1 - 0.05; // Sehr dezent (+/- 5% Helligkeit)
        
        COLOR.rgb += vec3(noise);
	}
}