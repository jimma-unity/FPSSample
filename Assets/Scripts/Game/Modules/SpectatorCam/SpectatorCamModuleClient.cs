using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpectatorCamModuleClient  
{
    public SpectatorCamModuleClient(GameWorld world)
    {
        m_world = world;
        m_UpdateSpectatorCam =  m_world.GetECSWorld().AddSystemManaged(new UpdateSpectatorCam(m_world));
        m_UpdateSpectatorCamControl =  m_world.GetECSWorld().AddSystemManaged(new UpdateSpectatorCamControl(m_world));

    }

    public void Shutdown()
    {
        m_world.GetECSWorld().DestroySystemManaged(m_UpdateSpectatorCam);       
        m_world.GetECSWorld().DestroySystemManaged(m_UpdateSpectatorCamControl);       
    }

    public void Update()
    {
        m_UpdateSpectatorCam.Update();
        m_UpdateSpectatorCamControl.Update();
    }
    
    GameWorld m_world;
    UpdateSpectatorCam m_UpdateSpectatorCam;
    UpdateSpectatorCamControl m_UpdateSpectatorCamControl;
}
