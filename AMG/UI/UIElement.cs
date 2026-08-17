namespace AMG.UI;

public abstract class UIElement
{
    protected UIElement()
    {
        // Automatically add this element to the currently constructing tab
        if (TabBase.CurrentConstructingTab != null)
        {
            TabBase.CurrentConstructingTab.Elements.Add(this);
        }
    }

    public abstract void Draw();
}
