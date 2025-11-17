using System.Diagnostics;
using UnityEngine;

namespace Core
{
    enum LogDisplayType
    {
        DefaultUnityLogging,
    }

    class LogOutput
    {
        // Basic logging function for other functions to use, default no case given.
        public static void Display(string LogText)
        {
            DefaultLogging(LogText);
        }

        // Ability to change the default way the logger works, in case better UI is added, etc.
        private static void DefaultLogging(string LogText)
        {
            UnityEngine.Debug.Log(LogText);
        }

        // Ability to display in different places, for example in game UI, optional addtional argument
        public static void Display(string LogText, LogDisplayType logDisplay)
        {
            switch (logDisplay)
            {
                case LogDisplayType.DefaultUnityLogging:
                    DefaultLogging(LogText);
                    return;
                default:
                    DefaultLogging(LogText);
                    return;
            }
        }
    }
}
