namespace GameOfChipsServer
{
    public static class ThreadManager
    {
        private static readonly List<Action?> ActionsExecuteOnMainThread = new ();
        private static readonly List<Action?> ActionsExecuteCopiedOnMainThread = new ();
        
        private static bool _actionToExecuteOnMainThread;

        /// <summary>Sets an action to be executed on the main thread.</summary>
        /// <param name="action">The action to be executed on the main thread.</param>
        public static void ExecuteOnMainThread(Action? action)
        {
            if (action == null)
            {
                Server.Message("No action to execute on main thread!");
                
                return;
            }

            lock (ActionsExecuteOnMainThread)
            {
                ActionsExecuteOnMainThread.Add(action);
                
                _actionToExecuteOnMainThread = true;
            }
        }

        /// <summary>Executes all code meant to run on the main thread. NOTE: Call this ONLY from the main thread.</summary>
        public static void UpdateMain()
        {
            if (!_actionToExecuteOnMainThread) 
                return;
            
            ActionsExecuteCopiedOnMainThread.Clear();
                
            lock (ActionsExecuteOnMainThread)
            {
                ActionsExecuteCopiedOnMainThread.AddRange(ActionsExecuteOnMainThread);
                ActionsExecuteOnMainThread.Clear();
                
                _actionToExecuteOnMainThread = false;
            }

            foreach (var t in ActionsExecuteCopiedOnMainThread)
                t?.Invoke();
        }
    }
}