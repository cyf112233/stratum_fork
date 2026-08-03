using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Avalonia.Threading;

namespace Stratum.Desktop.Services
{
    public static class SingleInstance
    {
        private static string _lockPath;
        private static string _socketPath;
        private static FileStream _lockStream;
        private static Socket _listener;

        public static event Action Activated;

        public static void Initialize(string dataDirectory)
        {
            _lockPath = Path.Combine(dataDirectory, "instance.lock");
            _socketPath = Path.Combine(dataDirectory, "instance.sock");
            Directory.CreateDirectory(dataDirectory);
        }

        public static bool TryActivateExisting()
        {
            if (_socketPath == null)
            {
                return false;
            }

            try
            {
                using var client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                client.Connect(new UnixDomainSocketEndPoint(_socketPath));
                client.Send(Encoding.UTF8.GetBytes("activate"));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool Acquire()
        {
            if (_lockPath == null)
            {
                return true;
            }

            try
            {
                _lockStream = new FileStream(_lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                    FileShare.None);
            }
            catch (IOException)
            {
                for (var i = 0; i < 10; i++)
                {
                    if (TryActivateExisting())
                    {
                        return false;
                    }

                    Thread.Sleep(100);
                }

                return false;
            }

            try
            {
                if (File.Exists(_socketPath))
                {
                    File.Delete(_socketPath);
                }

                _listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                _listener.Bind(new UnixDomainSocketEndPoint(_socketPath));
                _listener.Listen(1);
                var thread = new Thread(ListenLoop) { IsBackground = true };
                thread.Start();
            }
            catch
            {
                // Socket 失败不阻塞启动
            }

            return true;
        }

        public static void Release()
        {
            try
            {
                _listener?.Dispose();
            }
            catch
            {
            }

            try
            {
                _lockStream?.Dispose();
            }
            catch
            {
            }

            try
            {
                if (_socketPath != null && File.Exists(_socketPath))
                {
                    File.Delete(_socketPath);
                }
            }
            catch
            {
            }
        }

        private static void ListenLoop()
        {
            while (_listener != null)
            {
                try
                {
                    using var client = _listener.Accept();
                    var buffer = new byte[64];
                    client.Receive(buffer);
                    Dispatcher.UIThread.Post(() => Activated?.Invoke());
                }
                catch
                {
                    return;
                }
            }
        }
    }
}
