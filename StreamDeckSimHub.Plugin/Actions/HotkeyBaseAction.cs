﻿// Copyright (C) 2022 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using Microsoft.Extensions.Logging;
using SharpDeck;
using SharpDeck.Events.Received;
using StreamDeckSimHub.Plugin.Tools;

namespace StreamDeckSimHub.Plugin.Actions;

/// <summary>
/// This action sends a key stroke to the active window and it can update its state from a SimHub property. Concrete implementations
/// have to handle the conversion from SimHub property values into Stream Deck action states.
/// </summary>
public abstract class HotkeyBaseAction : StreamDeckAction<HotkeySettings>
{
    private readonly SimHubConnection _simHubConnection;

    private HotkeySettings _hotkeySettings;
    private Keyboard.VirtualKeyShort? _vks;
    private Keyboard.ScanCodeShort? _scs;
    private int _state;

    protected HotkeyBaseAction(SimHubConnection simHubConnection)
    {
        _hotkeySettings = new HotkeySettings();
        _simHubConnection = simHubConnection;
    }

    protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
    {
        _simHubConnection.PropertyChangedEvent += PropertyChangedEvent;

        var settings = args.Payload.GetSettings<HotkeySettings>();
        await SetSettings(settings);
        await base.OnWillAppear(args);
    }

    protected override async Task OnWillDisappear(ActionEventArgs<AppearancePayload> args)
    {
        _simHubConnection.PropertyChangedEvent -= PropertyChangedEvent;
        await _simHubConnection.Unsubscribe(_hotkeySettings.SimHubProperty);

        await base.OnWillDisappear(args);
    }

    /// <summary>
    /// Called when the value of a SimHub property has changed.
    /// </summary>
    private async void PropertyChangedEvent(object? sender, SimHubConnection.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == _hotkeySettings.SimHubProperty)
        {
            Logger.LogInformation("Property {PropertyName} changed to '{PropertyValue}'", e.PropertyName, e.PropertyValue);
            _state = ValueToState(e.PropertyType, e.PropertyValue);
            // see https://github.com/pre-martin/SimHubPropertyServer/blob/main/Property/SimHubProperty.cs, "TypeToString()"
            await SetStateAsync(_state);
        }
    }

    protected abstract int ValueToState(string propertyType, string? propertyValue);

    protected override async Task OnDidReceiveSettings(ActionEventArgs<ActionPayload> args, HotkeySettings settings)
    {
        await SetSettings(settings);
        await base.OnDidReceiveSettings(args, settings);
    }

    protected override async Task OnKeyDown(ActionEventArgs<KeyPayload> args)
    {
        if (_hotkeySettings.Ctrl) Keyboard.KeyDown(Keyboard.VirtualKeyShort.LCONTROL, Keyboard.ScanCodeShort.LCONTROL);
        if (_hotkeySettings.Alt) Keyboard.KeyDown(Keyboard.VirtualKeyShort.LMENU, Keyboard.ScanCodeShort.LMENU);
        if (_hotkeySettings.Shift) Keyboard.KeyDown(Keyboard.VirtualKeyShort.LSHIFT, Keyboard.ScanCodeShort.LSHIFT);
        if (_vks.HasValue && _scs.HasValue) Keyboard.KeyDown(_vks.Value, _scs.Value);

        await base.OnKeyDown(args);
    }

    protected override async Task OnKeyUp(ActionEventArgs<KeyPayload> args)
    {
        if (_vks.HasValue && _scs.HasValue) Keyboard.KeyUp(_vks.Value, _scs.Value);
        if (_hotkeySettings.Ctrl) Keyboard.KeyUp(Keyboard.VirtualKeyShort.LCONTROL, Keyboard.ScanCodeShort.LCONTROL);
        if (_hotkeySettings.Alt) Keyboard.KeyUp(Keyboard.VirtualKeyShort.LMENU, Keyboard.ScanCodeShort.LMENU);
        if (_hotkeySettings.Shift) Keyboard.KeyUp(Keyboard.VirtualKeyShort.LSHIFT, Keyboard.ScanCodeShort.LSHIFT);
        // Stream Deck always toggle the state for each keypress (at "key up", to be precise). So we have to set the
        // state again to the correct one, after Stream Deck has done its toggling stuff.
        await SetStateAsync(_state);

        await base.OnKeyUp(args);
    }

    private async Task SetSettings(HotkeySettings ac)
    {
        Logger.LogInformation(
            "SetSettings: Modifiers: Ctrl: {Ctrl}, Alt: {Alt}, Shift: {Shift}, Hotkey: {Hotkey}, SimHubProperty: {SimHubProperty}",
            ac.Ctrl, ac.Alt, ac.Shift, ac.Hotkey, ac.SimHubProperty);

        // Unsubscribe previous SimHub property, if it was set and is different than the new one.
        if (!string.IsNullOrEmpty(_hotkeySettings.SimHubProperty) && _hotkeySettings.SimHubProperty != ac.SimHubProperty)
        {
            await _simHubConnection.Unsubscribe(_hotkeySettings.SimHubProperty);
        }

        this._vks = null;
        this._scs = null;
        if (!string.IsNullOrEmpty(ac.Hotkey))
        {
            var virtualKeyShort = KeyboardUtils.FindVirtualKey(ac.Hotkey);
            if (virtualKeyShort == null)
            {
                Logger.LogError("Could not find VirtualKeyCode for hotkey '{Hotkey}'", ac.Hotkey);
                return;
            }

            var scanCodeShort =
                KeyboardUtils.MapVirtualKey((uint)virtualKeyShort, KeyboardUtils.MapType.MAPVK_VK_TO_VSC);
            if (scanCodeShort == 0)
            {
                Logger.LogError("Could not find ScanCode for hotkey '{Hotkey}'", ac.Hotkey);
                return;
            }

            this._vks = virtualKeyShort;
            this._scs = (Keyboard.ScanCodeShort)scanCodeShort;
        }

        // Subscribe SimHub property, if it is set and different than the previous one.
        if (!string.IsNullOrEmpty(ac.SimHubProperty) && ac.SimHubProperty != _hotkeySettings.SimHubProperty)
        {
            await _simHubConnection.Subscribe(ac.SimHubProperty);
        }

        this._hotkeySettings = ac;
    }
}