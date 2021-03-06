﻿using System;
using System.IO;
using System.Reflection;

namespace Emlin
{
    public static class ConstantValues
    {
        public const int NUMBER_OF_INPUTS = 128;
        public const int LENGTH_OF_SESSION_IN_MILLIS = 1500;
        public const int DEFAULT_HEALTH_VALUE = 100;
        public const int HEALTH_VALUE_THRESHOLD = 0;
        public static string KEYBOARD_FILE_NAME = "KeyboardData.txt";
        public static string KEYBOARD_CSV_FILE_NAME = "KeyboardData.csv";
        public static string MODEL_FILE_NAME = "knnClf.joblib";
        public static string KEYBOARD_DATA_FILEPATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) , Assembly.GetCallingAssembly().GetName().Name);

       
    }
}
