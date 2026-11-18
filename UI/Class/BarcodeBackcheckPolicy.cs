namespace UI
{
    public enum BarcodeBackcheckResult
    {
        Match,
        Empty,
        Mismatch
    }

    public static class BarcodeBackcheckPolicy
    {
        public static BarcodeBackcheckResult Classify(string expectedBarcode, string actualBarcode)
        {
            if (string.IsNullOrWhiteSpace(actualBarcode)) return BarcodeBackcheckResult.Empty;
            if (actualBarcode == expectedBarcode) return BarcodeBackcheckResult.Match;
            return BarcodeBackcheckResult.Mismatch;
        }
    }
}
