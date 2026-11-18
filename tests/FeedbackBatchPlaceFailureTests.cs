using System;
using UI;

internal static class FeedbackBatchPlaceFailureTests
{
    private static int Main()
    {
        var batch = new FeedbackBatch();
        batch.Begin(new[] { 1, 2 });
        batch.Cancel(1);

        if (batch.IsPending(1)) throw new Exception("放回料盘的模组不应保留待回检状态。");
        if (!batch.IsPending(2)) throw new Exception("其他已放入夹具的模组必须保留待回检状态。");
        return 0;
    }
}
