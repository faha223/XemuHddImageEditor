using System.Runtime.InteropServices;
using System.Linq;

namespace InteropHelpers
{
    public static class EndiannessHelper
    {
        public static uint ToSystemEndian(this uint bigEndian)
        {
            if(BitConverter.IsLittleEndian)
            {
                return (bigEndian & 0x000000ff) << 24 |
                       (bigEndian & 0x0000ff00) << 8 |
                       (bigEndian & 0x00ff0000) >> 8 |
                        bigEndian >> 24;
            }
            return bigEndian;
        }

        public static ulong ToSystemEndian(this ulong bigEndian)
        {
            if(BitConverter.IsLittleEndian)
            {
                return (bigEndian & 0x00000000000000ffL) << 56 |
                       (bigEndian & 0x000000000000ff00L) << 40 |
                       (bigEndian & 0x0000000000ff0000L) << 24 |
                       (bigEndian & 0x00000000ff000000L) << 8 |
                       (bigEndian & 0x000000ff00000000L) >> 8 |
                       (bigEndian & 0x0000ff0000000000L) >> 24 |
                       (bigEndian & 0x00ff000000000000L) >> 40 |
                        bigEndian >> 56;
            }
            return bigEndian;
        }

        public static T ToSystemEndian<T>(this T structure) where T: struct
        {
            if(BitConverter.IsLittleEndian)
            {
                var ms = new MemoryStream();
                var fields = structure.GetType().GetFields();
                foreach(var field in fields)
                {
                    var val = field.GetValue(structure);
                    if(val == null)
                        throw new Exception("val is null");
                    byte[] fieldBytes = Array.Empty<byte>();
                    var valType = val.GetType();
                    if(valType.IsEnum)
                        valType = Enum.GetUnderlyingType(valType);

                    if(valType == typeof(ulong))
                        fieldBytes = BitConverter.GetBytes((ulong)val);
                    else if(valType == typeof(uint))
                        fieldBytes = BitConverter.GetBytes((uint)val);
                    else if(valType == typeof(ushort))
                        fieldBytes = BitConverter.GetBytes((ushort)val);
                    if(valType == typeof(long))
                        fieldBytes = BitConverter.GetBytes((long)val);
                    else if(valType == typeof(int))
                        fieldBytes = BitConverter.GetBytes((int)val);
                    else if(valType == typeof(short))
                        fieldBytes = BitConverter.GetBytes((short)val);
                    else
                        throw new InvalidOperationException($"Unexpected Type in EndiannessHelper: {val?.GetType()}");

                    if(field.CustomAttributes.Any(c => c.AttributeType == typeof(BigEndianAttribute)))
                        fieldBytes = fieldBytes.Reverse().ToArray();

                    ms.Write(fieldBytes, 0, fieldBytes.Length);
                }

                return StructParser.Cast<T>(ms.ToArray());
            }
            return structure;
        }
    }
}