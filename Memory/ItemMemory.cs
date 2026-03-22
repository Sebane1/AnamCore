// © Anamnesis.
// Licensed under the MIT license.

namespace Anamnesis.Memory;

/// <summary>
/// One equipment piece (matches <c>EquipmentModelId</c> layout in game memory).
/// </summary>
public class ItemMemory : MemoryBase
{
    [Bind(0x000, BindFlags.ActorRefresh)]
    public ushort Base { get; set; }

    [Bind(0x002, BindFlags.ActorRefresh)]
    public byte Variant { get; set; }

    [Bind(0x003, BindFlags.ActorRefresh)]
    public byte Dye { get; set; }

    [Bind(0x004, BindFlags.ActorRefresh)]
    public byte Dye2 { get; set; }

    public ushort Set { get; set; }
}
