using System.Collections.Generic;

namespace ThumbSC
{
    public delegate int HookMemory(uint address, bool isRead, int size, int? value = null);

    public struct Region
    {
        public uint Begin { get; }
        public uint End { get; }
        public byte[] Buffer { get; }

        public Region(uint begin, uint end, byte[] buffer)
        {
            Begin = begin;
            End = end;
            Buffer = buffer;
        }
    }

    public class Memory
    {
        private List<Region> _list = new List<Region>();
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public HookMemory GlobalHook = null;
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public HookMemory Hook;
        private uint _begin;
        private uint _end;
        private byte[] _buffer;

        public Memory(HookMemory hook = null)
        {
            Hook = hook;
        }

        // ReSharper disable once UnusedMember.Global
        public void Map(uint address, uint size, bool isSpecial = false)
        {
            if (isSpecial) return;
            var chunk = new byte[size];
            _list.Add(new Region(address, address + size, chunk));
        }

        // ReSharper disable once UnusedMember.Global
        public void WriteBuffer(int address, byte[] buffer)
        {
            for (var i = 0; i < buffer.Length; i++)
                WriteByte(address + i, buffer[i]);
        }

        public int ReadInt(int address)
        {
            const int size = 4;
            var addr = (uint) address;

            if (!IsValidCache(addr, size))
            {
                if (!UpdateCache(addr))
                    return Hook?.Invoke(addr, true, size) ?? 0;
            }

            var pos = addr - _begin;
            var value = _buffer[pos++] |
                        (_buffer[pos++] << 8) |
                        (_buffer[pos++] << 16) |
                        (_buffer[pos] << 24);

            GlobalHook?.Invoke(addr, true, size, value);
            return value;
        }

        public void WriteInt(int address, int value)
        {
            const int size = 4;
            var addr = (uint) address;

            if (!IsValidCache(addr, size))
            {
                if (!UpdateCache(addr))
                {
                    Hook?.Invoke(addr, false, size, value);
                    return;
                }
            }

            GlobalHook?.Invoke(addr, false, size, value);

            var pos = addr - _begin;
            _buffer[pos++] = (byte) value;
            _buffer[pos++] = (byte) (value >> 8);
            _buffer[pos++] = (byte) (value >> 16);
            _buffer[pos] = (byte) (value >> 24);
        }

        public void WriteShort(int address, ushort value)
        {
            const int size = 2;
            var addr = (uint) address;
            
            if (!IsValidCache(addr, size))
            {
                if (!UpdateCache(addr))
                {
                    Hook?.Invoke(addr, false, size, value);
                    return;
                }
            }

            GlobalHook?.Invoke(addr, false, size, value);

            var pos = addr - _begin;
            _buffer[pos++] = (byte) value;
            _buffer[pos] = (byte) (value >> 8);
        }

        public ushort ReadShort(int address)
        {
            const int size = 2;
            var addr = (uint) address;
            
            if (!IsValidCache(addr, size))
            {
                if (!UpdateCache(addr))
                    return (ushort) (Hook?.Invoke(addr, true, size) ?? 0);
            }

            var pos = addr - _begin;
            var value = (ushort) (
                _buffer[pos++] |
                (_buffer[pos] << 8)
            );

            GlobalHook?.Invoke(addr, true, size, value);
            return value;
        }

        public void WriteByte(int address, byte value)
        {
            const int size = 1;
            var addr = (uint) address;

            if (!IsValidCache(addr, size))
            {
                if (!UpdateCache(addr))
                {
                    Hook?.Invoke(addr, false, size, value);
                    return;
                }
            }

            GlobalHook?.Invoke(addr, false, size, value);

            var pos = address - _begin;
            _buffer[pos] = value;
        }

        public byte ReadByte(int address)
        {
            const int size = 1;
            var addr = (uint) address;

            if (!IsValidCache(addr, size))
            {
                if (!UpdateCache(addr))
                    return (byte) (Hook?.Invoke(addr, true, size) ?? 0);
            }

            var pos = addr - _begin;
            var value = _buffer[pos];

            GlobalHook?.Invoke(addr, true, size, value);
            return value;
        }

        private bool IsValidCache(uint address, uint size)
        {
            return _begin <= address && address + size < _end;
        }

        private bool UpdateCache(uint address)
        {
            foreach (var page in _list)
            {
                if (!(page.Begin <= address && address < page.End)) continue;
                _begin = page.Begin;
                _end = page.End;
                _buffer = page.Buffer;
                return true;
            }

            _begin = 0;
            _end = 0;
            _buffer = null;
            return false;
        }
    }
}