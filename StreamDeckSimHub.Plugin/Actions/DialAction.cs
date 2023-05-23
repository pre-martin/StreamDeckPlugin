﻿// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using Microsoft.Extensions.Logging;
using SharpDeck;
using SharpDeck.Events.Received;
using SharpDeck.Layouts;
using SharpDeck.PropertyInspectors;
using StreamDeckSimHub.Plugin.PropertyLogic;
using StreamDeckSimHub.Plugin.SimHub;
using StreamDeckSimHub.Plugin.Tools;

namespace StreamDeckSimHub.Plugin.Actions;

[StreamDeckAction("net.planetrenner.simhub.dial")]
public class DialAction : StreamDeckAction<DialActionSettings>
{
    private readonly SimHubConnection _simHubConnection;
    private DialActionSettings _settings = new();
    private KeyboardUtils.Hotkey? _hotkey;
    private KeyboardUtils.Hotkey? _hotkeyLeft;
    private KeyboardUtils.Hotkey? _hotkeyRight;
    private readonly ShakeItStructureFetcher _shakeItStructureFetcher;
    private readonly IPropertyChangedReceiver _displayPropertyChangedReceiver;
    private string _displayFormat = "${0}";
    private PropertyChangedArgs? _lastDisplayPropertyChangedEvent;
    private readonly FormatHelper _formatHelper = new();

    public DialAction(SimHubConnection simHubConnection, ShakeItStructureFetcher shakeItStructureFetcher)
    {
        _simHubConnection = simHubConnection;
        _shakeItStructureFetcher = shakeItStructureFetcher;
        _displayPropertyChangedReceiver = new PropertyChangedDelegate(DisplayPropertyChanged);
    }

    /// <summary>
    /// Method to handle the event "fetchShakeItBassStructure" from the Property Inspector. Fetches the ShakeIt Bass structure
    /// from SimHub and sends the result through the event "shakeItBassStructure" back to the Property Inspector.
    /// </summary>
    [PropertyInspectorMethod("fetchShakeItBassStructure")]
    public async Task FetchShakeItBassStructure(FetchShakeItStructureArgs args)
    {
        var profiles = await _shakeItStructureFetcher.FetchBassStructure();
        await SendToPropertyInspectorAsync(new { message = "shakeItBassStructure", profiles, args.SourceId });
    }

    /// <summary>
    /// Method to handle the event "fetchShakeItMotorsStructure" from the Property Inspector. Fetches the ShakeIt Motors structure
    /// from SimHub and sends the result through the event "shakeItMotorsStructure" back to the Property Inspector.
    /// </summary>
    [PropertyInspectorMethod("fetchShakeItMotorsStructure")]
    public async Task FetchShakeItMotorsStructure(FetchShakeItStructureArgs args)
    {
        var profiles = await _shakeItStructureFetcher.FetchMotorsStructure();
        await SendToPropertyInspectorAsync(new { message = "shakeItMotorsStructure", profiles, args.SourceId });
    }

    protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
    {
        var settings = args.Payload.GetSettings<DialActionSettings>();
        Logger.LogInformation("OnWillAppear ({coords}): {settings}", args.Payload.Coordinates, settings);
        await SetSettings(settings, true);

        await base.OnWillAppear(args);
    }

    protected override async Task OnDidReceiveSettings(ActionEventArgs<ActionPayload> args, DialActionSettings settings)
    {
        Logger.LogInformation("OnDidReceiveSettings ({coords}): {settings}", args.Payload.Coordinates, settings);

        await SetSettings(settings, false);
        await base.OnDidReceiveSettings(args, settings);
    }

    protected override async Task OnDialRotate(ActionEventArgs<DialRotatePayload> args)
    {
        Logger.LogInformation("OnDialRotate ({coords}): Ticks: {ticks}, Pressed {pressed}", args.Payload.Coordinates, args.Payload.Ticks,
            args.Payload.Pressed);
        if (args.Payload.Ticks < 0)
        {
            KeyboardUtils.KeyDown(_hotkeyLeft);
            if (!string.IsNullOrWhiteSpace(_settings.SimHubControlLeft))
            {
                await _simHubConnection.SendTriggerInputPressed(_settings.SimHubControlLeft);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50));

            KeyboardUtils.KeyUp(_hotkeyLeft);
            if (!string.IsNullOrWhiteSpace(_settings.SimHubControlLeft))
            {
                await _simHubConnection.SendTriggerInputReleased(_settings.SimHubControlLeft);
            }
        }
        else if (args.Payload.Ticks > 0)
        {
            KeyboardUtils.KeyDown(_hotkeyRight);
            if (!string.IsNullOrWhiteSpace(_settings.SimHubControlRight))
            {
                await _simHubConnection.SendTriggerInputPressed(_settings.SimHubControlRight);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50));

