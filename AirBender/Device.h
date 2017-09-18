/*++

Module Name:

    device.h

Abstract:

    This file contains the device definitions.

Environment:

    User-mode Driver Framework 2

--*/

#include "Bluetooth.h"

EXTERN_C_START

//
// The device context performs the same job as
// a WDM device extension in the driver frameworks
//
typedef struct _DEVICE_CONTEXT
{
    WDFUSBDEVICE UsbDevice;

    WDFUSBINTERFACE UsbInterface;
    
    WDFUSBPIPE InterruptPipe;

    WDFUSBPIPE BulkReadPipe;

    WDFUSBPIPE BulkWritePipe;

    BOOLEAN DisableSSP;

    BOOLEAN Started;

    BD_ADDR BluetoothHostAddress;

    BOOLEAN Initialized;

    BTH_DEVICE_LIST ClientDeviceList;

    BYTE_ARRAY HidInitReports;

} DEVICE_CONTEXT, *PDEVICE_CONTEXT;

//
// This macro will generate an inline function called DeviceGetContext
// which will be used to get a pointer to the device context memory
// in a type safe manner.
//
WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(DEVICE_CONTEXT, DeviceGetContext)

//
// Function to initialize the device's queues and callbacks
//
NTSTATUS
AirBenderCreateDevice(
    _Inout_ PWDFDEVICE_INIT DeviceInit
    );

//
// Function to select the device's USB configuration and get a WDFUSBDEVICE
// handle
//
EVT_WDF_DEVICE_PREPARE_HARDWARE AirBenderEvtDevicePrepareHardware;
EVT_WDF_DEVICE_D0_ENTRY AirBenderEvtDeviceD0Entry;
EVT_WDF_DEVICE_D0_EXIT AirBenderEvtDeviceD0Exit;

NTSTATUS
InitPowerManagement(
    IN WDFDEVICE  Device,
    IN PDEVICE_CONTEXT Context);

VOID
InitHidInitReports(
    IN PDEVICE_CONTEXT Context);

EXTERN_C_END
