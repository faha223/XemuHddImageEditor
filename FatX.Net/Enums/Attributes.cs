namespace FatX.Net.Enums;

[Flags]
internal enum Attributes : byte
{
    ReadOnly = 0x01,
    System = 0x02,
    Hidden = 0x04,
    Volume = 0x08,
    Directory = 0x10
}