﻿namespace StreamDeckSimHub.Plugin.Actions;

/// <summary>
/// Settings for Flags Action, which are set in the Stream Deck UI.
/// </summary>
public class FlagsSettings
{
    public string NoFlag { get; set; } = "flags/flag-none.svg";
    public string BlackFlag { get; set; } = "flags/flag-black.svg";
    public string BlueFlag { get; set; } = "flags/flag-blue.svg";
    public string CheckeredFlag { get; set; } = "flags/flag-checkered.svg";
    public string GreenFlag { get; set; } = "flags/flag-green.svg";
    public string OrangeFlag { get; set; } = "flags/flag-orange.svg";
    public string WhiteFlag { get; set; } = "flags/flag-white.svg";
    public string YellowFlag { get; set; } = "flags/flag-yellow.svg";
    public string YellowFlagSec1 { get; set; } = "flags/flag-yellow-s1.png";
    public string YellowFlagSec2 { get; set; } = "flags/flag-yellow-s2.png";
    public string YellowFlagSec3 { get; set; } = "flags/flag-yellow-s3.png";

    public override string ToString()
    {
        return
            $"No: {NoFlag}, Black: {BlackFlag}, Blue: {BlueFlag}, Checkered: {CheckeredFlag}, Green: {GreenFlag}, Orange: {OrangeFlag}, White: {WhiteFlag}, Yellow: {YellowFlag}, YellowFlagSec1: {YellowFlagSec1}, YellowFlagSec2: {YellowFlagSec2}, YellowFlagSec3: {YellowFlagSec3}";
    }
}