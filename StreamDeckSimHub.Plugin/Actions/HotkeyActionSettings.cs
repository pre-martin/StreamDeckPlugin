﻿// Copyright (C) 2022 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

namespace StreamDeckSimHub.Plugin.Actions;

/// <summary>
/// Settings for Hotkey Action, which are set in the Stream Deck UI.
/// </summary>
public class HotkeyActionSettings : HotkeyBaseActionSettings
{
    public string TitleSimHubProperty { get; init; } = string.Empty;
    public string TitleFormat { get; init; } = string.Empty;

    public override string ToString()
    {
        return $"{base.ToString()}, TitleSimHubProperty: {TitleSimHubProperty}, TitleFormat: {TitleFormat}";
    }

}