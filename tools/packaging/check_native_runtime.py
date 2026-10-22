"""Prüft PE-Importe auf externe C/C++-Laufzeitabhängigkeiten."""

import argparse
from pathlib import Path
import struct


def imported_libraries(content):
    """Liest die Importnamen aus PE32-/PE32+-Dateien ohne native Werkzeuge."""
    def unpack(format_string, offset):
        try:
            return struct.unpack_from(format_string, content, offset)
        except struct.error as error:
            raise ValueError("Abgeschnittene PE-Datei") from error

    if content[:2] != b"MZ":
        raise ValueError("Keine PE-Datei")
    pe_offset, = unpack("<I", 0x3c)
    if content[pe_offset:pe_offset + 4] != b"PE\0\0":
        raise ValueError("Ungültige PE-Signatur")
    section_count, = unpack("<H", pe_offset + 6)
    optional_size, = unpack("<H", pe_offset + 20)
    optional = pe_offset + 24
    magic, = unpack("<H", optional)
    directories = {0x10b: 96, 0x20b: 112}.get(magic)
    if directories is None or optional_size < directories + 16:
        raise ValueError("Ungültiger optionaler PE-Header")
    imports_rva, imports_size = unpack("<II", optional + directories + 8)
    if not imports_rva or imports_size < 20:
        raise ValueError("Fehlende PE-Importtabelle")
    sections = []
    for index in range(section_count):
        section = optional + optional_size + 40 * index
        virtual_size, address, raw_size, raw_offset = unpack("<IIII", section + 8)
        sections.append((address, raw_size, raw_offset))

    def offset_for(address, size=1):
        for virtual_address, raw_size, raw_offset in sections:
            relative = address - virtual_address
            if 0 <= relative and relative + size <= raw_size:
                offset = raw_offset + relative
                if offset + size <= len(content):
                    return offset
        raise ValueError("PE-Import verweist außerhalb der Datei")

    libraries = []
    for index in range(imports_size // 20):
        descriptor = unpack("<IIIII", offset_for(imports_rva + index * 20, 20))
        if not any(descriptor):
            if not libraries:
                raise ValueError("Leere PE-Importtabelle")
            return libraries
        name_rva = descriptor[3]
        name = bytearray()
        for index_byte in range(260):
            value = content[offset_for(name_rva + index_byte)]
            if value == 0:
                break
            name.append(value)
        else:
            raise ValueError("Nicht abgeschlossener PE-Importname")
        try:
            libraries.append(name.decode("ascii"))
        except UnicodeDecodeError as error:
            raise ValueError("Ungültiger PE-Importname") from error
    raise ValueError("Nicht abgeschlossene PE-Importtabelle")


def check_static_runtime(content):
    """Verhindert externe VC-/UCRT-Abhängigkeiten in statischen Plug-in-Builds."""
    libraries = imported_libraries(content)
    external = [name for name in libraries if name.lower().startswith(
        ("msvcp", "msvcr", "vcruntime", "concrt", "ucrtbase", "api-ms-win-crt-"))]
    if external:
        raise ValueError("Externe C/C++-Laufzeit: " + ", ".join(external))
    return libraries


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("libraries", nargs="+", type=Path)
    arguments = parser.parse_args()
    for library in arguments.libraries:
        imports = check_static_runtime(library.read_bytes())
        print(f"PASS {library}: keine externe C/C++-Laufzeit; Importe: {', '.join(imports)}")
