namespace InteropHelpers
{
    public static class EndiannessHelper
    {
        public static T ToSystemEndian<T>(this T structure) where T: struct
        {
            //bool systemIsLittleEndian = BitConverter.IsLittleEndian;
            //bool structIsLittleEndian = !structure.GetType().CustomAttributes.Any(a => a.GetType() == typeof(BigEndianAttribute));

            // If the endianness of the system and the struct do not match, we need to reverse the byte order of each field
            //if(systemIsLittleEndian ^ structIsLittleEndian)
            if(BitConverter.IsLittleEndian)
            {
                var ms = new MemoryStream();
                var fields = structure.GetType().GetFields();
                foreach(var field in fields)
                {
                    var val = field.GetValue(structure) ?? throw new Exception("val is null");
                    byte[] fieldBytes = [];
                    var valType = val.GetType();
                    
                    if(valType.IsEnum)
                        valType = Enum.GetUnderlyingType(valType);            

                    if(valType == typeof(string))
                        fieldBytes = System.Text.Encoding.UTF8.GetBytes((string)val);
                    else if(valType == typeof(UInt64))
                        fieldBytes = BitConverter.GetBytes((UInt64)val);
                    else if(valType == typeof(UInt32))
                        fieldBytes = BitConverter.GetBytes((UInt32)val);
                    else if(valType == typeof(UInt16))
                        fieldBytes = BitConverter.GetBytes((UInt16)val);
                    else if(valType == typeof(Int64))
                        fieldBytes = BitConverter.GetBytes((Int64)val);
                    else if(valType == typeof(Int32))
                        fieldBytes = BitConverter.GetBytes((Int32)val);
                    else if(valType == typeof(Int16))
                        fieldBytes = BitConverter.GetBytes((Int16)val);
                    else if(valType == typeof(byte))
                        fieldBytes = [ (byte)val ];
                    else if(valType == typeof(sbyte))
                        fieldBytes = [ (byte)(sbyte)val ];
                    else
                        throw new InvalidOperationException($"Unexpected Type in EndiannessHelper: {val?.GetType()}");

                    if(field.CustomAttributes.Any(c => c.AttributeType == typeof(BigEndianAttribute)))
                        fieldBytes = [..fieldBytes.Reverse()];

                    ms.Write(fieldBytes, 0, fieldBytes.Length);
                }

                return StructParser.Cast<T>(ms.ToArray());
            }
            return structure;
        }
    }
}