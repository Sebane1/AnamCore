// © Anamnesis.
// Licensed under the MIT license.

namespace Anamnesis.Memory;

public class GlassesMemory : MemoryBase
{
    [Bind(0x000, BindFlags.ActorRefresh)] public ushort GlassesId { get; set; }
}
