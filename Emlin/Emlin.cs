﻿using Emlin.Encryption;
using Emlin.Python;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Emlin
{
    public partial class Emlin : Form
    {
        private static DataFormatter dataFormatter;
        private HealthSubject health;


        #region lower level stuff
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private static IntPtr _hookID = IntPtr.Zero;
        private LowLevelKeyboardProc _proc;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        public static extern int GetKeyState(int nVirtKey);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        #endregion
        LoadingWindow loadingWindow = new LoadingWindow();
        DevWindow devWindow = new DevWindow();

        public Emlin()
        {
            _proc = HookCallback;         
            _hookID = SetHook(_proc);

            
            InitializeComponent();

            CustomTimer timer = new CustomTimer(ConstantValues.LENGTH_OF_SESSION_IN_MILLIS);
            timer.Elapsed += TimerCountdown;
            timer.AutoReset = false;
            dataFormatter = new DataFormatter(timer);

#if DEBUG
            devWindow.Show();
#endif
            health = new HealthSubject();
            healthGraphView.SetHealthSubject(health);

            health.Attach(healthGraphView);
            health.SetValue(ConstantValues.DEFAULT_HEALTH_VALUE);
        }

        private void TimerCountdown(object sender, EventArgs e)
        {
            string filepath = Path.Combine(ConstantValues.KEYBOARD_DATA_FILEPATH, ConstantValues.KEYBOARD_FILE_NAME);

            dataFormatter.RemoveLastDataItem();
            List<KeysData> dataToWriteToFile = dataFormatter.DataRecorded;

            if (dataToWriteToFile.Count != 0)
            {
                PythonInterface pi = new PythonInterface();
                List<string> formattedData = new List<string>();

                foreach (KeysData keysData in dataToWriteToFile)
                {
                    formattedData.Add(DataFormatter.GetFormattedDataLine(keysData));
                }

                if (ModelFileExists())
                {
                    pi.TestUserInput(formattedData, health, ConstantValues.KEYBOARD_DATA_FILEPATH);
                }

                if (recordingEnabled.Checked)
                {
                    WriteEncryptedDataToFile(filepath, dataToWriteToFile);
                }
            }

            PossiblyShuffleLines(filepath);

            if(health.GetValue() < ConstantValues.HEALTH_VALUE_THRESHOLD)
            {
                // Uncomment to lock user out of computer
                //Process.Start(@"C:\WINDOWS\system32\rundll32.exe", "user32.dll,LockWorkStation");
                health.SetValue(ConstantValues.DEFAULT_HEALTH_VALUE);
            }

            dataFormatter.End();
        }

        private void PossiblyShuffleLines(string filepath)
        {
            // 1 in 100 chance of shuffling all the lines
            if(new Random().Next(1,100) == 42)
            {
                var lines = File.ReadAllLines(filepath);
                var rnd = new Random();
                lines = lines.OrderBy(line => rnd.Next()).ToArray();
                File.WriteAllLines(filepath, lines);
            }       
        }

        private static void WriteEncryptedDataToFile(string filepath, List<KeysData> dataToWriteToFile)
        {
            DataToFileWriter dtfw = new DataToFileWriter();
            dtfw.CreateDirectoryAndFile(filepath);
            dtfw.WriteRecordedDataToFile(dataToWriteToFile, filepath, new Encryptor());
        }

        private void SendKeyPressToCurrentSession(char charPressed, long timeInTicks)
        {
            WriteToDebugWindow(charPressed.ToString() + " pressed at " + new DateTime(timeInTicks).ToLongTimeString().ToString());
            lock (dataFormatter)
            {
                dataFormatter.KeyWasPressed(charPressed, timeInTicks);
            }
        }

        private void SendKeyReleaseToCurrentSession(char charReleased, long timeInTicks)
        {
            WriteToDebugWindow(charReleased.ToString() + " released at " + new DateTime(timeInTicks).ToLongTimeString().ToString());
            if (!OnlyKeyUpEvent(charReleased))
            {
                lock (dataFormatter)
                {
                    dataFormatter.KeyWasReleased(charReleased, timeInTicks);
                }
            }
        }

        private bool OnlyKeyUpEvent(char charReleased)
        {
            return charReleased == 164
                || charReleased == 165;
        }


        #region hook methods

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            int VK_SHIFT = 0x10;
            int VK_CAPS = 0X14;

            if (RecordingPossible(nCode, wParam))
            {
                int value = Marshal.ReadInt32(lParam);
                char key = (char)value;

                int capsState = GetKeyState(VK_CAPS) & 0x0001;

                if (capsState != 1 && (value >= 65 && value <= 90))
                {
                    int shiftState = GetKeyState(VK_SHIFT) & 0x0001;

                    key = char.ToLower(key);
                }

                if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    SendKeyPressToCurrentSession(key, DateTime.Now.Ticks);
                }
                else
                {
                    SendKeyReleaseToCurrentSession(key, DateTime.Now.Ticks);
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private bool RecordingPossible(int nCode, IntPtr wParam)
        {
            return nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_KEYUP);
        }

        #endregion

  
        private void WriteToDebugWindow(string output)
        {
            devWindow.textBox1.AppendText(output + Environment.NewLine);
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            notifyIcon1.Visible = true;
        }

        private void deleteDataBtn_Click(object sender, EventArgs e)
        {
            string warning = "You are about to delete all your recorded data. Are you sure you want to do this?";
            DialogResult areYouSureDeleteDR = MessageBox.Show(warning, "Delete Recorded Data", MessageBoxButtons.YesNo);

            if(areYouSureDeleteDR == DialogResult.Yes)
            {
                string filepath = ConstantValues.KEYBOARD_DATA_FILEPATH + "\\" + ConstantValues.KEYBOARD_FILE_NAME;
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);

                    string dataDeleted = "Your data was succesfully deleted.";
                    MessageBox.Show(dataDeleted, "Data Deleted", MessageBoxButtons.OK);
                }
                else
                {
                    string dataNotDeleted = "Could not find file to delete.";

                    MessageBox.Show(dataNotDeleted, "File Not Found", MessageBoxButtons.OK);
                }
            }     
        }

        private void GoToDataBtn_Click(object sender, EventArgs e)
        {
            Process.Start(ConstantValues.KEYBOARD_DATA_FILEPATH + "\\");
        }

        private void TeachModel_Click(object sender, EventArgs e)
        {
            string dataFilePath = Path.Combine(ConstantValues.KEYBOARD_DATA_FILEPATH, ConstantValues.KEYBOARD_FILE_NAME);
            string decryptedFilePath = Path.Combine(ConstantValues.KEYBOARD_DATA_FILEPATH, "D_" + ConstantValues.KEYBOARD_CSV_FILE_NAME);
            FileInfo file = new FileInfo(dataFilePath);

            Decryptor.DecryptFile(file, decryptedFilePath);

            loadingWindow.Show();

            DeleteOldGeneratedFiles();

            CreateModelInBackground();
        }

        private void DeleteOldGeneratedFiles()
        {
            string[] filePaths = Directory.GetFiles(ConstantValues.KEYBOARD_DATA_FILEPATH);
            foreach (string filePath in filePaths)
            {
                var name = new FileInfo(filePath).Name;
                if (!name.Equals(ConstantValues.KEYBOARD_FILE_NAME) && !name.Equals("D_" + ConstantValues.KEYBOARD_CSV_FILE_NAME))
                {
                    File.Delete(filePath);
                }
            }
        }

        private void CreateModelInBackground()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(Worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker_RunWorkerCompleted);
            worker.RunWorkerAsync();
        }

        void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            PythonInterface pi = new PythonInterface();
            pi.GenerateNonUserData(ConstantValues.KEYBOARD_DATA_FILEPATH);
            pi.ProcessUserAndGeneratedData(ConstantValues.KEYBOARD_DATA_FILEPATH);
            pi.TeachModel("KNN", ConstantValues.KEYBOARD_DATA_FILEPATH);
        }

        void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            loadingWindow.Hide();

            string message = "";
            string title = "";

            if (ModelFileExists())
            {
                message = "The model has been successfully trained on your data.";
                title = "Model Successfully Trained";
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                message = "Emlin failed to create your model. Please try again.";
                title = "Model Training Failed";
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ModelFileExists()
        {
            return File.Exists(Path.Combine(ConstantValues.KEYBOARD_DATA_FILEPATH, ConstantValues.MODEL_FILE_NAME));
        }
    }
}
