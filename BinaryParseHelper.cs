using System.Diagnostics.CodeAnalysis;

namespace ThumbSC
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class BinaryParseHelper
    {
        internal const int I0 = 1 << 0;
        internal const int I1 = 1 << 1;
        internal const int I7 = 1 << 7;
        internal const int I8 = 1 << 8;
        internal const int I9 = 1 << 9;
        internal const int I10 = 1 << 10;
        internal const int I11 = 1 << 11;
        internal const int I12 = 1 << 12;
        
        internal const int L1 = 0b1;
        internal const int L2 = 0b11;
        internal const int L3 = 0b111;
        internal const int L4 = 0b1111;
        internal const int L5 = 0b11111;
        internal const int L7 = 0b1111111;
        internal const int L8 = 0b11111111;
        internal const int L10 = 0b1111111111;
        internal const int L11 = 0b11111111111;
        
        internal const int FT = 1 << 5;
        internal const int FQ = 1 << 27;
        internal const int FV = 1 << 28;
        internal const int FC = 1 << 29;
        internal const int FZ = 1 << 30;
        internal const int FN = 1 << 31;
    }
}