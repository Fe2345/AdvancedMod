"""使用本机 DirectX 编译器检查 D3D9 字节码，或将 fx 编译为未压缩 Effect XNB。"""
import argparse
import ctypes as c
import pathlib
import struct


def blob_bytes(ptr):
    if not ptr.value:
        return b""
    table = c.cast(ptr, c.POINTER(c.POINTER(c.c_void_p))).contents
    address = c.WINFUNCTYPE(c.c_void_p, c.c_void_p)(table[3])(ptr)
    length = c.WINFUNCTYPE(c.c_size_t, c.c_void_p)(table[4])(ptr)
    result = c.string_at(address, length)
    c.WINFUNCTYPE(c.c_ulong, c.c_void_p)(table[2])(ptr)
    return result


def disassemble(data):
    compiler = c.WinDLL("d3dcompiler_43.dll")
    fn = compiler.D3DDisassemble
    fn.argtypes = [c.c_void_p, c.c_size_t, c.c_uint, c.c_char_p, c.POINTER(c.c_void_p)]
    output = c.c_void_p()
    hr = fn(data, len(data), 0, None, c.byref(output))
    result = blob_bytes(output)
    if hr < 0:
        raise RuntimeError(f"反汇编失败: {hr:#x}")
    return result.decode("ascii", errors="replace")


def seven(value):
    result = bytearray()
    while value >= 128:
        result.append((value & 127) | 128)
        value >>= 7
    result.append(value)
    return result


def compile_fx(source, output):
    compiler = c.WinDLL("d3dcompiler_43.dll")
    fn = compiler.D3DCompile
    fn.argtypes = [c.c_void_p, c.c_size_t, c.c_char_p, c.c_void_p, c.c_void_p,
                   c.c_char_p, c.c_char_p, c.c_uint, c.c_uint, c.POINTER(c.c_void_p), c.POINTER(c.c_void_p)]
    data = source.read_bytes()
    code, errors = c.c_void_p(), c.c_void_p()
    hr = fn(data, len(data), str(source).encode(), None, None, None, b"fx_2_0", 0, 0, c.byref(code), c.byref(errors))
    messages = blob_bytes(errors).decode(errors="replace")
    if messages:
        print(messages)
    compiled = blob_bytes(code)
    if hr < 0:
        raise RuntimeError(f"编译失败: {hr:#x}")
    reader = b"Microsoft.Xna.Framework.Content.EffectReader, Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553"
    # XNA Effect 的16字节平台封套；实际D3D9 effect字节码从FEFF0901开始。
    effect = struct.pack("<4I", 0xBCF00BCF, 16, 0, 0) + compiled
    body = b"\x01" + seven(len(reader)) + reader + struct.pack("<I", 0) + b"\x00\x01" + struct.pack("<I", len(effect)) + effect
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_bytes(b"XNBw\x05\x00" + struct.pack("<I", len(body) + 10) + body)
    print(f"已编译 {output}，{len(compiled)}字节D3D9 Effect")


def inspect(source, directory):
    data = source.read_bytes()
    directory.mkdir(parents=True, exist_ok=True)
    count = 0
    for offset in range(0, len(data) - 4):
        token = struct.unpack_from("<I", data, offset)[0]
        if token not in (0xFFFE0200, 0xFFFE0300, 0xFFFF0200, 0xFFFF0300):
            continue
        try:
            asm = disassemble(data[offset:])
        except RuntimeError:
            continue
        if "???" in asm or "// Parameters:" not in asm:
            continue
        (directory / f"{offset:04x}.asm").write_text(asm.rstrip("\0\r\n") + "\n", encoding="utf-8")
        print(f"{offset:#x}: {asm[:100]}")
        count += 1
    if not count:
        raise RuntimeError("未找到可反汇编的SM2/SM3程序")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=["inspect", "compile"])
    parser.add_argument("source", type=pathlib.Path)
    parser.add_argument("output", type=pathlib.Path)
    args = parser.parse_args()
    (inspect if args.mode == "inspect" else compile_fx)(args.source, args.output)
