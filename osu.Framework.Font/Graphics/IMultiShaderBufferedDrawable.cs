﻿// Copyright (c) andy840119 <andy840119@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics
{
    public interface IMultiShaderBufferedDrawable : IBufferedDrawable
    {
        IReadOnlyList<IShader> Shaders { get; set; }
    }
}
