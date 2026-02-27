using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClientFrontend : MonoBehaviour
{
    public ScoreBoard scoreboardPanel;
    public GameScore gameScorePanel;
    [SerializeField] MainMenu mainMenu;
    public ChatPanel chatPanel;
    public ServerPanel serverPanel;

    public bool m_ShowScorePanel;

    [SerializeField] SoundDef uiHighlightSound;
    [SerializeField] SoundDef uiSelectSound;
    [SerializeField] SoundDef uiSelectLightSound;
    [SerializeField] SoundDef uiCloseSound;

    Canvas m_ScoreboardPanelCanvas;
    Canvas m_GameScorePanelCanvas;
    Canvas m_ChatPanelCanvas;

    public enum MenuShowing
    {
        None,
        Main,
        Ingame
    }

    Interpolator m_MenuFader = new Interpolator(0.0f, Interpolator.CurveType.SmoothStep);
    const float k_MenuToggleEscapeHoldSeconds = 0.12f;
    bool m_EscapeWasDownNoBlock;
    float m_EscapeHeldDuration;

    public MenuShowing menuShowing { get; private set; } = MenuShowing.None;

    public int ActiveMainMenuNumber
    {
        get { return mainMenu.gameObject.activeSelf ? mainMenu.activeSubmenuNumber : -1; }
    }


    // Audio for menus. Called from events on the ui elements
    public void OnHighlight() { Game.SoundSystem.Play(uiHighlightSound); }
    public void OnSelect() { Game.SoundSystem.Play(uiSelectSound); }
    public void OnClose() { Game.SoundSystem.Play(uiCloseSound); }

    void Awake()
    {
        m_ScoreboardPanelCanvas = scoreboardPanel.GetComponent<Canvas>();
        m_GameScorePanelCanvas = gameScorePanel.GetComponent<Canvas>();
        m_ChatPanelCanvas = chatPanel.GetComponent<Canvas>();
        Clear();
    }

    public void Clear()
    {
        scoreboardPanel.SetPanelActive(false);
        gameScorePanel.SetPanelActive(false);
        mainMenu.SetPanelActive(MenuShowing.None);
        chatPanel.SetPanelActive(true); // active always as it has its own display/hide logic
        chatPanel.ClearMessages();
        serverPanel.SetPanelActive(false);
    }

    public void ShowMenu(MenuShowing show, float fadeTime = 0.0f)
    {
        if (menuShowing == show)
            return;
        menuShowing = show;
        m_MenuFader.MoveTo(show != MenuShowing.None ? 1.0f : 0.0f, fadeTime);
        if (menuShowing != MenuShowing.None)
            Game.SoundSystem.Play(uiSelectLightSound);
        else
            Game.SoundSystem.Play(uiCloseSound);
    }

    public void UpdateGame()
    {
        mainMenu.UpdateMenus();

        var clientLoop = Game.GetGameLoop<ClientGameLoop>();
        var canToggleIngameMenu = clientLoop != null && clientLoop.CanToggleIngameMenu();
        var escapeIsDown = Game.Input.GetKeyNoBlock(Key.Escape);
        var escapeReleased = !escapeIsDown && m_EscapeWasDownNoBlock;

        if (!Application.isFocused)
            m_EscapeHeldDuration = 0.0f;
        else if (escapeIsDown)
            m_EscapeHeldDuration += Time.unscaledDeltaTime;

        var deliberateEscapeRelease = escapeReleased && m_EscapeHeldDuration >= k_MenuToggleEscapeHoldSeconds;
        if (escapeReleased)
            m_EscapeHeldDuration = 0.0f;

        m_EscapeWasDownNoBlock = escapeIsDown;

        // Show/Hide fully for debug purposes
        var show = IngameHUD.showHud.IntValue > 0;
        if (m_ChatPanelCanvas.enabled != show)
        {
            m_ScoreboardPanelCanvas.enabled = show;
            m_GameScorePanelCanvas.enabled = show;
            m_ChatPanelCanvas.enabled = show;
        }

        // Toggle menu if not in editor
        var mouseUnlocked = !Game.GetMousePointerLock();
        if(!Application.isEditor && Application.isFocused && canToggleIngameMenu && deliberateEscapeRelease)
        {
            if (menuShowing == MenuShowing.None)
            {
                Console.EnqueueCommandNoHistory("menu 2 0.2");
                Game.SetMousePointerLock(false);
            }
            else if (mouseUnlocked)
            {
                Console.EnqueueCommandNoHistory("menu 0 0.2");
                Game.RequestMousePointerLock();
            }
        }

        // Fade main menu
        var fade = m_MenuFader.GetValue();
        var active = fade > 0.0f;
        if (mainMenu.GetPanelActive() != active)
            mainMenu.SetPanelActive(menuShowing);
        if (active)
            mainMenu.SetAlpha(fade);
    }

    public void UpdateChat(ChatSystemClient chatSystem)
    {
        chatPanel.Tick(chatSystem);
    }

    // Force showing of score board e.g. when dead
    public void SetShowScorePanel(bool showScorePanel)
    {
        m_ShowScorePanel = showScorePanel;
    }

    public void UpdateIngame(GameMode gameMode, LocalPlayer localPlayer)
    {
        var playerState = localPlayer.playerState;

        // Scoreboard
        scoreboardPanel.SetPanelActive(playerState.displayScoreBoard || Game.Input.GetKeyNoBlock(Key.Tab) || m_ShowScorePanel);

        // Game score panel
        gameScorePanel.SetPanelActive(playerState.displayGameScore);
    }
}

[DisableAutoCreation]
partial class ClientFrontendUpdate : BaseComponentSystem
{
    EntityQuery m_gameModeGroup;
    EntityQuery m_localPlayerGroup;

    public ClientFrontendUpdate(GameWorld world) : base(world)
    {
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_gameModeGroup = GetEntityQuery(typeof(GameMode));
        m_localPlayerGroup = GetEntityQuery(typeof(LocalPlayer));
    }

    protected override void OnUpdate()
    {
        var gameModeArray = m_gameModeGroup.ToComponentArray<GameMode>();
        if (gameModeArray.Length == 0)
            return;

        GameDebug.Assert(gameModeArray.Length == 1, "There should only be one gamemode. Found:{0}",
            gameModeArray.Length);

        var localPlayerArray = m_localPlayerGroup.ToComponentArray<LocalPlayer>();
        GameDebug.Assert(localPlayerArray.Length == 1, "There should only be one localplayer. Found:{0}",
            localPlayerArray.Length);

        var gameMode = gameModeArray[0];
        var localPlayer = localPlayerArray[0];

        Game.game.clientFrontend.UpdateIngame(gameMode, localPlayer);
    }

}



