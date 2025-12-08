using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Helper extensions for fire-and-forget tasks with exception handling.
    /// </summary>
    public static class TaskExtensions
    {
        public static async void Forget(this Task task, Action<Exception> exceptionHandler = null)
        {
            if (task == null)
            {
                return;
            }

            try
            {
                await task;
            }
            catch (Exception ex)
            {
                if (exceptionHandler != null)
                {
                    exceptionHandler(ex);
                }
                else
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}
