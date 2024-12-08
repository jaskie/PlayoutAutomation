using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using TAS.Client.NDIVideoPreview.Interop;

namespace TAS.Client.NDIVideoPreview.Model
{
    internal class NdiSourcesWatcher
    {
        private readonly Dictionary<string, string> _ndiSources = new Dictionary<string, string>();
        private readonly IntPtr NdiFindInstance;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public event EventHandler<NdiSourceEventArgs> SourceAdded;
        public event EventHandler<NdiSourceEventArgs> SourceRemoved;

        public NdiSourcesWatcher()
        {
            var findDesc = new NDIlib_find_create_t
            {
                p_groups = IntPtr.Zero,
                show_local_sources = true,
                p_extra_ips = IntPtr.Zero
            };
            NdiFindInstance = Ndi.NDIlib_find_create2(ref findDesc);
            var sourcesPoolThread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        if (Ndi.NDIlib_find_wait_for_sources(NdiFindInstance, int.MaxValue))
                            RefreshSources();
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            })
            {
                Name = "NDI source list pooling thread",
                Priority = ThreadPriority.BelowNormal,
                IsBackground = true
            };
            sourcesPoolThread.Start();
        }

        public IEnumerable<string> GetSources()
        {
            lock (((IDictionary)_ndiSources).SyncRoot)
            {
                return _ndiSources.Keys.ToArray();
            }
        }

        public void RefreshSources()
        {
            var numSources = 0;
            var ndiSources = Ndi.NDIlib_find_get_current_sources(NdiFindInstance, ref numSources);
            var sourcesAdded = new List<string>();
            List<string> sourcesToRemove;
            lock (((IDictionary)_ndiSources).SyncRoot)
            {
                sourcesToRemove = _ndiSources.Keys.ToList();
                if (numSources > 0)
                {
                    var sourceSizeInBytes = Marshal.SizeOf(typeof(NDIlib_source_t));
                    for (var i = 0; i < numSources; i++)
                    {
                        var p = IntPtr.Add(ndiSources, (i * sourceSizeInBytes));
                        var src = (NDIlib_source_t)Marshal.PtrToStructure(p, typeof(NDIlib_source_t));
                        var ndiName = Ndi.Utf8ToString(src.p_ndi_name);
                        var ndiAddress = Ndi.Utf8ToString(src.p_ip_address);
                        if (_ndiSources.ContainsKey(ndiName))
                        {
                            sourcesToRemove.Remove(ndiName);
                            if (_ndiSources[ndiName] != ndiAddress)
                            {
                                _ndiSources[ndiName] = ndiAddress;
                                Logger.Debug($"Updated source name:{ndiName} address:{ndiAddress}");
                            }
                        }
                        else
                        {
                            _ndiSources.Add(ndiName, ndiAddress);
                            sourcesAdded.Add(ndiName);
                            Logger.Debug($"Added source name:{ndiName} address:{ndiAddress}");
                        }
                    }
                }
                foreach (var source in sourcesToRemove)
                {
                    _ndiSources.Remove(source);
                    Logger.Debug($"Removed source name:{source}");
                }
            }
            foreach (var source in sourcesAdded)
                SourceAdded?.Invoke(this, new NdiSourceEventArgs(source));
            foreach (var source in sourcesToRemove)
                SourceRemoved?.Invoke(this, new NdiSourceEventArgs(source));
        }

        /// <summary>
        /// Find source by name or address
        /// </summary>
        /// <param name="sourceToFind">machine_name (source_name) OR addres:port</param>
        /// <returns>If found, Key = machine_name (source_name), Value = address:port</returns>
        internal KeyValuePair<string, string> FindSource(string sourceToFind)
        {
            lock (((IDictionary)_ndiSources).SyncRoot)
            {
                foreach (var source in _ndiSources)
                {
                    if (source.Key == sourceToFind || source.Value == sourceToFind)
                        return source;
                }
                return default;
            }
        }
    }

    internal class NdiSourceEventArgs : EventArgs
    {
        public NdiSourceEventArgs(string sourceName)
        {
            SourceName = sourceName;
        }
        public string SourceName { get; }
    }
}
