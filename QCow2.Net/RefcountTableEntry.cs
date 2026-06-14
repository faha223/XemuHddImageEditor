namespace QCow2.Net
{
    public class RefcountTableEntry(ulong bits)
    {
        private ulong _bits = bits;

        public ulong RefcountBlockOffset => 
            _bits & Constants.Bits9Through63;
    }
}