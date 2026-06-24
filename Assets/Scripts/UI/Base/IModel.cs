using System;

namespace MonSumo.UI.Base
{
    public interface IModel
    {
        event Action OnChanged;
    }
}
