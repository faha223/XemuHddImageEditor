using System.Runtime.InteropServices;

namespace InteropHelpers
{
    public static class StructParser
    {
        public static T Read<T>(this Stream stream, bool isBigEndian = false) where T: struct
        {
            Span<byte> bytes = new(new byte[Marshal.SizeOf(typeof(T))]);
            stream.ReadExactly(bytes);
            
            return Cast<T>(bytes, isBigEndian);
        }

        public static async Task<T> ReadAsync<T>(this Stream stream, bool isBigEndian = false) where T : struct
        {
            var bytes = new byte[Marshal.SizeOf(typeof(T))];
            await stream.ReadExactlyAsync(bytes);

            return Cast<T>(bytes, isBigEndian);
        }

        public static void Write<T>(this Stream stream, T structure) where T: struct
        {
            var bytes = new byte[Marshal.SizeOf(typeof(T))];
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            Marshal.StructureToPtr(structure, handle.AddrOfPinnedObject()!, false);

            stream.Write(bytes, 0, bytes.Length);

            handle.Free();
        }

        public static T Cast<T>(Span<byte> bytes, bool isBigEndian = false) where T: struct
        {
            return Cast<T>(bytes.ToArray(), isBigEndian);
        }

        public static T Cast<T>(byte[] bytes, bool isBigEndian = false) where T: struct
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject()!, typeof(T))!;
            handle.Free();

            if(isBigEndian)
                return theStructure.ToSystemEndian();

            return theStructure;
        }
    }
}