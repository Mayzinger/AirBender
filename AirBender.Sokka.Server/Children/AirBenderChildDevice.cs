﻿using System;
using System.Net.NetworkInformation;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AirBender.Sokka.Server.Host;
using Nefarius.Sub.Kinbaku.Core.Hub.Client;
using Nefarius.Sub.Kinbaku.Core.Plugins;
using Nefarius.Sub.Kinbaku.Core.Reports.Common;

namespace AirBender.Sokka.Server.Children
{
    public delegate void ChildDeviceDisconnectedEventHandler(object sender, EventArgs e);

    public delegate void InputReportReceivedEventHandler(object sender, InputReportEventArgs e);

    /// <summary>
    ///     Represents a managed wrapper for a Bluetooth host child device.
    /// </summary>
    internal abstract class AirBenderChildDevice : IDualShockDevice, IDisposable
    {
        private readonly CancellationTokenSource _inputCancellationTokenSourcePrimary = new CancellationTokenSource();
        private readonly CancellationTokenSource _inputCancellationTokenSourceSecondary = new CancellationTokenSource();
        private readonly IObservable<long> _outputReportSchedule = Observable.Interval(TimeSpan.FromMilliseconds(10));
        private readonly IDisposable _outputReportTask;

        /// <summary>
        ///     Creates a new child device.
        /// </summary>
        /// <param name="host">The host this child is connected to.</param>
        /// <param name="client">The client MAC address identifying this child.</param>
        /// <param name="index">The index this child is registered on the host device under.</param>
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

        public DualShockDeviceType DeviceType { get; protected set; }

        public PhysicalAddress HostAddress => HostDevice.HostAddress;

        public event ChildDeviceDisconnectedEventHandler ChildDeviceDisconnected;

        public event InputReportReceivedEventHandler InputReportReceived;
        public event RumbleRequestReceivedEventHandler RumbleRequestReceived;

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

        public virtual void Rumble(byte largeMotor, byte smallMotor)
        {
            throw new NotImplementedException();
        }

        public virtual void PairTo(PhysicalAddress host)
        {
            throw new NotSupportedException("You can not change the host address while connected via Bluetooth.");
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