using System;

namespace Incubator;

class ActionDisposable(Action a) : IDisposable
{
    public void Dispose() => a();
}
