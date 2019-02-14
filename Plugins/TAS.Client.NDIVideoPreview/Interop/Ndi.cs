using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace TAS.Client.NDIVideoPreview.Interop
{
    [SuppressUnmanagedCodeSecurity]
    public static class Ndi
    {

        public static bool NDIlib_initialize()
        {
            if (Is64Bit)
                return NDIlib64_initialize();
            else
                return NDIlib32_initialize();
        }

        public static void NDIlib_destroy()
        {
            if (Is64Bit)
                NDIlib64_destroy();
            else
                NDIlib32_destroy();
        }

        public static bool NDIlib_is_supported_CPU()
        {
            if (Is64Bit)
                return NDIlib64_is_supported_CPU();
            else
                return NDIlib32_is_supported_CPU();
        }

        static readonly bool Is64Bit = IntPtr.Size == 8;

        #region Utilities

        /// <summary>
        /// Adds runtime NDI_RUNTIME_DIR_V2 path to dll search path
        /// </summary>
        public static void AddRuntimeDir()
        {
            string ndi_path = Environment.GetEnvironmentVariable("NDI_RUNTIME_DIR_V2");
            if (!string.IsNullOrWhiteSpace(ndi_path))
                SetDllDirectory(ndi_path);
        }

        /// <summary>
        /// Converts string to IntPtr.
        /// </summary>
        /// <remarks>
        /// You have to use Marshal.FreeHGlobal() on the returned pointer.
        /// </remarks>
        /// <param name="str"></param>
        /// <returns></returns>
        public static IntPtr StringToUtf8(string str)
        {
            int len = Encoding.UTF8.GetByteCount(str);
            byte[] buffer = new byte[len + 1];
            Encoding.UTF8.GetBytes(str, 0, str.Length, buffer, 0);
            IntPtr nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);
            return nativeUtf8;
        }

        /// <summary>
        /// Same as StringToUtf8, but also returns length of returned object
        /// </summary>
        /// <remarks>
        /// You have to use Marshal.FreeHGlobal() on the returned pointer.
        /// </remarks>
        /// <param name="str"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static IntPtr StringToUtf8(string str, out int length)
        {
            length = Encoding.UTF8.GetByteCount(str);
            byte[] buffer = new byte[length + 1];
            Encoding.UTF8.GetBytes(str, 0, str.Length, buffer, 0);
            IntPtr nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);
            return nativeUtf8;
        }

        /// <summary>
        /// Convert IntPtr containing UTF-8 string to managed string
        /// </summary>
        /// <param name="nativeUtf8"></param>
        /// <returns></returns>
        public static string Utf8ToString(IntPtr nativeUtf8)
        {
            int len = 0;
            while (Marshal.ReadByte(nativeUtf8, len) != 0) ++len;
            byte[] buffer = new byte[len];
            Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }

        public static long NDIlib_send_timecode_synthesize = long.MaxValue;

        #endregion Utilities

        #region Find

        /// <summary>
        /// Create a new finder instance. This will return IntPtr.Zero if it fails.
        /// </summary>
        /// <param name="p_create_settings"></param>
        /// <returns></returns>
        public static IntPtr NDIlib_find_create2(ref NDIlib_find_create_t p_create_settings)
        {
            if (Is64Bit)
                return NDIlib64_find_create2(ref p_create_settings);
            else
                return NDIlib32_find_create2(ref p_create_settings);
        }

        /// <summary>
        /// Destroy an existing finder instance.
        /// </summary>
        /// <param name="p_instance"></param>
        public static void NDIlib_find_destroy(IntPtr p_instance)
        {
            if (Is64Bit)
                NDIlib64_find_destroy(p_instance);
            else
                NDIlib32_find_destroy(p_instance);
        }

        /// <summary>
        /// Wait until the number of online sources have changed.
        /// </summary>
        /// <param name="p_instance"></param>
        /// <param name="timeout_in_ms"></param>
        /// <returns></returns>
        public static bool NDIlib_find_wait_for_sources(IntPtr p_instance, int timeout_in_ms)
        {
            if (Is64Bit)
                return NDIlib64_find_wait_for_sources(p_instance, timeout_in_ms);
            else
                return NDIlib32_find_wait_for_sources(p_instance, timeout_in_ms);
        }

        /// <summary>
        /// Recover the current set of sources (i.e. the ones that exist right this second).
        /// </summary>
        /// <param name="p_instance"></param>
        /// <param name="p_no_sources"></param>
        /// <returns></returns>
        public static IntPtr NDIlib_find_get_current_sources(IntPtr p_instance, ref int p_no_sources)
        {
            if (Is64Bit)
                return NDIlib64_find_get_current_sources(p_instance, ref p_no_sources);
            else
                return NDIlib32_find_get_current_sources(p_instance, ref p_no_sources);
        }

        #endregion Find

        #region Recv

        /// <summary>
        /// Create a new receiver instance. This will return null if it fails.
        /// </summary>
        /// <param name="p_create_settings"></param>
        /// <returns></returns>
        public static IntPtr NDIlib_recv_create(ref NDIlib_recv_create_t p_create_settings)
        {
            if (Is64Bit)
                return NDIlib64_recv_create(ref p_create_settings);
            else
                return NDIlib32_recv_create(ref p_create_settings);
        }

        /// <summary>
        /// Destroy an existing receiver instance.
        /// </summary>
        /// <param name="p_instance"></param>
        public static void NDIlib_recv_destroy(IntPtr p_instance)
        {
            if (Is64Bit)
                NDIlib64_recv_destroy(p_instance);
            else
                NDIlib32_recv_destroy(p_instance);
        }

        /// <summary>
        /// Receive video, audio and meta-data frames.
        /// Any of the buffers can be NULL, in which case data of that type
        /// will not be captured in this call. This call can be called simultaneously
        /// on separate threads, so it is entirely possible to receive audio, video, metadata
        /// all on separate threads. This function will return NDIlib_frame_type_none if no
        /// data is received within the specified timeout. Buffers captured with this must
        /// be freed with the appropriate free function below.
        /// </summary>
        /// <param name="p_instance"></param>
        /// <param name="p_video_data"></param>
        /// <param name="p_audio_data"></param>
        /// <param name="p_meta_data"></param>
        /// <param name="timeout_in_ms"></param>
        /// <returns></returns>
        public static NDIlib_frame_type_e NDIlib_recv_capture(
                        IntPtr p_instance,                          // The library instance
                        ref NDIlib_video_frame_t p_video_data,      // The video data received (can be null)
                        ref NDIlib_audio_frame_t p_audio_data,		// The audio data received (can be null)
                        ref NDIlib_metadata_frame_t p_meta_data,    // The meta data data received (can be null)
                        uint timeout_in_ms)				            // The ammount of time in milliseconds to wait for data.
        {
            if (Is64Bit)
                return NDIlib64_recv_capture(p_instance, ref p_video_data, ref p_audio_data, ref p_meta_data, timeout_in_ms);
            else
                return NDIlib32_recv_capture(p_instance, ref p_video_data, ref p_audio_data, ref p_meta_data, timeout_in_ms);
        }

        // 
        /// <summary>
        /// Same as NDIlib_recv_capture, but only asks for video
        /// </summary>
        /// <param name="p_instance"></param>
        /// <param name="p_video_data"></param>
        /// <param name="timeout_in_ms"></param>
        /// <returns></returns>
        public static NDIlib_frame_type_e NDIlib_recv_capture(
                        IntPtr p_instance,                          // The library instance
                        ref NDIlib_video_frame_t p_video_data,      // The video data received (can be null)
                        uint timeout_in_ms)				            // The ammount of time in milliseconds to wait for data.
        {
            if (Is64Bit)
                return NDIlib64_recv_capture(p_instance, ref p_video_data, IntPtr.Zero, IntPtr.Zero, timeout_in_ms);
            else
                return NDIlib32_recv_capture(p_instance, ref p_video_data, IntPtr.Zero, IntPtr.Zero, timeout_in_ms);
        }

        /// <summary>
        /// Same as NDIlib_recv_capture, but only asks for audio
        /// </summary>
        /// <param name="p_instance"></param>
        /// <param name="p_audio_data"></param>
        /// <param name="timeout_in_ms"></param>
        /// <returns></returns>
        public static NDIlib_frame_type_e NDIlib_recv_capture(
                        IntPtr p_instance,                          // The library instance
                        ref NDIlib_audio_frame_t p_audio_data,      // The video data received (can be null)
                        uint timeout_in_ms)				            // The ammount of time in milliseconds to wait for data.
        {
            if (Is64Bit)
                return NDIlib64_recv_capture(p_instance, IntPtr.Zero, ref p_audio_data, IntPtr.Zero, timeout_in_ms);
            else
                return NDIlib32_recv_capture(p_instance, IntPtr.Zero, ref p_audio_data, IntPtr.Zero, timeout_in_ms);
        }

        /// <summary>
        /// Same as NDIlib_recv_capture, but only asks for metadata
        /// </summary>
        /// <param name="p_instance"></param>
        /// <param name="p_meta_data"></param>
        /// <param name="timeout_in_ms"></param>
        /// <returns></returns>
        public static NDIlib_frame_type_e NDIlib_recv_capture(
                        IntPtr p_instance,                          // The library instance
                        ref NDIlib_metadata_frame_t p_meta_data,    // The meta data data received (can be null)
                        uint timeout_in_ms)				            // The ammount of time in milliseconds to wait for data.
        {
            if (Is64Bit)
                return NDIlib64_recv_capture(p_instance, IntPtr.Zero, IntPtr.Zero, ref p_meta_data, timeout_in_ms);
            else
                return NDIlib32_recv_capture(p_instance, IntPtr.Zero, IntPtr.Zero, ref p_meta_data, timeout_in_ms);
        }

        /// <summary>
        /// Free buffers returned by capture for video
        /// </summary>
        /// <param name="p_instance"></param>
        /// <param name="p_video_data"></param>
        public static void NDIlib_recv_free_video(IntPtr p_instance, ref NDIlib_video_frame_t p_video_data)
        {
            if (Is64Bit)
                NDIlib64_recv_free_video(p_instance, ref p_video_data);
            else
                NDIlib32_recv_free_video(p_instance, ref p_video_data);
        }

        /// <summary>
        /// Free buffers returned by capture for audio
        /// </summary>
        /// <param name="p_instance"></param>
        /// <param name="p_audio_data"></param>
        public static void NDIlib_recv_free_audio(IntPtr p_instance, ref NDIlib_audio_frame_t p_audio_data)
        {
            if (Is64Bit)
                NDIlib64_recv_free_audio(p_instance, ref p_audio_data);
            else
                NDIlib32_recv_free_audio(p_instance, ref p_audio_data);
        }

        // Free the buffers returned by capture for meta-data
        public static void NDIlib_recv_free_metadata(IntPtr p_instance, ref NDIlib_metadata_frame_t p_meta_data)
        {
            if (Is64Bit)
                NDIlib64_recv_free_metadata(p_instance, ref p_meta_data);
            else
                NDIlib32_recv_free_metadata(p_instance, ref p_meta_data);
        }


        /// <summary>
        /// Send a meta message to the source that we are connected too. This returns FALSE if we are
        /// not currently connected to anything.
        /// </summary>
        /// <param name="p_instance"></param>
        /// <param name="p_meta_data"></param>
        /// <returns></returns>
        public static bool NDIlib_recv_send_metadata(IntPtr p_instance, ref NDIlib_metadata_frame_t p_meta_data)
        {
            if (Is64Bit)
                return NDIlib64_recv_send_metadata(p_instance, ref p_meta_data);
            else
                return NDIlib32_recv_send_metadata(p_instance, ref p_meta_data);
        }

        /// <summary>
        /// Set up-stream tally notifications. This returns FALSE if we are not currently connected to anything. That
        /// said, the moment that we do connect to something it will automatically be sent the tally state.
        /// </summary>
        /// <param name="p_instance"></param>
        /// <param name="p_tally"></param>
        /// <returns></returns>
        public static bool NDIlib_recv_set_tally(IntPtr p_instance, ref NDIlib_tally_t p_tally)
        {
            if (Is64Bit)
                return NDIlib64_recv_set_tally(p_instance, ref p_tally);
            else
                return NDIlib32_recv_set_tally(p_instance, ref p_tally);
        }

        /// <summary>
        /// Get the current performance structures. This can be used to determine if you have been calling NDIlib_recv_capture fast
        /// enough, or if your processing of data is not keeping up with real-time. The total structure will give you the total frame
        /// counts received, the dropped structure will tell you how many frames have been dropped. Either of these could be null.
        /// </summary>
        /// <param name="p_instance"></param>
        /// <param name="p_total"></param>
        /// <param name="p_dropped"></param>
        public static void NDIlib_recv_get_performance(IntPtr p_instance, ref NDIlib_recv_performance_t p_total, ref NDIlib_recv_performance_t p_dropped)
        {
            if (Is64Bit)
                NDIlib64_recv_get_performance(p_instance, ref p_total, ref p_dropped);
            else
                NDIlib32_recv_get_performance(p_instance, ref p_total, ref p_dropped);
        }

        /// <summary>
        /// This will allow you to determine the current queue depth for all of the frame sources at any time.
        /// </summary>
        /// <param name="p_instance"></param>
        /// <param name="p_total"></param>
        public static void NDIlib_recv_get_queue(IntPtr p_instance, ref NDIlib_recv_queue_t p_total)
        {
            if (Is64Bit)
                NDIlib64_recv_get_queue(p_instance, ref p_total);
            else
                NDIlib32_recv_get_queue(p_instance, ref p_total);
        }

        /// <summary>
        /// Connection based metadata is data that is sent automatically each time a new connection is received. You queue all of these
        /// up and they are sent on each connection. To reset them you need to clear them all and set them up again.
        /// </summary>
        /// <param name="p_instance"></param>
        public static void NDIlib_recv_clear_connection_metadata(IntPtr p_instance)
        {
            if (Is64Bit)
                NDIlib64_recv_clear_connection_metadata(p_instance);
            else
                NDIlib32_recv_clear_connection_metadata(p_instance);
        }

        /// <summary>
        /// Add a connection metadata string to the list of what is sent on each new connection. If someone is already connected then
        /// this string will be sent to them immediately.
        /// </summary>
        /// <param name="p_instance"></param>
        /// <param name="p_metadata"></param>
        public static void NDIlib_recv_add_connection_metadata(IntPtr p_instance, ref NDIlib_metadata_frame_t p_metadata)
        {
            if (Is64Bit)
                NDIlib64_recv_add_connection_metadata(p_instance, ref p_metadata);
            else
                NDIlib64_recv_add_connection_metadata(p_instance, ref p_metadata);
        }
        #endregion

        #region Utilities

        public static void NDIlib_util_audio_to_interleaved_16s(ref NDIlib_audio_frame_t p_src, ref NDIlib_audio_frame_interleaved_16s_t p_dst)
        {
            if (Is64Bit)
                NDIlib64_util_audio_to_interleaved_16s(ref p_src, ref p_dst);
            else
                NDIlib32_util_audio_to_interleaved_16s(ref p_src, ref p_dst);
        }

        public static void NDIlib_util_audio_to_interleaved_32f(ref NDIlib_audio_frame_t p_src, ref NDIlib_audio_frame_interleaved_32f_t p_dst)
        {
            if (Is64Bit)
                NDIlib64_util_audio_to_interleaved_32f(ref p_src, ref p_dst);
            else
                NDIlib32_util_audio_to_interleaved_32f(ref p_src, ref p_dst);
        }

        public static void NDIlib_util_audio_from_interleaved_16s(ref NDIlib_audio_frame_interleaved_16s_t p_src, ref NDIlib_audio_frame_t p_dst)
        {
            if (Is64Bit)
                NDIlib64_util_audio_from_interleaved_16s(ref p_src, ref p_dst);
            else
                NDIlib32_util_audio_from_interleaved_16s(ref p_src, ref p_dst);
        }

        public static void NDIlib_util_audio_from_interleaved_32f(ref NDIlib_audio_frame_interleaved_32f_t p_src, ref NDIlib_audio_frame_t p_dst)
        {
            if (Is64Bit)
                NDIlib64_util_audio_from_interleaved_32f(ref p_src, ref p_dst);
            else
                NDIlib32_util_audio_from_interleaved_32f(ref p_src, ref p_dst);
        }

        #endregion

        #region PInvoke

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetDllDirectory(string lpPathName);

        const string NDILib64Name = "Processing.NDI.Lib.x64.dll";
        const string NDILib32Name = "Processing.NDI.Lib.x86.dll";

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_initialize", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool NDIlib32_initialize();
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_initialize", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool NDIlib64_initialize();

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib32_destroy();
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib64_destroy();

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_is_supported_CPU", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool NDIlib32_is_supported_CPU();
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_is_supported_CPU", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool NDIlib64_is_supported_CPU();

        #region Find
        [DllImport(NDILib32Name, EntryPoint = "NDIlib_find_create2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr NDIlib32_find_create2(ref NDIlib_find_create_t p_create_settings);
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_find_create2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr NDIlib64_find_create2(ref NDIlib_find_create_t p_create_settings);

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_find_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib32_find_destroy(IntPtr p_instance);
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_find_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib64_find_destroy(IntPtr p_instance);

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_find_wait_for_sources", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool NDIlib32_find_wait_for_sources(IntPtr p_instance, int timeout_in_ms);
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_find_wait_for_sources", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool NDIlib64_find_wait_for_sources(IntPtr p_instance, int timeout_in_ms);

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_find_get_current_sources", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr NDIlib32_find_get_current_sources(IntPtr p_instance, ref int p_no_sources);
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_find_get_current_sources", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr NDIlib64_find_get_current_sources(IntPtr p_instance, ref int p_no_sources);
        #endregion Find

        #region Recv
        [DllImport(NDILib32Name, EntryPoint = "NDIlib_recv_create2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr NDIlib32_recv_create(ref NDIlib_recv_create_t p_create_settings);
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_recv_create2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr NDIlib64_recv_create(ref NDIlib_recv_create_t p_create_settings);

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_recv_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib32_recv_destroy(IntPtr p_instance);
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_recv_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib64_recv_destroy(IntPtr p_instance);

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_recv_capture", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern NDIlib_frame_type_e NDIlib32_recv_capture(
                        IntPtr p_instance,                          // The library instance
                        ref NDIlib_video_frame_t p_video_data,      // The video data received (can be null)
                        ref NDIlib_audio_frame_t p_audio_data,		// The audio data received (can be null)
                        ref NDIlib_metadata_frame_t p_meta_data,    // The meta data data received (can be null)
                        uint timeout_in_ms);				        // The ammount of time in milliseconds to wait for data.
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_recv_capture", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern NDIlib_frame_type_e NDIlib64_recv_capture(
                        IntPtr p_instance,                          // The library instance
                        ref NDIlib_video_frame_t p_video_data,      // The video data received (can be null)
                        ref NDIlib_audio_frame_t p_audio_data,		// The audio data received (can be null)
                        ref NDIlib_metadata_frame_t p_meta_data,    // The meta data data received (can be null)
                        uint timeout_in_ms);

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_recv_capture", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern NDIlib_frame_type_e NDIlib32_recv_capture(
                        IntPtr p_instance,                          // The library instance
                        ref NDIlib_video_frame_t p_video_data,      // The video data received (can be null)
                        IntPtr p_audio_data,		                // The audio data received (can be null)
                        IntPtr p_meta_data,                         // The meta data data received (can be null)
                        uint timeout_in_ms);				        // The ammount of time in milliseconds to wait for data.
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_recv_capture", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern NDIlib_frame_type_e NDIlib64_recv_capture(
                        IntPtr p_instance,                          // The library instance
                        ref NDIlib_video_frame_t p_video_data,      // The video data received (can be null)
                        IntPtr p_audio_data,		                // The audio data received (can be null)
                        IntPtr p_meta_data,                         // The meta data data received (can be null)
                        uint timeout_in_ms);

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_recv_capture", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern NDIlib_frame_type_e NDIlib32_recv_capture(
                        IntPtr p_instance,                          // The library instance
                        IntPtr p_video_data,                        // The video data received (can be null)
                        ref NDIlib_audio_frame_t p_audio_data,      // The audio data received (can be null)
                        IntPtr p_meta_data,                         // The meta data data received (can be null)
                        uint timeout_in_ms);				        // The ammount of time in milliseconds to wait for data.
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_recv_capture", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern NDIlib_frame_type_e NDIlib64_recv_capture(
                        IntPtr p_instance,                          // The library instance
                        IntPtr p_video_data,                        // The video data received (can be null)
                        ref NDIlib_audio_frame_t p_audio_data,      // The audio data received (can be null)
                        IntPtr p_meta_data,                         // The meta data data received (can be null)
                        uint timeout_in_ms);

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_recv_capture", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern NDIlib_frame_type_e NDIlib32_recv_capture(
                        IntPtr p_instance,                          // The library instance
                        IntPtr p_video_data,                        // The video data received (can be null)
                        IntPtr p_audio_data,		                // The audio data received (can be null)
                        ref NDIlib_metadata_frame_t p_meta_data,    // The meta data data received (can be null)
                        uint timeout_in_ms);				        // The ammount of time in milliseconds to wait for data.
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_recv_capture", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern NDIlib_frame_type_e NDIlib64_recv_capture(
                        IntPtr p_instance,                          // The library instance
                        IntPtr p_video_data,                        // The video data received (can be null)
                        IntPtr p_audio_data,		                // The audio data received (can be null)
                        ref NDIlib_metadata_frame_t p_meta_data,    // The meta data data received (can be null)
                        uint timeout_in_ms);

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_recv_free_video", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib32_recv_free_video(IntPtr p_instance, ref NDIlib_video_frame_t p_video_data);
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_recv_free_video", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib64_recv_free_video(IntPtr p_instance, ref NDIlib_video_frame_t p_video_data);

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_recv_free_audio", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib32_recv_free_audio(IntPtr p_instance, ref NDIlib_audio_frame_t p_audio_data);
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_recv_free_audio", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib64_recv_free_audio(IntPtr p_instance, ref NDIlib_audio_frame_t p_audio_data);

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_recv_free_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib32_recv_free_metadata(IntPtr p_instance, ref NDIlib_metadata_frame_t p_meta_data);
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_recv_free_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib64_recv_free_metadata(IntPtr p_instance, ref NDIlib_metadata_frame_t p_meta_data);

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_recv_send_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAsAttribute(UnmanagedType.U1)]
        private static extern bool NDIlib32_recv_send_metadata(IntPtr p_instance, ref NDIlib_metadata_frame_t p_meta_data);
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_recv_send_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAsAttribute(UnmanagedType.U1)]
        private static extern bool NDIlib64_recv_send_metadata(IntPtr p_instance, ref NDIlib_metadata_frame_t p_meta_data);

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_recv_set_tally", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAsAttribute(UnmanagedType.U1)]
        private static extern bool NDIlib32_recv_set_tally(IntPtr p_instance, ref NDIlib_tally_t p_tally);
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_recv_set_tally", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAsAttribute(UnmanagedType.U1)]
        private static extern bool NDIlib64_recv_set_tally(IntPtr p_instance, ref NDIlib_tally_t p_tally);

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_recv_get_performance", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib32_recv_get_performance(IntPtr p_instance, ref NDIlib_recv_performance_t p_total, ref NDIlib_recv_performance_t p_dropped);
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_recv_get_performance", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib64_recv_get_performance(IntPtr p_instance, ref NDIlib_recv_performance_t p_total, ref NDIlib_recv_performance_t p_dropped);

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_recv_get_queue", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib32_recv_get_queue(IntPtr p_instance, ref NDIlib_recv_queue_t p_total);
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_recv_get_queue", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib64_recv_get_queue(IntPtr p_instance, ref NDIlib_recv_queue_t p_total);

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_recv_clear_connection_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib32_recv_clear_connection_metadata(IntPtr p_instance);
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_recv_clear_connection_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib64_recv_clear_connection_metadata(IntPtr p_instance);

        [DllImport(NDILib32Name, EntryPoint = "NDIlib_recv_add_connection_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib32_recv_add_connection_metadata(IntPtr p_instance, ref NDIlib_metadata_frame_t p_metadata);
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_recv_add_connection_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib64_recv_add_connection_metadata(IntPtr p_instance, ref NDIlib_metadata_frame_t p_metadata);
        #endregion Recv

        #region Utilities
        // util_audio_to_interleaved_16s 
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_util_audio_to_interleaved_16s", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib64_util_audio_to_interleaved_16s(ref NDIlib_audio_frame_t p_src, ref NDIlib_audio_frame_interleaved_16s_t p_dst);
        [DllImport(NDILib32Name, EntryPoint = "NDIlib_util_audio_to_interleaved_16s", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib32_util_audio_to_interleaved_16s(ref NDIlib_audio_frame_t p_src, ref NDIlib_audio_frame_interleaved_16s_t p_dst);

        // util_audio_from_interleaved_16s 
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_util_audio_from_interleaved_16s", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib64_util_audio_from_interleaved_16s(ref NDIlib_audio_frame_interleaved_16s_t p_src, ref NDIlib_audio_frame_t p_dst);
        [DllImport(NDILib32Name, EntryPoint = "NDIlib_util_audio_from_interleaved_16s", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib32_util_audio_from_interleaved_16s(ref NDIlib_audio_frame_interleaved_16s_t p_src, ref NDIlib_audio_frame_t p_dst);


        // util_audio_to_interleaved_32f 
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_util_audio_to_interleaved_32f", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib64_util_audio_to_interleaved_32f(ref NDIlib_audio_frame_t p_src, ref NDIlib_audio_frame_interleaved_32f_t p_dst);
        [DllImport(NDILib32Name, EntryPoint = "NDIlib_util_audio_to_interleaved_32f", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib32_util_audio_to_interleaved_32f(ref NDIlib_audio_frame_t p_src, ref NDIlib_audio_frame_interleaved_32f_t p_dst);

        // util_audio_from_interleaved_32f 
        [DllImport(NDILib64Name, EntryPoint = "NDIlib_util_audio_from_interleaved_32f", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib64_util_audio_from_interleaved_32f(ref NDIlib_audio_frame_interleaved_32f_t p_src, ref NDIlib_audio_frame_t p_dst);
        [DllImport(NDILib32Name, EntryPoint = "NDIlib_util_audio_from_interleaved_32f", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NDIlib32_util_audio_from_interleaved_32f(ref NDIlib_audio_frame_interleaved_32f_t p_src, ref NDIlib_audio_frame_t p_dst);
        


        #endregion


        #endregion PInvoke
    }
}
