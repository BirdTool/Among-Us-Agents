using AMG.UI.Elements;
using AMG.UI.UIActions;
using UnityEngine;

namespace AMG.UI.Tabs;

public class AgentsTab : TabBase
{
    public AgentsTab() : base("Agents Controller") { }

    protected override void Build()
    {
        new MenuText("Agents controller");
        new MenuSubText("Choose the agents quantity");
        
        new MenuSlider(1f, 15f, 1f, (value) => 
        {
            AgentsController.AgentsCount = (int)value;
        });

        new MenuSubText("Choose the impostor quantity");
        
        new MenuSlider(1f, 3f, 2f, (value) => 
        {
            AgentsController.ImpostorCount = (int)value;
        });


        new MenuButton("Spawn Agents", () => 
        {
            AgentsController.SpawnAgents();
        });
    }
}
