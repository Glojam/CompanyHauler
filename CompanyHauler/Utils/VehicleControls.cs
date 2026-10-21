using LethalCompanyInputUtils.Api;
using LethalCompanyInputUtils.BindingPathEnums;
using UnityEngine.InputSystem;

namespace CompanyHauler.Utils;

/// <summary>
///  Available from BetterVehicleControls, licensed under MIT License.
///  Source: https://github.com/1A3Dev/LC-BetterVehicleControls/blob/master/source/Plugin.cs
/// </summary>
internal class VehicleControls : LcInputActions
{
    [InputAction(KeyboardControl.W, Name = "Gas Pedal", GamepadControl = GamepadControl.Unbound, ActionType = InputActionType.Value)]
    public InputAction GasPedalKey { get; set; } = null!;


    [InputAction(KeyboardControl.S, Name = "Brake Pedal", GamepadControl = GamepadControl.Unbound, ActionType = InputActionType.Value)]
    public InputAction BrakePedalKey { get; set; } = null!;


    [InputAction(KeyboardControl.Space, Name = "Jump", GamepadControl = GamepadControl.Unbound)]
    public InputAction JumpKey { get; set; } = null!;


    [InputAction(KeyboardControl.None, Name = "Magnet")]
    public InputAction ToggleMagnetKey { get; set; } = null!;
}
