﻿using System;
using System.Net.NetworkInformation;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AirBender.Common.Shared.Core;
using AirBender.Common.Shared.Reports;
using AirBender.Sokka.Server.Host;

namespace AirBender.Sokka.Server.Children
{
    public class InputReportEventArgs : EventArgs
    {
        public InputReportEventArgs(IInputReport report)
        {
            Report = report;
        }

        public IInputReport Report { get; }
    }

    public delegate void ChildDeviceDisconnectedEventHandler(object sender, EventArgs e);

    public delegate void InputReportReceivedEventHandler(object sender, InputReportEventArgs e);

    internal abstract class AirBenderChildDevice : IAirBenderChildDevice, IDisposable
    {
        private readonly CancellationTokenSource _inputCancellationTokenSourcePrimary = new CancellationTokenSource();
        private readonly CancellationTokenSource _inputCancellationTokenSourceSecondary = new CancellationTokenSource();
        private readonly IObservable<long> _outputReportSchedule = Observable.Interval(TimeSpan.FromMilliseconds(10));
        private readonly IDisposable _outputReportTask;

        protected AirBenderChildDevice(AirBenderHost host, PhysicalAddress client, int index)
        {
            HostDevice = host;
            ClientAddress = client;
            DeviceIndex = index;

            _outputReportTask = _outputReportSchedule.Subscribe(OnOutputReport);

            //
            // Start two tasks requesting input reports in parallel.
            // 
            // While on threads request gets completed, another request can be
            // queued by the other thread. This way no input can get lost because
            // there's always at least one pending request in the driver to get
            // completed. Each thread uses inverted calls for maximum performance.
            // 
            Task.Factory.StartNew(RequestInputReportWorker, _inputCancellationTokenSourcePrimary.Token);
            Task.Factory.StartNew(RequestInputReportWorker, _inputCancellationTokenSourceSecondary.Token);
        }

        protected AirBenderHost HostDevice { get; }

        public int DeviceIndex { get; }

        public PhysicalAddress ClientAddress { get; }

        public BthDeviceType DeviceType { get; protected set; }

        public event ChildDeviceDisconnectedEventHandler ChildDeviceDisconnected;

        public event InputReportReceivedEventHandler InputReportReceived;

        protected virtual void RequestInputReportWorker(object cancellationToken)
        {
        }

        protected void OnInputReport(IInputReport report)
        {
            InputReportReceived?.Invoke(this, new InputReportEventArgs(report));
        }

        protected virtual void OnOutputReport(long l)
        {
        }

        protected virtual void OnChildDeviceDisconnected(EventArgs e)
        {
            _outputReportTask?.Dispose();

            _inputCancellationTokenSourcePrimary.Cancel();
            _inputCancellationTokenSourceSecondary.Cancel();

            ChildDeviceDisconnected?.Invoke(this, e);
        }

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _outputReportTask?.Dispose();

                    _inputCancellationTokenSourcePrimary.Cancel();
                    _inputCancellationTokenSourceSecondary.Cancel();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~AirBenderChildDevice()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}