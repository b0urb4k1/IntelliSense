using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace ExcelDna.IntelliSense
{
    class IntelliSenseHelper : IDisposable
    {
        readonly SynchronizationContext _syncContextMain; // Main thread, not macro context
        readonly UIMonitor _uiMonitor;  // We want the UIMonitor here, because we might hook up other display enhancements

        // These need to get combined into a UIEnhancement class ....
        readonly IntelliSenseDisplay _display;
        readonly List<IIntelliSenseProvider> _providers = new List<IIntelliSenseProvider>();
        // TODO: Others

        public IntelliSenseHelper()
        {
            Logger.Initialization.Verbose("IntelliSenseHelper Constructor Begin");
            _syncContextMain = new WindowsFormsSynchronizationContext();
            _uiMonitor = new UIMonitor(_syncContextMain);
            _display = new IntelliSenseDisplay(_syncContextMain, _uiMonitor);

            _providers = new List<IIntelliSenseProvider>
            {
                new ExcelDnaIntelliSenseProvider(_syncContextMain),
                new WorkbookIntelliSenseProvider(),
            };

            RegisterIntellisense();
            Logger.Initialization.Verbose("IntelliSenseHelper Constructor End");
        }

        void RegisterIntellisense()
        {
            foreach (var provider in _providers)
            {
                provider.Invalidate += Provider_Invalidate;
                provider.Initialize();
                UpdateDisplay(provider);
            }
        }

        // We need to call Refresh on the main thread in a macro context,
        // and then GetFunctionInfos() to update the Display
        void Provider_Invalidate(object sender, EventArgs e)
        {
            RefreshProvider(sender);
        }

        // Must be called on the main thread, in a macro context
        // TODO: Still not sure how to delete / unregister...
        void RefreshProvider(object providerObj)
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1);
            IIntelliSenseProvider provider = (IIntelliSenseProvider)providerObj;
            provider.Refresh();
            UpdateDisplay(provider);
        }

        void UpdateDisplay(IIntelliSenseProvider provider)
        {
            var functionInfos = provider.GetFunctionInfos();
            _display.UpdateFunctionInfos(functionInfos);
        }

        // Must be called on the main thread, in a macro context
        // TODO: Still not sure how to delete / unregister...
        internal void RefreshProviders()
        {
            foreach (var provider in _providers)
            {
                RefreshProvider(provider);
            }
        }

        public void Dispose()
        {
            Logger.Initialization.Verbose("IntelliSenseHelper Dispose Start");

            foreach (var provider in _providers)
            {
                provider.Dispose();
            }
            _uiMonitor.Dispose();
            _display.Dispose();

            Logger.Initialization.Verbose("IntelliSenseHelper Dispose End");
        }
    }
}
