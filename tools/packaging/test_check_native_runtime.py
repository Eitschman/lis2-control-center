"""Gegenproben für PE32/PE32+ und unerlaubte externe Laufzeitimporte."""

import struct
import unittest

from check_native_runtime import check_static_runtime, imported_libraries


def pe_fixture(libraries, plus=False):
    """Erzeugt eine isolierte Importtabelle für beide PE-Headerformate."""
    content = bytearray(4096)
    content[:2] = b"MZ"
    struct.pack_into("<I", content, 0x3c, 128)
    content[128:132] = b"PE\0\0"
    struct.pack_into("<H", content, 134, 1)
    optional_size = 240 if plus else 224
    struct.pack_into("<H", content, 148, optional_size)
    struct.pack_into("<H", content, 152, 0x20b if plus else 0x10b)
    directories = 112 if plus else 96
    struct.pack_into("<II", content, 152 + directories + 8, 0x1000, (len(libraries) + 1) * 20)
    struct.pack_into("<IIII", content, 152 + optional_size + 8, 3584, 0x1000, 3584, 512)
    name_offset = 2048
    for index, library in enumerate(libraries):
        struct.pack_into("<IIIII", content, 512 + index * 20, 0, 0, 0, 0x1000 + name_offset - 512, 0)
        encoded = library.encode("ascii") + b"\0"
        content[name_offset:name_offset + len(encoded)] = encoded
        name_offset += len(encoded)
    return bytes(content)


class NativeRuntimeTests(unittest.TestCase):
    def test_system_imports_allowed_for_both_pe_formats(self):
        for plus in (False, True):
            with self.subTest(plus=plus):
                libraries = ["KERNEL32.dll", "oleaut32.dll", "USER32.dll"]
                self.assertEqual(libraries, check_static_runtime(pe_fixture(libraries, plus)))

    def test_external_runtime_families_rejected_for_both_pe_formats(self):
        for plus in (False, True):
            for library in ("MSVCP140.dll", "VCRUNTIME140.dll", "VCRUNTIME140_1.dll",
                            "MSVCR120.dll", "CONCRT140.dll", "ucrtbase.dll", "api-ms-win-crt-heap-l1-1-0.dll"):
                with self.subTest(plus=plus, library=library):
                    with self.assertRaisesRegex(ValueError, "Externe C/C\\+\\+-Laufzeit"):
                        check_static_runtime(pe_fixture(["KERNEL32.dll", library], plus))

    def test_rejects_truncated_and_non_pe_files(self):
        for content in (b"invalid", b"MZ", pe_fixture(["KERNEL32.dll"])[:600]):
            with self.assertRaises(ValueError):
                check_static_runtime(content)

    def test_rejects_missing_import_table(self):
        content = bytearray(pe_fixture(["KERNEL32.dll"]))
        struct.pack_into("<II", content, 152 + 96 + 8, 0, 0)
        with self.assertRaisesRegex(ValueError, "Fehlende PE-Importtabelle"):
            imported_libraries(content)

    def test_rejects_unterminated_import_table(self):
        content = bytearray(pe_fixture(["KERNEL32.dll"]))
        struct.pack_into("<I", content, 152 + 96 + 12, 20)
        with self.assertRaisesRegex(ValueError, "Nicht abgeschlossene PE-Importtabelle"):
            imported_libraries(content)

    def test_rejects_invalid_name_address(self):
        content = bytearray(pe_fixture(["KERNEL32.dll"]))
        struct.pack_into("<I", content, 512 + 12, 0xffffffff)
        with self.assertRaisesRegex(ValueError, "außerhalb"):
            imported_libraries(content)


if __name__ == "__main__":
    unittest.main()
