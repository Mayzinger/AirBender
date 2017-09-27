﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using AirBender.Common.Shared.Reports.DualShock3;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using Serilog;
using SokkaServer.Exceptions;
using SokkaServer.Host;
using SokkaServer.Util;

namespace SokkaServer.Children.DualShock3
{
    internal class AirBenderDualShock3 : AirBenderChildDevice
    {
        private readonly Dictionary<DualShock3Buttons, DualShock4Buttons> _btnMap;
        private readonly DualShock4Controller _ds4;

        private readonly byte[] _hidOutputReport =
        {
            0x52, 0x01, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0xFF, 0x27, 0x10, 0x00,
            0x32, 0xFF, 0x27, 0x10, 0x00, 0x32, 0xFF, 0x27,
            0x10, 0x00, 0x32, 0xFF, 0x27, 0x10, 0x00, 0x32,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00
        };

        private readonly byte[] _ledOffsets = { 0x02, 0x04, 0x08, 0x10 };

        public AirBenderDualShock3(AirBenderHost host, PhysicalAddress client, int index) : base(host, client, index)
        {
            _btnMap = new Dictionary<DualShock3Buttons, DualShock4Buttons>
            {
                {DualShock3Buttons.Select, DualShock4Buttons.Share},
                {DualShock3Buttons.LeftThumb, DualShock4Buttons.ThumbLeft},
                {DualShock3Buttons.RightThumb, DualShock4Buttons.ThumbRight},
                {DualShock3Buttons.Start, DualShock4Buttons.Options},
                {DualShock3Buttons.LeftTrigger, DualShock4Buttons.TriggerLeft},
                {DualShock3Buttons.RightTrigger, DualShock4Buttons.TriggerRight},
                {DualShock3Buttons.LeftShoulder, DualShock4Buttons.ShoulderLeft},
                {DualShock3Buttons.RightShoulder, DualShock4Buttons.ShoulderRight},
                {DualShock3Buttons.Triangle, DualShock4Buttons.Triangle},
                {DualShock3Buttons.Circle, DualShock4Buttons.Circle},
                {DualShock3Buttons.Cross, DualShock4Buttons.Cross},
                {DualShock3Buttons.Square, DualShock4Buttons.Square}
            };

            var vigem = new ViGEmClient();
            _ds4 = new DualShock4Controller(vigem);
            _ds4.Connect();

            if (index >= 0 && index < 4)
                _hidOutputReport[11] = _ledOffsets[index];
        }

