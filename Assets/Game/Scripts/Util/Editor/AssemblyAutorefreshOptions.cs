using UnityEditor;
using UnityEngine;

namespace Project
{
    public class AssemblyAutorefreshOptions
    {
        //Auto Refresh
        //kAutoRefresh has two posible values
        //0 = Auto Refresh Disabled
        //1 = Auto Refresh Enabled

        //This is called when you click on the 'Tools/Auto Refresh' and toggles its value
        [MenuItem("Project/Autorefresh Options/Auto Refresh")]
        private static void AutoRefreshToggle()
        {
            var status = EditorPrefs.GetInt("kAutoRefresh");
            if (status == 1)
                EditorPrefs.SetInt("kAutoRefresh", 0);
            else
                EditorPrefs.SetInt("kAutoRefresh", 1);
        }

        //This is called before 'Tools/Auto Refresh' is shown to check the current value
        //of kAutoRefresh and update the checkmark
        [MenuItem("Project/Autorefresh Options/Auto Refresh", true)]
        private static bool AutoRefreshToggleValidation()
        {
            var status = EditorPrefs.GetInt("kAutoRefresh");
            if (status == 1)
                Menu.SetChecked("Project/Autorefresh Options/Auto Refresh", true);
            else
                Menu.SetChecked("Project/Autorefresh Options/Auto Refresh", false);
            return true;
        }

        //Script Compilation During Play
        //ScriptCompilationDuringPlay has three posible values
        //0 = Recompile And Continue Playing
        //1 = Recompile After Finished Playing
        //2 = Stop Playing And Recompile

        //The following methods assign the three possible values to ScriptCompilationDuringPlay
        //depending on the option you selected
        [MenuItem(
            "Project/Autorefresh Options/Script Compilation During Play/Recompile And Continue Playing"
        )]
        private static void ScriptCompilationToggleOption0()
        {
            EditorPrefs.SetInt("ScriptCompilationDuringPlay", 0);
        }

        [MenuItem(
            "Project/Autorefresh Options/Script Compilation During Play/Recompile After Finished Playing"
        )]
        private static void ScriptCompilationToggleOption1()
        {
            EditorPrefs.SetInt("ScriptCompilationDuringPlay", 1);
        }

        [MenuItem(
            "Project/Autorefresh Options/Script Compilation During Play/Stop Playing And Recompile"
        )]
        private static void ScriptCompilationToggleOption2()
        {
            EditorPrefs.SetInt("ScriptCompilationDuringPlay", 2);
        }

        //This is called before 'Tools/Script Compilation During Play/Recompile And Continue Playing'
        //is shown to check for the current value of ScriptCompilationDuringPlay and update the checkmark
        [MenuItem(
            "Project/Autorefresh Options/Script Compilation During Play/Recompile And Continue Playing",
            true
        )]
        static bool ScriptCompilationValidation()
        {
            //Here, we uncheck all options before we show them
            Menu.SetChecked(
                "Project/Autorefresh Options/Script Compilation During Play/Recompile And Continue Playing",
                false
            );
            Menu.SetChecked(
                "Project/Autorefresh Options/Script Compilation During Play/Recompile After Finished Playing",
                false
            );
            Menu.SetChecked(
                "Project/Autorefresh Options/Script Compilation During Play/Stop Playing And Recompile",
                false
            );

            var status = EditorPrefs.GetInt("ScriptCompilationDuringPlay");

            //Here, we put the checkmark on the current value of ScriptCompilationDuringPlay
            switch (status)
            {
                case 0:
                    Menu.SetChecked(
                        "Project/Autorefresh Options/Script Compilation During Play/Recompile And Continue Playing",
                        true
                    );
                    break;
                case 1:
                    Menu.SetChecked(
                        "Project/Autorefresh Options/Script Compilation During Play/Recompile After Finished Playing",
                        true
                    );
                    break;
                case 2:
                    Menu.SetChecked(
                        "Project/Autorefresh Options/Script Compilation During Play/Stop Playing And Recompile",
                        true
                    );
                    break;
            }
            return true;
        }
    }
}
