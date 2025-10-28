using UnityEngine;

public class InputSystem
{
    // TODO: these should be put in some global setting
    public static Vector2 s_JoystickLookSensitivity = new Vector2(90.0f, 60.0f);

    static float maxMoveYaw;
    static float maxMoveMagnitude;

    private InputSystem_Actions _actions;
    private InputSystem_Actions.PlayerActions _playerActions;

    public InputSystem()
    {
        _actions = new();
        _actions.Enable();
        _playerActions = _actions.Player;
    }

    public void AccumulateInput(ref UserCommand command, float deltaTime)
    {
        // To accumulate move we store the input with max magnitude and uses that
        bool unblocked = Game.Input.IsNotBlocked();
        Vector2 moveInput = unblocked ? _playerActions.Move.ReadValue<Vector2>() : Vector2.zero;
        float angle = Vector2.Angle(Vector2.up, moveInput);
        if (moveInput.x < 0)
            angle = 360 - angle;
        float magnitude = Mathf.Clamp(moveInput.magnitude, 0, 1);       
        if (magnitude > maxMoveMagnitude)
        {
            maxMoveYaw = angle;
            maxMoveMagnitude = magnitude;
        }
        command.moveYaw = maxMoveYaw;
        command.moveMagnitude = maxMoveMagnitude;

        float invertY = Game.configInvertY.IntValue > 0 ? -1.0f : 1.0f;

        Vector2 deltaMousePos = new Vector2(0, 0);
        //var v = _playerActions.Aim.ReadValue<Vector2>();
        if(deltaTime > 0.0f)
            deltaMousePos += new Vector2(Game.Input.GetAxisRaw("Mouse X"), Game.Input.GetAxisRaw("Mouse Y") * invertY);
        deltaMousePos += deltaTime * (new Vector2(Game.Input.GetAxisRaw("RightStickX") * s_JoystickLookSensitivity.x, - invertY * Game.Input.GetAxisRaw("RightStickY") * s_JoystickLookSensitivity.y));
        deltaMousePos += deltaTime * (new Vector2(
            ((Game.Input.GetKey(KeyCode.Keypad4) ? -1.0f : 0.0f) + (Game.Input.GetKey(KeyCode.Keypad6) ? 1.0f : 0.0f)) * s_JoystickLookSensitivity.x,
            - invertY * Game.Input.GetAxisRaw("RightStickY") * s_JoystickLookSensitivity.y));

        command.lookYaw += deltaMousePos.x * Game.configMouseSensitivity.FloatValue;
        command.lookYaw = command.lookYaw % 360;
        while (command.lookYaw < 0.0f) command.lookYaw += 360.0f;

        command.lookPitch += deltaMousePos.y * Game.configMouseSensitivity.FloatValue;
        command.lookPitch = Mathf.Clamp(command.lookPitch, 0, 180);
        command.buttons.Or(UserCommand.Button.Jump, unblocked && _playerActions.Jump.WasPressedThisFrame()); 
        command.buttons.Or(UserCommand.Button.Boost,unblocked && _playerActions.Boost.IsPressed());
        command.buttons.Or(UserCommand.Button.PrimaryFire, unblocked && _playerActions.PrimaryFire.IsPressed());
        command.buttons.Or(UserCommand.Button.SecondaryFire, unblocked && _playerActions.SecondaryFire.WasPressedThisFrame());
        command.buttons.Or(UserCommand.Button.Ability1, unblocked && _playerActions.Sprint.IsPressed());
        command.buttons.Or(UserCommand.Button.Ability2, Game.Input.GetKey(KeyCode.E));
        command.buttons.Or(UserCommand.Button.Ability3, Game.Input.GetKey(KeyCode.Q));
        command.buttons.Or(UserCommand.Button.Reload, unblocked && _playerActions.Reload.WasPressedThisFrame());
        command.buttons.Or(UserCommand.Button.Melee, unblocked && _playerActions.Melee.WasPressedThisFrame());
        command.buttons.Or(UserCommand.Button.Use, Game.Input.GetKey(KeyCode.E));

        command.emote = unblocked && _playerActions.EmoteVictory.WasPressedThisFrame() ? CharacterEmote.Victory : CharacterEmote.None;
        command.emote = unblocked && _playerActions.EmoteDefeat.WasPressedThisFrame() ? CharacterEmote.Defeat : command.emote;
    }

    public void ClearInput(ref UserCommand command)     
    {
        maxMoveMagnitude = 0;
        command.ClearCommand();
    }
}