            KeyboardUtils.KeyUp(_hotkeyRight);
            if (!string.IsNullOrWhiteSpace(_settings.SimHubControlRight))
            {
                await _simHubConnection.SendTriggerInputReleased(_settings.SimHubControlRight);
            }
        }
    }

    protected override async Task OnDialPress(ActionEventArgs<DialPayload> args)
    {
        Logger.LogInformation("OnDialPress ({coords}): Pressed {pressed}", args.Payload.Coordinates, args.Payload.Pressed);

        if (args.Payload.Pressed)
        {
            KeyboardUtils.KeyDown(_hotkey);
            if (!string.IsNullOrWhiteSpace(_settings.SimHubControl))
            {
                await _simHubConnection.SendTriggerInputPressed(_settings.SimHubControl);
            }
        }
        else
        {
            KeyboardUtils.KeyUp(_hotkey);
            if (!string.IsNullOrWhiteSpace(_settings.SimHubControl))
            {
                await _simHubConnection.SendTriggerInputReleased(_settings.SimHubControl);
            }
        }
    }

    private async Task SetSettings(DialActionSettings settings, bool forceSubscribe)
    {
        var newDisplayFormat = _formatHelper.CompleteFormatString(settings.DisplayFormat);
        // Redisplay the title if the format for the title has changed.
        var recalcDisplay = newDisplayFormat != _displayFormat;
        _displayFormat = newDisplayFormat;
        if (recalcDisplay)
        {
            await RefireDisplayPropertyChanged();
        }

        // Unsubscribe previous SimHub "Display" property, if it was set and is different than the new one.
        if (!string.IsNullOrEmpty(_settings.DisplaySimHubProperty) && _settings.DisplaySimHubProperty != settings.DisplaySimHubProperty)
        {
            await _simHubConnection.Unsubscribe(_settings.DisplaySimHubProperty, _displayPropertyChangedReceiver);
            // In case of the new "Display" property being invalid or empty, we remove the old title value.
            _lastDisplayPropertyChangedEvent = null;
            await SetDisplayProperty(null);
        }

        _hotkey = KeyboardUtils.CreateHotkey(settings.Ctrl, settings.Alt, settings.Shift, settings.Hotkey);
        _hotkeyLeft = KeyboardUtils.CreateHotkey(settings.CtrlLeft, settings.AltLeft, settings.ShiftLeft, settings.HotkeyLeft);
        _hotkeyRight = KeyboardUtils.CreateHotkey(settings.CtrlRight, settings.AltRight, settings.ShiftRight, settings.HotkeyRight);

        // Subscribe SimHub property, if it is set and different than the previous one.
        if (!string.IsNullOrEmpty(settings.DisplaySimHubProperty) &&
            (settings.DisplaySimHubProperty != _settings.DisplaySimHubProperty || forceSubscribe))
        {
            await _simHubConnection.Subscribe(settings.DisplaySimHubProperty, _displayPropertyChangedReceiver);
        }

        _settings = settings;
    }

    /// <summary>
    /// Refire the last "DisplayPropertyChanged" event that was received from SimHub.
    /// </summary>
    private async Task RefireDisplayPropertyChanged()
    {
        if (_lastDisplayPropertyChangedEvent != null)
        {
            await DisplayPropertyChanged(_lastDisplayPropertyChangedEvent);
        }
    }

    private async Task DisplayPropertyChanged(PropertyChangedArgs args)
    {
        _lastDisplayPropertyChangedEvent = args;
        await SetDisplayProperty(args.PropertyValue);
    }

    private async Task SetDisplayProperty(IComparable? property)
    {
        var value = property ?? string.Empty;
        try
        {
            await SetFeedbackAsync(new LayoutA1 { Value = string.Format(_displayFormat, value) });
        }
        catch (FormatException)
        {
            await SetFeedbackAsync(new LayoutA1 { Value = value.ToString() });
        }
    }
}