﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;

namespace osu.Framework.Graphics.Sprites
{
    public interface IHasRuby : IDrawable
    {
        PositionText[] Rubies { get; set; }

        FontUsage RubyFont { get; set; }

        int RubyMargin { get; set; }

        Vector2 RubySpacing { get; set; }

        LyricTextAlignment RubyAlignment { get; set; }
    }
}
