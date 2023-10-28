﻿// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System.Collections.Concurrent;
using SharpDeck.Events.Received;

namespace StreamDeckSimHub.Plugin.Actions;

/// <summary>
/// Handles the detection of short and long (key/button) presses.
/// <p/>
/// If the time between KeyDown and KeyUp is shorter than "longPressTimeSpan", the callback "OnShortPress" will be called.
/// If the time is larger, the callback "OnLongPress" will be called.
/// </summary>
public class ShortAndLongPressHandler
{
    private ConcurrentStack<ActionEventArgs<KeyPayload>> KeyPressStack { get; } = new();
    public TimeSpan LongPressTimeSpan { get; set; }
    private Func<ActionEventArgs<KeyPayload>, Task> OnShortPress { get; }
    private Func<ActionEventArgs<KeyPayload>, Task> OnLongPress { get; }
    private Func<Task> OnLongReleased { get; }
    private CancellationTokenSource? _cancellationTokenSource;

    public ShortAndLongPressHandler(
        Func<ActionEventArgs<KeyPayload>, Task> onShortPress,
        Func<ActionEventArgs<KeyPayload>, Task> onLongPress,
        Func<Task> onLongReleased) : this(TimeSpan.FromMilliseconds(500), onShortPress, onLongPress, onLongReleased)
    {
    }

    public ShortAndLongPressHandler(
        TimeSpan longPressTimeSpan,
        Func<ActionEventArgs<KeyPayload>, Task> onShortPress,
        Func<ActionEventArgs<KeyPayload>, Task> onLongPress,
        Func<Task> onLongReleased)
    {
        LongPressTimeSpan = longPressTimeSpan;
        OnShortPress = onShortPress;
        OnLongPress = onLongPress;
        OnLongReleased = onLongReleased;
    }

    public Task KeyDown(ActionEventArgs<KeyPayload> args)
    {
        KeyPressStack.Push(args);
        if (LongPressTimeSpan > TimeSpan.Zero)
        {
            Task.Run(async () =>
            {
                try
                {
                    var me = this;
                    _cancellationTokenSource = new();
                    await Task.Delay(LongPressTimeSpan, _cancellationTokenSource.Token);
                    await me.TryHandlePress(OnLongPress);
                }
                catch (TaskCanceledException)
                {
                    // That is what we expect if a short press was faster
                }
            });
        }

        return Task.CompletedTask;
    }

    public async Task KeyUp()
    {
        await TryHandlePress(OnShortPress);
    }

    private async Task TryHandlePress(Func<ActionEventArgs<KeyPayload>, Task> handler)
    {
        // We reach this code either
        // - when "KeyUp" was received before the end of the "Delay" (= short key press)
        // - when the "Delay" has expired (= start of long key press).
        // - when "KeyUp" was received after the Delay (= end of long key press)
        if (KeyPressStack.TryPop(out var eventArgs))
        {
            _cancellationTokenSource?.Cancel();
            await handler(eventArgs);
        }
        else
        {
            await OnLongReleased();
        }
    }
}