        protected override void RequestInputReportWorker(object cancellationToken)
        {
            var token = (CancellationToken)cancellationToken;
            var requestSize = Marshal.SizeOf<AirBenderHost.AirbenderGetDs3InputReport>();
            var requestBuffer = Marshal.AllocHGlobal(requestSize);

            Marshal.StructureToPtr(
                new AirBenderHost.AirbenderGetDs3InputReport
                {
                    ClientAddress = ClientAddress.ToNativeBdAddr()
                },
                requestBuffer, false);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    int bytesReturned;

                    //
                    // This call blocks until the driver supplies new data.
                    //  
                    var ret = Driver.OverlappedDeviceIoControl(
                        HostDevice.DeviceHandle,
                        AirBenderHost.IoctlAirbenderGetDs3InputReport,
                        requestBuffer, requestSize, requestBuffer, requestSize,
                        out bytesReturned);

                    if (!ret && Marshal.GetLastWin32Error() == AirBenderHost.ErrorDevNotExist)
                        OnChildDeviceDisconnected(EventArgs.Empty);

                    if (ret)
                    {
                        var resp = Marshal.PtrToStructure<AirBenderHost.AirbenderGetDs3InputReport>(requestBuffer);

                        OnInputReport(new DualShock3InputReport(resp.ReportBuffer));

                        return;

                        //
                        // TODO: demo-code, remove!
                        // 
                        var report = new DualShock3InputReport(resp.ReportBuffer);
                        var ds4Report = new DualShock4Report();

                        ds4Report.SetAxis(DualShock4Axes.LeftThumbX, report.LeftThumbX);
                        ds4Report.SetAxis(DualShock4Axes.LeftThumbY, report.LeftThumbY);
                        ds4Report.SetAxis(DualShock4Axes.RightThumbX, report.RightThumbX);
                        ds4Report.SetAxis(DualShock4Axes.RightThumbY, report.RightThumbY);
                        ds4Report.SetAxis(DualShock4Axes.LeftTrigger, report.LeftTrigger);
                        ds4Report.SetAxis(DualShock4Axes.RightTrigger, report.RightTrigger);

                        foreach (var engagedButton in report.EngagedButtons)
                            ds4Report.SetButtons(_btnMap.Where(m => m.Key == engagedButton).Select(m => m.Value)
                                .ToArray());

                        if (report.EngagedButtons.Contains(DualShock3Buttons.DPadUp))
                            ds4Report.SetDPad(DualShock4DPadValues.North);
                        if (report.EngagedButtons.Contains(DualShock3Buttons.DPadRight))
                            ds4Report.SetDPad(DualShock4DPadValues.East);
                        if (report.EngagedButtons.Contains(DualShock3Buttons.DPadDown))
                            ds4Report.SetDPad(DualShock4DPadValues.South);
                        if (report.EngagedButtons.Contains(DualShock3Buttons.DPadLeft))
                            ds4Report.SetDPad(DualShock4DPadValues.West);

                        if (report.EngagedButtons.Contains(DualShock3Buttons.DPadUp)
                            && report.EngagedButtons.Contains(DualShock3Buttons.DPadRight))
                            ds4Report.SetDPad(DualShock4DPadValues.Northeast);
                        if (report.EngagedButtons.Contains(DualShock3Buttons.DPadRight)
                            && report.EngagedButtons.Contains(DualShock3Buttons.DPadDown))
                            ds4Report.SetDPad(DualShock4DPadValues.Southeast);
                        if (report.EngagedButtons.Contains(DualShock3Buttons.DPadDown)
                            && report.EngagedButtons.Contains(DualShock3Buttons.DPadLeft))
                            ds4Report.SetDPad(DualShock4DPadValues.Southwest);
                        if (report.EngagedButtons.Contains(DualShock3Buttons.DPadLeft)
                            && report.EngagedButtons.Contains(DualShock3Buttons.DPadUp))
                            ds4Report.SetDPad(DualShock4DPadValues.Northwest);

                        if (report.EngagedButtons.Contains(DualShock3Buttons.Ps))
                            ds4Report.SetSpecialButtons(DualShock4SpecialButtons.Ps);

                        _ds4.SendReport(ds4Report);

#if DEBUG
                        var sb = new StringBuilder();

                        foreach (var b in resp.ReportBuffer)
                            sb.Append($"{b:X2} ");

                        foreach (var engagedButton in report.EngagedButtons)
                            Log.Information($"Button pressed: {engagedButton}");

                        if (sb.Length > 0)
                            Log.Information(sb.ToString());
#endif
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(requestBuffer);
            }
        }

        protected override void OnOutputReport(long l)
        {
            int bytesReturned;
            var requestSize = Marshal.SizeOf<AirBenderHost.AirbenderSetDs3OutputReport>();
            var requestBuffer = Marshal.AllocHGlobal(requestSize);

            Marshal.StructureToPtr(
                new AirBenderHost.AirbenderSetDs3OutputReport
                {
                    ClientAddress = ClientAddress.ToNativeBdAddr(),
                    ReportBuffer = _hidOutputReport
                },
                requestBuffer, false);

            var ret = Driver.OverlappedDeviceIoControl(
                HostDevice.DeviceHandle,
                AirBenderHost.IoctlAirbenderSetDs3OutputReport,
                requestBuffer, requestSize, IntPtr.Zero, 0,
                out bytesReturned);

            if (!ret && Marshal.GetLastWin32Error() == AirBenderHost.ErrorDevNotExist)
            {
                Marshal.FreeHGlobal(requestBuffer);
                throw new AirBenderDeviceNotFoundException();
            }

            Marshal.FreeHGlobal(requestBuffer);
        }
    }
}