using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MyLibVLC
{
    // http://www.videolan.org/developers/vlc/doc/doxygen/html/group__libvlc.html

    static class LibVlc
    {
        #region core
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_new(int argc, [MarshalAs(UnmanagedType.LPArray,
          ArraySubType = UnmanagedType.LPStr)] string[] argv);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_release(IntPtr instance);
        #endregion

        #region media
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_media_new_location(IntPtr p_instance,
          IntPtr text);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_release(IntPtr p_meta_desc);
        #endregion

        #region media player
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_media_player_new_from_media(IntPtr media);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_release(IntPtr player);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_set_hwnd(IntPtr player, IntPtr drawable);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_media_player_get_media(IntPtr player);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_set_media(IntPtr player, IntPtr media);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_media_player_play(IntPtr player);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_pause(IntPtr player);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_stop(IntPtr player);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_media_player_get_state(IntPtr player);
        #endregion

        #region exception
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_clearerr();

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_errmsg();
        #endregion
    }

    class VlcException : Exception
    {
        protected string _err;

        public VlcException()
            : base()
        {
            IntPtr errorPointer = LibVlc.libvlc_errmsg();
            _err = errorPointer == IntPtr.Zero ? "VLC Exception"
                : Marshal.PtrToStringAuto(errorPointer);
        }

        public override string Message { get { return _err; } }
    }

    class VlcMedia : IDisposable
    {
        internal IntPtr Handle;

        public VlcMedia(IntPtr VlcInstanceHandle, string location)
        {
            byte[] retArray = Encoding.UTF8.GetBytes(location);
            byte[] retArrayZ = new byte[retArray.Length + 1];
            Array.Copy(retArray, retArrayZ, retArray.Length);
            retArrayZ[retArrayZ.Length - 1] = 0;
            IntPtr url = Marshal.AllocHGlobal(retArrayZ.Length);
            Marshal.Copy(retArrayZ, 0, url, retArrayZ.Length);

            Handle = LibVlc.libvlc_media_new_location(VlcInstanceHandle, url);
            if (Handle == IntPtr.Zero) throw new VlcException();
        }

        public void Dispose()
        {
            LibVlc.libvlc_media_release(Handle);
        }
    }

    public class SimpleRtspPlayer : IDisposable
    {
        private string mLocation;

        public string Location
        {
            get { return mLocation; }
            set { mLocation = value; }
        }
        private IntPtr mDrawHandle;

        public IntPtr DrawHandle
        {
            get { return mDrawHandle; }
            set { mDrawHandle = value; }
        }
        private IntPtr VlcInstanceHandle;
        private IntPtr VlcMediaPlayerHandle;
        private string[] args = new string[] {
                "-I", "dummy", "--ignore-config",
                @"--plugin-path=plugins"
            };
        //播放器状态，0:stop 1:playing
        private int playerState = 0;
        //0:stop 1:start 2:dispose
        private System.Collections.Queue cmdQueue = new System.Collections.Queue();

        private SemaphoreSlim tryDisposeSemaphore = new SemaphoreSlim(0);
        private SemaphoreSlim tmpSemaphore = new SemaphoreSlim(0);

        //发送命令，注意queue的线程安全
        private void sendCommand(int command)
        {
            lock (this.cmdQueue.SyncRoot)
            {
                this.cmdQueue.Enqueue(command);
                tmpSemaphore.Release();
            }
        }
        //辅助线程调用，用于获取命令，注意线程安全
        public int checkCommand()
        {
            lock (this.cmdQueue.SyncRoot)
            {
                if (cmdQueue.Count > 0)
                {
                    return Convert.ToInt32(this.cmdQueue.Dequeue());
                }
                else
                {
                    //没有命令
                    return -1;
                }
            }
        }

        public SimpleRtspPlayer(string location, IntPtr drawHandle)
        {
            mLocation = location;
            mDrawHandle = drawHandle;
            VlcInstanceHandle = LibVlc.libvlc_new(args.Length, args);
            if (VlcInstanceHandle == IntPtr.Zero) throw new VlcException();

            Thread tt = new Thread(() =>
            {
                int vlcState;
                int command;
                while (true)
                {
                    command = checkCommand();
                    switch(command)
                    {
                            //没有命令
                        case -1:
                            if (playerState == 1)
                            {
                                //IDLE/CLOSE=0, OPENING=1, BUFFERING=2, PLAYING=3, PAUSED=4, STOPPING=5, ENDED=6, ERROR=7
                                vlcState = LibVlc.libvlc_media_player_get_state(VlcMediaPlayerHandle);
                                if (vlcState == 7 || vlcState == 6)//检测到错误，重启并暂停0.5秒
                                {
                                    //停止播放
                                    TryStop();
                                    TryPlay();
                                    Thread.Sleep(500);
                                }
                                else//没有检测到错误，暂停0.1秒
                                {
                                    Thread.Sleep(100);
                                }
                            }
                            else//入口：没有命令且stop状态
                            {
                                tmpSemaphore.Wait();
                            }
                            break;
                        case 0://停止播放命令
                            if (playerState == 1)
                            {
                                TryStop();
                                playerState = 0;
                                //第一次wait消耗信号
                                tmpSemaphore.Wait();
                                //第二次wait等待信号
                                tmpSemaphore.Wait();
                            }
                            else
                            {
                                //此次的信号由case 0：playerState == 1消耗
                                //但是在停止状态，所以继续wait
                                tmpSemaphore.Wait();
                            }
                            break;
                        case 1://开始播放命令
                            if (playerState != 1)
                            {//此次的信号由case 0：playerState == 1消耗
                                TryPlay();
                                playerState = 1;
                            }
                            else
                            {
                                //只需要消耗一个信号
                                tmpSemaphore.Wait();
                            }
                            break;
                            //垃圾回收命令,不是break，而是return。
                        case 2:
                            //消耗一个信号
                            if (playerState == 1)
                            {
                                tmpSemaphore.Wait();
                            }
                            TryDispose();
                            return;
                    }
                }
            });
            tt.IsBackground = true;
            tt.Start();
        }

        public void Stop()
        {
            sendCommand(0);
        }

        public void Play()
        {
            sendCommand(1);
        }

        public void Dispose()
        {
            sendCommand(2);
            tryDisposeSemaphore.Wait();
        }

        private void TryPlay()
        {
            using (VlcMedia media = new VlcMedia(VlcInstanceHandle, mLocation))
            {
                VlcMediaPlayerHandle = LibVlc.libvlc_media_player_new_from_media(media.Handle);
                if (VlcMediaPlayerHandle == IntPtr.Zero) throw new VlcException();
            }
            LibVlc.libvlc_media_player_set_hwnd(VlcMediaPlayerHandle, mDrawHandle);

            LibVlc.libvlc_media_player_play(VlcMediaPlayerHandle);
        }

        private void TryStop()
        {
            LibVlc.libvlc_media_player_stop(VlcMediaPlayerHandle);
            LibVlc.libvlc_media_player_release(VlcMediaPlayerHandle);
        }

        private void TryDispose()
        {
            if (playerState == 1) TryStop();
            LibVlc.libvlc_release(VlcInstanceHandle);
            tryDisposeSemaphore.Release();
        }
    }
}
