﻿// Copyright (c) andy840119 <andy840119@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;

namespace osu.Framework.Graphics.Sprites
{
    public interface IHasRomaji : IDrawable
    {
        PositionText[] Romajies { get; set; }

        FontUsage RomajiFont { get; set; }

        int RomajiMargin { get; set; }

        Vector2 RomajiSpacing { get; set; }

        LyricTextAlignment RomajiAlignment { get; set; }
    }
}
