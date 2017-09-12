#pragma once

#include "uthash.h"

typedef struct _BD_ADDR
{
    BYTE Address[6];

} BD_ADDR;

typedef struct _BTH_HANDLE
{
    BYTE Lsb;
    BYTE Msb;

} BTH_HANDLE;

typedef struct _BTH_DEVICE
{
    BTH_HANDLE Handle;

    UT_hash_handle hh;

} BTH_DEVICE, *PBTH_DEVICE;

#pragma warning( push )  
#pragma warning( disable : 4702 ) 
VOID FORCEINLINE 
BTH_ADD_DEVICE(
    PBTH_DEVICE DeviceList, 
    BTH_HANDLE Handle)
{
    PBTH_DEVICE device = (PBTH_DEVICE)malloc(sizeof(BTH_DEVICE));
    device->Handle = Handle;

    HASH_ADD(hh, DeviceList, Handle, sizeof(BTH_HANDLE), device);
#pragma warning( pop )  
}

VOID FORCEINLINE
BTH_REMOVE_DEVICE(
    PBTH_DEVICE DeviceList,
    PBTH_DEVICE Device)
{
    HASH_DEL(DeviceList, Device);
    free(Device);
}

ULONG FORCEINLINE
BTH_GET_DEVICE_COUNT(
    PBTH_DEVICE DeviceList)
{
    return HASH_COUNT(DeviceList);
}

#define BD_LINK_LENGTH  0x10

static const BYTE BD_LINK[BD_LINK_LENGTH] =
{ 
    0x56, 0xE8, 0x81, 0x38, 0x08, 0x06, 0x51, 0x41, 
    0xC0, 0x7F, 0x12, 0xAA, 0xD9, 0x66, 0x3C, 0xCE 
};
