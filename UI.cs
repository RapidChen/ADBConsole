using UnityEngine;
using UnityEditor;
using NUnit.Framework;


namespace ADBConsole
{
    public class UI
    {
        static StartProgram startProgram;
        [MenuItem("AdbConsole/start %&o")]
        static void AdbConsole()
        {
            Debug.Log("Console started here.");
            startProgram = new StartProgram();
            startProgram.ADBStart();
        }

        
        [MenuItem("AdbConsole/stop %&l")]
        static void StopConsole()
        {
            startProgram.Stop();
        }
        













    }
}


