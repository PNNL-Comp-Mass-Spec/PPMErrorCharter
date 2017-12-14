
namespace PPMErrorCharter
{
    internal class MassErrorHistogramResult
    {
        public int BinCountOriginal { get; }
        public int BinCountRefined { get; set; }

        public MassErrorHistogramResult(int binCountOriginal, int binCountRefined = 0)
        {
            BinCountOriginal = binCountOriginal;
            BinCountRefined = binCountRefined;
        }
    }
}
