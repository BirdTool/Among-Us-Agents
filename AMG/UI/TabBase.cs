using System.Collections.Generic;

namespace AMG.UI;

public abstract class TabBase
{
    public static TabBase CurrentConstructingTab { get; private set; }
    
    public string Name { get; private set; }
    public List<UIElement> Elements { get; } = new();

    protected TabBase(string name)
    {
        Name = name;
        CurrentConstructingTab = this;
        Build();
        CurrentConstructingTab = null;
    }

    // Derived classes should instantiate UI elements here
    protected abstract void Build();

    public virtual void Draw()
    {
        foreach (var element in Elements)
        {
            element.Draw();
        }
    }
}
