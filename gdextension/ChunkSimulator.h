#pragma once

#include <godot_cpp/classes/ref_counted.hpp>
#include <godot_cpp/variant/packed_byte_array.hpp>

namespace godot {

    class ChunkSimulator : public RefCounted {
        GDCLASS(ChunkSimulator, RefCounted);

    protected:
        static void _bind_methods();

    public:
        ChunkSimulator();
        ~ChunkSimulator();

        // Nimmt das Pixel-Array als Referenz/Pointer entgegen
        void simulate_chunk(PackedByteArray pixels, int width, int height, bool is_even_tick);
    };

}
