using System.Collections.Generic;

namespace UI
{
    /// <summary>
    /// Keeps the module numbers that still need upper-camera feedback.
    /// The collection intentionally survives a manual stop so the next run
    /// only rechecks modules that did not finish.
    /// </summary>
    public sealed class FeedbackBatch
    {
        private readonly HashSet<int> pendingModules = new HashSet<int>();

        public bool HasPending
        {
            get { return pendingModules.Count > 0; }
        }

        public bool TryBegin(IEnumerable<int> moduleNumbers)
        {
            if (HasPending) return false;
            Begin(moduleNumbers);
            return true;
        }

        public void Begin(IEnumerable<int> moduleNumbers)
        {
            pendingModules.Clear();
            foreach (int moduleNumber in moduleNumbers)
            {
                pendingModules.Add(moduleNumber);
            }
        }

        public bool IsPending(int moduleNumber)
        {
            return pendingModules.Contains(moduleNumber);
        }

        public void Complete(int moduleNumber)
        {
            pendingModules.Remove(moduleNumber);
        }

        public void Cancel(int moduleNumber)
        {
            pendingModules.Remove(moduleNumber);
        }

        public void Clear()
        {
            pendingModules.Clear();
        }
    }
}
