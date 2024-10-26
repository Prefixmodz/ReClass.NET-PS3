using System;
using System.Drawing;
using System.Windows.Forms;
using ReClassNET.Core;
using ReClassNET.Debugger;
using ReClassNET.Plugins;
using ReClassNET.Util.Conversion;
using PS3Plugin.Pointer32;
using PS3Plugin.TextPointers;
using System.Runtime.CompilerServices;
using ReClassNET.Memory;

namespace PS3Plugin
{
    public class PS3PluginExt : Plugin, ICoreProcessFunctions
    {
        private readonly object sync = new object();

        private IPluginHost host;
        private static TMAPI PS3 = new TMAPI();

        private byte[] lastBuffer;
        private uint lastAddress;

        public override Image Icon => Properties.Resources.icon;

        public override bool Initialize(IPluginHost host)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));

            host.Process.CoreFunctions.RegisterFunctions("PS3", this);
            host.Process.BitConverter = EndianBitConverter.Big;

            return true;
        }

        public override void Terminate()
        {
            host = null;
        }

        public bool IsProcessValid(IntPtr process)
        {
            return true;
        }

        public IntPtr OpenRemoteProcess(IntPtr Pid, ProcessAccess desiredAccess)
        {
            //host.MainWindow.CurrentClassNode.AddressFormula = ((uint)(id.ToInt64() & 0xFFFFFFFF)).ToString("X");
            //return id;
            lock (sync)
            {
                try
                {
                    
                }
                catch (Exception ex)
                {

                }
                return Pid;
            }
        }

        public void CloseRemoteProcess(IntPtr process)
        {
            
        }

        public bool ReadRemoteMemory(IntPtr process, IntPtr address, ref byte[] buffer, int offset, int size)
        {
            lock (sync)
            {
                try
                {
                    uint newAddress = (uint)(address.ToInt64() & 0xFFFFFFFF);
                    byte[] newBuffer = PS3.Ext.ReadBytes(newAddress, size);
                    if (checkNotEmpty(newBuffer))
                    {
                        Buffer.BlockCopy(newBuffer, 0, buffer, 0, size);
                        lastAddress = newAddress;
                        lastBuffer = newBuffer;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                // Prevents data from nulling when peeking memory fails.
                if (lastBuffer != null && (uint)(address.ToInt64() & 0xFFFFFFFF) == lastAddress)
                {
                    Buffer.BlockCopy(lastBuffer, 0, buffer, 0, size);
                    return true;
                }

                return false;
            }
        }

        private bool checkNotEmpty(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0) return false;
            foreach (byte b in buffer)
            {
                if (b > 0) return true;
            }

            return false;
        }

        public bool WriteRemoteMemory(IntPtr process, IntPtr address, ref byte[] buffer, int offset, int size)
        {
            lock (sync)
            {
                try
                {
                    uint newAddr = (uint)(address.ToInt64() & 0xFFFFFFFF);
                    byte[] newBuffer = new byte[size];
                    Buffer.BlockCopy(buffer, 0, newBuffer, 0, size);
                    PS3.SetMemory(newAddr, newBuffer);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
        }

        public void EnumerateProcesses(EnumerateProcessCallback callbackProcess)
        {
            lock (sync)
            {
                try
                {
                    PS3.ConnectTarget();
                    PS3.AttachProcess();

                    PS3TMAPI.GetProcessList(TMAPI.Target, out TMAPI.Parameters.ProcessIDs);
                    foreach (uint proc in TMAPI.Parameters.ProcessIDs)
                    {
                        var processdata = new EnumerateProcessData
                        {

                            Id = (IntPtr)proc,
                            Name = PS3.GetProcessName(proc),
                            Path = PS3.GetProcessPath(proc),

                        };
                        callbackProcess(ref processdata);
                    }
                    var sectiondata = new SectionProtection()
                    {
                        
                    };
                    
                }

                catch (Exception ex)
                {
                    MessageBox.Show("Could not connect PS3", "Connection Error");
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public void EnumerateRemoteSectionsAndModules(IntPtr process, EnumerateRemoteSectionCallback callbackSection, EnumerateRemoteModuleCallback callbackModule)
        {
            lock (sync)
            {
                try
                {
                   
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }


        public void ControlRemoteProcess(IntPtr process, ControlRemoteProcessAction action)
        {
            switch (action) { case ControlRemoteProcessAction.Suspend: PS3.ProcessStop(); break; case ControlRemoteProcessAction.Resume: PS3.ProcessContinue(); break; case ControlRemoteProcessAction.Terminate: PS3.ProcessKill(); break; }
        }

        public bool AttachDebuggerToProcess(IntPtr id)
        {
            lock (sync)
            {
                PS3.AttachProcess();

            }
            return true;
        }

        public void DetachDebuggerFromProcess(IntPtr id)
        {
            // Not supported.
        }

        public bool AwaitDebugEvent(ref DebugEvent evt, int timeoutInMilliseconds)
        {
            // Not supported.

            return false;
        }

        public void HandleDebugEvent(ref DebugEvent evt)
        {
            lock(sync) { PS3TMAPI.ProcessContinue(TMAPI.Target, TMAPI.Parameters.ProcessID); }
        }

        public bool SetHardwareBreakpoint(IntPtr id, IntPtr address, HardwareBreakpointRegister register, HardwareBreakpointTrigger trigger, HardwareBreakpointSize size, bool set)
        {
            // Not supported.
            return false;
        }

        public override CustomNodeTypes GetCustomNodeTypes()
        {
            return new CustomNodeTypes
            {
                CodeGenerator = new PS3PluginCodeGenerator(),
                Serializer = new PS3PluginNodeConverter(),
                NodeTypes = new[] { typeof(Pointer32Node), typeof(Utf16TextPtr32Node), typeof(Utf8TextPtr32Node) }
            };
        }
    }
}
