using System;
using System.Collections.Generic;

namespace OverseerProtocol.GameAbstractions.Overrides;

public sealed class ItemOverrideReferenceCatalog
{
    public HashSet<string> RuntimeItemIds { get; } = new(StringComparer.Ordinal);

    public bool HasRuntimeItemCatalog => RuntimeItemIds.Count > 0;
}
