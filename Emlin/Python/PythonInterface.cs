﻿

using System;
using System.Diagnostics;
using System.IO;

namespace Emlin.Python
{
    class PythonInterface
    {
        public static void TestRunPython()
        {
            // full path of python interpreter  
            string python = @"D:\Python27\python.exe";

            // python app to call  
            string myPythonApp = "\"C:\\Users\\Gerome\\Dropbox\\CI301-The Individual Project\\Emlin\\Emlin\\Python\\test.py\"";

            // dummy parameters to send Python script  
            int x = 2;
            int y = 5;


            // Create new process start info 
            ProcessStartInfo myProcessStartInfo = new ProcessStartInfo(python)
            {

                // make sure we can read the output from stdout 
                UseShellExecute = false,
                RedirectStandardOutput = true,

                Arguments = myPythonApp + " " + x + " " + y
            };


            Process myProcess = new Process
            {
                // assign start information to the process 
                StartInfo = myProcessStartInfo
            };

            // start process 
            myProcess.Start();

            // Read the standard output of the app we called.  
            StreamReader myStreamReader = myProcess.StandardOutput;
            string myString = myStreamReader.ReadLine();

            // wait exit signal from the app we called 
            myProcess.WaitForExit();

            // close the process 
            myProcess.Close();

            // write the output we got from python app 
            Console.WriteLine("Value received from script: " + myString);
        }
    }
}