using UnityEngine;
using UnityEditor;
using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Threading;    //to run commands concurrently
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace ADBConsole
{
    class StartProgram
    {
        bool bTest;
        string pattern = @"[VIDWEF][/]";
        Thread m_adbListenThread;
        Queue adbOutputQueue;
        ADBAccess m_ADBAccess;
        bool m_bSuspendLogout;
        System.IO.StreamWriter m_logFile;

        bool m_OnlyUnity = false;

        const int maxLines = 100000;
        List<String> m_logList;

        bool m_bCMDQueuing;

        //This queue contains ADB commands.
        //Reader is for dequeue the request CMD by ADBAccess
        //Writer is for enqueue new CMD by this Form1 class.
        Queue m_ADBCommandQueue;
        Queue m_ADBCommandQueueReader;
        Queue m_ADBCommandQueueWriter;


        private void StartADBThread()
        {
            m_adbListenThread = new Thread(new ThreadStart(StartListen));
            m_adbListenThread.Start();
        }
        void Init()
        {
            m_logList = new List<string>(5);

            m_bCMDQueuing = false;
            m_adbListenThread = null;
            bTest = true;
            /*
            String FILE_NAME = "AdbMessage";
            int suffix = 1;

            String fileName = @".\" + FILE_NAME + ".log";
            while (System.IO.File.Exists(fileName))
            {
                fileName = @".\" + FILE_NAME + suffix.ToString() + ".log";
                ++suffix;
            }

            //m_logFile = new System.IO.StreamWriter( fileName );
            */
            m_ADBCommandQueue = new Queue();
            m_ADBCommandQueueReader = Queue.Synchronized(m_ADBCommandQueue);
            m_ADBCommandQueueWriter = Queue.Synchronized(m_ADBCommandQueue);

            m_ADBAccess = new ADBAccess();
            m_ADBAccess.WinOutputQueueReader = m_ADBCommandQueueReader;
        }

        private void StartListen()
        {
            adbOutputQueue = new Queue(1000, 2);

            while (true)
            {
                try
                {
                    if (!m_ADBAccess.isCMDOutputQueueEmpty())
                    {
                        adbOutputQueue.Enqueue(m_ADBAccess.CMDOutputDequeue() + "\r\n");
                    }


                    if (0 == adbOutputQueue.Count)
                    {
                        //only flush at idle time
                        //m_logFile.Flush();
                        //only sleep when there is no string in the queue.
                        System.Threading.Thread.Sleep(2);
                    }
                    else
                    {
                        if (!m_bSuspendLogout)
                        {
                            DisplayMessage(adbOutputQueue.Dequeue().ToString());
                        }
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine("WinListen Thread Exception: " + err.Message);
                    //Cleanup();
                    break;
                }
            }
        }

        private delegate void DisplayDelegate(string message);
        private void DisplayMessage(string message)
        {
            DisplayDelegate logDelegate;
            m_logList.Add(message);
            try
            {
                Match match = Regex.Match(message, pattern);
                if (match.Success)
                {
                    char tagLevel = match.Value[0];
                    switch (tagLevel)
                    {
                        case 'I':
                        case 'V':
                        case 'D':
                            {
                                logDelegate = UnityEngine.Debug.Log;
                                break;
                            }
                        case 'W':
                            {
                                logDelegate = UnityEngine.Debug.LogWarning;
                                break;
                            }
                        case 'E':
                        case 'F':
                            {
                                logDelegate = UnityEngine.Debug.LogError;
                                break;
                            }
                        default:
                            {
                                throw(new Exception());
                            }
                    }
                    logDelegate(message);
                }
            }
            catch (Exception)
            {
                UnityEngine.Debug.LogError("Exception from RegEx");
            }

            //m_logFile.Write(message);
        }


        public void ADBStart()
        {
            Init();
            if (null == m_adbListenThread)
            {
                StartADBThread();
                System.Threading.Thread.Sleep(100);
                m_ADBAccess.StartCMD();
                System.Threading.Thread.Sleep(200);

                {
                    string ADBCMD;
                    m_ADBCommandQueue.Clear();
                    ADBCMD = "adb logcat -c";
                    m_bCMDQueuing = true;
                    m_ADBCommandQueueWriter.Enqueue(ADBCMD);
                    ADBCMD = "adb logcat -v time -s Unity ";
                    m_ADBCommandQueueWriter.Enqueue(ADBCMD);
                    m_bCMDQueuing = false;
                }
            }
            else
            {
                m_bSuspendLogout = false;
            }
        }

        public void Stop()
        {
            m_ADBAccess.Exit();
        }   
    }
}
