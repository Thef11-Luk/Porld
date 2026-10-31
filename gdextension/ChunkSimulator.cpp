#include "ChunkSimulator.h"
#include <godot_cpp/core/class_db.hpp>

namespace godot {

    ChunkSimulator::ChunkSimulator() {}
    ChunkSimulator::~ChunkSimulator() {}

    void ChunkSimulator::simulate_chunk(PackedByteArray pixels, int width, int height, bool is_even_tick) {
        // Hol dir einen direkten, schreibbaren Zeiger auf die Rohdaten im Speicher
        int64_t len = pixels.size();
        uint8_t* ptr = pixels.ptrw();

        if (ptr == nullptr || len == 0) return;

        // Hier kommt gleich deine High-Performance Schleife rein!
        // Da wir hier direkt auf 'ptr' zugreifen, gibt es null Overhead.
        UtilityFunctions::print("c++ modul erfolgreich geladen");
    }

    void ChunkSimulator::_bind_methods() {
        ClassDB::bind_method(D_METHOD("simulate_chunk", "pixels", "width", "height", "is_even_tick"), &ChunkSimulator::simulate_chunk);
    }

}
